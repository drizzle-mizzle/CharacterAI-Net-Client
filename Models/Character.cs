namespace CharacterAI.Models
{
    public class Character
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Greeting { get; set; }
        public string? Tgt { get; set; }
        public string? AvatarUrl { get; set; }
        public string? HistoryExternalId { get; set; }
        public bool? IsPublic { get; set; }
        public ulong? Interactions { get; set; }
        public string? Author { get; set; }
        public bool? ImageGenEnabled { get; set; }
        public ulong? SearchScore { get; set; }
        public string Title
        {
            get => title!;
            set => title = value.Trim(' ');
        }
        public string Description
        {
            get => description!;
            set => description = value.Trim(' ');
        }

        private string? title;
        private string? description;
    }
}
