using System.Net;

namespace RenovateDashboard.App.Tests.Services;

internal sealed class FakeHttpMessageHandler(
    Dictionary<string, (HttpStatusCode Status, string Body)> responses) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var key = request.RequestUri!.ToString();
        if (responses.TryGetValue(key, out var r))
        {
            return Task.FromResult(new HttpResponseMessage(r.Status)
            {
                Content = new StringContent(r.Body, System.Text.Encoding.UTF8, "application/json")
            });
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("not found")
        });
    }
}
