using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Granite.Web.Client.Services.Auth;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string TokenKey = "auth_token";
    private readonly IJSRuntime _jsRuntime;
    private readonly IJwtService _jwtService;

    public CustomAuthenticationStateProvider(IJSRuntime jsRuntime, IJwtService jwtService)
    {
        _jsRuntime = jsRuntime;
        _jwtService = jwtService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await GetTokenAsync();
            
            if (string.IsNullOrWhiteSpace(token))
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            if (_jwtService.IsTokenExpired(token))
            {
                await ClearTokenAsync();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claimsPrincipal = _jwtService.DecodeToken(token);
            if (claimsPrincipal == null)
            {
                await ClearTokenAsync();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            return new AuthenticationState(claimsPrincipal);
        }
        catch
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public virtual async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetTokenAsync(string token)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task ClearTokenAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}