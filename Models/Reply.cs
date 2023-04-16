namespace CharacterAI.Models
{
    public class Reply
    {
        public ulong? Id { get; set; }
        public string? Text { get; set; }
        public string? ImageRelPath { get; set; }
        public bool HasImage => ImageRelPath != null;
    }
}
