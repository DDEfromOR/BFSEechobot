using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Protocol.Payloads;

namespace Microsoft.Bot.Protocol
{
    public class ContentStream
    {
        private readonly ContentStreamAssembler _assembler;
        private ConcurrentStream _stream;

        internal ContentStream(Guid id, ContentStreamAssembler assembler)
        {
            if(assembler == null)
            {
                throw new ArgumentNullException();
            }
            Id = id;
            _assembler = assembler;
        }

        public Guid Id { get; private set; }
        
        public string Type { get; set; }

        public int? Length { get; set; }

        public ConcurrentStream GetStream()
        {
            if (_stream == null)
            {
                _stream = (ConcurrentStream)_assembler.GetPayloadStream();
            }
            return _stream;
        }

        public void Cancel()
        {
            _assembler.Close();
        }
    }
}
