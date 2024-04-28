using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using CharacterAiNet.Exceptions;
using CharacterAiNet.Models.DTO;
using CharacterAiNet.Models.HTTP;
using CharacterAiNet.Models.WS;
using NLog;
using RestSharp;
using Websocket.Client;

namespace CharacterAiNet
{
    public class CharacterAiClient : IDisposable
    {
        protected internal RestClient BaseRestClient { get; }
        protected internal RestClient NeoRestClient { get; }
        protected internal WebsocketClient WsClient { get; }
        protected internal string CloudFlareCookie { get; set; } = null!;
        protected internal bool Init { get; set; } = false;

        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private Stopwatch _sw;


        public CharacterAiClient()
        {
            BaseRestClient = new RestClient("https://plus.character.ai");
            NeoRestClient = new RestClient("https://neo.character.ai");

            WsClient = new WebsocketClient(new("wss://neo.character.ai/ws/"))
            {
                ReconnectTimeout = TimeSpan.FromSeconds(15)
            };

            WsClient.MessageReceived.Subscribe(msg => { });
            WsClient.Start();

            _sw = Stopwatch.StartNew();
        }

        const int refreshTimeout = 300_000;
        protected internal async Task RefreshAsync()
        {
            if (Init && _sw.ElapsedMilliseconds < refreshTimeout) return;

            await this.InitializeAsync();
            lock (WsClient)
            {
                WsClient.Reconnect().Wait();
            }

            _sw.Restart();
        }

        #region Dispose

        private bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                WsClient.Dispose();
                BaseRestClient.Dispose();
                BaseRestClient.Dispose();
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
            
            var response = await client.BaseRestClient.ExecuteAsync(request);
            string? cfCookie = response?.Headers?.FirstOrDefault(h => h.Name!.StartsWith("Set-Cookie") && h.Value!.ToString()!.StartsWith("__cf_bm"))?.Value?.ToString();
            if (string.IsNullOrEmpty(cfCookie))
                throw new OperationFailedException($"Failed to initialize a CharacterAI client. Details:\n  headers: [ {response?.Headers?.ParseHeaders() ?? "???"} ]\n  content: {response?.Content ?? "???"}");

            client.CloudFlareCookie = cfCookie.Split(';').First();
            client.Init = true;
        }

        public static async Task<Character> GetCharacterInfo(this CharacterAiClient client, string characterId, string authToken)
        {
            await client.RefreshAsync();

            var request = new RestRequest("/chat/character/info/", Method.Post);

            request.AddBasicHeaders();
            request.AddOrUpdateHeader("Authorization", authToken);
            request.AddOrUpdateHeader("Cookie", client.CloudFlareCookie);
            
            request.AddJsonBody($"{{ \"external_id\": \"{characterId}\" }}");

            var response = await client.BaseRestClient.ExecuteAsync(request);
            var infoResponse = response.Content?.Deserialize<InfoResponse>()!;

            return infoResponse.character;
        }

        public static async Task<List<SearchResult>> SearchAsync(this CharacterAiClient client, string query, string authToken)
        {
            await client.RefreshAsync();

            var request = new RestRequest($"/api/trpc/search.search", Method.Get);

            request.AddBasicHeaders();
            request.AddOrUpdateHeader("Authorization", authToken);
            request.AddOrUpdateHeader("Cookie", client.CloudFlareCookie);

            request.AddQueryParameter("batch", "1");
            request.AddQueryParameter("input", $"{{\"0\":{{\"json\":{{\"searchQuery\":\"{query}\"}}}}}}");

            var response = await client.BaseRestClient.ExecuteAsync(request);
            var result = response.Content?.Deserialize<JsonArray>()!.First();

            var searchResponse = result.Deserialize<SearchResponse>()!;

            return searchResponse.result.data.json ?? [];
        }

        public static async Task<List<Chat>> GetChatsAsync(this CharacterAiClient client, string characterId, string authToken)
        {
            await client.RefreshAsync();

            var request = new RestRequest($"/chats/recent/{characterId}", Method.Get);

            request.AddBasicHeaders();
            request.AddOrUpdateHeader("Authorization", authToken);
            request.AddOrUpdateHeader("Cookie", client.CloudFlareCookie);
            
            var response = await client.NeoRestClient.ExecuteAsync(request);
            var result = response.Content?.Deserialize<RecentResponse>();

            return result?.chats ?? [];
        }


        public static async Task<bool> PingAsync(this CharacterAiClient client)
        {
            var request = new RestRequest("/ping", Method.Get);
            var response = await client.NeoRestClient.ExecuteAsync(request);
            var status = response.Content?.Deserialize<JsonObject>()?["status"]?.GetValue<string>();

            return status?.Equals("OK") ?? false;
        }

        public static async Task CreateNewChatAsync(this CharacterAiClient client, string characterId, string authToken)
        {
            var chat = new ChatShort()
            {
                character_id = characterId,
                
            }


            var payload = new NewChatPayload()
            {
                with_greeting = true,
                chat = 
            };

            var message = new WsRequestMessage()
            {
                command = "create_chat",
                origin_id = "web-next",
                request_id = Guid.NewGuid().ToString(),

            };

            client.WsClient.Send
        }




        private static void AddBasicHeaders(this RestRequest request)
        {
            request.AddOrUpdateHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            request.AddOrUpdateHeader("Accept-Language", "en-US,en;q=0.5"); 
            request.AddOrUpdateHeader("Accept-Encoding", "gzip, deflate, br");  
            request.AddOrUpdateHeader("Content-Type", "application/json");
            request.AddOrUpdateHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        }

        private static T? Deserialize<T>(this string json)
            => JsonSerializer.Deserialize<T>(json);

        private static string ParseHeaders(this IEnumerable<Parameter> arr)
            => string.Join(" | ", arr.Select(p => $"{p.Name}: {p.Value}"));

    }
}
