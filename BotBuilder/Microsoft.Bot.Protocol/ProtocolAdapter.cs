using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Protocol.Payloads;
using Microsoft.Bot.Protocol.PayloadTransport;
using Microsoft.Bot.Protocol.Utilities;

namespace Microsoft.Bot.Protocol
{
    public class ProtocolAdapter
    {
        private readonly RequestHandler _requestHandler;
        private readonly IPayloadSender _payloadSender;
        private readonly IPayloadReceiver _payloadReceiver;
        private readonly IRequestManager _requestManager;
        private readonly SendOperations _sendOperations;
        private readonly IStreamManager _streamManager;
        private readonly PayloadAssemblerManager _assemblerManager;

        public ProtocolAdapter(RequestHandler requestHandler, IRequestManager requestManager, IPayloadSender payloadSender, IPayloadReceiver payloadReceiver)
        {
            _requestHandler = requestHandler;
            _requestManager = requestManager;
            _payloadSender = payloadSender;
            _payloadReceiver = payloadReceiver;

            _sendOperations = new SendOperations(_payloadSender);
            _streamManager = new StreamManager(OnCancelStream);
            _assemblerManager = new PayloadAssemblerManager(_streamManager, OnReceiveRequest, OnReceiveResponse);

            _payloadReceiver.Subscribe(_assemblerManager.GetPayloadStream, _assemblerManager.OnReceive);
        }

        public async Task<ReceiveResponse> SendRequestAsync(Request request)
        {
            var requestId = Guid.NewGuid();

            await _sendOperations.SendRequestAsync(requestId, request).ConfigureAwait(false);

            // wait for the response
            var response = await _requestManager.GetResponseAsync(requestId).ConfigureAwait(false);

            return response;
        }

        private async Task OnReceiveRequest(Guid id, ReceiveRequest request)
        {
            // request is done, we can handle it
            if (_requestHandler != null)
            {
                var response = await _requestHandler.ProcessRequestAsync(request).ConfigureAwait(false);

                if (response != null)
                {
                    await _sendOperations.SendResponseAsync(id, response).ConfigureAwait(false);
                }
            }
        }

        private async Task OnReceiveResponse(Guid id, ReceiveResponse response)
        {
            // we received the response to something, signal it
            await _requestManager.SignalResponse(id, response).ConfigureAwait(false);
        }

        private void OnCancelStream(PayloadAssembler contentStreamAssembler)
        {
            Background.Run(() => _sendOperations.SendCancelStreamAsync(contentStreamAssembler.Id));
        }
    }
}
