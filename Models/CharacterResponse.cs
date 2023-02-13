using Newtonsoft.Json;
using CharacterAI.Services;

namespace CharacterAI.Models
{
    public class CharacterResponse : CommonService
    {
        public List<Reply>? Replies { get; }
        public string? LastUserMsgId { get; }
        public string? ErrorReason { get; }
        public bool IsSuccessful { get; }

        public CharacterResponse(HttpResponseMessage httpResponse)
        {
            dynamic response = GetCharacterResponse(httpResponse);

            if (response is string)
            {
                ErrorReason = response;
                IsSuccessful = false;
            }
            else
            {
                Replies = GetCharacterReplies(response);
                LastUserMsgId = response.last_user_msg_id;
                IsSuccessful = true;
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

    internal class Reply
    {
        public string? Id { get; set; }
        public string? Text { get; set; }
        public string? ImageRelPath { get; set; }
        public bool HasImage { get; set; }
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