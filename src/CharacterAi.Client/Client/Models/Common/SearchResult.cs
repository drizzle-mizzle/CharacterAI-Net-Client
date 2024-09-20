namespace CharacterAi.Client.Models.Common
{
    public class SearchResult
    {
        public string? avatar_file_name { get; set; }
        public string? external_id { get; set; }
        public string? greeting { get; set; }
        public string? participant__name { get; set; }
        public int participant__num_interactions { get; set; }
        public int priority { get; set; }
        public int score { get; set; }
        public string? title { get; set; }
        public string? user__username { get; set; }
        public string? visibility { get; set; }
    }
}
