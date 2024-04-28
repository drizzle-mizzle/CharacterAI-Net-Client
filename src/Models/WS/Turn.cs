namespace CharacterAiNet.Models.WS
{
    internal class Turn
    {
        public string primary_candidate_id { get; set; }
        public Author author { get; set; }
        public TurnKey turn_key { get; set; }
        public IEnumerable<Candidates> candidates { get; set; }

        public DateTime? create_time { get; set; }
        public DateTime? last_update_time { get; set; }
        public string state { get; set; }
    }

    internal class Author
    {
        public string author_id { get; set; }
        public bool is_human { get; set; }
        public string name { get; set; }
    }

    internal class TurnKey
    {
        public string chat_id { get; set; }
        public string turn_id { get; set; }
    }

    internal class Candidates
    {
        public string candidate_id { get; set; }
        public string raw_content { get; set; }
        public bool is_final { get; set; }
        public DateTime? create_time { get; set; }
    }

}
