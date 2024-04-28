namespace CharacterAiNet.Models.DTO
{
    public class Character
    {
        public string? avatar_file_name { get; set; }
        public string? base_img_prompt { get; set; }
        public bool comments_enabled { get; set; }
        public bool copyable { get; set; }
        public string? default_voice_id { get; set; }
        public string? description { get; set; }
        public string? external_id { get; set; }
        public string? greeting { get; set; }
        public string? identifier { get; set; }
        public bool img_gen_enabled { get; set; }
        public string? img_prompt_regex { get; set; }
        public string? name { get; set; }
        public string? participant__name { get; set; }
        public ulong participant__num_interactions { get; set; }
        public string? participant__user__username { get; set; }
        public dynamic songs { get; set; }
        public dynamic starter_prompts { get; set; }
        public bool strip_img_prompt_from_msg { get; set; }
        public string? title { get; set; }
        public int upvotes { get; set; }
        public string? usage { get; set; }
        public string? user__username { get; set; }
        public string? visibility { get; set; }
        public string? voice_id { get; set; }
    }
}
