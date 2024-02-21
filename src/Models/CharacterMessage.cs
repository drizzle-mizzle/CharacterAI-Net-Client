namespace CharacterAiNetApiWrapper.Models
{
    public class CharacterMessage
    {
        public required string UuId { get; set; }
        public required string Text { get; set; }
        public string? ImageRelPath { get; set; }
        public bool HasImage => ImageRelPath != null;
    }
}
