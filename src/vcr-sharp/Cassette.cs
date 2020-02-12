namespace VcrSharp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class Cassette
    {
        private int currentIndex = 0;
        private readonly string? cassettePath;
        private List<CachedRequestResponse> cachedEntries;
        private List<CachedRequestResponse> storedEntries;

        public Cassette(string cassettePath)
        {
            this.cassettePath = cassettePath;
        }

        async Task SetupCache()
        {
            if (File.Exists(cassettePath))
            {
                var task = Task.Factory.StartNew(() => JsonSerializer.Deserialize<CachedRequestResponseArray>(File.ReadAllText(cassettePath)));
                var contents = await task;
                cachedEntries = new List<CachedRequestResponse>(contents.HttpInteractions ?? Array.Empty<CachedRequestResponse>());
            }
            else
            {
                cachedEntries = new List<CachedRequestResponse>();
            }

            storedEntries = new List<CachedRequestResponse>();
        }

        static async Task<bool> MatchesRequest(CachedRequestResponse cached, HttpRequestMessage request)
        {
            if (!string.Equals(cached.Request.Method, request.Method.Method, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (cached.Request.Uri != request.RequestUri)
            {
                return false;
            }
            return true;
        }

        internal async Task<CacheResult> FindCachedResponseAsync(HttpRequestMessage request)
        {
            if (cachedEntries == null)
            {
                await SetupCache();
            }

            if (currentIndex < 0 || currentIndex >= cachedEntries.Count)
            {
                return CacheResult.Missing();
            }

            var entry = cachedEntries[currentIndex];
            currentIndex++;
            if (await MatchesRequest(entry, request))
            {
                // persist the existing cached entry to disk
                storedEntries.Add(entry);
                return CacheResult.Success(Serializer.Deserialize(entry.Response));
            }

            return CacheResult.Missing();
        }

        internal async Task StoreCachedResponseAsync(HttpRequestMessage request, HttpResponseMessage freshResponse)
        {
            if (cachedEntries == null)
            {
                await SetupCache();
            }

            var cachedResponse = new CachedRequestResponse
            {
                Request = await Serializer.Serialize(request),
                Response = await Serializer.Serialize(freshResponse)
            };

            storedEntries.Add(cachedResponse);
        }

        internal Task FlushToDisk()
        {
            var json = new CachedRequestResponseArray
            {
                HttpInteractions = storedEntries.ToArray()
            };

            var text = JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true });
            var directory = Path.GetDirectoryName(cassettePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(cassettePath, text);
            return Task.CompletedTask;
        }
    }

    internal class Body
    {
        [JsonPropertyName("encoding")]
        public string Encoding { get; set; }
        [JsonPropertyName("Base64_string")]
        public string Base64String { get; set; }
    }

    internal class CachedRequest
    {
        [JsonPropertyName("method")]
        public string Method { get; set; }
        [JsonPropertyName("uri")]
        public Uri Uri { get; set; }
        [JsonPropertyName("body")]
        public Body Body { get; set; }
        [JsonPropertyName("headers")]
        public Dictionary<string, string[]> Headers { get; set; }
    }

    internal class Status
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    internal class CachedResponse
    {
        [JsonPropertyName("status")]
        public Status Status { get; set; }
        [JsonPropertyName("headers")]
        public Dictionary<string, string[]> Headers { get; set; }
        [JsonPropertyName("body")]
        public Body Body { get; set; }
    }

    internal class CachedRequestResponse
    {
        [JsonPropertyName("request")]
        public CachedRequest Request { get; set; }
        [JsonPropertyName("response")]
        public CachedResponse Response { get; set; }
    }

    internal class CachedRequestResponseArray
    {
        [JsonPropertyName("http_interactions")]
        public CachedRequestResponse[] HttpInteractions { get; set; }
    }
}
