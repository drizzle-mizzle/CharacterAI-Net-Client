#region Assembly CharacterAI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using System;
using System.Dynamic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using CharacterAI.Models;

namespace CharacterAI.Services
{
    public class CommonService
    {
        internal static bool Success(string logText = "")
        {
            Log(logText + "\n", ConsoleColor.Green);
            return true;
        }

        internal static bool Failure(string logText = "", HttpResponseMessage? response = null)
        {
            if (logText != "")
            {
                Log(logText + "\n", ConsoleColor.Red);
            }

            if (response != null)
            {
                HttpRequestMessage requestMessage = response!.RequestMessage;
                Uri requestUri = requestMessage.RequestUri;
                string text = response!.Content?.ReadAsStringAsync().Result;
                string text2 = requestMessage.Content?.ReadAsStringAsync().Result;
                DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
                defaultInterpolatedStringHandler.AppendLiteral("Error!\n Request failed! (");
                defaultInterpolatedStringHandler.AppendFormatted(requestUri);
                defaultInterpolatedStringHandler.AppendLiteral(")\n");
                Log(defaultInterpolatedStringHandler.ToStringAndClear(), ConsoleColor.Red);
                Log(" Response: " + response!.ReasonPhrase + "\n" + ((text2 == null) ? "" : (" Request Content: " + text2 + "\n")) + ((text2 == null) ? "" : (" Response Content: " + text + "\n")), ConsoleColor.Red);
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
            dynamic val = new ExpandoObject();
            if (!string.IsNullOrEmpty(imgPath))
            {
                val.image_description_type = "AUTO_IMAGE_CAPTIONING";
                val.image_origin_type = "UPLOADED";
                val.image_rel_path = imgPath;
            }

            val.character_external_id = charInfo.Id;
            val.chunks_to_pad = 8;
            val.history_external_id = historyId;
            val.is_proactive = false;
            val.ranking_method = "random";
            val.staging = false;
            val.stream_every_n_steps = 16;
            val.text = msg;
            val.tgt = charInfo.Tgt;
            val.voice_enabled = false;
            return val;
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
