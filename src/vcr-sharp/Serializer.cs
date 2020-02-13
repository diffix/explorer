﻿namespace VcrSharp
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    public class Serializer
    {
        internal static async Task<CachedRequest> Serialize(HttpRequestMessage request)
        {
            return new CachedRequest
            {
                Headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray()),
                Method = request.Method.Method,
                Uri = request.RequestUri.ToString(),
                Body = await ParseContent(request.Content)
            };
        }

        internal static HttpRequestMessage Deserialize(CachedRequest cachedRequest)
        {
            var request = new HttpRequestMessage();
            request.Method = new HttpMethod(cachedRequest.Method);
            request.RequestUri = new Uri(cachedRequest.Uri);
            foreach (var kvp in cachedRequest.Headers)
            {
                if (IsValidHeader(kvp.Key))
                {
                    request.Headers.Add(kvp.Key, kvp.Value);
                }
            }

            request.Content = SetContent(cachedRequest.Body, "");
            return request;
        }

        internal static async Task<CachedResponse> Serialize(HttpResponseMessage freshResponse)
        {
            var responseHeaders = freshResponse.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray());
            foreach (var kvp in freshResponse.Content.Headers)
            {
                responseHeaders.Add(kvp.Key, kvp.Value.ToArray());
            }

            return new CachedResponse
            {
                Headers = responseHeaders,
                Status = new Status
                {
                    Code = (int)freshResponse.StatusCode,
                    Message = freshResponse.StatusCode.ToString()
                },
                Body = await ParseContent(freshResponse.Content)
            };
        }

        internal static HttpResponseMessage Deserialize(CachedResponse cachedResponse)
        {
            var statusCode = (System.Net.HttpStatusCode)cachedResponse.Status.Code;
            var response = new HttpResponseMessage(statusCode);
            foreach (var kvp in cachedResponse.Headers)
            {
                if (IsValidHeader(kvp.Key))
                {
                    response.Headers.Add(kvp.Key, kvp.Value);
                }
            }

            if (cachedResponse.Headers.TryGetValue("Content-Type", out var values))
            {
                // TODO: we have encoding information here, we should use that instead of assuming UTF-8
                var value = values.ElementAt(0);
                var array = value.Split(';');
                var rawText = array[0];
                response.Content = SetContent(cachedResponse.Body, rawText);
            }
            return response;
        }

        static async Task<Body> ParseContent(HttpContent content)
        {
            if (content == null)
            {
                return new Body
                {
                    Encoding = "",
                    Text = ""
                };
            }

            var text = await content.ReadAsStringAsync();
            return new Body
            {
                Text = text,
                Encoding = "UTF8-8BIT"
            };
        }

        internal static HttpContent SetContent(Body body, string mediaType)
        {
            if (body.Encoding == "ASCII-8BIT")
            {
                return new StringContent(body.Text);
            }

            if (body.Encoding == "UTF8-8BIT")
            {
                return new StringContent(body.Text, Encoding.UTF8, mediaType);
            }

            return null;
        }

        static bool IsValidHeader(string header)
        {
            if (header == "Content-Length"
                || header == "Last-Modified"
                || header == "Expires"
                || header == "Content-Type")
            {
                return false;
            }

            return true;
        }
    }
}
