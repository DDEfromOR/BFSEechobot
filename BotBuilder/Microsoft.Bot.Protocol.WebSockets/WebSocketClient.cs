using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Protocol.Payloads;
using Microsoft.Bot.Protocol.PayloadTransport;
using Microsoft.Bot.Protocol.Utilities;

namespace Microsoft.Bot.Protocol.WebSockets
{
    public class WebSocketClient
    {
        private readonly string _url;
        private readonly RequestHandler _requestHandler;
        private readonly IPayloadSender _sender;
        private readonly IPayloadReceiver _receiver;
        private readonly RequestManager _requestManager;
        private readonly ProtocolAdapter _protocolAdapter;
        private readonly bool _autoReconnect;

        public WebSocketClient(string url, RequestHandler requestHandler = null, bool autoReconnect = true)
        {
            _url = url;
            _requestHandler = requestHandler;
            _autoReconnect = autoReconnect;
            
            _requestManager = new RequestManager();

            _sender = new PayloadSender();
            _sender.Disconnected += OnConnectionDisconnected;
            _receiver = new PayloadReceiver();
            _receiver.Disconnected += OnConnectionDisconnected;

            _protocolAdapter = new ProtocolAdapter(_requestHandler, _requestManager, _sender, _receiver);
        }

        public async Task ConnectAsync()
        {
            var clientWebSocket = new ClientWebSocket();
            await clientWebSocket.ConnectAsync(new Uri(_url), CancellationToken.None);
            var socketTransport = new WebSocketTransport(clientWebSocket);

            _sender.Connect(socketTransport);
            _receiver.Connect(socketTransport);
        }

        public Task<ReceiveResponse> SendAsync(Request message)
        {
            return _protocolAdapter.SendRequestAsync(message);
        }

        public void Disconnect()
        {
            _sender.Disconnect();
            _receiver.Disconnect();
        }

        private void OnConnectionDisconnected(object sender, EventArgs e)
        {
            if (_autoReconnect)
            {
                // Try to rerun the client connection
                Task.Delay(100);
                Background.Run(ConnectAsync);
            }
        }
    }
}
