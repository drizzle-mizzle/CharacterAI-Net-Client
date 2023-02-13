using CharacterAI.Models;
using CharacterAI.Services;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace CharacterAI
{
    public class Integration : CommonService
    {
        private readonly HttpClient _httpClient = new();
        private readonly string? _userToken;

        // basically, just a result of GetInfo()
        public Character charInfo = new();
        public Integration(string userToken)
            => _userToken = userToken;

        // Use it to quickly setup integration with a character and get-last/create-new chat with it.
        // Provide 'reset' to simply create a new chat.
        public async Task<bool> Setup(string charID = "", bool reset = false)
        {
            if (reset)
                return await CreateNewDialog();
            else
            {
                charInfo.Id = charID;
                return await GetInfo() && await GetHistory();
            }
        }

        // Send message and get reply
        public async Task<dynamic> CallCharacter(string msg = "", string imgPath = "", string? primaryMsgId = null, string? parentMsgId = null)
        {
            var contentDynamic = BasicCallContent(charInfo, msg, imgPath);

            // Fetch new answer (aka "perform swipe").
            if (parentMsgId is not null)
            {   // When parent_msg_id is present, character will generate new response for a last message.
                contentDynamic.parent_msg_id = parentMsgId;
            }
            // Or set new (swiped) answer as one to reply on.
            else if (primaryMsgId is not null)
            {   // Provide primary_msg_id to point out which character's response you've chosen.
                contentDynamic.primary_msg_id = primaryMsgId;
                // (seen_msg_ids[] is also required, either it just won't work, but I didn't bother to collect
                //  every single swiped message, just fill it with chosen one)
                contentDynamic.seen_msg_ids = new string[] { primaryMsgId };
            }

            // Prepare request content data
            var requestContent = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(contentDynamic)));
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            // Create request
            HttpRequestMessage request = new(HttpMethod.Post, "https://beta.character.ai/chat/streaming/");
            request.Content = requestContent;
            request.Headers.Add("accept-encoding", "gzip");
            request = SetHeadersForRequest(request);

            // Send request
            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string eMsg = "⚠️ Failed to send message!";
                Failure(eMsg, response: response);

                return eMsg;
            }

            return new CharacterResponse(response);
        }

        // Get info about character
        // returns true if successful
        // returns false if fails
        private async Task<bool> GetInfo()
        {
            string url = "https://beta.character.ai/chat/character/info/";

            HttpRequestMessage request = new(HttpMethod.Post, url);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "external_id", charInfo.Id! }
            });
            request = SetHeadersForRequest(request);

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return Failure(response: response);

            var content = await response.Content.ReadAsStringAsync();
            var charParsed = JsonConvert.DeserializeObject<dynamic>(content)?.character;
            if (charParsed is null)
                return Failure("Something went wrong...", response: response);

            charInfo.Name = charParsed.name;
            charInfo.Greeting = charParsed.greeting;
            charInfo.Description = charParsed.description;
            charInfo.Title = charParsed.title;
            charInfo.Tgt = charParsed.participant__user__username;
            charInfo.AvatarUrl = $"https://characterai.io/i/400/static/avatars/{charParsed.avatar_file_name}";

            return true;
        }

        // Fetch last chat histoty or create one
        // returns true if successful
        // returns false if fails
        private async Task<bool> GetHistory()
        {
            HttpRequestMessage request = new(HttpMethod.Post, "https://beta.character.ai/chat/history/continue/");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "character_external_id", charInfo.Id! }
            });
            request = SetHeadersForRequest(request);

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return Failure(response: response);

            var content = await response.Content.ReadAsStringAsync();
            var externalId = JsonConvert.DeserializeObject<dynamic>(content)?.external_id;
            if (externalId is null)
                return await CreateNewDialog();

            charInfo.HistoryExternalId = externalId;

            return true;
        }

        // Create new chat
        // returns true if successful
        // returns false if fails
        private async Task<bool> CreateNewDialog()
        {
            HttpRequestMessage request = new(HttpMethod.Post, "https://beta.character.ai/chat/history/create/");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { 
                { "character_external_id", charInfo.Id! },
                { "override_history_set", null! }
            });
            request = SetHeadersForRequest(request);

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return Failure(response: response);

            var content = await response.Content.ReadAsStringAsync();
            var externalId = JsonConvert.DeserializeObject<dynamic>(content)?.external_id;
            if (externalId is null)
                return Failure("Something went wrong...", response: response);

            charInfo.HistoryExternalId = externalId;

            return true;
        }

        // Upload image on a server. Use it to attach image to your reply.
        // returns image path if successful
        // returns null if fails
        public async Task<string?> UploadImage(byte[] img)
        {
            var image = new ByteArrayContent(img);
            image.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://beta.character.ai/chat/upload-image/");
            request.Content = new MultipartFormDataContent { { image, "\"image\"", $"\"image.jpg\"" } };        
            request = SetHeadersForRequest(request);

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Failure(response: response);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<dynamic>(content)?.value;
        }

        // Search for a character
        public async Task<SearchResult> Search(string query)
        {
            string url = $"https://beta.character.ai/chat/characters/search/?query={query}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request = SetHeadersForRequest(request);

            using var response = await _httpClient.SendAsync(request);

            return new SearchResult(response);
        }

        private HttpRequestMessage SetHeadersForRequest(HttpRequestMessage request)
        {
            var headers = new string[]
            {
                "Accept", "application/json, text/plain, */*",
                "Authorization", $"Token {_userToken}",
                "accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7",
                "accept-encoding", "deflate, br",
                "ContentType", "application/json",
                "dnt", "1",
                "Origin", "https://beta.character.ai",
                "Referer", $"https://beta.character.ai/" + (charInfo?.Id is null ? "search?" : $"chat?char={charInfo.Id}"),
                "sec-ch-ua", "\"Not?A_Brand\";v=\"8\", \"Chromium\";v=\"108\", \"Google Chrome\";v=\"108\"",
                "sec-ch-ua-mobile", "?0",
                "sec-ch-ua-platform", "Windows",
                "sec-fetch-dest", "empty",
                "sec-fetch-mode", "cors",
                "sec-fetch-site", "same-origin",
                "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36"
            };

            for (int i = 0; i < headers.Length-1; i+=2)
                request!.Headers.Add(headers[i], headers[i+1]);

            return request!;
        }
    }
}