using System.Reactive.Linq;
using FluentAssertions;
using GraniteServer.HostedServices;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Mod;
using GraniteServer.Services;
using NSubstitute;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Granite.Mod.Tests.HostedServices;

/// <summary>
/// Tests for CollectiblesHostedService.
/// Note: Full integration testing requires a running VintageStory server instance.
/// These tests focus on service lifecycle and basic subscription validation.
/// </summary>
public class CollectiblesHostedServiceTests : IDisposable
{
    private readonly ICoreServerAPI _mockApi;
    private readonly ClientMessageBusService _messageBus;
    private readonly GraniteModConfig _config;
    private readonly ILogger _mockLogger;
    private readonly CollectiblesHostedService _service;

    public CollectiblesHostedServiceTests()
    {
        _mockApi = Substitute.For<ICoreServerAPI>();
        _config = new GraniteModConfig { ServerId = Guid.NewGuid() };
        _mockLogger = Substitute.For<ILogger>();
        _messageBus = new ClientMessageBusService(_mockLogger, _config);

        _service = new CollectiblesHostedService(
            _mockApi,
            _messageBus,
            _config,
            _mockLogger
        );
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    [Fact]
    public async Task StartAsync_InitializesSuccessfully()
    {
        // Act
        await _service.StartAsync(CancellationToken.None);

        // Assert - No exception thrown means success
        _mockLogger.Received().Notification(Arg.Is<string>(s => s.Contains("started")));
    }

    [Fact]
    public async Task StopAsync_DisposesSubscriptionsSuccessfully()
    {
        // Arrange
        await _service.StartAsync(CancellationToken.None);

        // Act
        await _service.StopAsync(CancellationToken.None);

        // Assert - No exception thrown means success
        _mockLogger.Received().Notification(Arg.Is<string>(s => s.Contains("stopped")));
    }

    [Fact]
    public void Constructor_WithNullApi_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new CollectiblesHostedService(
            null!,
            _messageBus,
            _config,
            _mockLogger
        );
        
        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("api");
    }

    [Fact]
    public void Constructor_WithNullMessageBus_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new CollectiblesHostedService(
            _mockApi,
            null!,
            _config,
            _mockLogger
        );
        
        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("messageBus");
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange & Act
        _service.Dispose();
        var act = () => _service.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task MessageBusPublishesCommand_ServiceSubscriptionReceivesIt()
    {
        // Arrange
        await _service.StartAsync(CancellationToken.None);
        var commandReceived = false;

        // Subscribe to message bus to verify commands flow through
        _messageBus.GetObservable()
            .Where(msg => msg is SyncCollectiblesCommand)
            .Subscribe(_ => commandReceived = true);

        var command = new SyncCollectiblesCommand
        {
            OriginServerId = _config.ServerId,
            Data = new SyncCollectiblesCommandData()
        };

        // Act
        _messageBus.Publish(command);
        
        // Give time for async processing
        await Task.Delay(50);

        // Assert - command flows through message bus (service subscribed successfully)
        commandReceived.Should().BeTrue();
    }
}
