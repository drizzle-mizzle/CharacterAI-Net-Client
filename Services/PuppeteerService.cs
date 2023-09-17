using Newtonsoft.Json;
using PuppeteerExtraSharp.Plugins.ExtraStealth.Evasions;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
using PuppeteerExtraSharp;
using PuppeteerSharp;
using System.Diagnostics;
using CharacterAI.Models;
using static CharacterAI.Services.CommonService;
using PuppeteerSharp.BrowserData;

namespace CharacterAI.Services
{
    public class PuppeteerService
    {
        public string EXEC_PATH { get; }

        private IBrowser? _browser;
        private IPage? _searchPage;

        private readonly bool _caiPlusMode;
        private readonly string _caiToken;
        private readonly List<int> _requestQueue = new();

        internal protected PuppeteerService(string caiToken, bool caiPlusMode, string? customBrowserDirectory, string? customBrowserExecutablePath)
        {
            _caiPlusMode = caiPlusMode;
            _caiToken = caiToken;

            var dir = string.IsNullOrWhiteSpace(customBrowserDirectory) ? null : customBrowserDirectory;
            var exe = string.IsNullOrWhiteSpace(customBrowserExecutablePath) ? null : customBrowserExecutablePath;

            EXEC_PATH = exe ?? TryToDownloadBrowserAsync(dir ?? $"{CD}{SC}puppeteer-chrome").Result;
        }

        /// <param name="killDuplicates">Kill all other browser processes launched from this folder.</param>
        internal protected async Task LaunchBrowserAsync(bool killDuplicates)
        {
            if (killDuplicates) KillBrowser();

            var pex = new PuppeteerExtra();
            var args = new[]
            {
                "--no-default-browser-check", "--no-sandbox", "--disable-setuid-sandbox", "--no-first-run", "--single-process",
                "--disable-default-apps", "--disable-features=Translate", "--disable-infobars", "--disable-dev-shm-usage",
                "--mute-audio", "--ignore-certificate-errors", "--use-gl=egl"
            };
            var launchOptions = new LaunchOptions()
            {
                Args = args,
                Headless = true,
                Timeout = 1_200_000, // 15 minutes
                ExecutablePath = EXEC_PATH,
                IgnoredDefaultArgs = new[] { "--disable-extensions" } // https://github.com/puppeteer/puppeteer/blob/main/docs/troubleshooting.md#chrome-headless-doesnt-launch-on-windows
            };
            var stealthPlugin = new StealthPlugin(new StealthHardwareConcurrencyOptions(1));
            
            try
            {
                Log("\nLaunching browser... ");
                _browser = await pex.Use(stealthPlugin).LaunchAsync(launchOptions);
                Success("OK");

                Log("Opening character.ai page... ");
                _searchPage = await _browser.NewPageAsync();
                await TryToOpenCaiPage();
                Log("OK", ConsoleColor.Green);

                if (_caiPlusMode) Log($" [c.ai+ Mode Enabled]\n\n", ConsoleColor.Yellow);
                else Log("\n");
            }
            catch (Exception e)
            {
                Failure("Fail", e: e);
                Log("\nTrying again...\n");
                await LaunchBrowserAsync(killDuplicates);
            }
        }

        public void KillBrowser()
        {
            try
            {
                var runningProcesses = Process.GetProcesses();
                foreach (var process in runningProcesses)
                {
                    bool isPuppeteerChrome = process.ProcessName.Contains("chrome") &&
                                             process.MainModule != null &&
                                             process.MainModule.FileName == EXEC_PATH;

                    if (isPuppeteerChrome) process.Kill();
                }

                _browser = null;
                _searchPage = null;
            }
            catch(Exception e)
            {
                Failure("Failed to kill browser. This error won't affect the workflow of your application, " +
                        "but if you will relaunch your application and see this error again, it will mean " +
                        "that the old Puppeteer browser process probably is still running in the background " +
                        "and consumes the memory of your machine.\n",
                        e: e);
            }
        }

        internal async Task<PuppeteerResponse> RequestGetAsync(string url, string? customAuthToken = null)
        {
            if (_browser is null)
            {
                Failure("You need to launch the browser first!");
                return new PuppeteerResponse(null, false);
            }

            IPage page;
            try
            {
                page = await _browser.NewPageAsync();
            }
            catch
            {
                lock (_browser)
                {
                    LaunchBrowserAsync(true).Wait();
                    return RequestGetAsync(url, customAuthToken).Result;
                }
            }

            await page.SetRequestInterceptionAsync(true);
            page.Request += (s, e) => ContinueRequest(e, null, HttpMethod.Get, "application/json", customAuthToken);

            var response = await page.GoToAsync(url);
            var content = await response.TextAsync();
            _ = page.CloseAsync();

            return new PuppeteerResponse(content, response.Ok);
        }

        internal async Task<PuppeteerResponse> RequestPostAsync(string url, dynamic? data = null, string contentType = "application/json", string? customAuthToken = null)
        {
            if (_browser is null)
            {
                Failure("You need to launch the browser first!");
                return new PuppeteerResponse(null, false);
            }

            IPage page;
            try
            {
                page = await _browser.NewPageAsync();
            }
            catch
            {
                lock (_browser)
                {
                    LaunchBrowserAsync(true).Wait();
                    return RequestPostAsync(url, data, contentType, customAuthToken).Result;
                }
            }

            await page.SetRequestInterceptionAsync(true);
            page.Request += (s, e) => ContinueRequest(e, data, HttpMethod.Post, contentType, customAuthToken);

            var response = await page.GoToAsync(url);
            var content = await response.TextAsync();
            _ = page.CloseAsync();

            return new PuppeteerResponse(content, response.Ok);
        }

        internal async Task<PuppeteerResponse> RequestPostWithDownloadAsync(string url, dynamic? data = null, string? customAuthToken = null)
        {
            if (_browser is null)
            {
                Failure("You need to launch the browser first!");
                return new PuppeteerResponse(null, false);
            }

            int? requestId = await WaitForTurnAsync();
            if (requestId is null) return new PuppeteerResponse(null, false);

            string downloadPath = $"{CD}{SC}puppeteer-temps{SC}{requestId}";
            if (Directory.Exists(downloadPath)) Directory.Delete(downloadPath, true);
            Directory.CreateDirectory(downloadPath);

            IPage page;
            try
            {
                page = await _browser.NewPageAsync();
            }
            catch
            {
                lock (_browser)
                {
                    LaunchBrowserAsync(true).Wait();
                    return RequestPostWithDownloadAsync(url, data, customAuthToken).Result;
                }
            }

            await page.SetRequestInterceptionAsync(true);
            await page.Client.SendAsync("Page.setDownloadBehavior", new { behavior = "allow", downloadPath });
            page.Request += (s, e) => ContinueRequest(e, data, HttpMethod.Post, "application/json", customAuthToken);

            try { await page.GoToAsync(url); } // it will always throw an exception
            catch (NavigationException)
            {
                // "download" is a temporary file name where response content is saved
                string responsePath = $"{downloadPath}{SC}download";

                // Wait 90 seconds for the response to download
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(3000);
                    if (File.Exists(responsePath)) break;
                    if (i == 30) return new PuppeteerResponse(null, false);
                }

                _ = page.CloseAsync();
                _requestQueue.Remove((int)requestId);

                var content = await File.ReadAllTextAsync(responsePath);
                try { Directory.Delete(downloadPath, recursive: true); } catch { };

                if (string.IsNullOrEmpty(content))
                    return new PuppeteerResponse(null, false);

                return new PuppeteerResponse(content, true);
            }

            return new PuppeteerResponse(null, false); // not really needed
        }

        /// <returns>
        /// Reloaded page.
        /// </returns>
        internal async Task TryToLeaveQueueAsync(bool log = true)
        {
            if (_searchPage is null) return;

            await Task.Delay(15000);
            if (log) Log("\n15sec has passed, reloading... ");

            var response = await _searchPage.ReloadAsync();
            string content = await response.TextAsync();

            if (content.Contains("Waiting Room"))
            {
                if (log) Log(":(\nWait...");
                await TryToLeaveQueueAsync(log);
            }
        }

        internal async Task<FetchResponse> FetchRequestAsync(string url, string method, dynamic? data = null, string contentType = "application/json", string? customAuthToken = null)
        {
            if (_browser is null || _searchPage is null)
            {
                Failure("You need to launch the browser first!");
                return new FetchResponse(null);
            }

            string jsFunc = "async () => {" +
            $"  var response = await fetch('{url}', {{ " +
            $"      method: '{method}', " +
            "       headers: " +
            "       { " +
            "           'accept': 'application/json, text/plain, */*', " +
            "           'accept-encoding': 'gzip, deflate, br'," +
            $"          'authorization': 'Token {customAuthToken ?? _caiToken}', " +
            $"          'content-type': '{contentType}', " +
            $"          'origin': '{url}', " +
            $"          'user-agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36'" +
            "       }" + (data is null ? "" :
            $"    , body: JSON.stringify({JsonConvert.SerializeObject(data)}) ") +
            "   });" +
            "   var responseStatus = response.status;" +
            "   var responseContent = await response.text();" +
            "   return JSON.stringify({ status: responseStatus, content: responseContent });" +
            "}";

            try
            {
                var response = await _searchPage.EvaluateFunctionAsync(jsFunc);
                var fetchResponse = new FetchResponse(response);
                if (fetchResponse.InQueue)
                {
                    lock(_searchPage)
                        TryToLeaveQueueAsync(log: false).Wait();

                    fetchResponse = await FetchRequestAsync(url, method, data, contentType, customAuthToken);
                }

                return fetchResponse;
            }
            catch (Exception e)
            {
                Failure(e: e);
                return new FetchResponse(null);
            }
        }

        /// <returns>
        /// Browser executable path.
        /// </returns>
        internal static async Task<string> TryToDownloadBrowserAsync(string path)
        {
            using var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions() { Path = path, Browser = SupportedBrowser.Chrome });

            if (browserFetcher.GetInstalledBrowsers().FirstOrDefault() is InstalledBrowser ib)
            {
                return ib.GetExecutablePath();
            }

            Log($"\nDownloading browser...\nPath: ");
            Log($"{path}\n", ConsoleColor.Yellow);

            int top = Console.GetCursorPosition().Top;
            int progress = 0;

            browserFetcher.DownloadProgressChanged += (sender, args) =>
            {
                var pp = args.ProgressPercentage;
                if (pp <= progress) return;

                progress = pp;
                Console.SetCursorPosition(0, top);

                string logProgress = $"Progress: [{new string('=', pp / 2)}{new string(' ', 50 - (pp / 2))}] ";
                int l = logProgress.Length;
                Log(logProgress);

                if (pp < 100)
                {
                    string oo = $"{pp}% ";
                    l += oo.Length;
                    Log(oo);
                }
                else
                {
                    l += 5;
                    Log("100% ", ConsoleColor.Green);
                }

                string mb = $"({Math.Round(args.BytesReceived / 1024000.0f, 2)}/{Math.Round(args.TotalBytesToReceive / 1024000.0f, 2)} mb)";
                l += mb.Length;
                Log(mb, ConsoleColor.Yellow);
                Log(new string(' ', Console.WindowWidth - l));
            };
            var browser = await browserFetcher.DownloadAsync();

            return browser.GetExecutablePath();
        }

        private async Task TryToOpenCaiPage()
        {
            if (_searchPage is null) return;

            var response = await _searchPage.GoToAsync($"https://{(_caiPlusMode ? "plus" : "beta")}.character.ai/search?"); // most lightweight page
            string content = await response.TextAsync();

            if (content.Contains("Waiting Room"))
            {
                Log("\nYou are now in line. Wait... ");
                await TryToLeaveQueueAsync();
            }
        }

        private async void ContinueRequest(RequestEventArgs args, dynamic? data, HttpMethod method, string contentType, string? customAuthToken = null)
        {
            var r = args.Request;
            var payload = CreateRequestPayload(method, data, contentType, customAuthToken ?? _caiToken);

            await r.ContinueAsync(payload);
        }

        private static Payload CreateRequestPayload(HttpMethod method, dynamic? data, string contentType, string caiToken)
        {
            var headers = new Dictionary<string, string> {
                { "authorization", $"Token {caiToken}" },
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

        private async Task<int?> WaitForTurnAsync()
        {
            int requestId;

            while (true)
            {
                requestId = new Random().Next(32767);
                if (!_requestQueue.Contains(requestId)) break;
            }
            _requestQueue.Add(requestId);

            for (int i = 0; i < 60; i++)
            {
                if (_requestQueue.First() == requestId) break;
                if (i == 60) return null;

                await Task.Delay(3000);
            }

            return requestId;
        }
    }
}
