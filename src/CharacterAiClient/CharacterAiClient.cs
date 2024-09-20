using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using CharacterAi.Exceptions;
using CharacterAi.Models;
using CharacterAi.Models.Common;
using CharacterAi.Models.WS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace CharacterAi
{
    public class CharacterAiClient : IDisposable
    {
        private const int RefreshTimeout = 60_000; // minute

        internal HttpClient HttpClient { get; set; }

        /// <summary>
        /// authToken : client
        /// </summary>
        internal Dictionary<string, WsConnection> WsConnections { get; } = [];

        private readonly Stopwatch _sw = Stopwatch.StartNew();


        public CharacterAiClient()
        {
            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            HttpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
        }


        public void Refresh()
        {
            if (_sw.ElapsedMilliseconds < RefreshTimeout)
            {
                return;
            }

            lock (HttpClient)
            {
                this.InitializeAsync().Wait();
            }

            _sw.Restart();
        }


        #region Dispose

        private bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            HttpClient.Dispose();
            WsConnections.Clear();

            _disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion Dispose
    }

    public static class CharacterAiClientExt
    {
        // private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public static async Task InitializeAsync(this CharacterAiClient characterAiClient)
        {
            var response = await characterAiClient.HttpClient.GetAsync("https://character.ai/");
            string cookie = string.Join(';', response.Headers.Where(h => h.Key.ToLower() == "set-cookie").Select(h => h.Value));
            characterAiClient.HttpClient.DefaultRequestHeaders.Remove("Cookie");
            characterAiClient.HttpClient.DefaultRequestHeaders.Add("Cookie", cookie);
        }

        /// <returns>signInAttemptId - not really needed, but you can have it</returns>
        public static async Task SendLoginEmailAsync(this CharacterAiClient characterAiClient, string email)
        {
            characterAiClient.Refresh();

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://character.ai/api/trpc/auth.login?batch=1")
            {
                Content = new StringContent("{\"0\":{\"json\":{\"email\": \"" + email + "\", \"host\":\"https://character.ai\"}}}", Encoding.UTF8, "application/json")
            };

            var response = await characterAiClient.HttpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                throw new OperationFailedException($"Failed to send login link to email {email} | Details: {response.HumanizeHttpResponseError()}");
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


        public static async Task<AuthorizedUser> LoginByLinkAsync(this CharacterAiClient characterAiClient, string link)
        {
            int attempt = 0;

            while (true)
            {
                characterAiClient.Refresh();
                attempt++;

                var response = await characterAiClient.HttpClient.GetAsync(link);
                if (response.StatusCode is HttpStatusCode.InternalServerError && attempt < 5)
                {
                    await Task.Delay(3000);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new OperationFailedException($"Failed to login with link {link} | Details: {response.HumanizeHttpResponseError()}");
                }

                var content = await response.Content.ReadAsStringAsync();
                content = content[content.IndexOf("https://auth", StringComparison.Ordinal)..];
                content = Regex.Replace(content, "\"]\\)<\\/script><script>self\\.__next_f\\.push\\(\\[\\d,\"", string.Empty);
                content = content[..(content.IndexOf("\",", StringComparison.Ordinal) - 1)];

                var parts = content.Split(["oobCode=", "\\u0026apiKey=", "\\u0026"], StringSplitOptions.RemoveEmptyEntries);
                string oobCode = parts[1];
                string apiKey = parts[2];
                string email = parts[3].Split(["email%3D", "%26"], StringSplitOptions.RemoveEmptyEntries)[1];

                for (int i = 0; i < 5; i++)
                {
                    email = Uri.UnescapeDataString(email);
                    if (email.Contains('@'))
                    {
                        break;
                    }
                }

                var authCode = link.Split('/').Last();

                return await FollowUpAuthAsync(characterAiClient, email, apiKey, oobCode, authCode);
            }
        }


        private static async Task<AuthorizedUser> FollowUpAuthAsync(CharacterAiClient characterAiClient, string email, string apiKey, string oobCode, string authCode)
        {
            var postRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithEmailLink?key={apiKey}")
            {
                Content = new StringContent($"{{\"email\":\"{email}\",\"oobCode\":\"{oobCode}\"}}", Encoding.UTF8, "application/json")
            };

            var postResponse = await characterAiClient.HttpClient.SendAsync(postRequestMessage);
            if (!postResponse.IsSuccessStatusCode)
            {
                throw new OperationFailedException($"Failed to login with link | Details: {postResponse.HumanizeHttpResponseError()}");
            }

            string postContent = await postResponse.Content.ReadAsStringAsync();
            var jPostContent = JsonConvert.DeserializeObject<JObject>(postContent)!;

            var getRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://character.ai/login/jwt?googleJwt={jPostContent["idToken"]}&uuid={authCode}&open_mobile=false");
            getRequestMessage.Headers.TryAddWithoutValidation("Accept", "application/json");
            getRequestMessage.Headers.TryAddWithoutValidation("X-Nextjs-data", "1");

            var getResponse = await characterAiClient.HttpClient.SendAsync(getRequestMessage);
            if (!getResponse.IsSuccessStatusCode)
            {
                throw new OperationFailedException($"Failed to login with link | Details: {getResponse.HumanizeHttpResponseError()}");
            }

            string getContent = await getResponse.Content.ReadAsStringAsync();
            var jGetContent = JsonConvert.DeserializeObject<JObject>(getContent)!["pageProps"];
            var userInfo = jGetContent!["user"]!["user"];

            var authorizedUser = new AuthorizedUser
            {
                Token = jGetContent["token"]!.ToString(),
                UserId = userInfo!["id"]!.ToString(),
                Username = userInfo["username"]!.ToString(),
                UserEmail = jGetContent!["user"]!["email"]!.ToString(),
                UserImageUrl = userInfo["account"]!["avatar_file_name"]?.ToString()
            };

            return authorizedUser;
        }

        public static async Task<CaiCharacter> GetCharacterInfoAsync(this CharacterAiClient characterAiClient, string characterId, string? authToken = null)
        {
            characterAiClient.Refresh();

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://plus.character.ai/chat/character/info/")
            {
                Content = new StringContent($"{{\"external_id\":\"{characterId}\"}}", Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrEmpty(authToken))
            {
                requestMessage.Headers.Add("Authorization", $"Token {authToken}");
            }

            var response = await characterAiClient.HttpClient.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();
            var character = JsonConvert.DeserializeObject<JObject>(content)?["character"]?.ToString();

            if (character is null)
            {
                throw new OperationFailedException($"Failed to get character info | Details: {response.HumanizeHttpResponseError()}");
            }

            return JsonConvert.DeserializeObject<CaiCharacter>(character)!;
        }

        public static async Task<List<SearchResult>> SearchAsync(this CharacterAiClient client, string query, string? authToken = null)
        {
            client.Refresh();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://character.ai/api/trpc/search.search?batch=1&input={{\"0\":{{\"json\":{{\"searchQuery\":\"{query}\"}}}}}}");

            if (!string.IsNullOrEmpty(authToken))
            {
                requestMessage.Headers.Add("Authorization", $"Token {authToken}");
            }

            var response = await client.HttpClient.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<JArray>(content)?.FirstOrDefault()?.ToString()!;
            var json = JsonConvert.DeserializeObject<JObject>(result)?["result"]?["data"]?["json"]?.ToString();

            if (json is null)
            {
                throw new OperationFailedException($"Failed to perform search request | Details: {response.HumanizeHttpResponseError()}");
            }

            return JsonConvert.DeserializeObject<List<SearchResult>>(json)!;
        }

        public static async Task<List<CaiChat>> GetChatsAsync(this CharacterAiClient client, string characterId, string authToken)
        {
            client.Refresh();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://neo.character.ai/chats/recent/{characterId}");
            requestMessage.Headers.Add("Authorization", $"Token {authToken}");

            var response = await client.HttpClient.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();
            var jContent = JsonConvert.DeserializeObject<JObject>(content);
            var chats = jContent["chats"] as JArray;
            if (chats is null)
            {
                throw new OperationFailedException($"Failed to get chats | Details: {response.HumanizeHttpResponseError()}");
            }

            var caiChats = chats.Select(token => token.ToObject<CaiChat>());

            return caiChats.ToList();
        }


        public static async Task<Guid> CreateNewChatAsync(this CharacterAiClient client, string characterId, int userId, string authToken)
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

            var wsConnection = client.GetWsConnection(authToken);
            var wsMessage = JsonConvert.SerializeObject(message);
            wsConnection.LastMessage = null;
            wsConnection.Send(wsMessage);

            for (int i = 0; i < 10; i++)
            {
                if (wsConnection.LastMessage is not null)
                {
                    return wsConnection.LastMessage.chat!.chat_id;
                }

                await Task.Delay(1000);
            }

            throw new OperationFailedException("Timed out");
        }


        // PRIVATE

        private static WsConnection GetWsConnection(this CharacterAiClient client, string authToken)
        {
            client.WsConnections.TryGetValue(authToken, out var connection);
            if (connection is not null)
            {
                return connection;
            }

            lock (client.WsConnections)
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

                client.WsConnections.Add(authToken, connection);

                return connection;
            }
        }

        private static string HumanizeHttpResponseError(this HttpResponseMessage? response)
        {
            string details;
            if (response is null)
            {
                details = "Failed to get response from CharacterAI";
            }
            else
            {
                details = $"{response.StatusCode:D} ({response.StatusCode:G})";
                details += $"\nHeaders: {(response.Headers is null || response.Headers.Any() ? "\n" + string.Join("\n", response.Headers!.Select(h => $"[ '{h.Key}'='{h.Value}' ]")) : "none")}";
                details += $"\nContent: {(string.IsNullOrEmpty(response.Content.ReadAsStringAsync().Result) ? "none" : response.Content)}";
            }

            return details;
        }

        private static T? Deserialize<T>(this string json)
            => JsonConvert.DeserializeObject<T>(json);

    }
}
