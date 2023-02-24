#region Assembly CharacterAI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

namespace CharacterAI.Models
{
    public class Character
    {
        private string? _title;

        private string? _description;

        public bool IsEmpty => Id == null;

        public bool IsCopyable { get; set; }

        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? Greeting { get; set; }

        public string? Tgt { get; set; }

        public bool? IsPublic { get; set; }

        public ulong? Interactions { get; set; }

        public string? Author { get; set; }

        public bool? ImageGenEnabled { get; set; }

        public ulong? SearchScore { get; set; }

        public string? AvatarUrlFull { get; set; }

        public string? AvatarUrlMini { get; set; }

        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value.Trim(' ');
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value.Trim(' ');
            }
        }

        public Character(dynamic? character = null)
        {
            if ((object)character != null)
            {
                Id = character.external_id;
                IsCopyable = character.copyable;
                Name = character.participant__name;
                Title = character.title;
                Greeting = character.greeting;
                Description = character.description;
                Author = character.user__username;
                AvatarUrlFull = $"https://characterai.io/i/400/static/avatars/{(object?)character.avatar_file_name}";
                AvatarUrlMini = $"https://characterai.io/i/80/static/avatars/{(object?)character.avatar_file_name}";
                IsPublic = character.visibility == "PUBLIC";
                Interactions = character.participant__num_interactions;
                ImageGenEnabled = (bool)character.img_gen_enabled;
                SearchScore = character.search_score;
                Tgt = character.participant__user__username;
            }
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
