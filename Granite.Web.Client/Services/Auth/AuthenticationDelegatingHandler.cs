using System.Net.Http.Headers;

namespace Granite.Web.Client.Services.Auth;

public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly CustomAuthenticationStateProvider _authStateProvider;

    public AuthenticationDelegatingHandler(CustomAuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Don't add token to auth endpoints
        if (request.RequestUri?.AbsolutePath.Contains("/api/auth/") == false)
        {
            var token = await _authStateProvider.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}