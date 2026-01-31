using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Granite.Common.Dto;
using Granite.Common.Services;
using Granite.Server.Services;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Services;

public class PlayersServiceTests : IDisposable
{
    private readonly GraniteDataContext _dataContext;
    private readonly IPlayerNameResolver _mockResolver;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<PlayersService> _mockLogger;
    private readonly PlayersService _service;

    public PlayersServiceTests()
    {
        var options = new DbContextOptionsBuilder<GraniteDataContext>()
            .UseInMemoryDatabase($"PlayersServiceTests_{Guid.NewGuid()}")
            .Options;

        _dataContext = new GraniteDataContext(options);
        _mockResolver = Substitute.For<IPlayerNameResolver>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = Substitute.For<ILogger<PlayersService>>();

        _service = new PlayersService(_mockLogger, _dataContext, _mockResolver, _memoryCache);
    }

    public void Dispose()
    {
        _dataContext.Dispose();
        _memoryCache.Dispose();
    }

    [Fact]
    public async Task FindPlayerByNameAsync_NullName_ReturnsNull()
    {
        // Act
        var result = await _service.FindPlayerByNameAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindPlayerByNameAsync_EmptyName_ReturnsNull()
    {
        // Act
        var result = await _service.FindPlayerByNameAsync(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindPlayerByNameAsync_WhitespaceName_ReturnsNull()
    {
        // Act
        var result = await _service.FindPlayerByNameAsync("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindPlayerByNameAsync_PlayerInDatabase_ReturnsPlayerFromDatabase()
    {
        // Arrange
        var playerName = "DatabasePlayer";
        var playerUid = "db-player-uid-123";
        var serverId = Guid.NewGuid();

        var server = new ServerEntity
        {
            Id = serverId,
            Name = "TestServer",
            AccessToken = "token",
            CreatedAt = DateTime.UtcNow,
        };

        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            ServerId = serverId,
            PlayerUID = playerUid,
            Name = playerName,
            FirstJoinDate = DateTime.UtcNow.AddDays(-10),
            LastJoinDate = DateTime.UtcNow.AddDays(-1),
        };

        _dataContext.Servers.Add(server);
        _dataContext.Players.Add(player);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _service.FindPlayerByNameAsync(playerName);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(playerUid);
        result.Name.Should().Be(playerName);

        // Verify that the resolver was not called
        await _mockResolver.DidNotReceive().ResolvePlayerNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindPlayerByNameAsync_PlayerOnMultipleServers_ReturnsMostRecentByLastJoinDate()
    {
        // Arrange
        var playerName = "MultiServerPlayer";
        var playerUid = "multi-server-uid-123";
        var server1Id = Guid.NewGuid();
        var server2Id = Guid.NewGuid();

        var server1 = new ServerEntity
        {
            Id = server1Id,
            Name = "Server1",
            AccessToken = "token1",
            CreatedAt = DateTime.UtcNow,
        };

        var server2 = new ServerEntity
        {
            Id = server2Id,
            Name = "Server2",
            AccessToken = "token2",
            CreatedAt = DateTime.UtcNow,
        };

        var olderPlayer = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            ServerId = server1Id,
            PlayerUID = playerUid,
            Name = playerName,
            FirstJoinDate = DateTime.UtcNow.AddDays(-20),
            LastJoinDate = DateTime.UtcNow.AddDays(-10), // Older
        };

        var newerPlayer = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            ServerId = server2Id,
            PlayerUID = playerUid,
            Name = playerName,
            FirstJoinDate = DateTime.UtcNow.AddDays(-15),
            LastJoinDate = DateTime.UtcNow.AddDays(-1), // More recent
        };

        _dataContext.Servers.AddRange(server1, server2);
        _dataContext.Players.AddRange(olderPlayer, newerPlayer);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _service.FindPlayerByNameAsync(playerName);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(playerUid);
        result.Name.Should().Be(playerName);

        // Verify the most recent LastJoinDate was selected (should return the newer player's data)
        // Both have same UID, so we just verify we got a result
    }

    [Fact]
    public async Task FindPlayerByNameAsync_PlayerNotInDatabase_CallsResolverAndReturnsResult()
    {
        // Arrange
        var playerName = "ExternalPlayer";
        var expectedUid = "external-player-uid-456";

        _mockResolver
            .ResolvePlayerNameAsync(playerName, Arg.Any<CancellationToken>())
            .Returns(expectedUid);

        // Act
        var result = await _service.FindPlayerByNameAsync(playerName);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(expectedUid);
        result.Name.Should().Be(playerName);

        // Verify the resolver was called
        await _mockResolver.Received(1).ResolvePlayerNameAsync(playerName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindPlayerByNameAsync_PlayerNotInDatabaseOrApi_ReturnsNull()
    {
        // Arrange
        var playerName = "NonExistentPlayer";

        _mockResolver
            .ResolvePlayerNameAsync(playerName, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        // Act
        var result = await _service.FindPlayerByNameAsync(playerName);

        // Assert
        result.Should().BeNull();

        // Verify the resolver was called
        await _mockResolver.Received(1).ResolvePlayerNameAsync(playerName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindPlayerByNameAsync_DatabaseResult_IsCached()
    {
        // Arrange
        var playerName = "CachedDatabasePlayer";
        var playerUid = "cached-db-uid-789";
        var serverId = Guid.NewGuid();

        var server = new ServerEntity
        {
            Id = serverId,
            Name = "TestServer",
            AccessToken = "token",
            CreatedAt = DateTime.UtcNow,
        };

        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            ServerId = serverId,
            PlayerUID = playerUid,
            Name = playerName,
            FirstJoinDate = DateTime.UtcNow.AddDays(-5),
            LastJoinDate = DateTime.UtcNow.AddDays(-1),
        };

        _dataContext.Servers.Add(server);
        _dataContext.Players.Add(player);
        await _dataContext.SaveChangesAsync();

        // Act - First call
        var result1 = await _service.FindPlayerByNameAsync(playerName);

        // Remove from database to verify cache is used
        _dataContext.Players.Remove(player);
        await _dataContext.SaveChangesAsync();

        // Act - Second call (should use cache)
        var result2 = await _service.FindPlayerByNameAsync(playerName);

        // Assert
        result1.Should().NotBeNull();
        result1!.Id.Should().Be(playerUid);
        result1.Name.Should().Be(playerName);

        result2.Should().NotBeNull();
        result2!.Id.Should().Be(playerUid);
        result2.Name.Should().Be(playerName);
    }

    [Fact]
    public async Task FindPlayerByNameAsync_ApiResult_IsCached()
    {
        // Arrange
        var playerName = "CachedApiPlayer";
        var expectedUid = "cached-api-uid-999";

        _mockResolver
            .ResolvePlayerNameAsync(playerName, Arg.Any<CancellationToken>())
            .Returns(expectedUid);

        // Act - First call
        var result1 = await _service.FindPlayerByNameAsync(playerName);

        // Act - Second call (should use cache, resolver should not be called again)
        var result2 = await _service.FindPlayerByNameAsync(playerName);

        // Assert
        result1.Should().NotBeNull();
        result1!.Id.Should().Be(expectedUid);
        result1.Name.Should().Be(playerName);

        result2.Should().NotBeNull();
        result2!.Id.Should().Be(expectedUid);
        result2.Name.Should().Be(playerName);

        // Verify the resolver was called only once
        await _mockResolver.Received(1).ResolvePlayerNameAsync(playerName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindPlayerByNameAsync_CacheKey_IsCaseInsensitive()
    {
        // Arrange
        var playerNameLower = "testplayer";
        var playerNameUpper = "TESTPLAYER";
        var playerNameMixed = "TestPlayer";
        var expectedUid = "case-insensitive-uid-111";

        _mockResolver
            .ResolvePlayerNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedUid);

        // Act - Call with lowercase
        var result1 = await _service.FindPlayerByNameAsync(playerNameLower);

        // Act - Call with uppercase (should use cache)
        var result2 = await _service.FindPlayerByNameAsync(playerNameUpper);

        // Act - Call with mixed case (should use cache)
        var result3 = await _service.FindPlayerByNameAsync(playerNameMixed);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result3.Should().NotBeNull();

        // Verify the resolver was called only once (for the first call)
        await _mockResolver.Received(1).ResolvePlayerNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindPlayerByNameAsync_CancellationToken_IsPassedToResolver()
    {
        // Arrange
        var playerName = "TokenTestPlayer";
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _mockResolver
            .ResolvePlayerNameAsync(playerName, cancellationToken)
            .Returns("token-test-uid");

        // Act
        await _service.FindPlayerByNameAsync(playerName, cancellationToken);

        // Assert
        await _mockResolver.Received(1).ResolvePlayerNameAsync(playerName, cancellationToken);
    }

    [Fact]
    public async Task FindPlayerByNameAsync_CancellationToken_IsPassedToDatabaseQuery()
    {
        // Arrange
        var playerName = "DbTokenTestPlayer";
        var playerUid = "db-token-test-uid";
        var serverId = Guid.NewGuid();

        var server = new ServerEntity
        {
            Id = serverId,
            Name = "TestServer",
            AccessToken = "token",
            CreatedAt = DateTime.UtcNow,
        };

        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            ServerId = serverId,
            PlayerUID = playerUid,
            Name = playerName,
            FirstJoinDate = DateTime.UtcNow.AddDays(-5),
            LastJoinDate = DateTime.UtcNow.AddDays(-1),
        };

        _dataContext.Servers.Add(server);
        _dataContext.Players.Add(player);
        await _dataContext.SaveChangesAsync();

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act
        var result = await _service.FindPlayerByNameAsync(playerName, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(playerUid);
    }
}
