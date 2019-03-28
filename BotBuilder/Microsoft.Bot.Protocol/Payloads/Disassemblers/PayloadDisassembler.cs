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
    public abstract class PayloadDisassembler
    {
        protected static JsonSerializer Serializer = JsonSerializer.Create(SerializationSettings.DefaultSerializationSettings);

        private IPayloadSender Sender { get; set; }

        private Stream Stream { get; set; }

        private int? StreamLength { get; set; }

        private int SendOffset { get; set; }

        private Guid Id { get; set; }

        public abstract char Type { get; }
        
        public PayloadDisassembler(IPayloadSender sender, Guid id)
        {
            Sender = sender;
            Id = id;
        }

        public abstract Task<StreamWrapper> GetStream();
        
        public async Task Disassemble()
        {
            var w = await GetStream().ConfigureAwait(false);

            Stream = w.Stream;
            StreamLength = w.StreamLength;
            SendOffset = 0;

            await Send().ConfigureAwait(false);
        }

        private Task Send()
        {
            long length = StreamLength.HasValue ? StreamLength.Value : Stream.Length;

            // if there is more to send queue it up
            if (SendOffset < length)
            {
                int count = (int)Math.Min(length - SendOffset, TransportConstants.MaxPayloadLength);        // can at most fit in an int
                SendOffset += count;

                var header = new Header()
                {
                    Type = Type,
                    Id = Id,
                    PayloadLength = count,
                    End = SendOffset >= length
                };

                Sender.SendPayload(header, Stream, Send);
            }

            return Task.CompletedTask;
        }

        protected static async Task<StreamDescription> GetStreamDescription(HttpContentStream stream)
        {
            var description = new StreamDescription()
            {
                Id = stream.Id.ToString("D")
            };
            
            if (stream.Content.Headers.TryGetValues(HeaderNames.ContentType, out IEnumerable<string> contentType))
            {
                description.Type = contentType?.FirstOrDefault();
            }
            
            if (stream.Content.Headers.TryGetValues(HeaderNames.ContentLength, out IEnumerable<string> contentLength))
            {
                var value = contentLength?.FirstOrDefault();
                if (value != null && Int32.TryParse(value, out int length))
                {
                    description.Length = length;
                }
            }
            else
            {
                description.Length = (int?)stream.Content.Headers.ContentLength;
            }

            return description;
        }
        
        protected static void Serialize<T>(T item, out MemoryStream stream, out int length)
        {
            stream = new MemoryStream();
            using (var textWriter = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                using (var jsonWriter = new JsonTextWriter(textWriter))
                {
                    Serializer.Serialize(jsonWriter, item);
                    jsonWriter.Flush();
                }
            }
            length = (int)stream.Position;
            stream.Position = 0;
        }
    }
}
