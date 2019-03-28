using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Protocol.Payloads;
using Microsoft.Bot.Protocol.PayloadTransport;

namespace Microsoft.Bot.Protocol.WebSockets
{
    public class WebSocketServer
    {
        private readonly RequestHandler _requestHandler;
        private readonly RequestManager _requestManager;
        private readonly IPayloadSender _sender;
        private readonly IPayloadReceiver _receiver;
        private readonly ProtocolAdapter _protocolAdapter;
        private readonly WebSocketTransport _websocketTransport;
        private TaskCompletionSource<string> _closedSignal;

        public WebSocketServer(WebSocket socket, RequestHandler requestHandler)
        {
            _websocketTransport = new WebSocketTransport(socket);
            _requestHandler = requestHandler;

            _requestManager = new RequestManager();

            _sender = new PayloadSender();
            _sender.Disconnected += OnConnectionDisconnected;
            _receiver = new PayloadReceiver();
            _receiver.Disconnected += OnConnectionDisconnected;

            _protocolAdapter = new ProtocolAdapter(_requestHandler, _requestManager, _sender, _receiver);
        }

        public Task StartAsync()
        {
            _closedSignal = new TaskCompletionSource<string>();
            _sender.Connect(_websocketTransport);
            _receiver.Connect(_websocketTransport);
            return _closedSignal.Task;
        }

        public Task<ReceiveResponse> SendAsync(Request request)
        {
            return _protocolAdapter.SendRequestAsync(request);
        }

        public void Disconnect()
        {
            _sender.Disconnect();
            _receiver.Disconnect();
        }

        private void OnConnectionDisconnected(object sender, EventArgs e)
        {
            _closedSignal.SetResult("close");
        }
    }
}
