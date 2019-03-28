using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Protocol.Payloads;
using Microsoft.Bot.Protocol.Transport;

namespace Microsoft.Bot.Protocol.PayloadTransport
{
    public interface IPayloadReceiver
    {
        bool IsConnected { get; }

        event TransportDisconnectedEventHandler Disconnected;
        
        void Connect(ITransportReceiver receiver);

        void Subscribe(Func<Header, Stream> getStream, Action<Header, Stream, int> receiveAction);

        void Disconnect(TransportDisconnectedEventArgs e = null);
    }
}
