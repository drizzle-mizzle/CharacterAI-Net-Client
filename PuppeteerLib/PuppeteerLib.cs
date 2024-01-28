using Newtonsoft.Json;
using PuppeteerExtraSharp.Plugins.ExtraStealth.Evasions;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
using PuppeteerExtraSharp;
using PuppeteerSharp;
using System.Diagnostics;
using PuppeteerSharp.BrowserData;
using PuppeteerLib.Models;
using static SharedUtils.Common;

namespace PuppeteerLib
{
    public static class PuppeteerLib
    {
        /// <returns>
        /// Browser executable path.
        /// </returns>
        public static async Task<string> TryToDownloadBrowserAsync(string path)
        {
            using var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions { Path = path, Browser = SupportedBrowser.Chromium });

            if (browserFetcher.GetInstalledBrowsers().FirstOrDefault() is InstalledBrowser ib)
            {
                string exePath = ib.GetExecutablePath(); 
                LogGreen($"Found installed browser: {exePath}");
                return exePath;
            }

            Log($"Downloading browser...\nPath: ");
            Log($"{path}\n", ConsoleColor.Yellow);

            int top = Console.GetCursorPosition().Top;
            int progress = 0;

            object locker = new();
            browserFetcher.DownloadProgressChanged += (sender, args) =>
            {
                lock (locker)
                {
                    var pp = args.ProgressPercentage;
                    if (pp <= progress) return;

                    progress = pp;
                    Console.SetCursorPosition(0, top);

                    string logProgress = $"Progress: [{new string('=', pp / 2)}{new string(' ', 50 - pp / 2)}] ";
                    int l = logProgress.Length;

                    Console.Write(new string('\b', Console.WindowWidth));
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
                    Task.Delay(10).Wait();
                }
            };
            var browser = await browserFetcher.DownloadAsync(BrowserTag.Latest);

            return browser.GetExecutablePath();
        }

        public static async Task<IBrowser?> LaunchBrowserInstanceAsync(string path)
        {
            IBrowser? result;
            try
            {
                var pex = new PuppeteerExtra();
                var args = new[]
                {
                    "--no-default-browser-check", "--no-sandbox", "--disable-setuid-sandbox", "--no-first-run",
                    "--single-process", "--disable-default-apps", "--disable-features=Translate", "--disable-infobars",
                    "--disable-dev-shm-usage", "--mute-audio", "--ignore-certificate-errors", "--use-gl=egl", "--ptag"
                };

                var launchOptions = new LaunchOptions()
                {
                    Args = args,
                    Headless = true,
                    Timeout = 1_200_000, // 15 minutes
                    ExecutablePath = path,
                    IgnoredDefaultArgs = new[] { "--disable-extensions" } // https://github.com/puppeteer/puppeteer/blob/main/docs/troubleshooting.md#chrome-headless-doesnt-launch-on-windows
                };

                var stealthPlugin = new StealthPlugin(new StealthHardwareConcurrencyOptions(1));
                var browser = await pex.Use(stealthPlugin).LaunchAsync(launchOptions);

                result = browser;
            }
            catch (Exception e)
            {
                LogRed($"Failed to launch browser", e);

                result = null;
            }

            return result;
        }

        //public static async Task<bool> KillBrowserInstanceAsync(IBrowser browser)
        //{
        //    bool result;

        //    int pid = browser.Process.Id;

        //    try
        //    {
        //        Process.GetProcessById(pid).Kill();
        //        result = true;
        //    }
        //    catch (Exception e)
        //    {
        //        LogRed($"Failed to kill process \"{pid}\"", e);
        //        result = false;
        //    }

        //    try
        //    {
        //        await browser.DisposeAsync();
        //    }
        //    catch (Exception e)
        //    {
        //        Log($"Failed to dispose browser object: {e.Message}\n");
        //    }

        //    return result;
        //}


        private static async Task<PuppeteerResponse> RequestAsync(IBrowser browser, HttpMethod method, string url, string authToken, string contentType, dynamic? data = null)
        {
            await using var page = await browser.NewPageAsync();
            try
            {
                await page.SetRequestInterceptionAsync(true);
                page.Request += (_, e) => ContinueRequest(e, data, method, contentType, authToken);

                var response = await page.GoToAsync(url);
                string content = await response.TextAsync();

                return new PuppeteerResponse(content, response.Ok);
            }
            catch
            {
                return new PuppeteerResponse(null, false);
            }
        }


        public static async Task<PuppeteerResponse> RequestGetAsync(this IBrowser browser, string url, string authToken)
            => await RequestAsync(browser, HttpMethod.Get, url, authToken, "application/json");


        public static async Task<PuppeteerResponse> RequestPostAsync(this IBrowser browser, string url, string authToken, dynamic? data = null, string contentType = "application/json")
            => await RequestAsync(browser, HttpMethod.Post, url, authToken, contentType, data);


        public static async Task<FetchResponse> FetchRequestAsync(IPage page, string url, string method, string authToken, dynamic? data = null, string contentType = "application/json")
        {
            try
            {
                string jsFunc = "async () => {" +
                                $"  var response = await fetch('{url}', {{ " +
                                $"      method: '{method}', " +
                                "       headers: " +
                                "       { " +
                                "           'accept': 'application/json, text/plain, */*', " +
                                "           'accept-encoding': 'gzip, deflate, br'," +
                                $"          'authorization': 'Token {authToken}', " +
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

                var response = await page.EvaluateFunctionAsync(jsFunc);
                var fetchResponse = new FetchResponse(response);

                return fetchResponse;
            }
            catch (Exception e)
            {
                LogRed(e: e);
                return new FetchResponse(null);
            }
        }


        public static async Task<PuppeteerResponse> RequestPostWithDownloadAsync(this IBrowser browser, int requestId, string url, string authToken, dynamic? data = null)
        {
            // "download" is a temporary file name where response content is saved
            string requestPath = $"{CD}{SC}puppeteer-temps{SC}{requestId}";
            string downloadPath = $"{requestPath}{SC}download";

            // Reacrete directory
            if (Directory.Exists(requestPath)) Directory.Delete(requestPath, true);
            Directory.CreateDirectory(requestPath);

            string? content = null;
            await using var page = await browser.NewPageAsync();
            try
            {
                await page.SetRequestInterceptionAsync(true);
                await page.Client.SendAsync("Page.setDownloadBehavior", new { behavior = "allow", requestPath });
                page.Request += (_, e) => ContinueRequest(e, data, HttpMethod.Post, "application/json", authToken);

                // It will always throw a NavigationException exception, but it will perform the request
                await page.GoToAsync(url);
            }
            catch (NavigationException)
            {
                content = await TryToExtractResponseAsync(downloadPath);
            }
            catch
            {
                return new PuppeteerResponse(null, false);
            }

            return new PuppeteerResponse(content, true);
        }


        private static async Task<string?> TryToExtractResponseAsync(string downloadPath)
        {
            // Wait 90 seconds for the response to download
            for (int i = 0; i <= 30; i++)
            {
                await Task.Delay(3000);
                if (File.Exists(downloadPath)) break;
                if (i == 30) return null;
            }

            try
            {
                string content = await File.ReadAllTextAsync(downloadPath);
                Directory.Delete(downloadPath, recursive: true);

                return content;
            }
            catch
            {
                return null;
            }
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
