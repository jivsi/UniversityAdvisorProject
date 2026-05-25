using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using UniversityFinder.Services;

namespace UniversityFinder.Tests.TestHelpers;

internal static class TestServiceFactory
{
    public static IConfiguration CreateConfiguration(Dictionary<string, string?>? extra = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Supabase:Url"] = "https://test.supabase.co",
            ["Supabase:AnonKey"] = "test-anon-key",
            ["OpenAI:ApiKey"] = string.Empty
        };

        if (extra != null)
        {
            foreach (var pair in extra)
                values[pair.Key] = pair.Value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    public static SupabaseService CreateSupabaseService(
        Func<HttpRequestMessage, HttpResponseMessage>? handler = null)
    {
        var httpHandler = handler != null
            ? new FakeHttpMessageHandler(handler)
            : new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse("[]"));

        var httpClient = new HttpClient(httpHandler);
        return new SupabaseService(httpClient, CreateConfiguration(), NullLogger<SupabaseService>.Instance);
    }

    public static NacidScraperService CreateNacidScraperService(
        SupabaseService? supabase = null,
        Func<HttpRequestMessage, HttpResponseMessage>? nacidHandler = null)
    {
        var supabaseService = supabase ?? CreateSupabaseService();
        var httpHandler = nacidHandler != null
            ? new FakeHttpMessageHandler(nacidHandler)
            : new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse("""{"totalCount":0,"result":[]}"""));

        var httpClient = new HttpClient(httpHandler);
        return new NacidScraperService(
            httpClient,
            supabaseService,
            NullLogger<NacidScraperService>.Instance);
    }

    public static OpenAiService CreateOpenAiService(
        Func<HttpRequestMessage, HttpResponseMessage>? handler = null,
        string? apiKey = null)
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["OpenAI:ApiKey"] = apiKey ?? string.Empty
        });

        var httpHandler = handler != null
            ? new FakeHttpMessageHandler(handler)
            : new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse("[]"));

        return new OpenAiService(new HttpClient(httpHandler), config, NullLogger<OpenAiService>.Instance);
    }
}
