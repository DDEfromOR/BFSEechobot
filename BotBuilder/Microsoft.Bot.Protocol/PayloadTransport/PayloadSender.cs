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
    /// <summary>
    /// On Send: queues up sends and sends them along the transport
    /// On Receive: receives a packet header and some bytes and dispatches it to the subscriber
    /// </summary>
    public class PayloadSender : IPayloadSender
    {
        private readonly SendQueue<SendPacket> _sendQueue;
        private readonly EventWaitHandle _connectedEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

        private ITransportSender _sender;

        public PayloadSender()
        {
            _sendQueue = new SendQueue<SendPacket>(this.WritePacketAsync);
        }

        public event TransportDisconnectedEventHandler Disconnected;

        public bool IsConnected
        {
            get { return _sender != null; }
        }

        public void Connect(ITransportSender sender)
        {
            if (_sender != null)
            {
                throw new InvalidOperationException("Already connected.");
            }

            _sender = sender;

            _connectedEvent.Set();
        }

        public void SendPayload(Header header, Stream payload, Func<Task> sentCallback)
        {
            var packet = new SendPacket()
            {
                Header = header,
                Payload = payload,
                SentCallback = sentCallback
            };
            _sendQueue.Post(packet);
        }

        public void Disconnect(TransportDisconnectedEventArgs e = null)
        {
            bool didDisconnect = false;

            lock (_connectedEvent)
            {
                try
                {
                    if (_sender != null)
                    {
                        _sender.Close();
                        _sender.Dispose();
                        didDisconnect = true;
                    }
                }
                catch (Exception)
                {
                }
                _sender = null;
            }

            if (didDisconnect)
            {
                _connectedEvent.Reset();
                Disconnected?.Invoke(this, e ?? TransportDisconnectedEventArgs.Empty);
            }
        }

        private byte[] _sendHeaderBuffer = new byte[TransportConstants.MaxHeaderLength];
        private byte[] _sendContentBuffer = new byte[TransportConstants.MaxPayloadLength];

        private async Task WritePacketAsync(SendPacket packet)
        {
            _connectedEvent.WaitOne();

            TransportDisconnectedEventArgs disconnectedArgs = null;

            try
            {
                int length;

                // Note: there are no zero-byte sends; sending a length of 0 means the transport was closed

                int headerLength = HeaderSerializer.Serialize(packet.Header, _sendHeaderBuffer, 0);

                // Send: Packet Header
                length = await _sender.SendAsync(_sendHeaderBuffer, 0, headerLength).ConfigureAwait(false);
                if (length == 0)
                {
                    throw new TransportDisconnectedException();
                }

                int offset = 0;

                // Send content in chunks
                if (packet.Header.PayloadLength > 0 && packet.Payload != null)
                {
                    do
                    {
                        int count = Math.Min(packet.Header.PayloadLength - offset, TransportConstants.MaxPayloadLength);

                        // copy the stream to the buffer
                        count = await packet.Payload.ReadAsync(_sendContentBuffer, 0, count).ConfigureAwait(false);

                        // Send: Packet content
                        length = await _sender.SendAsync(_sendContentBuffer, 0, count).ConfigureAwait(false);
                        if (length == 0)
                        {
                            throw new TransportDisconnectedException();
                        }

                        offset += count;
                    } while (offset < packet.Header.PayloadLength);
                }

                if (packet.SentCallback != null)
                {
                    Background.Run(() => packet.SentCallback());
                }
            }
            catch (Exception e)
            {
                disconnectedArgs = new TransportDisconnectedEventArgs()
                {
                    Reason = e.Message
                };
                Disconnect(disconnectedArgs);
            }
        }
    }
}
