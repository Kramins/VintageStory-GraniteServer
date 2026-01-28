using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Granite.Web.Client;
using Granite.Web.Client.Services.Api;
using Granite.Web.Client.Services.SignalR;
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

// Configure HttpClient with base address
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress) });

// Register API clients
builder.Services.AddScoped<IPlayersApiClient, PlayersApiClient>();
builder.Services.AddScoped<IModsApiClient, ModsApiClient>();
builder.Services.AddScoped<IServerApiClient, ServerApiClient>();
builder.Services.AddScoped<IAuthApiClient, AuthApiClient>();
builder.Services.AddScoped<IWorldApiClient, WorldApiClient>();

// Register SignalR service
builder.Services.AddScoped<ISignalRService, SignalRService>();

await builder.Build().RunAsync();
