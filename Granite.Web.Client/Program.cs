using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Granite.Web.Client;
using Granite.Web.Client.Services.Api;
using Granite.Web.Client.Services.SignalR;
using Granite.Web.Client.Services.Auth;
using MudBlazor.Services;
using Fluxor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add Fluxor state management
builder.Services.AddFluxor(options =>
{
    options.ScanAssemblies(typeof(Program).Assembly);
});

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Authentication services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
    provider.GetRequiredService<CustomAuthenticationStateProvider>());

// Configure HttpClient with authentication handler
builder.Services.AddScoped<AuthenticationDelegatingHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthenticationDelegatingHandler>();
    handler.InnerHandler = new HttpClientHandler();
    
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress)
    };
    
    return httpClient;
});

// Register API clients
builder.Services.AddScoped<IPlayersApiClient, PlayersApiClient>();
builder.Services.AddScoped<IModsApiClient, ModsApiClient>();
builder.Services.AddScoped<IServerApiClient, ServerApiClient>();
builder.Services.AddScoped<IAuthApiClient, AuthApiClient>();
builder.Services.AddScoped<IWorldApiClient, WorldApiClient>();

// Register SignalR service
builder.Services.AddScoped<ISignalRService, SignalRService>();

await builder.Build().RunAsync();
