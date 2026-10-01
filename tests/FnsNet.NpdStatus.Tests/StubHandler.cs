using System.Net;
using System.Text;

namespace FnsNet.NpdStatus.Tests;

/// <summary>
/// Answers with a canned response and records what was sent.
/// </summary>
/// <remarks>
/// Every test here is offline. The service is reachable only from Russian networks, so one that
/// touched it would fail for most contributors and pass for the rest — worse than no test.
/// </remarks>
internal sealed class StubHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _body;
    private readonly string _contentType;

    public StubHandler(HttpStatusCode statusCode, string body, string contentType = "application/json")
    {
        _statusCode = statusCode;
        _body = body;
        _contentType = contentType;
    }

    public string? LastRequestBody { get; private set; }

    public Uri? LastRequestUri { get; private set; }

    public HttpMethod? LastMethod { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        LastMethod = request.Method;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_body, Encoding.UTF8, _contentType),
        };
    }

    public static (NpdStatusClient Client, StubHandler Handler) Create(
        HttpStatusCode statusCode,
        string body,
        string contentType = "application/json",
        NpdStatusOptions? options = null)
    {
        var handler = new StubHandler(statusCode, body, contentType);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://statusnpd.nalog.ru/"),
        };

        return options is null
            ? (new NpdStatusClient(httpClient), handler)
            : (new NpdStatusClient(httpClient, Microsoft.Extensions.Options.Options.Create(options)), handler);
    }
}
