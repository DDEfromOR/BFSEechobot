using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Protocol.PayloadTransport
{
    public class TransportDisconnectedEventArgs : EventArgs
    {
        public string Reason { get; set; }

        public new static TransportDisconnectedEventArgs Empty = new TransportDisconnectedEventArgs();
    }
}
