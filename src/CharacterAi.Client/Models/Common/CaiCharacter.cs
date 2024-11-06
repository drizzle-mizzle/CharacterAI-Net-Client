namespace CharacterAi.Client.Models.Common
{
    public class CaiCharacter
    {
        public string? external_id { get; set; }
        public string? participant__name { get; set; }

        public string? title { get; set; }
        public string? description { get; set; }
        public string? definition { get; set; }
        public string? greeting { get; set; }
        public string? avatar_file_name { get; set; }

        public int? participant__num_interactions { get; set; }
        public bool img_gen_enabled { get; set; } = false;

        public int priority { get; set; }
        public string? user__username { get; set; }
        public string? visibility { get; set; }
    }
}
