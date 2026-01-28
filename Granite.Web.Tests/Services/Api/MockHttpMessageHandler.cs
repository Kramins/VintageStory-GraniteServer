using System.Net;

namespace Granite.Web.Tests.Services.Api;

/// <summary>
/// Mock HTTP message handler for testing HTTP clients.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string _responseContent = "{}";
    private readonly List<(string Url, HttpMethod Method)> _requestHistory = new();

    public void SetResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseContent = content;
        _statusCode = statusCode;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _requestHistory.Add((request.RequestUri?.ToString() ?? "", request.Method));

        return await Task.FromResult(new HttpResponseMessage
        {
            StatusCode = _statusCode,
            Content = new StringContent(_responseContent, System.Text.Encoding.UTF8, "application/json"),
            RequestMessage = request
        });
    }

    public void VerifyRequest(string urlContains, HttpMethod method)
    {
        var found = _requestHistory.Any(r => r.Url.Contains(urlContains) && r.Method == method);
        if (!found)
        {
            throw new InvalidOperationException($"Request not found: {method} containing {urlContains}");
        }
    }

    public void ClearHistory()
    {
        _requestHistory.Clear();
    }
}
