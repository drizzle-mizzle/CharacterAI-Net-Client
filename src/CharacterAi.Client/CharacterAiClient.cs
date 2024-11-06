using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using CharacterAi.Client.Exceptions;
using CharacterAi.Client.Models;
using CharacterAi.Client.Models.Common;
using CharacterAi.Client.Models.WS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace CharacterAi.Client
{
    public class CharacterAiClient : IDisposable
    {
        private const int RefreshTimeout = 60_000; // minute

        private const string CAI_URI = "https://character.ai";

        private readonly HttpClient HTTP_CLIENT;
        private readonly Stopwatch _sw;

        /// <summary>
        /// authToken : client
        /// </summary>
        private Dictionary<string, WsConnection> WsConnections { get; } = [];

        private bool _init = false;


        public CharacterAiClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            HTTP_CLIENT = new HttpClient(handler);

            (string header, string value)[] defaultHeaders =
            [
                ("Accept", "application/json"),
                ("Accept-Encoding", "gzip, deflate"),
                ("Accept-Language", "en-US,en;q=0.5"),
                ("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"),
                ("Referer", "https://character.ai")
            ];

            foreach (var dh in defaultHeaders)
            {
                HTTP_CLIENT.DefaultRequestHeaders.Add(dh.header, dh.value);
            }

            _sw = Stopwatch.StartNew();

            Refresh();
        }


        /// <returns>signInAttemptId - not really needed, but you can have it</returns>
        public async Task SendLoginEmailAsync(string email)
        {
            Refresh();

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{CAI_URI}/api/trpc/auth.login?batch=1")
            {
                Content = new StringContent("{\"0\":{\"json\":{\"email\": \"" + email + "\", \"host\":\"https://character.ai\"}}}", Encoding.UTF8, "application/json")
            };

            var response = await HTTP_CLIENT.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                throw new CharacterAiException($"Failed to send login link to email {email}", (int)response.StatusCode, HumanizeHttpResponseError(response));
            }

            // var content = await response.Content.ReadAsStringAsync();
            // var jContent = JsonConvert.DeserializeObject<JArray>(content);
            //
            // var signInAttemptId = jContent?.First?["result"]?["data"]?["json"]?.ToString();
            // if (signInAttemptId is null)
            // {
            //     throw new OperationFailedException($"Failed to send login link to email {email} | Details: {response.HumanizeHttpResponseError()}");
            // }
        }


        public async Task<AuthorizedUser> LoginByLinkAsync(string link)
        {
            var attempt = 0;

            while (true)
            {
                Refresh();
                attempt++;

                var response = await HTTP_CLIENT.GetAsync(link);
                if (response.StatusCode is HttpStatusCode.InternalServerError && attempt < 5)
                {
                    await Task.Delay(3000);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new CharacterAiException($"Failed to login with link {link}", (int)response.StatusCode, HumanizeHttpResponseError(response));
                }

                var content = await response.Content.ReadAsStringAsync();
                content = content[content.IndexOf("https://auth", StringComparison.Ordinal)..];
                content = Regex.Replace(content, "\"]\\)<\\/script><script>self\\.__next_f\\.push\\(\\[\\d,\"", string.Empty);
                content = content[..(content.IndexOf("\",", StringComparison.Ordinal) - 1)];

                var parts = content.Split(["oobCode=", "\\u0026apiKey=", "\\u0026"], StringSplitOptions.RemoveEmptyEntries);
                var oobCode = parts[1];
                var apiKey = parts[2];
                var email = parts[3].Split(["email%3D", "%26"], StringSplitOptions.RemoveEmptyEntries)[1];

                for (var i = 0; i < 5; i++)
                {
                    email = Uri.UnescapeDataString(email);
                    if (email.Contains('@'))
                    {
                        break;
                    }
                }

                var authCode = link.Split('/').Last();

                return await FollowUpAuthAsync(email, apiKey, oobCode, authCode);
            }
        }


        private async Task<AuthorizedUser> FollowUpAuthAsync(string email, string apiKey, string oobCode, string authCode)
        {
            var signInRequest = new HttpRequestMessage(HttpMethod.Post, $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithEmailLink?key={apiKey}")
            {
                Content = new StringContent($"{{\"email\":\"{email}\",\"oobCode\":\"{oobCode}\"}}", Encoding.UTF8, "application/json")
            };

            var signInResponse = await HTTP_CLIENT.SendAsync(signInRequest);
            if (!signInResponse.IsSuccessStatusCode)
            {
                throw new CharacterAiException($"Failed to login with link", (int)signInResponse.StatusCode, HumanizeHttpResponseError(signInResponse));
            }

            var signInContent = await signInResponse.Content.ReadAsStringAsync();
            var jSignInConent = JsonConvert.DeserializeObject<JObject>(signInContent)!;

            var loginRequest = new HttpRequestMessage(HttpMethod.Get, $"https://character.ai/login/jwt?googleJwt={jSignInConent["idToken"]}&uuid={authCode}&open_mobile=false");

            var loginResponse = await HTTP_CLIENT.SendAsync(loginRequest);
            if (!loginResponse.IsSuccessStatusCode)
            {
                throw new CharacterAiException($"Failed to login with link", (int)loginResponse.StatusCode, HumanizeHttpResponseError(loginResponse));
            }

            var cookies = loginResponse.Headers.Where(h => h.Key.ToLower().StartsWith("set-cookie")).SelectMany(c => c.Value).ToList();
            var webNextAuthTokenCookie = cookies.First(c => c.Contains("web-next-auth")).Split(';').First();

            var authRequest = new HttpRequestMessage(HttpMethod.Get, CAI_URI);
            authRequest.Headers.Add("Coookie", webNextAuthTokenCookie);
            authRequest.Headers.TryAddWithoutValidation("X-Nextjs-data", "1");

            var authResponse = await HTTP_CLIENT.SendAsync(authRequest);
            var content = await authResponse.Content.ReadAsStringAsync();
            var jContent = JsonConvert.DeserializeObject<JObject>(content)!["pageProps"];
            var userInfo = jContent!["user"]!["user"];

            var authorizedUser = new AuthorizedUser
            {
                Token = jContent["token"]!.ToString(),
                UserId = userInfo!["id"]!.ToString(),
                Username = userInfo["username"]!.ToString(),
                UserEmail = jContent!["user"]!["email"]!.ToString(),
                UserImageUrl = $"https://characterai.io/i/200/static/avatars/{userInfo["account"]!["avatar_file_name"]}"
            };

            return authorizedUser;
        }

        public async Task<CaiCharacter> GetCharacterInfoAsync(string characterId, string? authToken = null)
        {
            Refresh();

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://plus.character.ai/chat/character/info/")
            {
                Content = new StringContent($"{{\"external_id\":\"{characterId}\"}}", Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrEmpty(authToken))
            {
                requestMessage.Headers.Add("Authorization", $"Token {authToken}");
            }

            var response = await HTTP_CLIENT.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();
            var character = JsonConvert.DeserializeObject<JObject>(content)?["character"]?.ToString();

            if (character is null)
            {
                throw new CharacterAiException($"Failed to get character info", (int)response.StatusCode, HumanizeHttpResponseError(response));
            }

            return JsonConvert.DeserializeObject<CaiCharacter>(character)!;
        }

        public async Task<List<CaiCharacter>> SearchAsync(string query, string? authToken = null)
        {
            Refresh();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://character.ai/api/trpc/search.search?batch=1&input={{\"0\":{{\"json\":{{\"searchQuery\":\"{query}\"}}}}}}");

            if (!string.IsNullOrEmpty(authToken))
            {
                requestMessage.Headers.Add("Authorization", $"Token {authToken}");
            }

            var response = await HTTP_CLIENT.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<JArray>(content)?.FirstOrDefault()?.ToString()!;
            var json = JsonConvert.DeserializeObject<JObject>(result)?["result"]?["data"]?["json"]?.ToString();

            if (json is null)
            {
                throw new CharacterAiException($"Failed to perform search request", (int)response.StatusCode, HumanizeHttpResponseError(response));
            }

            return JsonConvert.DeserializeObject<List<CaiCharacter>>(json)!;
        }

        public async Task<List<CaiChat>> GetChatsAsync(string characterId, string authToken)
        {
            Refresh();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://neo.character.ai/chats/recent/{characterId}");
            requestMessage.Headers.Add("Authorization", $"Token {authToken}");

            var response = await HTTP_CLIENT.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();
            var jContent = JsonConvert.DeserializeObject<JObject>(content);
            var chats = jContent["chats"] as JArray;
            if (chats is null)
            {
                throw new CharacterAiException($"Failed to get chats", (int)response.StatusCode, HumanizeHttpResponseError(response));
            }

            var caiChats = chats.Select(token => token.ToObject<CaiChat>());

            return caiChats.ToList();
        }


        public async Task<Guid> CreateNewChatAsync(string characterId, int userId, string authToken)
        {
            var chat = new CaiChatShort()
            {
                character_id = characterId,
                chat_id = Guid.NewGuid(),
                creator_id = userId.ToString(),
                type = "TYPE_ONE_ON_ONE",
                visibility = "VISIBILITY_PRIVATE"
            };

            var payload = new NewChatPayload()
            {
                with_greeting = true,
                chat = chat
            };

            var message = new WsRequestMessage()
            {
                command = "create_chat",
                payload = payload
            };

            var wsConnection = GetWsConnection(authToken);
            var wsMessage = JsonConvert.SerializeObject(message);
            wsConnection.LastMessage = null;
            wsConnection.Send(wsMessage);

            for (var i = 0; i < 10; i++)
            {
                if (wsConnection.LastMessage is not null)
                {
                    return wsConnection.LastMessage.chat!.chat_id;
                }

                await Task.Delay(1000);
            }

            throw new CharacterAiException("Timed out", 0, "?");
        }


        // PRIVATE

        private WsConnection GetWsConnection(string authToken)
        {
            WsConnections.TryGetValue(authToken, out var connection);
            if (connection is not null)
            {
                return connection;
            }

            lock (WsConnections)
            {
                connection = new WsConnection
                {
                    Client = new WebsocketClient(new Uri("wss://neo.character.ai/ws/"), () =>
                    {
                        var wsClient = new ClientWebSocket();
                        wsClient.Options.SetRequestHeader("Cookie", $"HTTP_AUTHORIZATION=\"Token {authToken}\"");

                        return wsClient;
                    })
                };

                connection.Client.MessageReceived.Subscribe(msg =>
                {
                    var wsResponseMessage = JsonConvert.DeserializeObject<WsResponseMessage>(msg.Text!)!;
                    connection.LastMessage = wsResponseMessage;
                });

                connection.Client.Start().Wait();

                WsConnections.Add(authToken, connection);

                return connection;
            }
        }

        private static string HumanizeHttpResponseError(HttpResponseMessage? response)
        {
            string details;
            if (response is null)
            {
                details = "Failed to get response from SakuraFM";
            }
            else
            {
                details = $"{response.StatusCode:D} ({response.StatusCode:G})\nHeaders: ";

                var headers = response.Headers.ToList();
                if (response.Headers is null || headers.Count == 0)
                {
                    details += "none";
                }
                else
                {
                    details += string.Join("\n", headers.Select(h => $"[ '{h.Key}'='{h.Value}' ]"));
                }

                var content = Task.Run(async () => await response.Content.ReadAsStringAsync()).GetAwaiter().GetResult();
                if (string.IsNullOrEmpty(content))
                {
                    content = "none";
                }

                details += $"\nContent: {content}";
            }

            return details;
        }


        public void Refresh()
        {
            if (_init && _sw.ElapsedMilliseconds < RefreshTimeout)
            {
                return;
            }

            lock (HTTP_CLIENT)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, CAI_URI);
                var response = HTTP_CLIENT.SendAsync(request).GetAwaiter().GetResult();

                var cookies = response.Headers.Single(h => h.Key.ToLower().StartsWith("set-cookie")).Value;
                HTTP_CLIENT.DefaultRequestHeaders.Remove("Cookie");
                HTTP_CLIENT.DefaultRequestHeaders.Add("Cookie", cookies);
                _init = true;
            }

            _sw.Restart();
        }


        #region Dispose

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposedValue)
            {
                return;
            }

            HTTP_CLIENT.Dispose();

            _disposedValue = true;
        }


        private bool _disposedValue;

        #endregion Dispose
    }

}
