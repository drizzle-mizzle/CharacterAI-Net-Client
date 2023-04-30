using CharacterAI.Models;
using PuppeteerSharp;
using System.Dynamic;

namespace CharacterAI.Services
{
    public partial class CommonService
    {

        internal static string CD = Directory.GetCurrentDirectory();
        internal static char slash = Path.DirectorySeparatorChar;

        internal static readonly string WARN_SIGN = "⚠";
        internal static string DEFAULT_CHROME_PATH = $"{CD}{slash}puppeteer-chrome";

        /// <returns>
        /// Chrome executable path.
        /// </returns>
        internal static async Task<string> TryToDownloadBrowser(string? customChromeDir)
        {
            string path = string.IsNullOrWhiteSpace(customChromeDir) ? DEFAULT_CHROME_PATH : customChromeDir;
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

        /// <summary>
        /// Log and return true
        /// </summary>
        internal static bool Success(string logText = "")
        {
            Log(logText + "\n", ConsoleColor.Green);

            return true;
        }

        /// <summary>
        /// Log and return false
        /// </summary>
        internal static bool Failure(string? logText = null, string? response = null, Exception? e = null)
        {
            if (logText is not null)
                Log($"{logText}\n", ConsoleColor.Red);

            if (response is not null)
                Log($"Response:\n{response}\n", ConsoleColor.Red);

            if (e is not null)
                Log($"Exception details:\n{e}\n", ConsoleColor.Red);

            return false;
        }

        internal static void Log(string text, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }

        internal static void WriteToLogFile(string text)
        {
            try
            {
                string fileName = $"{CD}{slash}log.txt";

                if (!File.Exists(fileName))
                    File.Create(fileName);

                File.AppendAllText(fileName, text + "\n\n------------------------\n\n");
            }
            catch { Failure("Woops."); }
        }

        internal static dynamic BasicCallContent(Character charInfo, string msg, string? imgPath, string historyId)
        {
            dynamic content = new ExpandoObject();

            if (!string.IsNullOrEmpty(imgPath))
            {
                content.image_description_type = "AUTO_IMAGE_CAPTIONING";
                content.image_origin_type = "UPLOADED";
                content.image_rel_path = imgPath;
            }

            content.character_external_id = charInfo.Id!;
            content.chunks_to_pad = 8;
            //content.enable_tti = true; // have no idea what is it
            content.give_room_introductions = true;
            content.history_external_id = historyId;
            content.is_proactive = false; // have no idea what is it
            content.num_candidates = 1; // have no idea what is it
            content.ranking_method = "random";
            content.staging = false; // have no idea what is it
            content.stream_every_n_steps = 16;
            content.text = msg;
            content.tgt = charInfo.Tgt!;
            content.voice_enabled = false;

            return content;
        }
    }
}
