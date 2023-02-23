using Newtonsoft.Json;
using System.Text.RegularExpressions;
using CharacterAI.Services;
using System.Runtime.Remoting;
using Newtonsoft.Json.Linq;

namespace CharacterAI.Models
{
    public class CharacterResponse : CommonService
    {
        public List<Reply> Replies { get; } = new();
        public ulong LastUserMsgId { get; }
        public string? ErrorReason { get; }
        public bool IsSuccessful { get => ErrorReason is null; }

        public CharacterResponse(HttpResponseMessage httpResponse)
        {
            dynamic responseParsed = ParseCharacterResponse(httpResponse).Result;

            if (responseParsed is string)
                ErrorReason = responseParsed;
            else
            {
                Replies = GetCharacterReplies(responseParsed.replies);
                LastUserMsgId = responseParsed.last_user_msg_id;
            }
        }

        private static async Task<dynamic> ParseCharacterResponse(HttpResponseMessage httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                string eMsg = "⚠️ Failed to send message!";
                Failure(eMsg, response: httpResponse);

                return eMsg;
            }

            try
            {
                string[] chunks = (await httpResponse.Content.ReadAsStringAsync()).Split("\n");
                string finalChunk = chunks.First((string c) => ((dynamic)JsonConvert.DeserializeObject<object>(c)).is_final_chunk == true);
                return JsonConvert.DeserializeObject<object>(finalChunk);
            }
            catch (Exception e)
            {
                try
                {
                    // if the message was filtered, use the last coherent thought from the stream
                    RegexOptions options = RegexOptions.Multiline | RegexOptions.RightToLeft;
                    string pattern = @"\{.{0,}""replies"":[ ]{0,}\[.+\],.+\}";
                    // this is a crude way of doing so, but it gets the job done
                    var match = Regex.Matches(e.ToString(), pattern, options);
                    string finalChunk = match[0].ToString();
                    return JsonConvert.DeserializeObject<object>(finalChunk);
                }
                catch
                {
                    // gotta extend
                    string eMsg = "⚠️ Message has been sent successfully, but something went wrong... (probably, character reply was filtered and deleted, try again)";
                    Failure($"{eMsg}\n {e}", response: httpResponse);
                    return eMsg2;
                }
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
