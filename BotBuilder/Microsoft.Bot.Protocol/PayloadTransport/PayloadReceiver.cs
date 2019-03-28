using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Protocol.Payloads;
using Microsoft.Bot.Protocol.Transport;
using Microsoft.Bot.Protocol.Utilities;

namespace Microsoft.Bot.Protocol.PayloadTransport
{
    public class PayloadReceiver : IPayloadReceiver
    {
        private Func<Header, Stream> _getStream;
        private Action<Header, Stream, int> _receiveAction;
        private readonly EventWaitHandle _connectedEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

        private ITransportReceiver _receiver;
        
        public PayloadReceiver()
        {
        }

        public event TransportDisconnectedEventHandler Disconnected;

        public bool IsConnected
        {
            get { return _receiver != null; }
        }

        public void Connect(ITransportReceiver receiver)
        {
            if (_receiver != null)
            {
                throw new InvalidOperationException("Already connected.");
            }

            _receiver = receiver;

            RunReceive();

            _connectedEvent.Set();
        }

        public void Subscribe(
            Func<Header, Stream> getStream,
            Action<Header, Stream, int> receiveAction)
        {
            _getStream = getStream;
            _receiveAction = receiveAction;
        }

        public void Disconnect(TransportDisconnectedEventArgs e = null)
        {
            bool didDisconnect = false;

            lock (_connectedEvent)
            {
                try
                {
                    if (_receiver != null)
                    {
                        _receiver.Close();
                        _receiver.Dispose();
                        didDisconnect = true;
                    }
                }
                catch (Exception)
                {
                }
                _receiver = null;
            }

            if (didDisconnect)
            {
                _connectedEvent.Reset();
                Disconnected?.Invoke(this, e ?? TransportDisconnectedEventArgs.Empty);
            }
        }

        private void RunReceive()
        {
            Background.Run(ReceivePacketsAsync);
        }
        
        private byte[] _receiveHeaderBuffer = new byte[TransportConstants.MaxHeaderLength];
        private byte[] _receiveContentBuffer = new byte[TransportConstants.MaxPayloadLength];
        
        private async Task ReceivePacketsAsync()
        {
            _connectedEvent.WaitOne();

            bool isClosed = false;
            int length;
            TransportDisconnectedEventArgs disconnectArgs = null;
            
            while (_receiver != null && _receiver.IsConnected && !isClosed)
            {
                // receive a single packet
                try
                {
                    // read the header
                    int headerOffset = 0;
                    while (headerOffset < TransportConstants.MaxHeaderLength)
                    {
                        length = await _receiver.ReceiveAsync(_receiveHeaderBuffer, headerOffset, TransportConstants.MaxHeaderLength - headerOffset).ConfigureAwait(false);
                        if (length == 0)
                        {
                            throw new TransportDisconnectedException("Stream closed while reading header bytes");
                        }

                        headerOffset += length;
                    }
                    
                    // deserialize the bytes into a header
                    var header = HeaderSerializer.Deserialize(_receiveHeaderBuffer, 0, TransportConstants.MaxHeaderLength);
                    
                    // read the payload
                    var contentStream = _getStream(header);

                    var buffer = PayloadTypes.IsStream(header) ?
                        new byte[header.PayloadLength] :
                        _receiveContentBuffer;

                    int offset = 0;

                    if (header.PayloadLength > 0)
                    {
                        do
                        {
                            // read in chunks
                            int count = Math.Min(header.PayloadLength - offset, TransportConstants.MaxPayloadLength);

                            // read the content
                            length = await _receiver.ReceiveAsync(buffer, 0, count).ConfigureAwait(false);
                            if (length == 0)
                            {
                                throw new TransportDisconnectedException("Stream closed while reading payload bytes");
                            }

                            if (contentStream != null)
                            {
                                // write chunks to the contentStream if it's not a stream type
                                if (!PayloadTypes.IsStream(header))
                                {
                                    await contentStream.WriteAsync(buffer, 0, length).ConfigureAwait(false);
                                }
                            }
                            offset += length;
                        } while (offset < header.PayloadLength);

                        // give the full payload buffer to the contentStream if it's a stream
                        if(contentStream != null && PayloadTypes.IsStream(header))
                        {
                            ((ConcurrentStream)contentStream).GiveBuffer(buffer, length);
                        }
                    }

                    _receiveAction(header, contentStream, offset);
                }
                catch(TransportDisconnectedException de)
                {
                    isClosed = true;
                    disconnectArgs = new TransportDisconnectedEventArgs()
                    {
                        Reason = de.Reason
                    };
                }
                catch (Exception e)
                {
                    isClosed = true;
                    disconnectArgs = new TransportDisconnectedEventArgs()
                    {
                        Reason = e.Message
                    };
                }
            }

            Disconnect(disconnectArgs);
        }
    }
}
