﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Protocol.Payloads;
using Microsoft.Bot.Protocol.Utilities;
using Newtonsoft.Json;

namespace Microsoft.Bot.Protocol.Payloads
{
    public class ReceiveResponseAssembler : PayloadAssembler
    {
        private readonly Func<Guid, ReceiveResponse, Task> _onCompleted;
        private readonly IStreamManager _streamManager;
        private readonly int? _length;

        public ReceiveResponseAssembler(Header header, IStreamManager streamManager, Func<Guid, ReceiveResponse, Task> onCompleted)
            : base(header.Id)
        {
            _streamManager = streamManager;
            _onCompleted = onCompleted;

            _length = header.End ? (int?)header.PayloadLength : null;
        }

        public override Stream CreatePayloadStream()
        {
            if (_length.HasValue)
            {
                return new MemoryStream(_length.Value);
            }
            else
            {
                return new MemoryStream();
            }
        }

        public override void OnReceive(Header header, Stream stream, int contentLength)
        {
            // Call base functionality first so that we can fire off a new Task when completed
            base.OnReceive(header, stream, contentLength);

            if (header.End)
            {
                // Move stream back to the beginning for reading
                stream.Position = 0;

                // Execute the request on a seperate Task
                Background.Run(() => ProcessResponse(stream));
            }
            // else: still receiving data into the stream
        }

        private async Task ProcessResponse(Stream stream)
        {
            using (var textReader = new StreamReader(stream))
            {
                using (var jsonReader = new JsonTextReader(textReader))
                {
                    var responsePayload = Serializer.Deserialize<ResponsePayload>(jsonReader);

                    var response = new ReceiveResponse()
                    {
                        StatusCode = responsePayload.StatusCode,
                        Streams = new List<ContentStream>()
                    };

                    if (responsePayload.Streams != null)
                    {
                        foreach (var streamDescription in responsePayload.Streams)
                        {
                            if (!Guid.TryParse(streamDescription.Id, out Guid id))
                            {
                                throw new InvalidDataException($"Stream description id '{streamDescription.Id}' is not a Guid");
                            }

                            var streamAssembler = _streamManager.GetPayloadAssembler(id);
                            streamAssembler.ContentType = streamDescription.Type;
                            streamAssembler.ContentLength = streamDescription.Length;

                            response.Streams.Add(new ContentStream(id, streamAssembler)
                            {
                                Length = streamDescription.Length,
                                Type = streamDescription.Type
                            });
                        }
                    }

                    await _onCompleted(this.Id, response).ConfigureAwait(false);
                }
            }
        }
    }
}
