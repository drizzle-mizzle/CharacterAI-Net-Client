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

        public CharacterResponse(string? response)
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

        private static dynamic ParseCharacterResponse(string? response)
        {
            if (response is null) return "{WARN_SIGN}️ Something went wrong.";
            
            try
            {
                string[] chunks = response.Split("\n");
                string finalChunk = chunks.First(c => JsonConvert.DeserializeObject<dynamic>(c)!.is_final_chunk == true);
                
                return JsonConvert.DeserializeObject<dynamic>(finalChunk)!;
            }
            catch (Exception e)
            {
                string eMsg = "{WARN_SIGN}️ Message has been sent successfully, but something went wrong...\n(probably, CharacterAI servers are down, or character reply was filtered and deleted, try again)";
                Failure(eMsg, e: e);

                return eMsg;
            }
        }

        private static List<Reply> GetCharacterReplies(JArray jReplies)
        {
            var replies = new List<Reply>();

            foreach (dynamic reply in jReplies)
            {
                replies.Add(new Reply
                {
                    Id = reply.id,
                    Text = reply?.text,
                    ImageRelPath = reply?.image_rel_path,
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