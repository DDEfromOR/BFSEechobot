﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Bot.Protocol
{
    public partial class Request
    {
        public const string GET = "GET";
        public const string POST = "POST";
        public const string PUT = "PUT";
        public const string DELETE = "DELETE";

        public void AddStream(HttpContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (Streams == null)
            {
                Streams = new List<HttpContentStream>();
            }

            Streams.Add(
                new HttpContentStream()
                {
                    Content = content
                });
        }
        
        public static Request CreateGet(string path = null, HttpContent body = null)
        {
            return CreateRequest(GET, path, body);
        }

        public static Request CreatePost(string path = null, HttpContent body = null)
        {
            return CreateRequest(POST, path, body);
        }

        public static Request CreatePut(string path = null, HttpContent body = null)
        {
            return CreateRequest(PUT, path, body);
        }

        public static Request CreateDelete(string path = null, HttpContent body = null)
        {
            return CreateRequest(DELETE, path, body);
        }

        public static Request CreateRequest(string method, string path = null, HttpContent body = null)
        {
            var request = new Request()
            {
                Verb = method,
                Path = path
            };

            if (body != null)
            {
                request.AddStream(body);
            }

            return request;
        }
    }
}
