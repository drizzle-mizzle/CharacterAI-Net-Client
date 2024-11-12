using CharacterAi.Client.Models.WS;
using Newtonsoft.Json;
using Websocket.Client;

namespace CharacterAi.Client.Models
{
    public class WsConnection
    {
        public WebsocketClient Client { get; set; }

        /// <summary>
        /// request_id : message
        /// </summary>
        public List<WsResponseMessage> Messages { get; } = [];

        public void Send(string message)
            => Client.Send(message);

        public string GetAllMessagesFormatted() => string.Join("; ", Messages.Select(JsonConvert.SerializeObject));

    }
}
