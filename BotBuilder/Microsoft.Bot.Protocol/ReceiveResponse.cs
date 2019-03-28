using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Bot.Protocol
{
    public class ReceiveResponse
    {
        /// <summary>
        /// Status - The Response Status
        /// </summary>
        public int StatusCode { get; set; }


        public List<ContentStream> Streams { get; set; }
    }
}
