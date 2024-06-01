using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using CharacterAi.Exceptions;
using CharacterAi.Models;
using CharacterAi.Models.DTO;
using CharacterAi.Models.HTTP;
using CharacterAi.Models.WS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RestSharp;
using Websocket.Client;
using Logger = NLog.Logger;

namespace CharacterAi
{
    public class CharacterAiClient : IDisposable
    {
        internal RestClient BaseCaiClient { get; }
        internal RestClient NeoCaiClient { get; }

        /// <summary>
        /// authToken : client
        /// </summary>
        internal Dictionary<string, WsConnection> WsConnections { get; } = [];
        internal string Cookie { get; set; } = null!;
        internal bool Init { get; set; } = false;

        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private Stopwatch _sw;

        public CharacterAiClient()
        {
            BaseCaiClient = new RestClient("https://character.ai");
            NeoCaiClient = new RestClient("https://neo.character.ai");
            
            _sw = Stopwatch.StartNew();
        }

        const int refreshTimeout = 60_000; // minute
        protected internal async Task RefreshAsync()
        {
            if (Init && _sw.ElapsedMilliseconds < refreshTimeout) return;

            await this.InitializeAsync();

            _sw.Restart();
        }

        #region Dispose

        private bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                BaseCaiClient.Dispose();
                BaseCaiClient.Dispose();
                _sw = null!;

                _disposedValue = true;
            }
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
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public static async Task InitializeAsync(this CharacterAiClient client)
        {
            var request = new RestRequest("/", Method.Get);

            var response1 = await client.BaseCaiClient.ExecuteAsync(request);
            client.Cookie = string.Join(';', response1.Cookies!.Cast<Cookie>().Select(c => $"{c.Name}={c.Value}"));

            client.Init = true;
        }

        public static async Task<int?> GetUserIdAsync(this CharacterAiClient client, string authToken, string webNextAuthToken)
        {
            await client.RefreshAsync();

            var request = new RestRequest($"/_next/data/qlSaBr0Gn7DVPbtemuE7m/index.json", Method.Get);

            request.AddBasicHeaders();
            request.AddOrUpdateHeader("Authorization", authToken.FixToken());
            request.AddOrUpdateHeader("Cookie", client.Cookie);
            request.AddOrUpdateHeader("X-Nextjs-Data", "1");
            var response = await client.BaseCaiClient.ExecuteAsync(request);
            var jObject = response.Content?.Deserialize<JObject>();
            var userId = jObject?["pageProps"]?["user"]?["user"]?["id"]
                ?? throw new OperationFailedException("Failed to get user account info. Make sure you have specified a correct web-next-auth token.");

            return (int)userId;
        }

        public static async Task<Character> GetCharacterInfoAsync(this CharacterAiClient client, string characterId, string authToken)
        {
            await client.RefreshAsync();

            var request = new RestRequest("/chat/character/info/", Method.Post);

            request.AddBasicHeaders();
            request.AddOrUpdateHeader("Authorization", authToken.FixToken());
            request.AddOrUpdateHeader("Cookie", client.Cookie);
            
            request.AddJsonBody($"{{ \"external_id\": \"{characterId}\" }}");

            var response = await client.BaseCaiClient.ExecuteAsync(request);
            var infoResponse = response.Content?.Deserialize<InfoResponse>()!;

            return infoResponse.character;
        }

        public static async Task<List<SearchResult>> SearchAsync(this CharacterAiClient client, string query, string authToken)
        {
            await client.RefreshAsync();

            var request = new RestRequest($"/api/trpc/search.search", Method.Get);

            request.AddBasicHeaders();
            request.AddOrUpdateHeader("Authorization", authToken.FixToken());
            request.AddOrUpdateHeader("Cookie", client.Cookie);

            request.AddQueryParameter("batch", "1");
            request.AddQueryParameter("input", $"{{\"0\":{{\"json\":{{\"searchQuery\":\"{query}\"}}}}}}");

            var response = await client.BaseCaiClient.ExecuteAsync(request);
            var result = response.Content?.Deserialize<JsonArray>()?.FirstOrDefault();

            var searchResponse = result?.Deserialize<SearchResponse>()!;

            return searchResponse?.result?.data?.json ?? [];
        }

        public static async Task<List<Chat>> GetChatsAsync(this CharacterAiClient client, string characterId, string authToken)
        {
            await client.RefreshAsync();

            var request = new RestRequest($"/chats/recent/{characterId}", Method.Get);

            request.AddBasicHeaders();
            request.AddOrUpdateHeader("Authorization", authToken.FixToken());
            request.AddOrUpdateHeader("Cookie", client.Cookie);
            
            var response = await client.NeoCaiClient.ExecuteAsync(request);
            var result = response.Content?.Deserialize<RecentResponse>();

            return result?.chats ?? [];
        }


        public static async Task<bool> PingAsync(this CharacterAiClient client)
        {
            var request = new RestRequest("/ping", Method.Get);
            var response = await client.NeoCaiClient.ExecuteAsync(request);
            var status = response.Content?.Deserialize<JsonObject>()?["status"]?.GetValue<string>();

            return status?.Equals("OK") ?? false;
        }

        public static async Task<Guid> CreateNewChatAsync(this CharacterAiClient client, string characterId, int creatorId, string authToken)
        {
            Guid requestId = Guid.NewGuid();

            var chat = new ChatShort()
            {
                character_id = characterId,
                chat_id = Guid.NewGuid(),
                creator_id = creatorId.ToString(),
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
                origin_id = "web-next",
                request_id = requestId,
                payload = payload
            };

            var wsConnection = client.GetWsConnection(authToken);
            wsConnection.Send(JsonConvert.SerializeObject(message));

            for (int i = 0; i < 10; i++)
            {
                wsConnection.Messages.TryGetValue(requestId, out WsResponseMessage? responseMessage);
                if (responseMessage is null)
                {
                    await Task.Delay(1000);
                    continue;
                }

                Guid chatId = responseMessage.chat!.chat_id;
                wsConnection.Messages.Remove(requestId);

                return chatId;
            }

            throw new OperationFailedException("Timed out");
        }


        // PRIVATE

        private static WsConnection GetWsConnection(this CharacterAiClient client, string authToken)
        {
            WsConnection? connection;
            client.WsConnections.TryGetValue(authToken, out connection);
            if (connection is not null) return connection;

            lock (client.WsConnections)
            {
                connection = new WsConnection();
                connection.Client = new WebsocketClient(new Uri("wss://neo.character.ai/ws/"), () =>
                {
                    var client = new ClientWebSocket();
                    client.Options.SetRequestHeader("Cookie", $"HTTP_AUTHORIZATION=\"{authToken}\"");

                    return client;
                });

                connection.Client.MessageReceived.Subscribe(msg =>
                {
                    try
                    {
                        var wsResponseMessage = JsonConvert.DeserializeObject<WsResponseMessage>(msg.Text!)!;
                        connection.Messages.Add(wsResponseMessage.request_id, wsResponseMessage);
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"Failed to receive WS message: {ex}");
                    }
                });

                connection.Client.Start().Wait();

                client.WsConnections.Add(authToken, connection);

                return connection;
            }
        }


        private static string FixToken(this string token)
        {
            token = token.Trim();
            return token.StartsWith("Token") ? token : $"Token {token}";
        }


        private static void AddBasicHeaders(this RestRequest request)
        {
            request.AddOrUpdateHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            request.AddOrUpdateHeader("Accept-Language", "en-US,en;q=0.5"); 
            request.AddOrUpdateHeader("Accept-Encoding", "gzip, deflate, br, zstd");  
            request.AddOrUpdateHeader("Content-Type", "application/json");
            request.AddOrUpdateHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        }

        private static T? Deserialize<T>(this string json)
            => JsonConvert.DeserializeObject<T>(json);

    }
}
