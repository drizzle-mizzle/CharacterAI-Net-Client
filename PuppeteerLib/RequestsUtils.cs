using System.Diagnostics;
using Newtonsoft.Json;
using PuppeteerExtraSharp.Plugins.ExtraStealth.Evasions;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
using PuppeteerExtraSharp;
using PuppeteerSharp;
using PuppeteerSharp.BrowserData;
using PuppeteerLib.Models;
using Tmds.Utils;
using static SharedUtils.Common;

namespace PuppeteerLib
{
    public static class RequestsUtils
    {
        private static readonly TimeSpan oneMinute = new(0, 1, 0);

        private static readonly bool DEBUG = Environment.GetEnvironmentVariable("PUPLOG") is not null;

        /// <returns>
        /// Browser instance
        /// </returns>
        public static async Task<IBrowser?> LaunchBrowserInstanceAsync(string path)
        {
            IBrowser? result;
            try
            {
                var pex = new PuppeteerExtra();
                string[] args =
                [
                    "--no-default-browser-check", "--no-sandbox", "--disable-setuid-sandbox", "--no-first-run",
                    "--single-process", "--disable-default-apps", "--disable-features=Translate", "--disable-infobars",
                    "--disable-dev-shm-usage", "--mute-audio", "--ignore-certificate-errors", "--use-gl=egl", "--ptag"
                ];

                var launchOptions = new LaunchOptions()
                {
                    Args = args,
                    Headless = true,
                    Timeout = 1_200_000, // 15 minutes
                    ExecutablePath = path,
                    IgnoredDefaultArgs = ["--disable-extensions"] // https://github.com/puppeteer/puppeteer/blob/main/docs/troubleshooting.md#chrome-headless-doesnt-launch-on-windows
                };

                var stealthPlugin = new StealthPlugin(new StealthHardwareConcurrencyOptions(1));
                var browser = await pex.Use(stealthPlugin).LaunchAsync(launchOptions);

                result = browser;
            }
            catch (Exception e)
            {
                LogRed("Failed to launch browser", e);

                result = null;
            }

            return result;
        }


        public static async Task<GotoResponse> GetGotoRequestAsync(Guid requestId, string browserExePath, string url, string authToken)
            => await RequestAsync(requestId, browserExePath, HttpMethod.Get, url, authToken, "application/json");


        public static async Task<GotoResponse> PostGotoRequestAsync(Guid requestId, string browserExePath, string url, string authToken, dynamic? data = null, string contentType = "application/json")
            => await RequestAsync(requestId, browserExePath, HttpMethod.Post, url, authToken, contentType, data);


        private static async Task<GotoResponse> RequestAsync(Guid requestId, string browserExePath, HttpMethod method, string url, string authToken, string contentType, dynamic? data = null)
        {
            if (DEBUG)
                Log($"{DateTime.Now} START [{requestId.ToString().Split('-')[0]}] | {method} GotoRequest -> {url}\n");

            string savePath = $"{CD}{SC}puppeteer-temps{SC}{requestId}";
            
            string? content = null;
            var executor = new FunctionExecutor(options =>
            {
                options.StartInfo.RedirectStandardError = true;
                options.OnExit = proc =>
                {
                    if (proc.ExitCode != 1) return;

                    content = File.ReadAllText($"{savePath}");
                    File.Delete(savePath);
                };
            });

            await executor.RunAsync(async args =>
            {
                try
                {
                    await Task.Yield();

                    string saveDir = args[1];
                    await using var oneshotBrowser = await LaunchBrowserInstanceAsync(args[0]);
                    if (oneshotBrowser is null) return 0;

                    await using var page = await oneshotBrowser.NewPageAsync();
                    await page.SetRequestInterceptionAsync(true);
                    page.Request += (_, e) => ContinueRequest(e, data, method, contentType, authToken);

                    // It will always throw a NavigationException exception, but it will perform the request
                    var response = await page.GoToAsync(url).WaitAsync(oneMinute);
                    string text = await response.TextAsync();
                    await File.WriteAllTextAsync(saveDir, text);

                    return 1;
                }
                catch (Exception e)
                {
                    LogRed(e: e);
                }

                return 0;
            }, args: [browserExePath, savePath]);

            if (DEBUG)
                Log($"{DateTime.Now} END [{requestId.ToString().Split('-')[0]}] | {method} GotoRequest\n");

            return new GotoResponse(content, !string.IsNullOrEmpty(content));
        }


        public static async Task<GotoResponse> RequestPostWithDownloadAsync(Guid requestId, string browserExePath, string url, string authToken, dynamic? data = null)
        {
            if (DEBUG)
                Log($"{DateTime.Now} START [{requestId.ToString().Split('-')[0]}] | POST RequestPostWithDownload -> {url}\n");

            string downloadPath = $"{CD}{SC}puppeteer-temps{SC}{requestId}";

            // Reacrete directory
            if (Directory.Exists(downloadPath)) Directory.Delete(downloadPath, true);
            Directory.CreateDirectory(downloadPath);

            string? content = null;
            var executor = new FunctionExecutor(options =>
            {
                options.StartInfo.RedirectStandardError = true;
                options.OnExit = proc =>
                {
                    if (proc.ExitCode != 1) return;

                    content = File.ReadAllText($"{downloadPath}{SC}download");
                    Directory.Delete(downloadPath, recursive: true);
                };
            });


            await executor.RunAsync(async args =>
            {
                try
                {
                    await Task.Yield();

                    string downloadDir = args[1];
                    await using var oneshotBrowser = await LaunchBrowserInstanceAsync(args[0]);
                    if (oneshotBrowser is null) return 0;

                    await using var page = await oneshotBrowser.NewPageAsync();
                    await page.SetRequestInterceptionAsync(true);
                    await page.Client.SendAsync("Page.setDownloadBehavior", new { behavior = "allow", downloadDir }).WaitAsync(oneMinute);
                    page.Request += (_, e) => ContinueRequest(e, data, HttpMethod.Post, "application/json", authToken);

                    // It will always throw a NavigationException exception, but it will perform the request
                    await page.GoToAsync(url).WaitAsync(oneMinute);
                }
                catch (NavigationException)
                {
                    // Wait 90 seconds for the response to download
                    for (int i = 0; i <= 30; i++)
                    {
                        Task.Delay(3000).Wait();
                        if (File.Exists(downloadPath)) return 1;
                    }
                }
                catch (Exception e)
                {
                    LogRed(e: e);
                }

                return 0;
            }, args: [browserExePath, downloadPath]);

            if (DEBUG)
                Log($"{DateTime.Now} END [{requestId.ToString().Split('-')[0]}] | POST RequestPostWithDownload\n");

            return new GotoResponse(content, !string.IsNullOrEmpty(content));
        }


        public static async Task<FetchResponse> FetchRequestPostAsync(IPage page, string url, string authToken, dynamic? data = null, string contentType = "application/json")
        {
            var requestId = Guid.NewGuid();
            if (DEBUG)
                Log($"{DateTime.Now} START [{requestId.ToString().Split('-')[0]}] | POST FetchRequestPost -> {url}\n");

            try
            {
                string jsFunc = "async () => {"                                                                                                                              +
                                $"  var response = await fetch('{url}', {{ "                                                                                                 +
                                $"      method: 'POST', "                                                                                                                    +
                                "       headers: "                                                                                                                           +
                                "       { "                                                                                                                                  +
                                "           'accept': 'application/json, text/plain, */*', "                                                                                 +
                                "           'accept-encoding': 'gzip, deflate, br',"                                                                                         +
                                $"          'authorization': 'Token {authToken}', "                                                                                          +
                                $"          'content-type': '{contentType}', "                                                                                               +
                                $"          'origin': '{url}', "                                                                                                             +
                                $"          'user-agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36'" +
                                "       }"                                                                                                                                   + (data is null
                                    ? ""
                                    : $", body: JSON.stringify({JsonConvert.SerializeObject(data)}) ")            +
                                "   });"                                                                          +
                                "   var responseStatus = response.status;"                                        +
                                "   var responseContent = await response.text();"                                 +
                                "   return JSON.stringify({ status: responseStatus, content: responseContent });" +
                                "}";

                var response = await page.EvaluateFunctionAsync(jsFunc);
                var fetchResponse = new FetchResponse(response);

                return fetchResponse;
            }
            catch (Exception e)
            {
                LogRed(e: e);
                return new FetchResponse(null);
            }
            finally
            {
                if (DEBUG)
                    Log($"{DateTime.Now} END [{requestId.ToString().Split('-')[0]}] | POST FetchRequestPost\n");
            }
        }

        public static async Task<FetchResponse> FetchRequestGetAsync(IPage page, string url, string authToken)
        {
            var requestId = Guid.NewGuid();
            if (DEBUG)
                Log($"{DateTime.Now} START [{requestId.ToString().Split('-')[0]}] | GET FetchRequestGet -> {url}\n");

            try
            {
                string jsFunc = "async () => {"                                                                                                                              +
                                $"  var response = await fetch('{url}', {{ "                                                                                                 +
                                $"      method: 'GET', "                                                                                                                    +
                                "       headers: "                                                                                                                           +
                                "       { "                                                                                                                                  +
                                "           'accept': 'application/json, text/plain, */*', "                                                                                 +
                                "           'accept-encoding': 'gzip, deflate, br',"                                                                                         +
                                $"          'authorization': 'Token {authToken}', "                                                                                          +
                                $"          'content-type': 'application/json', "                                                                                               +
                                $"          'origin': '{url}', "                                                                                                             +
                                $"          'user-agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36'" +
                                "       }"                                                               +
                                "   });"                                                                                                                                     +
                                "   var responseStatus = response.status;"                                                                                                   +
                                "   var responseContent = await response.text();"                                                                                            +
                                "   return JSON.stringify({ status: responseStatus, content: responseContent });"                                                            +
                                "}";

                var response = await page.EvaluateFunctionAsync(jsFunc);
                var fetchResponse = new FetchResponse(response);

                return fetchResponse;
            }
            catch (Exception e)
            {
                LogRed(e: e);
                return new FetchResponse(null);
            }
            finally
            {
                if (DEBUG)
                    Log($"{DateTime.Now} END [{requestId.ToString().Split('-')[0]}] | GET FetchRequestGet\n");
            }
        }

        /// <returns>
        /// Full path to the browser executable file
        /// </returns>
        public static async Task<string> TryToDownloadBrowserAsync(string path, bool log = true)
        {
            using var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions { Path = path, Browser = SupportedBrowser.Chromium });

            if (browserFetcher.GetInstalledBrowsers().FirstOrDefault() is InstalledBrowser ib)
            {
                string exePath = ib.GetExecutablePath();

                if (log) LogGreen($"Found installed browser: {exePath}");
                return exePath;
            }

            if (log)
            {
                Log($"Downloading browser...\nPath: ");
                Log($"{path}\n", ConsoleColor.Yellow);
            }

            int top = Console.GetCursorPosition().Top;
            int progress = 0;

            object locker = new();

            if (log)
            {
                browserFetcher.DownloadProgressChanged += (sender, args) =>
                {
                    lock (locker)
                    {
                        var pp = args.ProgressPercentage;
                        if (pp <= progress) return;

                        progress = pp;
                        string logProgress = $"[{new string('=', pp / 2)}{new string(' ', 50 - pp / 2)}] ";

                        Console.SetCursorPosition(0, top);
                        Log(new string('\b', Console.WindowWidth));
                        Console.SetCursorPosition(0, top + 1);
                        Log(new string('\b', Console.WindowWidth));
                        Console.SetCursorPosition(0, top + 1);
                        Log("Progress:\n", ConsoleColor.Green);
                        Log(logProgress);

                        if (pp < 100)
                        {
                            string oo = $"{pp}% ";
                            Log(oo);
                        }
                        else
                        {
                            Log("100% ", ConsoleColor.Green);
                        }

                        string mb = $"({Math.Round(args.BytesReceived / 1024000.0f, 2)}/{Math.Round(args.TotalBytesToReceive / 1024000.0f, 2)} mb)\n";

                        Log(mb, ConsoleColor.Yellow);
                        Task.Delay(10).Wait();
                    }
                };
            }

            var browser = await browserFetcher.DownloadAsync(BrowserTag.Latest);

            return browser.GetExecutablePath();
        }


        private static async void ContinueRequest(RequestEventArgs args, dynamic? data, HttpMethod method, string contentType, string authToken)
        {
            var r = args.Request;
            var payload = CreateRequestPayload(method, data, contentType, authToken);

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

            string? serializedData = data is string or null ? data : (string?)JsonConvert.SerializeObject(data);

            return new Payload() { Method = method, Headers = headers, PostData = serializedData };
        }


        public static bool InQueue(this string content)
            => content.Contains("Waiting Room");


        /// <returns>
        /// Reloaded page.
        /// </returns>
        public static async Task<bool> TryToLeaveQueueAsync(this IPage page)
        {
            try
            {   // Try for 2 minutes
                for (int i = 0; i < 24; i++)
                {
                    var response = await page.ReloadAsync();
                    string content = await response.TextAsync();
                    if (content.InQueue())
                        await Task.Delay(5000);
                    else
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
