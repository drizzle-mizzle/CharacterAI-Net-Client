namespace CharacterAi.Client.Models.Common
{
    public class CaiChat
    {
        public string? character_avatar_uri { get; set; }
        public string character_id { get; set; }
        public string character_name { get; set; }
        // public List<CharacterTranslation>? character_translations { get; set; }
        public string character_visibility { get; set; }
        public Guid chat_id { get; set; }
        public DateTime? create_time { get; set; }
        public int creator_id { get; set; }
        public string state { get; set; }
        public string type { get; set; }
        public string visibility { get; set; }
    }

    public class CaiChatShort
    {
        public string? character_id { get; set; }
        public Guid chat_id { get; set; }
        public string? creator_id { get; set; }
        public string? type { get; set; }
        public string? visibility { get; set; }
    }

    // public class CharacterTranslation
    // {
    //     public CharacterTranslationName name { get; set; }
    // }
    //
    // public class CharacterTranslationName
    // {
    //     public string? ja_JP { get; set; }
    //     public string? ko { get; set; }
    //     public string? ru { get; set; }
    //     public string? zh_CN { get; set; }
    // }
}
