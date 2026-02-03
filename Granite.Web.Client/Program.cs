using Fluxor;
using Granite.Web.Client;
using Granite.Web.Client.Handlers.Events;
using Granite.Web.Client.HostedServices;
using Granite.Web.Client.Services;
using Granite.Web.Client.Services.Api;
using Granite.Web.Client.Services.Api.Players;
using Granite.Web.Client.Services.Auth;
using Granite.Web.Client.Services.SignalR;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Events;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add Fluxor state management
builder.Services.AddFluxor(options =>
{
    options
        .ScanAssemblies(typeof(Program).Assembly)
        .AddMiddleware<LoggingMiddleware>()
        .WithLifetime(StoreLifetime.Singleton);
});

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Authentication services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthenticationStateProvider>()
);

// Configure HttpClient with IHttpClientFactory and authentication handler
builder.Services.AddScoped<AuthenticationDelegatingHandler>();
builder
    .Services.AddHttpClient(
        "GraniteApi",
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress
            );
        }
    )
    .AddHttpMessageHandler<AuthenticationDelegatingHandler>();

// Register API clients as Singleton - they use IHttpClientFactory which properly manages scopes
builder.Services.AddSingleton<IServerPlayersApiClient, ServerPlayersApiClient>();
builder.Services.AddSingleton<IPlayersApiClient, PlayersApiClient>();
builder.Services.AddSingleton<IModsApiClient, ModsApiClient>();
builder.Services.AddSingleton<IServerApiClient, ServerApiClient>();
builder.Services.AddSingleton<IAuthApiClient, AuthApiClient>();
builder.Services.AddSingleton<IWorldApiClient, WorldApiClient>();

// Register message bus and event handling infrastructure
builder.Services.AddSingleton<ClientMessageBusService>();
builder.Services.AddSingleton<MessageBridgeService>();

// Register event handlers as Scoped - they'll inject IDispatcher from their own scope
builder.Services.AddScoped<IEventHandler<PlayerWhitelistedEvent>, PlayerEventHandlers>();
builder.Services.AddScoped<IEventHandler<PlayerUnwhitelistedEvent>, PlayerEventHandlers>();
builder.Services.AddScoped<IEventHandler<PlayerBannedEvent>, PlayerEventHandlers>();
builder.Services.AddScoped<IEventHandler<PlayerUnbannedEvent>, PlayerEventHandlers>();
builder.Services.AddScoped<IEventHandler<PlayerLeaveEvent>, PlayerEventHandlers>();
builder.Services.AddScoped<IEventHandler<PlayerJoinedEvent>, PlayerEventHandlers>();
builder.Services.AddScoped<IEventHandler<PlayerKickedEvent>, PlayerEventHandlers>();

// Register SignalR service
builder.Services.AddScoped<ISignalRService, SignalRService>();

await builder.Build().RunAsync();
