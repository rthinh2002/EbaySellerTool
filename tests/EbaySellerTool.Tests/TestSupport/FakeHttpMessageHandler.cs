using System.Net;
using System.Text;

namespace EbaySellerTool.Tests.TestSupport;

/// <summary>Records every request and answers with queued responses (or a default JSON body).</summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler, IHttpClientFactory
{
    private readonly Queue<HttpResponseMessage> _responses = new();

    public List<(HttpRequestMessage Request, string Body)> Requests { get; } = [];

    public FakeHttpMessageHandler RespondWith(HttpStatusCode statusCode, string json)
    {
        _responses.Enqueue(new HttpResponseMessage(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        return this;
    }

    public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));

        return _responses.Count > 0
            ? _responses.Dequeue()
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
    }
}
