using System;
using System.Threading.Tasks;
using FluentAssertions;
using Granite.Common.Dto;
using Granite.Server.Services;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using GraniteServer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Services;

public class ServersServiceTests : IDisposable
{
    private readonly GraniteDataContext _dataContext;
    private readonly ILogger<ServersService> _mockLogger;
    private readonly PersistentMessageBusService _mockMessageBus;
    private readonly ServersService _service;

    public ServersServiceTests()
    {
        var options = new DbContextOptionsBuilder<GraniteDataContext>()
            .UseInMemoryDatabase($"ServersServiceTests_{Guid.NewGuid()}")
            .Options;

        _dataContext = new GraniteDataContext(options);
        _mockLogger = Substitute.For<ILogger<ServersService>>();
        
        // Mock the dependencies for PersistentMessageBusService
        var mockScopeFactory = Substitute.For<IServiceScopeFactory>();
        var mockMessageBusLogger = Substitute.For<ILogger<PersistentMessageBusService>>();
        _mockMessageBus = Substitute.ForPartsOf<PersistentMessageBusService>(mockScopeFactory, mockMessageBusLogger);

        _service = new ServersService(_mockLogger, _dataContext, _mockMessageBus);
    }

    public void Dispose()
    {
        _dataContext.Dispose();
    }

    #region CreateServerAsync Tests

    [Fact]
    public async Task CreateServerAsync_ValidRequest_CreatesServerSuccessfully()
    {
        // Arrange
        var request = new CreateServerRequestDTO
        {
            Name = "Test Server",
            Description = "Test Description",
            Port = 42420,
            MaxClients = 10,
        };

        // Act
        var result = await _service.CreateServerAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Test Server");
        result.Description.Should().Be("Test Description");
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.AccessToken.Length.Should().BeGreaterThan(20); // Base64 encoded 32 bytes
        result.IsOnline.Should().BeFalse();
        result.LastSeenAt.Should().BeNull();

        // Verify it's in the database
        var dbServer = await _dataContext.Servers.FindAsync(result.Id);
        dbServer.Should().NotBeNull();
        dbServer!.Name.Should().Be("Test Server");
        dbServer.Port.Should().Be(42420);
        dbServer.MaxClients.Should().Be(10);
    }

    [Fact]
    public async Task CreateServerAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingServer = new ServerEntity
        {
            Id = Guid.NewGuid(),
            Name = "Existing Server",
            AccessToken = "token123",
            CreatedAt = DateTime.UtcNow,
        };
        _dataContext.Servers.Add(existingServer);
        await _dataContext.SaveChangesAsync();

        var request = new CreateServerRequestDTO
        {
            Name = "Existing Server",
            Description = "Different description",
        };

        // Act & Assert
        var act = async () => await _service.CreateServerAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateServerAsync_WithAllConfigOptions_StoresAllProperties()
    {
        // Arrange
        var request = new CreateServerRequestDTO
        {
            Name = "Full Config Server",
            Description = "With all options",
            Port = 42420,
            WelcomeMessage = "Welcome!",
            MaxClients = 20,
            Password = "secret123",
            MaxChunkRadius = 8,
            WhitelistMode = true,
            AllowPvP = false,
            AllowFireSpread = true,
            AllowFallingBlocks = false,
        };

        // Act
        var result = await _service.CreateServerAsync(request);

        // Assert
        var dbServer = await _dataContext.Servers.FindAsync(result.Id);
        dbServer.Should().NotBeNull();
        dbServer!.Port.Should().Be(42420);
        dbServer.WelcomeMessage.Should().Be("Welcome!");
        dbServer.MaxClients.Should().Be(20);
        dbServer.Password.Should().Be("secret123");
        dbServer.MaxChunkRadius.Should().Be(8);
        dbServer.WhitelistMode.Should().Be(true);
        dbServer.AllowPvP.Should().Be(false);
        dbServer.AllowFireSpread.Should().Be(true);
        dbServer.AllowFallingBlocks.Should().Be(false);
    }

    [Fact]
    public async Task CreateServerAsync_GeneratesUniqueTokens()
    {
        // Arrange
        var request1 = new CreateServerRequestDTO { Name = "Server1" };
        var request2 = new CreateServerRequestDTO { Name = "Server2" };

        // Act
        var result1 = await _service.CreateServerAsync(request1);
        var result2 = await _service.CreateServerAsync(request2);

        // Assert
        result1.AccessToken.Should().NotBe(result2.AccessToken);
    }

    #endregion

    #region GetServerByIdAsync Tests

    [Fact]
    public async Task GetServerByIdAsync_ExistingServer_ReturnsServerDTO()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var server = new ServerEntity
        {
            Id = serverId,
            Name = "Test Server",
            Description = "Test Description",
            AccessToken = "secret-token",
            CreatedAt = DateTime.UtcNow,
            IsOnline = true,
            LastSeenAt = DateTime.UtcNow,
        };
        _dataContext.Servers.Add(server);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _service.GetServerByIdAsync(serverId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(serverId);
        result.Name.Should().Be("Test Server");
        result.Description.Should().Be("Test Description");
        result.IsOnline.Should().BeTrue();
        result.LastSeenAt.Should().NotBeNull();
        // AccessToken should NOT be included in ServerDTO
    }

    [Fact]
    public async Task GetServerByIdAsync_NonExistentServer_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetServerByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateServerAsync Tests

    [Fact]
    public async Task UpdateServerAsync_ExistingServer_UpdatesSuccessfully()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var originalToken = "original-token";
        var server = new ServerEntity
        {
            Id = serverId,
            Name = "Original Name",
            Description = "Original Description",
            AccessToken = originalToken,
            CreatedAt = DateTime.UtcNow,
            Port = 42420,
        };
        _dataContext.Servers.Add(server);
        await _dataContext.SaveChangesAsync();

        var updateRequest = new UpdateServerRequestDTO
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Port = 42421,
            MaxClients = 15,
        };

        // Act
        var result = await _service.UpdateServerAsync(serverId, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(serverId);
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");

        // Verify in database
        var dbServer = await _dataContext.Servers.FindAsync(serverId);
        dbServer.Should().NotBeNull();
        dbServer!.Name.Should().Be("Updated Name");
        dbServer.Description.Should().Be("Updated Description");
        dbServer.Port.Should().Be(42421);
        dbServer.MaxClients.Should().Be(15);
        dbServer.AccessToken.Should().Be(originalToken); // Token should not change
    }

    [Fact]
    public async Task UpdateServerAsync_NonExistentServer_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateServerRequestDTO
        {
            Name = "Updated Name",
            Description = "Updated Description",
        };

        // Act
        var result = await _service.UpdateServerAsync(nonExistentId, updateRequest);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateServerAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var server1 = new ServerEntity
        {
            Id = Guid.NewGuid(),
            Name = "Server 1",
            AccessToken = "token1",
            CreatedAt = DateTime.UtcNow,
        };
        var server2 = new ServerEntity
        {
            Id = Guid.NewGuid(),
            Name = "Server 2",
            AccessToken = "token2",
            CreatedAt = DateTime.UtcNow,
        };
        _dataContext.Servers.AddRange(server1, server2);
        await _dataContext.SaveChangesAsync();

        var updateRequest = new UpdateServerRequestDTO
        {
            Name = "Server 1", // Try to use Server 1's name for Server 2
            Description = "Updated",
        };

        // Act & Assert
        var act = async () => await _service.UpdateServerAsync(server2.Id, updateRequest);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateServerAsync_SameName_AllowsUpdate()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var server = new ServerEntity
        {
            Id = serverId,
            Name = "My Server",
            Description = "Old Description",
            AccessToken = "token",
            CreatedAt = DateTime.UtcNow,
        };
        _dataContext.Servers.Add(server);
        await _dataContext.SaveChangesAsync();

        var updateRequest = new UpdateServerRequestDTO
        {
            Name = "My Server", // Same name
            Description = "New Description",
        };

        // Act
        var result = await _service.UpdateServerAsync(serverId, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Description.Should().Be("New Description");
    }

    #endregion

    #region DeleteServerAsync Tests

    [Fact]
    public async Task DeleteServerAsync_ExistingServer_DeletesSuccessfully()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var server = new ServerEntity
        {
            Id = serverId,
            Name = "Server To Delete",
            AccessToken = "token",
            CreatedAt = DateTime.UtcNow,
        };
        _dataContext.Servers.Add(server);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _service.DeleteServerAsync(serverId);

        // Assert
        result.Should().BeTrue();

        // Verify it's deleted from database
        var dbServer = await _dataContext.Servers.FindAsync(serverId);
        dbServer.Should().BeNull();
    }

    [Fact]
    public async Task DeleteServerAsync_NonExistentServer_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteServerAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteServerAsync_ServerWithPlayers_DeletesCascade()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var server = new ServerEntity
        {
            Id = serverId,
            Name = "Server With Players",
            AccessToken = "token",
            CreatedAt = DateTime.UtcNow,
        };
        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            ServerId = serverId,
            PlayerUID = "player-uid-123",
            Name = "Test Player",
            FirstJoinDate = DateTime.UtcNow,
            LastJoinDate = DateTime.UtcNow,
        };
        _dataContext.Servers.Add(server);
        _dataContext.Players.Add(player);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _service.DeleteServerAsync(serverId);

        // Assert
        result.Should().BeTrue();

        // Verify both server and player are deleted (cascade)
        var dbServer = await _dataContext.Servers.FindAsync(serverId);
        dbServer.Should().BeNull();
        var dbPlayer = await _dataContext.Players.FindAsync(player.Id);
        dbPlayer.Should().BeNull();
    }

    #endregion

    #region RegenerateAccessTokenAsync Tests

    [Fact]
    public async Task RegenerateAccessTokenAsync_ExistingServer_GeneratesNewToken()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var originalToken = "original-token-12345";
        var server = new ServerEntity
        {
            Id = serverId,
            Name = "Test Server",
            AccessToken = originalToken,
            CreatedAt = DateTime.UtcNow,
        };
        _dataContext.Servers.Add(server);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _service.RegenerateAccessTokenAsync(serverId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(serverId);
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.AccessToken.Should().NotBe(originalToken);
        result.AccessToken.Length.Should().BeGreaterThan(20);

        // Verify in database
        var dbServer = await _dataContext.Servers.FindAsync(serverId);
        dbServer.Should().NotBeNull();
        dbServer!.AccessToken.Should().Be(result.AccessToken);
        dbServer.AccessToken.Should().NotBe(originalToken);
    }

    [Fact]
    public async Task RegenerateAccessTokenAsync_NonExistentServer_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.RegenerateAccessTokenAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RegenerateAccessTokenAsync_GeneratesUniqueTokens()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var server = new ServerEntity
        {
            Id = serverId,
            Name = "Test Server",
            AccessToken = "original-token",
            CreatedAt = DateTime.UtcNow,
        };
        _dataContext.Servers.Add(server);
        await _dataContext.SaveChangesAsync();

        // Act
        var result1 = await _service.RegenerateAccessTokenAsync(serverId);
        var result2 = await _service.RegenerateAccessTokenAsync(serverId);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.AccessToken.Should().NotBe(result2!.AccessToken);
    }

    #endregion

    #region GetServersAsync Tests (existing functionality)

    [Fact]
    public async Task GetServersAsync_WithMultipleServers_ReturnsAllServers()
    {
        // Arrange
        var server1 = new ServerEntity
        {
            Id = Guid.NewGuid(),
            Name = "Server 1",
            AccessToken = "token1",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
        };
        var server2 = new ServerEntity
        {
            Id = Guid.NewGuid(),
            Name = "Server 2",
            AccessToken = "token2",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
        };
        _dataContext.Servers.AddRange(server1, server2);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _service.GetServersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Server 1"); // Ordered by CreatedAt
        result[1].Name.Should().Be("Server 2");
    }

    [Fact]
    public async Task GetServersAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetServersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion
}
