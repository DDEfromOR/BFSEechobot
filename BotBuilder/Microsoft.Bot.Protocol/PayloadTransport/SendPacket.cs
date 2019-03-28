using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Protocol.Payloads;

namespace Microsoft.Bot.Protocol.PayloadTransport
{
    public class SendPacket
    {
        public Header Header { get; set; }

        public Stream Payload { get; set; }

        public Func<Task> SentCallback { get; set; }
    }
}
