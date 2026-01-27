using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Fluxor;
using MudBlazor.Services;
using Xunit;

namespace Granite.Web.Tests.Configuration;

public class ProgramConfigurationTests
{
    [Fact]
    public void FluxorServices_ShouldBeRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFluxor(options =>
        {
            options.ScanAssemblies(typeof(Granite.Web.Client._Imports).Assembly);
        });

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var store = serviceProvider.GetService<IStore>();

        // Assert
        Assert.NotNull(store);
    }

    [Fact]
    public void MudBlazorServices_ShouldRegisterSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => services.AddMudServices());
        Assert.Null(exception);
        
        // Verify services collection is not empty
        Assert.NotEmpty(services);
    }

    [Fact]
    public void HttpClient_ShouldBeConfiguredWithBaseAddress()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "https://localhost:5001"
            })
            .Build();

        services.AddHttpClient("GraniteAPI", client =>
        {
            var apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:5001";
            client.BaseAddress = new Uri(apiBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        var httpClient = httpClientFactory?.CreateClient("GraniteAPI");

        // Assert
        Assert.NotNull(httpClient);
        Assert.NotNull(httpClient.BaseAddress);
        Assert.Equal("https://localhost:5001/", httpClient.BaseAddress.ToString());
        Assert.True(httpClient.DefaultRequestHeaders.Contains("Accept"));
    }
}
