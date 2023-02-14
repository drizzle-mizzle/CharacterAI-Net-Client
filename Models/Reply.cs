namespace CharacterAI.Models
{
    public class Reply
    {
        public string? Id { get; set; }
        public string? Text { get; set; }
        public string? ImageRelPath { get; set; }
        public bool HasImage { get => ImageRelPath != null; }
    }
}
