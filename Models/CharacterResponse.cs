using Newtonsoft.Json;
using CharacterAI.Services;
using Newtonsoft.Json.Linq;

namespace CharacterAI.Models
{
    public class CharacterResponse : CommonService
    {
        public Reply? Response { get; }
        public string? LastUserMsgUuId { get; }
        public string? ErrorReason { get; }
        public bool IsSuccessful => ErrorReason is null;

        // response is PuppeteerResponse or FetchReponse
        public CharacterResponse(dynamic response)
        {
            dynamic responseParsed = ParseCharacterResponse(response);

            if (responseParsed is string)
                ErrorReason = responseParsed;
            else
            {
                Response = GetCharacterReply(responseParsed.replies);
                LastUserMsgUuId = responseParsed.last_user_msg_uuid;
            }
        }

        private static dynamic ParseCharacterResponse(dynamic response)
        {
            if (!response.IsSuccessful)
            {
                Failure(response?.Status, response?.Content);
                return $"Failed to fetch response";
            }
            
            string content = response.Content;
            try
            {
                var chunks = content.Split("\n").ToList();
                var parsedChunks = chunks.ConvertAll(JsonConvert.DeserializeObject<dynamic>);
                parsedChunks.Reverse(); // Only last chunks contains "abort" or "final_chunk", so it will be a bit faster to find

                var finalChunk = parsedChunks.FirstOrDefault(c => c?.is_final_chunk == true, null);
                if (finalChunk is not null) return finalChunk;

                var abortMsg = $"{WARN_SIGN} Seems like character response was filtered!";
                var lastMessageChunk = parsedChunks.Find(c => c?.replies is not null);
                if (lastMessageChunk is null) return abortMsg;

                // Not sure if it actually works, as it really hard to test it, and there's not always any "last words",
                // sometimes response is being filtered on the very beginning.
                var lastReply = GetCharacterReply((JArray)lastMessageChunk.replies);
                var lastWords = lastReply is null ? "" : $" It was cut off on:\n{lastReply.Text}";

                return abortMsg + lastWords;
            }
            catch (Exception e)
            {
                string eMsg = $"{WARN_SIGN} Something went wrong...\n";
                Failure($"{response?.Status ?? "?"} | {eMsg}", content?.Trim()?.Replace("\n\n", ""), e: e);

                return eMsg;
            }
        }

        private static Reply? GetCharacterReply(JArray? jReplies)
        {
            if (string.IsNullOrWhiteSpace(jReplies?.ToString()) || !jReplies.Any()) return null;

            var reply = jReplies.First() as dynamic;
            return new Reply()
            {
                UuId = (string)(reply?.uuid ?? ""),
                Text = reply?.text ?? "",
                ImageRelPath = reply?.image_rel_path
            };
        }
    }
}

// {
//   "replies":
//   [
//     {
//       "text": "some text",
//       "id": some_reply_id
//     },
//     {
//       "text": "some text 2",
//       "id": some_reply_id2
//     }
//   ],
//   "is_final_chunk": true,
//   "last_user_msg_id": 4485105154
//   "src_char": // bloat?
//   {
//     "participant": {"name": "Char Name"},
//     "avatar_file_name": "Avatar Path"
//   },
// }