using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static SharedUtils.Common;

namespace CharacterAI.Models
{
    public class CharacterResponse
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
                Response = GetCharacterResponse(responseParsed.replies);
                LastUserMsgUuId = responseParsed.last_user_msg_uuid;
            }
        }

        
        /// <returns>string with message if error or filtered; JToken if ok</returns>
        private static dynamic ParseCharacterResponse(dynamic response)
        {
            if (!response.IsSuccessful)
            {
                LogRed(response.Content);
                return $"Something went wrong";
            }
            
            string content = response.Content;
            try
            { 
                var chunks = content.Split("\n").ToList();
                var parsedChunks = chunks.ConvertAll(JsonConvert.DeserializeObject<dynamic>);
                
                var abortChunk = parsedChunks.FirstOrDefault(pc => pc?.abort == true || pc?.abort == "true");
                if (abortChunk is not null)
                {
                    string abortMsg = "Seems like character response was filtered!";

                    var lastMessageChunk = parsedChunks.FirstOrDefault(c => c?.replies is not null);
                    if (lastMessageChunk is not null && GetCharacterResponse((JArray)lastMessageChunk.replies) is Reply lastReply)
                        abortMsg += $" It was cut off on:\n{lastReply.Text}";
                    else if (abortChunk.error is string er)
                        abortMsg += $" ({er})";
                    
                    return abortMsg;
                }

                var finalChunk = parsedChunks.FirstOrDefault(c => c?.is_final_chunk == true, null);
                if (finalChunk is not null)
                    return finalChunk;
            }
            catch (Exception e)
            {
                LogRed(content?.Trim().Replace("\n\n", ""), e: e);
            }

            return $"Something went wrong...";
        }

        private static Reply? GetCharacterResponse(JArray? jReplies)
        {
            if (jReplies is null || !jReplies.Any())
                return null;

            var reply = jReplies.First() as dynamic;
            return new Reply()
            {
                UuId = $"{reply?.uuid ?? string.Empty}",
                Text = $"{reply?.text ?? string.Empty}",
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