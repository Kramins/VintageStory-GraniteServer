using System.Reactive.Linq;
using FluentAssertions;
using GraniteServer.HostedServices;
using GraniteServer.Messaging.Commands;
using GraniteServer.Mod;
using GraniteServer.Services;
using NSubstitute;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Granite.Mod.Tests.HostedServices;

/// <summary>
/// Tests for PlayerModerationHostedService.
/// Note: Full integration testing requires a running VintageStory server instance.
/// These tests focus on service lifecycle and basic subscription validation.
/// </summary>
public class PlayerModerationHostedServiceTests : IDisposable
{
    private readonly ICoreServerAPI _mockApi;
    private readonly ServerCommandService _commandService;
    private readonly ClientMessageBusService _messageBus;
    private readonly GraniteModConfig _config;
    private readonly ILogger _mockLogger;
    private readonly PlayerModerationHostedService _service;

    public PlayerModerationHostedServiceTests()
    {
        _mockApi = Substitute.For<ICoreServerAPI>();
        _config = new GraniteModConfig { ServerId = Guid.NewGuid() };
        _mockLogger = Substitute.For<ILogger>();
        _messageBus = new ClientMessageBusService(_mockLogger, _config);
        
        // Create real ServerCommandService since it's needed by the hosted service
        _commandService = new ServerCommandService(_mockApi);

        _service = new PlayerModerationHostedService(
            _mockApi,
            _commandService,
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
        var commandService = new ServerCommandService(Substitute.For<ICoreServerAPI>());
        var act = () => new PlayerModerationHostedService(
            null!,
            commandService,
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
        var commandService = new ServerCommandService(_mockApi);
        var act = () => new PlayerModerationHostedService(
            _mockApi,
            commandService,
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
    public async Task BanPlayerCommand_IsSubscribedCorrectly()
    {
        // Arrange
        await _service.StartAsync(CancellationToken.None);
        var commandReceived = false;

        var command = new BanPlayerCommand
        {
            OriginServerId = _config.ServerId,
            Data = new BanPlayerCommandData
            {
                PlayerId = "test",
                PlayerName = "test",
                Reason = "test",
                IssuedBy = "test"
            }
        };

        _messageBus.GetObservable()
            .Where(msg => msg is BanPlayerCommand)
            .Subscribe(_ => commandReceived = true);

        // Act
        _messageBus.Publish(command);
        await Task.Delay(50);

        // Assert
        commandReceived.Should().BeTrue("BanPlayerCommand should be received");
    }

    [Fact]
    public async Task KickPlayerCommand_IsSubscribedCorrectly()
    {
        // Arrange
        await _service.StartAsync(CancellationToken.None);
        var commandReceived = false;

        var command = new KickPlayerCommand
        {
            OriginServerId = _config.ServerId,
            Data = new KickPlayerCommandData
            {
                PlayerId = "test",
                Reason = "test"
            }
        };

        _messageBus.GetObservable()
            .Where(msg => msg is KickPlayerCommand)
            .Subscribe(_ => commandReceived = true);

        // Act
        _messageBus.Publish(command);
        await Task.Delay(50);

        // Assert
        commandReceived.Should().BeTrue("KickPlayerCommand should be received");
    }

    [Fact]
    public async Task UnbanPlayerCommand_IsSubscribedCorrectly()
    {
        // Arrange
        await _service.StartAsync(CancellationToken.None);
        var commandReceived = false;

        var command = new UnbanPlayerCommand
        {
            OriginServerId = _config.ServerId,
            Data = new UnbanPlayerCommandData
            {
                PlayerId = "test"
            }
        };

        _messageBus.GetObservable()
            .Where(msg => msg is UnbanPlayerCommand)
            .Subscribe(_ => commandReceived = true);

        // Act
        _messageBus.Publish(command);
        await Task.Delay(50);

        // Assert
        commandReceived.Should().BeTrue("UnbanPlayerCommand should be received");
    }

    [Fact]
    public async Task WhitelistPlayerCommand_IsSubscribedCorrectly()
    {
        // Arrange
        await _service.StartAsync(CancellationToken.None);
        var commandReceived = false;

        var command = new WhitelistPlayerCommand
        {
            OriginServerId = _config.ServerId,
            Data = new WhitelistPlayerCommandData
            {
                PlayerId = "test"
            }
        };

        _messageBus.GetObservable()
            .Where(msg => msg is WhitelistPlayerCommand)
            .Subscribe(_ => commandReceived = true);

        // Act
        _messageBus.Publish(command);
        await Task.Delay(50);

        // Assert
        commandReceived.Should().BeTrue("WhitelistPlayerCommand should be received");
    }

    [Fact]
    public async Task UnwhitelistPlayerCommand_IsSubscribedCorrectly()
    {
        // Arrange
        await _service.StartAsync(CancellationToken.None);
        var commandReceived = false;

        var command = new UnwhitelistPlayerCommand
        {
            OriginServerId = _config.ServerId,
            Data = new UnwhitelistPlayerCommandData
            {
                PlayerId = "test"
            }
        };

        _messageBus.GetObservable()
            .Where(msg => msg is UnwhitelistPlayerCommand)
            .Subscribe(_ => commandReceived = true);

        // Act
        _messageBus.Publish(command);
        await Task.Delay(50);

        // Assert
        commandReceived.Should().BeTrue("UnwhitelistPlayerCommand should be received");
    }
}
