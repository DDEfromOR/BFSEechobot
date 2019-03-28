﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Protocol.PayloadTransport;
using Microsoft.Bot.Protocol.Utilities;

namespace Microsoft.Bot.Protocol.Payloads
{
    public class SendOperations
    {
        private readonly IPayloadSender _payloadSender;
        
        public SendOperations(IPayloadSender payloadSender)
        {
            _payloadSender = payloadSender;
        }

        public async Task SendRequestAsync(Guid id, Request request)
        {
            var disassembler = new RequestDisassembler(_payloadSender, id, request);

            await disassembler.Disassemble().ConfigureAwait(false);

            if (request.Streams != null)
            {
                foreach (var contentStream in request.Streams)
                {
                    var contentDisassembler = new HttpContentStreamDisassembler(_payloadSender, contentStream);

                    await contentDisassembler.Disassemble().ConfigureAwait(false);
                }
            }
        }

        public async Task SendResponseAsync(Guid id, Response response)
        {
            var disassembler = new ResponseDisassembler(_payloadSender, id, response);

            await disassembler.Disassemble().ConfigureAwait(false);

            if (response.Streams != null)
            {
                foreach (var contentStream in response.Streams)
                {
                    var contentDisassembler = new HttpContentStreamDisassembler(_payloadSender, contentStream);

                    await contentDisassembler.Disassemble().ConfigureAwait(false);
                }
            }
        }

        public async Task SendCancelAllAsync(Guid id)
        {
            var disassembler = new CancelDisassembler(_payloadSender, id, PayloadTypes.CancelAll);

            await disassembler.Disassemble().ConfigureAwait(false);
        }

        public async Task SendCancelStreamAsync(Guid id)
        {
            var disassembler = new CancelDisassembler(_payloadSender, id, PayloadTypes.CancelStream);

            await disassembler.Disassemble().ConfigureAwait(false);
        }
    }
}
