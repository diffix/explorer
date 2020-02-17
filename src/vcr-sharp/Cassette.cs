namespace VcrSharp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    public class Cassette
    {
        private int currentIndex = 0;
        private readonly string cassettePath;
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
                var file = new FileInfo(cassettePath);
                using var stream = file.OpenRead();
                using var reader = new StreamReader(stream);
                var serializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var task = Task.Factory.StartNew(() => serializer.Deserialize<CachedRequestResponseArray>(reader));
                // var task = Task.Factory.StartNew(() => JsonSerializer.Deserialize<CachedRequestResponseArray>(File.ReadAllText(cassettePath)));
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
            if (cached.Request.Uri != request.RequestUri.ToString())
            {
                return false;
            }
            var reqBody = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync();
            if (cached.Request.Body.Text != reqBody)
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
            var data = new CachedRequestResponseArray
            {
                HttpInteractions = storedEntries.ToArray()
            };

            // var text = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            var directory = Path.GetDirectoryName(cassettePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // File.WriteAllText(cassettePath, text);

            var serializer = new SerializerBuilder()
                .DisableAliases()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var file = new FileInfo(cassettePath);
            using var stream = file.Open(FileMode.OpenOrCreate, FileAccess.Write);
            using var writer = new StreamWriter(stream);
            serializer.Serialize(writer, data);

            return Task.CompletedTask;
        }
    }

    internal class Body
    {
        public string Encoding { get; set; }

        [YamlMember(ScalarStyle = YamlDotNet.Core.ScalarStyle.Literal)]
        public string Text { get; set; }
    }

    internal class CachedRequest
    {
        public string Method { get; set; }
        public string Uri { get; set; }
        public Body Body { get; set; }
        public Dictionary<string, string[]> Headers { get; set; }
    }

    internal class Status
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }

    internal class CachedResponse
    {
        public Status Status { get; set; }
        public Dictionary<string, string[]> Headers { get; set; }
        public Body Body { get; set; }
    }

    internal class CachedRequestResponse
    {
        public CachedRequest Request { get; set; }
        public CachedResponse Response { get; set; }
    }

    internal class CachedRequestResponseArray
    {
        public CachedRequestResponse[] HttpInteractions { get; set; }
    }
}
