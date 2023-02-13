using Newtonsoft.Json;
using CharacterAI.Services;

namespace CharacterAI.Models
{
    public class CharacterResponse : CommonService
    {
        public List<Reply>? Replies { get => replies; }
        public string? LastUserMsgId { get => lastUserMsgId; }
        public string? ErrorReason { get => errorReason; }
        public bool IsSuccessful { get => errorReason is null; }

        private readonly List<Reply>? replies = null;
        private readonly string? lastUserMsgId = null;
        private readonly string? errorReason = null;

        public CharacterResponse(HttpResponseMessage httpResponse)
        {
            dynamic response = GetCharacterResponse(httpResponse);

            if (response is string)
                errorReason = response;
            else
            {
                replies = GetCharacterReplies(response);
                lastUserMsgId = response.last_user_msg_id;
            }
        }

        private static async Task<dynamic> GetCharacterResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string eMsg = "⚠️ Failed to send message!";
                Failure(eMsg, response: response);

                return eMsg;
            }

            try
            {
                string[] chunks = (await response.Content.ReadAsStringAsync()).Split("\n");
                string finalChunk = chunks.First(c => JsonConvert.DeserializeObject<dynamic>(c)!.is_final_chunk == true);

                return JsonConvert.DeserializeObject<dynamic>(finalChunk)!;
            }
            catch (Exception e)
            {
                // gotta extend
                string eMsg = "⚠️ Message has been sent successfully, but something went wrong... (probably, character reply was filtered and deleted, try again)";
                Failure($"{eMsg}\n {e}");

                return eMsg;
            }
        }

        private static List<Reply> GetCharacterReplies(dynamic finalChunk)
        {
            var replies = new List<Reply>();

            foreach (dynamic dReply in finalChunk.replies)
            {
                replies.Add(new Reply
                {
                    Id = dReply.id,
                    Text = dReply.text,
                    ImageRelPath = dReply.image_rel_path,
                    HasImage = string.IsNullOrEmpty(dReply.image_rel_path)
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