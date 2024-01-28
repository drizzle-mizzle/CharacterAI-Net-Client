namespace CharacterAI.Models
{
    internal class History
    {
        public string? Id { get; }
        public DateTime? CreatedAt { get; }
        public DateTime? LastInteraction { get; }
        //public List<Message>? Messages { get; }

        public History(dynamic history)
        {
            Id = history.external_id;
            CreatedAt = DateTime.Parse(history.created);
            LastInteraction = DateTime.Parse(history.last_interaction);
        }
    }
}
