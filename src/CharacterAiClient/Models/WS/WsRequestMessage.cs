using CharacterAi.Models.Common;

namespace CharacterAi.Models.WS
{
    public class WsRequestMessage
    {
        public string command { get; set; }
        public IPayload payload { get; set; }
    }

    public class CallPayload : IPayload
    {
        public string character_id { get; set; }
        public int num_candidates { get; set; }
        public string selected_language { get; set; }
        public bool tts_enabled { get; set; }
        public string user_name { get; set; }
        public Turn turn { get; set; }
        public PreviousAnnotations previous_annotations { get; set; }
    }

    public class NewChatPayload : IPayload
    {
        public CaiChatShort chat { get; set; }
        public bool with_greeting { get; set; }
    }

    public interface IPayload { }


    public class PreviousAnnotations
    {
        public int bad_memory { get; set; } = 0;
        public int boring { get; set; } = 0;
        public int ends_chat_early { get; set; } = 0;
        public int funny { get; set; } = 0;
        public int helpful { get; set; } = 0;
        public int inaccurate { get; set; } = 0;
        public int interesting { get; set; } = 0;
        public int @long { get; set; } = 0;
        public int not_bad_memory { get; set; } = 0;
        public int not_boring { get; set; } = 0;
        public int not_ends_chat_early { get; set; } = 0;
        public int not_funny { get; set; } = 0;
        public int not_helpful { get; set; } = 0;
        public int not_inaccurate { get; set; } = 0;
        public int not_interesting { get; set; } = 0;
        public int not_long { get; set; } = 0;
        public int not_out_of_character { get; set; } = 0;
        public int not_repetitive { get; set; } = 0;
        public int not_short { get; set; } = 0;
        public int out_of_character { get; set; } = 0;
        public int repetitive { get; set; } = 0;
        public int @short { get; set; } = 0;
    }

}
