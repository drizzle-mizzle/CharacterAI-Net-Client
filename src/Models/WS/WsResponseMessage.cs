namespace CharacterAiNet.Models.WS
{
    internal class WsResponseMessage
    {
        public string command { get; set; }
        public string request_id { get; set; }
        public Turn turn { get; set; }

        public ChatInfo chat_info { get; set; }

    }

    internal class ChatInfo
    {
        public string type { get; set; }

    }
}
