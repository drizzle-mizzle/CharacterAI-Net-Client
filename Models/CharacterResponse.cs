#region Assembly CharacterAI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CharacterAI.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CharacterAI.Models
{
    public class CharacterResponse : CommonService
    {
        public List<Reply> Replies { get; } = new List<Reply>();


        public ulong LastUserMsgId { get; }

        public string? ErrorReason { get; }

        public bool IsSuccessful => ErrorReason == null;

        public CharacterResponse(HttpResponseMessage httpResponse)
        {
            dynamic result = ParseCharacterResponse(httpResponse).Result;
            if (result is string)
            {
                ErrorReason = result;
                return;
            }

            Replies = GetCharacterReplies(result.replies);
            LastUserMsgId = result.last_user_msg_id;
        }

        private static async Task<dynamic> ParseCharacterResponse(HttpResponseMessage httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                string eMsg = "⚠\ufe0f Failed to send message!";
                CommonService.Failure(eMsg, httpResponse);
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
                    RegexOptions options = RegexOptions.Multiline | RegexOptions.RightToLeft;
                    string pattern = @"\{.{0,}""replies"":[ ]{0,}\[.+\],.+\}";
                    var match = Regex.Matches(e.ToString(), pattern, options);
                    string finalChunk = match[0].ToString();
                    return JsonConvert.DeserializeObject<object>(finalChunk);
                }
                catch
                {
                    string eMsg2 = "⚠\ufe0f Message has been sent successfully, but something went wrong... (probably, character reply was filtered and deleted, try again)";
                    eMsg2 += "\n " + e;
                    return eMsg2;
                }
            }
        }

        private static List<Reply> GetCharacterReplies(JArray jReplies)
        {
            List<Reply> list = new List<Reply>();
            foreach (dynamic jReply in jReplies)
            {
                list.Add(new Reply
                {
                    Id = jReply.id,
                    Text = jReply?.text,
                    ImageRelPath = jReply?.image_rel_path
                });
            }

            return list;
        }
    }
}
#if false // Decompilation log
'177' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.0\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.0\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.0\ref\net7.0\System.Net.Http.dll'
------------------
Resolve: 'System.Linq.Expressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq.Expressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.0\ref\net7.0\System.Linq.Expressions.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.0\ref\net7.0\System.Console.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Found single assembly: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\t y\.nuget\packages\newtonsoft.json\13.0.2\lib\net6.0\Newtonsoft.Json.dll'
------------------
Resolve: 'Microsoft.CSharp, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'Microsoft.CSharp, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.0\ref\net7.0\Microsoft.CSharp.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.0\ref\net7.0\System.Linq.dll'
#endif
