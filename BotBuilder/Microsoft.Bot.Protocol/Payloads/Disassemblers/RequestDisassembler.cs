using System;
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
    public class RequestDisassembler : PayloadDisassembler
    {
        public Request Request { get; private set; }
        
        public override char Type => PayloadTypes.Request;

        public RequestDisassembler(IPayloadSender sender, Guid id, Request request)
            : base(sender, id)
        {
            Request = request;
        }

        public override async Task<StreamWrapper> GetStream()
        {
            var payload = new RequestPayload()
            {
                Verb = Request.Verb,
                Path = Request.Path,
            };

            if(Request.Streams != null)
            {
                payload.Streams = new List<StreamDescription>();
                foreach (var contentStream in Request.Streams)
                {
                    var description = await GetStreamDescription(contentStream).ConfigureAwait(false);

                    payload.Streams.Add(description);
                }
            }

            Serialize(payload, out MemoryStream memoryStream, out int streamLength);

            return new StreamWrapper()
            {
                Stream = memoryStream,
                StreamLength = streamLength
            };
        }
    }
}
