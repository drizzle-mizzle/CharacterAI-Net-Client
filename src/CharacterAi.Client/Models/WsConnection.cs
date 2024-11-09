using CharacterAi.Client.Models.WS;
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
    }
}
