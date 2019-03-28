using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Bot.Protocol
{
    public static class ReceiveResponseExtensions
    {        
        public static T ReadBodyAsJson<T>(this ReceiveResponse response)
        {
            ContentStream contentStream = response.Streams?.FirstOrDefault();
            if (contentStream != null)
            {
                var stream = contentStream.GetStream();
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    using (var jsonReader = new JsonTextReader(reader))
                    {
                        var serializer = JsonSerializer.Create(SerializationSettings.DefaultDeserializationSettings);
                        return serializer.Deserialize<T>(jsonReader);
                    }
                }
            }
            return default(T);
        }

        public static string ReadBodyAsString(this ReceiveResponse response)
        {
            ContentStream contentStream = response.Streams?.FirstOrDefault();
            if (contentStream != null)
            {
                var stream = contentStream.GetStream();
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            return null;
        }
    }
}
