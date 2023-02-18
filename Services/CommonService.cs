using CharacterAI.Models;
using System.Dynamic;

namespace CharacterAI.Services
{
    public partial class CommonService
    {

        // Log and return true
        internal static bool Success(string logText = "")
        {
            Log(logText + "\n", ConsoleColor.Green);

            return true;
        }

        // Log and return false
        internal static bool Failure(string logText = "", HttpResponseMessage? response = null)
        {
            if (logText != "")
                Log(logText + "\n", ConsoleColor.Red);

            if (response is not null)
            {
                var request = response.RequestMessage!;
                var url = request.RequestUri;
                var responseContent = response.Content?.ReadAsStringAsync().Result;
                var requestContent = request.Content?.ReadAsStringAsync().Result;

                Log($"Error!\n Request failed! ({url})\n", ConsoleColor.Red);
                Log(color: ConsoleColor.Red,
                    text: $" Response: {response.ReasonPhrase}\n" +
                          (requestContent is null ? "" : $" Request Content: {requestContent}\n") +
                          (requestContent is null ? "" : $" Response Content: {responseContent}\n")
                    );
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
            //content.enable_tti = true; have no idea what is it
            content.history_external_id = historyId;
            content.is_proactive = false;
            content.ranking_method = "random";
            content.staging = false;
            content.stream_every_n_steps = 16;
            content.text = msg;
            content.tgt = charInfo.Tgt!;
            content.voice_enabled = false;

            return content;
        }
    }
}
