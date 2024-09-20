using CharacterAi.Models.WS;
using Websocket.Client;

namespace CharacterAi.Models
{
    public class WsConnection
    {
        public WebsocketClient Client { get; set; }

        /// <summary>
        /// request_id : message
        /// </summary>
        public WsResponseMessage? LastMessage { get; set; }

        public void Send(string message)
            => Client.Send(message);
    }
}
