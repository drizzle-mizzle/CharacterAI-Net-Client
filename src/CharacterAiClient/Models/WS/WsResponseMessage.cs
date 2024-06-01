using CharacterAi.Models.DTO;

namespace CharacterAi.Models.WS
{
    public class WsResponseMessage
    {
        public string command { get; set; }
        public Guid request_id { get; set; }
        public Turn? turn { get; set; }

        public ChatInfo? chat_info { get; set; }
        public Chat? chat { get; set; }
    }

    public class ChatInfo
    {
        public string type { get; set; }

    }
}
