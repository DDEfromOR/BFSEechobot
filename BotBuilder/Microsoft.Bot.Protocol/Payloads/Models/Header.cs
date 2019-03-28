using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Bot.Protocol.Payloads
{
    public class Header
    {
        public char Type { get; set; }

        public int PayloadLength { get; set; }

        public Guid Id { get; set; }

        public bool End { get; set; }
    }
}
