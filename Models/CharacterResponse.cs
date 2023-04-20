using Newtonsoft.Json;
using CharacterAI.Services;
using Newtonsoft.Json.Linq;

namespace CharacterAI.Models
{
    public class CharacterResponse : CommonService
    {
        public List<Reply> Replies { get; } = new();
        public ulong LastUserMsgId { get; }
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
                Replies = GetCharacterReplies(responseParsed.replies);
                LastUserMsgId = responseParsed.last_user_msg_id;
            }
        }

        private static dynamic ParseCharacterResponse(dynamic response)
        {
            if (!response.IsSuccessful)
            {
                Failure(response?.Status, response?.Content);
                return $"{WARN_SIGN} Failed to fetch response.\n(probably, CharacterAI servers are down, try again later)";
            }
            
            string content = response.Content;
            try
            {
                var chunks = content.Split("\n").ToList();
                var parsedChunks = chunks.ConvertAll(JsonConvert.DeserializeObject<dynamic>);
                parsedChunks.Reverse(); // Only last chunks contains "abort" or "final_chunk", so it will be a bit faster to find

                // Check if message was filtered.
                // Aborted message last chunk will look like: { "abort": true, "error": "No eligible candidates", "last_user_msg_id": "...", "last_user_msg_uuid": "..." }
                if (parsedChunks.All(c => c?.abort is null))
                    return parsedChunks.First(c => c?.is_final_chunk == true)!;

                // Return last normal chunk, before filter aborted message stream
                var eMsg = $"{WARN_SIGN} Character response was filtered.";
                var lastMessageChunk = parsedChunks.FirstOrDefault(c => c?.replies is not null);
                if (lastMessageChunk is null) return eMsg;
                
                var lastReply = GetCharacterReplies((JArray)lastMessageChunk.replies)?.FirstOrDefault();
                var lastWords = lastReply is null ? "" : $" It was cut off on:\n{lastReply.Text}";

                return eMsg + lastWords;
            }
            catch (Exception e)
            {
                string eMsg = $"{WARN_SIGN} Something went wrong...\n";
                Failure($"{response?.Status ?? "?"} | {eMsg}", content?.Trim()?.Replace("\n\n", ""), e: e);

                return eMsg;
            }
        }

        private static List<Reply> GetCharacterReplies(JArray? jReplies)
        {
            var replies = new List<Reply>();
            if (string.IsNullOrWhiteSpace(jReplies?.ToString())) return replies;

            foreach (dynamic reply in jReplies)
            {
                var replyId = reply?.id;
                if (replyId is null) continue;

                replies.Add(new Reply
                {
                    Id = replyId,
                    Text = reply?.text,
                    ImageRelPath = reply?.image_rel_path
                });
            }

            return replies;
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