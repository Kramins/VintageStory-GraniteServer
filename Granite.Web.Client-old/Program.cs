using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Fluxor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add Fluxor state management
builder.Services.AddFluxor(options =>
{
    options.ScanAssemblies(typeof(Program).Assembly);
});

// Add MudBlazor services
builder.Services.AddMudServices();

// Add HttpClient for API calls
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    };
    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    return httpClient;
});

await builder.Build().RunAsync();
