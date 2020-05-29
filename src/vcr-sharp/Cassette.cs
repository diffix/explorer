namespace VcrSharp
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    public class Cassette : IDisposable
    {
        private readonly string cassettePath;

        private readonly EventWaitHandle cacheWaitHandle;

        private LinkedList<CachedRequestResponse> cachedEntries;

        private readonly ConcurrentQueue<CachedRequestResponse> storedEntries;

        public Cassette(string cassettePath)
        {
            this.cassettePath = cassettePath;
            storedEntries = new ConcurrentQueue<CachedRequestResponse>();

            cacheWaitHandle = new EventWaitHandle(true, EventResetMode.AutoReset);

            SetupCache();
        }

        void SetupCache()
        {
            if (File.Exists(cassettePath))
            {
                var file = new FileInfo(cassettePath);
                using var stream = file.OpenRead();
                using var reader = new StreamReader(stream);
                var serializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var contents = serializer.Deserialize<CachedRequestResponseArray>(reader);
                cachedEntries = new LinkedList<CachedRequestResponse>(
                    contents?.HttpInteractions ?? Array.Empty<CachedRequestResponse>());
            }
            else
            {
                cachedEntries = new LinkedList<CachedRequestResponse>();
            }
        }

        public static string GenerateVcrFilename(object caller, [CallerMemberName] string name = "") =>
            $"{caller.GetType().Name}.{name}";

        static bool MatchesRequest(CachedRequest cached, CachedRequest fresh)
        {
            if (!string.Equals(cached.Method, fresh.Method, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (cached.Uri != fresh.Uri)
            {
                return false;
            }
            if (cached.Body.Text != fresh.Body.Text)
            {
                return false;
            }
            return true;
        }

        internal async Task<CacheResult> FindCachedResponse(HttpRequestMessage request)
        {
            // awaiting must be done *outside* the area protected by the WaitHandle to avoid deadlocks.
            var freshRequest = await Serializer.Serialize(request);

            // Lock the WaitHandle so only one thread can enter at a time
            cacheWaitHandle.WaitOne();

            var result = CacheResult.Missing();
            var entry = cachedEntries.FirstOrDefault(cached => MatchesRequest(cached.Request, freshRequest));

            if (entry != default)
            {
                cachedEntries.Remove(entry);
                // persist the existing cached entry to disk
                StoreEntry(entry);
                result = CacheResult.Success(Serializer.Deserialize(entry.Response));
            }

            // Unlock the WaitHandle 
            cacheWaitHandle.Set();

            return result;
        }

        internal async Task StoreCachedResponseAsync(HttpRequestMessage request, HttpResponseMessage freshResponse)
        {
            var cachedResponse = new CachedRequestResponse
            {
                Request = await Serializer.Serialize(request),
                Response = await Serializer.Serialize(freshResponse)
            };

            StoreEntry(cachedResponse);
        }

        private void StoreEntry(CachedRequestResponse entry)
        {
            storedEntries.Enqueue(entry);
        }

        internal void FlushToDisk()
        {
            if (storedEntries.IsEmpty)
            {
                return;
            }

            var data = new CachedRequestResponseArray
            {
                HttpInteractions = storedEntries.ToArray()
            };

            var directory = Path.GetDirectoryName(cassettePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var serializer = new SerializerBuilder()
                .DisableAliases()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var file = new FileInfo(cassettePath);
            using var stream = file.Open(FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(stream);
            serializer.Serialize(writer, data);
        }

        public void Dispose()
        {
            FlushToDisk();
            cacheWaitHandle.Dispose();
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
