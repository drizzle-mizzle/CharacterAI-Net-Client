using Newtonsoft.Json.Linq;

namespace CharacterAI.Models
{
    public class Character
    {
        public Character(dynamic? character = null)
        {
            if (character is null) return;

            bool noPic = string.IsNullOrEmpty($"{character.avatar_file_name}");

            Id = character.external_id;
            IsCopyable = character.copyable ?? true;
            Name = character.participant__name;
            Title = character.title;
            Greeting = character.greeting;
            Description = character.description;
            Author = character.user__username;
            AvatarUrlFull = noPic ? null : $"https://characterai.io/i/400/static/avatars/{character.avatar_file_name}";
            AvatarUrlMini = noPic ? null : $"https://characterai.io/i/80/static/avatars/{character.avatar_file_name}";
            IsPublic = character.visibility is null || character.visibility == "PUBLIC";
            Interactions = character.participant__num_interactions;
            ImageGenEnabled = character.img_gen_enabled;
            SearchScore = character.search_score;
            Tgt = character.participant__user__username;
        }

        public bool IsEmpty => Id is null;
        public bool IsCopyable { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Greeting { get; set; }
        public string? Tgt { get; set; }
        public bool IsPublic { get; set; }
        public ulong? Interactions { get; set; }
        public string? Author { get; set; }
        public bool? ImageGenEnabled { get; set; }
        public ulong? SearchScore { get; set; }
        public string? AvatarUrlFull { get; set; }
        public string? AvatarUrlMini { get; set; }

        public string Title
        {
            get => _title!;
            set => _title = value?.Trim(' ');
        }
        public string? Description
        {
            get => _description!;
            set => _description = value?.Trim(' ');
        }

        private string? _title;
        private string? _description;
    }
}
