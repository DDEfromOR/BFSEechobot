﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Protocol.PayloadTransport;
using Microsoft.Bot.Protocol.Transport;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Microsoft.Bot.Protocol.Payloads
{
    public class HttpContentStreamDisassembler : PayloadDisassembler
    {
        public HttpContentStream ContentStream { get; private set; }
        
        public override char Type => PayloadTypes.Stream;

        public HttpContentStreamDisassembler(IPayloadSender sender, HttpContentStream contentStream)
            : base(sender, contentStream.Id)
        {
            ContentStream = contentStream;
        }

        public override async Task<StreamWrapper> GetStream()
        {
            var stream = await ContentStream.Content.ReadAsStreamAsync().ConfigureAwait(false);

            // TODO, this isn't right, not sure how to tell if a .NET stream has unlimited length...
            var length = (int)Math.Min(Int32.MaxValue, stream.Length);

            return new StreamWrapper()
            {
                Stream = stream,
                StreamLength = length
            };
        }
    }
}
