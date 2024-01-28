namespace CharacterAI.Models
{
    public class Reply
    {
        //public ulong Id { get; set; } // not needed anymore ig
        public required string UuId { get; set; }
        public required string Text { get; set; }
        public string? ImageRelPath { get; set; }
        public bool HasImage => ImageRelPath != null;
    }
}
