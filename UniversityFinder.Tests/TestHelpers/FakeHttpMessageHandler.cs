using System.Net;

namespace UniversityFinder.Tests.TestHelpers;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    public FakeHttpMessageHandler(Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> routes)
        : this(request =>
        {
            var path = request.RequestUri?.PathAndQuery ?? string.Empty;
            foreach (var (key, routeHandler) in routes)
            {
                if (path.Contains(key, StringComparison.OrdinalIgnoreCase))
                    return routeHandler(request);
            }

            return JsonResponse("[]");
        })
    {
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(_handler(request));

    public static HttpResponseMessage JsonResponse(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
}
