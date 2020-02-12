﻿#pragma warning disable CA1822 // make method static
namespace Explorer.Api.Tests
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using Explorer.Api;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Configuration;

    public class TestWebAppFactory : WebApplicationFactory<Startup>
    {
        public static readonly ExplorerConfig Config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json")
            .Build()
            .GetSection("Explorer")
            .Get<ExplorerConfig>();

        public HttpRequestMessage CreateHttpRequest(HttpMethod method, string endpoint, object data)
        {
            var request = new HttpRequestMessage(method, endpoint);
            if (data != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(data));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(System.Net.Mime.MediaTypeNames.Application.Json);
            }
            return request;
        }

        public HttpClient CreateExplorerApiHttpClient(string testClassName, string vcrSessionName)
        {
            var cassettePath = GetVcrCasettePath(testClassName, vcrSessionName);
            var handler = new VcrSharp.ReplayingHandler(cassettePath);
            return CreateDefaultClient(handler);
        }

        public HttpClient CreateAircloakApiHttpClient(string testClassName, string vcrSessionName)
        {
            var cassettePath = GetVcrCasettePath(testClassName, vcrSessionName);
            return CreateAircloakApiHttpClient(cassettePath);
        }

#pragma warning disable CA2000 // call IDisposable.Dispose on handler object
        public HttpClient CreateAircloakApiHttpClient(string cassettePath)
        {
            var handler = new VcrSharp.ReplayingHandler(cassettePath);
            var client = new HttpClient(handler, true) { BaseAddress = Config.AircloakApiUrl };
            if (!client.DefaultRequestHeaders.TryAddWithoutValidation("auth-token", Config.AircloakApiKey))
            {
                throw new Exception("Failed to add Http header 'auth-token'");
            }
            return client;
        }
#pragma warning restore CA2000 // call IDisposable.Dispose on handler object

        public string GetVcrCasettePath(string testClassName, string vcrSessionName)
        {
            return $"../../../.vcr/{testClassName}.{vcrSessionName}.json";
        }
    }
}
#pragma warning restore CA1822 // make method static
