using CharacterAI.Models;
using System.Dynamic;

namespace CharacterAI.Services
{
    public partial class CommonService
    {

        internal static string CD = Directory.GetCurrentDirectory();
        internal static char SC = Path.DirectorySeparatorChar;

        internal static string CHROME_PATH = $"{CD}{SC}puppeteer-chrome";

        // Log and return true
        internal static bool Success(string logText = "")
        {
            Log(logText + "\n", ConsoleColor.Green);

            return true;
        }

        // Log and return false
        internal static bool Failure(string logText = "", PuppeteerResponse? response = null)
        {
            if (logText != "")
                Log(logText + "\n", ConsoleColor.Red);

            if (response is not null)
            {
                //var request = response.OriginalResponse.Request!;
                //var rPayload = response.OriginalRequestPayload;

                //Log(color: ConsoleColor.Red,
                //    text: $"Error!\n Request failed! ({request.Url})\n  " +
                //          $"{string.Join("\n  ", rPayload.Headers)}\n" +
                //          $" Request Content: {(rPayload.PostData == null ? "empty" : $"\n  {rPayload.PostData}" )}\n");

                //Log(color: ConsoleColor.Magenta,
                //    text: $"\n Response:\n  {string.Join("\n  ", response.OriginalResponse.Headers)}\n" +
                //          $" Response Content:\n  {response.Content}\n");
            }

            return false;
        }

        internal static void Log(string text, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
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
