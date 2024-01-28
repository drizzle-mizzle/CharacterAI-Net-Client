using System.Diagnostics;
using System.Dynamic;
using CharacterAI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static SharedUtils.Common;
using static PuppeteerLib.PuppeteerLib;
using PuppeteerLib.Models;
using PuppeteerSharp;

namespace CharacterAI.Client
{
    public class CharacterAiClient : IDisposable
    {
        private string _browserExecutablePath = null!;
        private readonly List<int> _heavyRequestsQueue = new();
        //private readonly List<int> _liteRequestsQueue = new();

        /// <summary>
        /// Browser : Usages
        /// </summary>
        //private readonly Dictionary<IBrowser, int> _browsersPool = new();
        private IBrowser _browser = null!;
        private IPage _page = null!;
        
        /// <summary>
        /// Create new integration with CharacterAI
        /// </summary>
        public CharacterAiClient(string? customBrowserDirectory = null, string? customBrowserExecutablePath = null)
        {
            var dir = string.IsNullOrWhiteSpace(customBrowserDirectory) ? null : customBrowserDirectory;
            var exe = string.IsNullOrWhiteSpace(customBrowserExecutablePath) ? null : customBrowserExecutablePath;

            _browserExecutablePath = exe ?? TryToDownloadBrowserAsync(dir ?? $"{CD}{SC}puppeteer-chrome").Result;
        }

        public async Task LaunchBrowserAsync()
        {            
            _browser = (await LaunchBrowserInstanceAsync(_browserExecutablePath))!;
            _page = await _browser.NewPageAsync();
            await _page.GoToAsync("https://plus.character.ai/search");
            bool ok = false;
            while (!ok)
                ok = await _page.TryToLeaveQueueAsync();
        }

        //private IBrowser? GetBrowser()
        //{
        //    try
        //    {
        //        lock (_browsersPool)
        //        {
        //            var browser = _browsersPool.FirstOrDefault(b => b.Value < 10).Key;

        //            if (browser is null)
        //            {
        //                browser = LaunchBrowserInstanceAsync(path: _browserExecutablePath).Result;

        //                if (browser is not null)
        //                    _browsersPool.TryAdd(browser, 0);
        //            }

        //            return browser;
        //        }
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //    finally
        //    {
        //        lock (_browsersPool)
        //            foreach (var browser in _browsersPool.Where(b => b.Value >= 10).Select(b => b.Key))
        //            {
        //                _browsersPool.Remove(browser);
        //                Task.Run(async () => await KillBrowserInstanceAsync(browser));
        //            }
        //    }
        //}

        public void EnsureAllChromeInstancesAreKilled()
        {
            if (string.IsNullOrEmpty(_browserExecutablePath))
                throw new Exception("No browser path");

            try
            {
                string browserDir = _browserExecutablePath[..(_browserExecutablePath.LastIndexOf(SC))];
                var allProcessesInDir = Process.GetProcesses().Where(proc =>
                    proc.MainModule != null && proc.MainModule.FileName.StartsWith(browserDir));

                foreach (var proc in allProcessesInDir)
                {
                    try { proc.Kill(); }
                    catch (Exception e) { LogRed($"(Warning) Failed to kill \"{proc.Id}\"", e); }
                }
            }
            catch (Exception e)
            {
                LogRed("(Warning) Failed to kill browser instances", e);
            }
        }


        /// <summary>
        /// Send message and get response
        /// </summary>
        /// <returns>new CharacterResponse()</returns>
        public async Task<CharacterResponse> CallCharacterAsync(string characterId, string characterTgt,
            string historyId, string message = "", string? imagePath = null, string? primaryMsgUuId = null,
            string? parentMsgUuId = null, string authToken = "", bool plusMode = false)
        {
            var contentDynamic = BasicCallContent(characterId, characterTgt, message, imagePath, historyId);

            // Fetch new answer ("perform swipe").
            if (parentMsgUuId is not null)
            { // When parent_msg_id is present, character will generate new response for a last message.
                contentDynamic.parent_msg_uuid = parentMsgUuId;
            }
            // Or set new (swiped) answer as one to reply on.
            else if (primaryMsgUuId is not null)
            { // Provide primary_msg_id to point out which character's response you've chosen.
                contentDynamic.primary_msg_uuid = primaryMsgUuId;
                // (seen_msg_ids[] is also required, either it just won't work, but I didn't bother to collect
                //  every single swiped message, just fill it with chosen one)
                contentDynamic.seen_msg_uuids = new[] { primaryMsgUuId };
            }

            string sub = plusMode ? "plus" : "beta";
            string url = $"https://{sub}.character.ai/chat/streaming/";
            
            FetchResponse fetchResponse = await FetchRequestAsync(_page, url, "POST", authToken, contentDynamic);
            if (fetchResponse.InQueue)
            {
                lock (_page)
                {
                    while (!_page.TryToLeaveQueueAsync().Result)
                        Task.Delay(10000).Wait();
                }

                fetchResponse = await FetchRequestAsync(_page, url, "POST", authToken, contentDynamic);
            }

            var fetchResult = new CharacterResponse(fetchResponse);
            
            if (!fetchResponse.IsBlocked)
                return fetchResult;

            if (WaitForTurnHeavy() is not int requsetId)
                return fetchResult;

            // Fallback on slower but more stable request method
            try
            {
                var puppeteerResponse = await PuppeteerLib.PuppeteerLib.RequestPostWithDownloadAsync(_browser, requsetId, url, authToken, contentDynamic);
                return new CharacterResponse(puppeteerResponse); // OK
            }
            catch (Exception e)
            {
                LogRed(e: e);
                return fetchResult;
            }
        }

        /// <summary>
        /// Get info about character
        /// </summary>
        /// <returns>new Character; can throw Exception</returns>
        public async Task<Character> GetInfoAsync(string characterId, string authToken = "", bool plusMode = false)
        {
            string sub = (plusMode) ? "plus" : "beta";
            string url = $"https://{sub}.character.ai/chat/character/info/";
            var data = new Dictionary<string, string> { { "external_id", characterId } };

            var response = await _browser.RequestPostAsync(url, authToken, data);

            dynamic? character;
            if (response.InQueue)
                character = null;
            else if (response.IsSuccessful)
            {
                var parsed = JsonConvert.DeserializeObject<dynamic>(response.Content!)
                    ?? throw new Exception("No content");

                if (parsed.character is JArray)
                    throw new Exception($"Failed to get character info. Perhaps the character is private?{(parsed.error is string e ? $" | Error: {e}" : "")}");

                character = parsed.character;
            }
            else
            {
                LogRed(response.Content);
                character = null;
            }

            return new Character(character);
        }

        public async Task<string?> GetLastChatAsync(string characterId, string authToken = "", bool plusMode = false)
        {
            string sub = plusMode ? "plus" : "beta";
            string url = $"https://{sub}.character.ai/chat/history/continue/";

            var data = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "character_external_id", characterId }
            });

            var response = await _browser.RequestPostAsync(url, authToken, data);

            if (response.IsSuccessful)
                return JsonConvert.DeserializeObject<dynamic>(response.Content!)?.external_id;

            await Task.Delay(5000);
            return await CreateNewChatAsync(characterId, authToken, plusMode);

        }

        /// <summary>
        /// Create new chat with a character
        /// </summary>
        /// <returns>returns chat_history_id if successful; null if fails.</returns>
        public async Task<string?> CreateNewChatAsync(string characterId, string authToken = "", bool plusMode = false)
        {
            string sub = plusMode ? "plus" : "beta";
            string url = $"https://{sub}.character.ai/chat/history/create/";
            var data = new Dictionary<string, string>
            {
                { "character_external_id", characterId }
            };

            var response = await _browser.RequestPostAsync(url, authToken, data);

            // Their servers are shit and sometimes it requires a second request
            if (!response.IsSuccessful)
            {
                await Task.Delay(3000);
                response = await _browser.RequestPostAsync(url, authToken, data);
            }

            if (!response.IsSuccessful)
            {
                LogRed(response.Content);
                return null;
            }

            var externalId = JsonConvert.DeserializeObject<dynamic>(response.Content!)?.external_id;
            if (externalId is null)
                LogRed(response.Content);

            return externalId;
        }

        // Search for a character
        public async Task<SearchResponse> SearchAsync(string query, string authToken = "", bool plusMode = false)
        {
            string sub = plusMode ? "plus" : "beta";
            string url = $"https://{sub}.character.ai/chat/characters/search/?query={query}";
            
            var response = await _browser.RequestGetAsync(url, authToken);

            return new SearchResponse(response, query);
        }

        /// <summary>
        /// Here is listed the whole list of all known payload parameters.
        /// Some of these are useless, some seems to be not really used yet in actual API, some do simply have unknown purpose,
        /// thus they are either commented or set with default value taken from cai site.
        /// </summary>
        private static dynamic BasicCallContent(string characterId, string characterTgt, string msg, string? imgPath, string historyId)
        {
            dynamic content = new ExpandoObject();

            content.character_external_id = characterId;
            content.history_external_id = historyId;
            content.text = msg;
            content.tgt = characterTgt;

            if (!string.IsNullOrEmpty(imgPath))
            {
                content.image_description = "";
                content.image_description_type = "AUTO_IMAGE_CAPTIONING";
                content.image_origin_type = "UPLOADED";
                content.image_rel_path = imgPath;
            }

            // Unknown, unused and default params
            content.give_room_introductions = true;
            //initial_timeout : null
            //insert_beginning : null
            content.is_proactive = false;
            content.mock_response = false;
            //model_properties_version_keys : ""
            //model_properties_version_keys : ""
            //model_server_address : null
            content.num_candidates = 1;
            //override_prefix : null
            //override_rank : null
            //prefix_limit : null
            //prefix_token_limit : null
            //rank_candidates : null
            content.ranking_method = "random";
            //retry_last_user_msg_uuid : null
            content.CallCharacterAsyncstaging = false;
            content.stream_every_n_steps = 16;
            //stream_params : null
            //unsanitized_characters : null
            content.voice_enabled = false;

            return content;
        }

        private int? WaitForTurnHeavy()
        {
            int requestId;

            while (true)
            {
                requestId = new Random().Next(32767);
                lock (_heavyRequestsQueue)
                    if (!_heavyRequestsQueue.Contains(requestId)) break;
            }

            lock (_heavyRequestsQueue)
                _heavyRequestsQueue.Add(requestId);

            try
            {
                for (int i = 0; i <= 50; i++)
                {
                    lock (_heavyRequestsQueue)
                        if (_heavyRequestsQueue.Count == 0 || _heavyRequestsQueue[0] == requestId)
                            break;

                    if (i == 50)
                        return null;

                    Task.Delay(3000).Wait();
                }

                return requestId;
            }
            catch
            {
                return null;
            }
            finally
            {
                lock (_heavyRequestsQueue)
                    _heavyRequestsQueue.Remove(requestId);
            }
        }

        //private int? WaitForTurnLite()
        //{
        //    int requestId;

        //    while (true)
        //    {
        //        requestId = new Random().Next(32767);
        //        lock (_liteRequestsQueue)
        //            if (!_liteRequestsQueue.Contains(requestId)) break;
        //    }

        //    lock (_liteRequestsQueue)
        //        _liteRequestsQueue.Add(requestId);

        //    try
        //    {
        //        for (int i = 0; i <= 40; i++)
        //        {
        //            lock (_liteRequestsQueue)
        //                if (_liteRequestsQueue.Count < 20 || _liteRequestsQueue[..20].Contains(requestId))
        //                    break;

        //            if (i == 40)
        //                return null;

        //            Task.Delay(3000).Wait();
        //        }

        //        return requestId;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //    finally
        //    {
        //        lock (_liteRequestsQueue)
        //            _liteRequestsQueue.Remove(requestId);
        //    }
        //}

        #region IDisposable implementation with finalizer

        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);   
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                try
                {
                    EnsureAllChromeInstancesAreKilled();
                }
                catch { }

                try
                {
                    Directory.Delete($"{CD}{SC}puppeteer-temps", true);
                }
                catch { }

                try
                {
                    lock (_heavyRequestsQueue)
                        _heavyRequestsQueue.Clear();

                    _browser = null!;
                    _browserExecutablePath = null!;
                }
                catch { }
            }

            _disposed = true;
        }
        #endregion
    }
}
