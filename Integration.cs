using CharacterAI.Models;
using CharacterAI.Services;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using PuppeteerExtraSharp;
using System.Diagnostics;
using PuppeteerSharp;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
using PuppeteerExtraSharp.Plugins.ExtraStealth.Evasions;

namespace CharacterAI
{
    public class Integration : CommonService
    {
        private IPage _chatPage = null!;
        private IBrowser _browser = null!;
        private Character _currentCharacter = new();
        private readonly string? _userToken;
        private readonly List<string> _chatsList = new();

        public Character CurrentCharacter => _currentCharacter;
        public List<string> Chats => _chatsList;

        public string EXEC_PATH = null!;

        public Integration(string userToken)
            => _userToken = userToken;

        /// <summary>
        /// Use it to quickly setup integration with a character and get-last/create-new chat with it.
        /// </summary>
        /// <returns>SetupResult</returns>
        public async Task<SetupResult> SetupAsync(string? characterId = null, bool startWithNewChat = false)
        {
            Log($"\nStarting character setup...\n  (Character ID: {characterId ?? _currentCharacter.Id})\n");
            Log("Fetching character info... ");

            // Get info about character
            var character = await GetInfoAsync(characterId);
            if (character.IsEmpty)
                return new SetupResult(false, "Failed to get character info.");

            _ = Success($"OK\n  (Character name: {character.Name})");

            // Set it as a current character and forget all (local) chats with a previous character
            _currentCharacter = character;
            _chatsList.Clear();

            Log("Fetching dialog history... ");

            // Find last chat or create a new one
            var historyId = startWithNewChat ? await CreateNewChatAsync() : await GetLastChatAsync();
            if (historyId is null)
                return new SetupResult(false, "Failed to get chat history.");

            Success($"OK\n  (History ID: {historyId})");

            // Remember chat
            _chatsList.Add(historyId);

            Log("\nCharacterAI - ");
            Success("Ready\n");
        
            return new SetupResult(true);
        }

        /// <summary>
        /// Short version of SetupAsync for a current character
        /// </summary>
        /// <returns>SetupResult</returns>
        public async Task<SetupResult> Reset()
        {
            _chatsList.Clear();
            var historyId = await CreateNewChatAsync();
            if (historyId is null)
                return new SetupResult(false, "Failed to create new chat.");

            _chatsList.Add(historyId);

            return new SetupResult(true);
        }

        /// <summary>
        /// Send message and get reply
        /// </summary>
        /// <returns>CharacterResponse</returns>
        public async Task<CharacterResponse> CallCharacterAsync(string message = "", string? imagePath = null, string? historyId = null, ulong? primaryMsgId = null, ulong? parentMsgId = null)
        {
            var contentDynamic = BasicCallContent(_currentCharacter, message, imagePath, historyId ?? _chatsList.First());

            // Fetch new answer ("perform swipe").
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
                contentDynamic.seen_msg_ids = new ulong[] { (ulong)primaryMsgId };
            }

            string url = "https://beta.character.ai/chat/streaming/";
            var response = await RequestForCall(url, contentDynamic);

            return new CharacterResponse(response);
        }

        /// <summary>
        /// Get info about character
        /// </summary>
        /// <returns>Character</returns>
        public async Task<Character> GetInfoAsync(string? characterId = null)
        {
            string url = "https://beta.character.ai/chat/character/info/";
            var data = new Dictionary<string, string> { { "external_id", characterId ?? CurrentCharacter.Id! } };
            var response = await RequestPost(url, data);

            dynamic? character = null;

            if (response.IsSuccessful)
                character = JsonConvert.DeserializeObject<dynamic>(response.Content!)?.character;
            else
                Failure(response: response.Content);

            return new Character(character);
        }

        public async Task<string?> GetLastChatAsync(string? characterId = null)
        {
            string url = "https://beta.character.ai/chat/history/continue/";
            
            var data = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "character_external_id", characterId ?? CurrentCharacter.Id! }
            });

            var response = await RequestPost(url, data);

            return response.IsSuccessful ?
                JsonConvert.DeserializeObject<dynamic>(response.Content!)?.external_id :
                await CreateNewChatAsync(characterId);
        }

        /// <summary>
        /// Create new chat with a character
        /// </summary>
        /// <returns>returns chat_history_id if successful; null if fails</returns>
        public async Task<string?> CreateNewChatAsync(string? characterId = null)
        {
            string url = "https://beta.character.ai/chat/history/create/";
            var data = new Dictionary<string, string> {
                { "character_external_id", characterId ?? _currentCharacter.Id! }
            };

            var response = await RequestPost(url, data);
            if (!response.IsSuccessful)
            {
                Failure(response: response.Content);
                return null;
            }

            var externalId = JsonConvert.DeserializeObject<dynamic>(response.Content!)?.external_id;
            if (externalId is null)
                Failure("Something went wrong...", response: response.Content);

            return externalId!;
        }

        // not working
        //public async Task<HistoriesResponse> GetHistoriesAsync(string? characterId = null)
        //{
        //    string url = "https://beta.character.ai/chat/character/histories/";

        //    var data = new Dictionary<string, string> {
        //        { "external_id", characterId ?? _currentCharacter.Id! },
        //        { "number", "50" } // Idk what it is. Probably an amount of chats to show. Default value is 50, so I'll just leave it like this.
        //    };

        //    var response = await Request(HttpMethod.Get, url, data);

        //    return new HistoriesResponse(response);
        //}

        // Search for a character
        public async Task<SearchResponse> SearchAsync(string query)
        {
            string url = $"https://beta.character.ai/chat/characters/search/?query={query}";
            var response = await RequestGet(url);

            return new SearchResponse(response);
        }

        /// <summary>
        /// CURRENTLY NOT WORKING
        /// Upload image on a server. Use it to attach image to your reply.
        /// </summary>
        /// <returns>
        /// image path if successful; null if fails
        /// </returns>
        public async Task<string?> UploadImageAsync(byte[] img, string fileName = "image.jpeg")
        {
            return null;

            string url = "https://beta.character.ai/chat/upload-image/";

            if (!fileName.Contains('.')) fileName += ".jpeg";
            string ext = fileName.Split(".").Last();

            var content = new ByteArrayContent(img);
            content.Headers.ContentType = new MediaTypeHeaderValue($"image/{ext}");

            string boundary = "----RandomBoundaryString" + new Random().Next(1024).ToString();
            var formData = new MultipartFormDataContent(boundary) { { content, "image", fileName } };

            string data = await formData.ReadAsStringAsync();
            string contentType = $"multipart/form-data; boundary={boundary}";
            WriteToLogFile(data);

            var response = await RequestPost(url, data, contentType);

            if (response.IsSuccessful)
                return JsonConvert.DeserializeObject<dynamic>(response.Content!)?.value;

            Failure(response: response.Content);
            return null;
        }


        private async Task<PuppeteerResponse> RequestGet(string url)
        {
            if (_browser is null)
            {
                Failure("You need to launch a browser first!\n Use `await LaunchChromeAsync()`");
                return new PuppeteerResponse(null, false);
            }

            var page = await _browser.NewPageAsync();
            await page.SetRequestInterceptionAsync(true);

            page.Request += (s, e) => ContinueRequest(e, null, HttpMethod.Get, "application/json");

            var response = await page.GoToAsync(url);
            var content = await response.TextAsync();
            _ = page.CloseAsync();

            return new PuppeteerResponse(content, response.Ok);
        }

        private async Task<PuppeteerResponse> RequestPost(string url, dynamic? data = null, string contentType = "application/json")
        {
            if (_browser is null)
            {
                Failure("You need to launch a browser first!\n Use `await LaunchChromeAsync()`");
                return new PuppeteerResponse(null, false);
            }

            var page = await _browser.NewPageAsync();
            await page.SetRequestInterceptionAsync(true);

            page.Request += (s, e) => ContinueRequest(e, data, HttpMethod.Post, contentType);

            var response = await page.GoToAsync(url);
            var content = await response.TextAsync();
            _ = page.CloseAsync();

            return new PuppeteerResponse(content, response.Ok);
        }

        // YES, THAT IS THE ONLY WAY IT CAN WORK RIGHT NOW
        private async Task<string?> RequestForCall(string url, dynamic data)
        {
            if (_browser is null)
            {
                Failure("You need to launch a browser first!\n Use `await LaunchChromeAsync()`");
                return null;
            }

            string jsFunc = $"async () => await fetch('{url}', {{ " +
            "    method: 'POST', " +
            "    headers: " +
            "    { " +
            "        'accept': 'application/json, text/plain, */*', " +
            "        'accept-encoding': 'gzip, deflate, br'," +
            $"       'authorization': 'Token {_userToken}', " +
            $"       'content-type': 'application/json', " +
            "        'origin': 'https://beta.character.ai/' " +
            "    }, " +
            $"   body: JSON.stringify({JsonConvert.SerializeObject(data)}) " +
            "}).then((response) => response.text())";

            string? content = null;
            try
            {
                var response = await _chatPage.EvaluateFunctionAsync(jsFunc);
                content = response.ToString();
            }
            catch (Exception e) { Failure(e: e); }

            return content;
        }

        private async void ContinueRequest(RequestEventArgs args, dynamic? data, HttpMethod method, string contentType)
        {
            var r = args.Request;
            var payload = CreateRequestPayload(method, data, contentType);

            await r.ContinueAsync(payload);
        }

        private Payload CreateRequestPayload(HttpMethod method, dynamic? data, string contentType)
        {
            var headers = new Dictionary<string, string> {
                { "authorization", $"Token {_userToken}" },
                { "accept", "application/json, text/plain, */*" },
                { "accept-encoding", "gzip, deflate, br" },
                { "content-type", contentType },
                { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36" }
            };
            string? serializedData;
            if (data is string || data is null)
                serializedData = data;
            else
                serializedData = JsonConvert.SerializeObject(data);

            return new Payload() { Method = method, Headers = headers, PostData = serializedData };
        }

        public async Task LaunchChromeAsync(string? customChromeDir = null)
        {
            try
            {
                PrepareDirectories();
                EXEC_PATH = await TryToDownloadBrowser(customChromeDir);

                // Stop all other puppeteer-chrome instances
                KillChromes(EXEC_PATH);
                Log("\n(If it hangs on for some reason, just try to relaunch the application)\nLaunching browser... ");

                var pex = new PuppeteerExtra();
                var stealthPlugin = new StealthPlugin(new StealthHardwareConcurrencyOptions(12));

                _browser = await pex.Use(stealthPlugin).LaunchAsync(new()
                {
                    Headless = true,
                    UserDataDir = $"{CD}{slash}puppeteer-user",
                    ExecutablePath = EXEC_PATH,
                    Args = new []
                    {
                        "--no-default-browser-check", "--no-sandbox", "--no-service-autorun",
                        "--mute-audio", "--ignore-certificate-errors",
                        "--disable-gpu", "--disable-infobars", "--disable-dev-shm-usage",
                        "--disable-setuid-sandbox", "--disable-blink-features=AutomationControlled",
                        "--disable-accelerated-2d-canvas", "--disable-features=ChromeWhatsNewUI",
                        "--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36"
                    },
                    Timeout = 1_200_000 // 15 minutes
                });

                _chatPage = await _browser.NewPageAsync();
                await _chatPage.GoToAsync($"https://beta.character.ai/");

                Success("OK");
            }
            catch (Exception e)
            {
                Failure(e.ToString());
            }
        }

        private static async Task<string> TryToDownloadBrowser(string? customPath)
        {
            string path = string.IsNullOrWhiteSpace(customPath) ? CHROME_PATH : customPath;
            using var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions() { Path = path });
            var revision = await browserFetcher.GetRevisionInfoAsync();

            if (!revision.Local)
            {
                Log("\nIt may take some time on the first launch, because it will need to download a Chrome executable (~450mb).\n" +
                      "If this process takes too much time, ensure you have a good internet connection (timeout = 20 minutes).\n");

                Log("\nDownloading browser... ");
                await browserFetcher.DownloadAsync();
                Success("OK");
            }

            return revision.ExecutablePath;
        }

        public static void KillChromes(string execPath)
        {
            var runningProcesses = Process.GetProcesses();

            foreach (var process in runningProcesses)
            {
                bool isPuppeteerChrome = process.ProcessName == "chrome" &&
                                         process.MainModule != null &&
                                         process.MainModule.FileName == execPath;

                if (isPuppeteerChrome) process.Kill();
            }
        }

        private static void PrepareDirectories()
        {
            string userPath = $"{CD}{slash}puppeteer-user";
            string tempsPath = $"{CD}{slash}puppeteer-temps";

            if (!Directory.Exists(userPath)) Directory.CreateDirectory(userPath);
            if (!Directory.Exists(tempsPath)) Directory.CreateDirectory(tempsPath);
        }
    }
}