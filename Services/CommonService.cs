using CharacterAI.Models;
using PuppeteerSharp;
using System.Dynamic;

namespace CharacterAI.Services
{
    /// <summary>
    /// Some underhood logic
    /// </summary>
    public class CommonService
    {

        internal static string CD = Directory.GetCurrentDirectory();
        internal static char SC = Path.DirectorySeparatorChar;
        internal static readonly string WARN_SIGN = "⚠";

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
                string fileName = $"{CD}{SC}log.txt";

                if (!File.Exists(fileName))
                    File.Create(fileName);

                File.AppendAllText(fileName, text + "\n\n------------------------\n\n");
            }
            catch { Failure("Woops."); }
        }

        /// <summary>
        /// Here is listed the whole list of all known payload parameters.
        /// Some of these are useless, some seems to be not really used yet in actual API, some do simply have unknown purpose,
        /// thus they are either commented or set with default value taken from cai site.
        /// </summary>
        internal static dynamic BasicCallContent(string characterId, string characterTgt, string msg, string? imgPath, string historyId)
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

        internal static void ClearTemps()
        {
            try { Directory.Delete($"{CD}{SC}puppeteer-temps", true); } catch { }
        }
    }
}
