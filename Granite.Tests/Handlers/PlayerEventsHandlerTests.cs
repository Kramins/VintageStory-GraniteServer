using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Events;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Handlers;

public class PlayerEventsHandlerTests
{
    private readonly GraniteDataContext _mockDataContext;
    private readonly PlayerEventsHandler _handler;

    public PlayerEventsHandlerTests()
    {
        var options = new DbContextOptionsBuilder<GraniteDataContext>()
            .UseInMemoryDatabase($"PlayerEventsTests_{Guid.NewGuid()}")
            .Options;

        _mockDataContext = new GraniteDataContext(options);
        _handler = new PlayerEventsHandler(_mockDataContext);
    }

    [Fact]
    public async Task Handle_PlayerJoinedEvent_CreatesNewPlayer()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var playerUID = "player123";
        var playerName = "TestPlayer";
        var @event = new PlayerJoinedEvent
        {
            OriginServerId = serverId,
            Data = new PlayerJoinedEventData
            {
                PlayerUID = playerUID,
                PlayerName = playerName,
                SessionId = Guid.NewGuid(),
                IpAddress = "192.168.1.1"
            }
        };

        // Act
        await ((IEventHandler<PlayerJoinedEvent>)_handler).Handle(@event);

        // Assert
        var savedPlayer = _mockDataContext.Players.Single(p => p.PlayerUID == playerUID && p.ServerId == serverId);
        savedPlayer.Name.Should().Be(playerName);
    }

    [Fact]
    public async Task Handle_PlayerJoinedEvent_CreatesSessionWithIpTracking()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var ipAddress = "192.168.1.1";
        var @event = new PlayerJoinedEvent
        {
            OriginServerId = serverId,
            Data = new PlayerJoinedEventData
            {
                PlayerUID = "player123",
                PlayerName = "TestPlayer",
                SessionId = sessionId,
                IpAddress = ipAddress
            }
        };

        // Act
        await ((IEventHandler<PlayerJoinedEvent>)_handler).Handle(@event);

        // Assert
        var savedSession = _mockDataContext.PlayerSessions.Single(s => s.Id == sessionId);
        savedSession.IpAddress.Should().Be(ipAddress);
    }

    [Fact]
    public async Task Handle_PlayerLeaveEvent_ClosesSessionAndCalculatesDuration()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var joinDate = DateTime.UtcNow.AddHours(-1);
        var session = new PlayerSessionEntity
        {
            Id = sessionId,
            JoinDate = joinDate,
            LeaveDate = null,
            Duration = null
        };

        var @event = new PlayerLeaveEvent
        {
            Data = new PlayerLeaveEventData { SessionId = sessionId }
        };

        _mockDataContext.PlayerSessions.Add(session);
        _mockDataContext.SaveChanges();

        // Act
        await ((IEventHandler<PlayerLeaveEvent>)_handler).Handle(@event);

        // Assert
        var updated = _mockDataContext.PlayerSessions.Single(s => s.Id == sessionId);
        updated.LeaveDate.HasValue.Should().BeTrue();
        updated.Duration.HasValue.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PlayerBannedEvent_StoresBanWithPermanentDate()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var banReason = "Cheating";
        var issuer = "Admin";
        var player = new PlayerEntity
        {
            Id = playerId,
            PlayerUID = "player123",
            ServerId = Guid.NewGuid()
        };

        var @event = new PlayerBannedEvent
        {
            OriginServerId = player.ServerId,
            Data = new PlayerBannedEventData
            {
                PlayerUID = player.PlayerUID,
                Reason = banReason,
                IssuedBy = issuer,
                ExpirationDate = DateTime.MaxValue
            }
        };

        _mockDataContext.Players.Add(player);
        _mockDataContext.SaveChanges();

        // Act
        await ((IEventHandler<PlayerBannedEvent>)_handler).Handle(@event);

        // Assert
        var updated = _mockDataContext.Players.Single(p => p.Id == playerId);
        updated.IsBanned.Should().BeTrue();
        updated.BanReason.Should().Be(banReason);
        updated.BanBy.Should().Be(issuer);
        updated.BanUntil.Should().Be(DateTime.MaxValue);
    }

    [Fact]
    public async Task Handle_PlayerBannedEvent_StoresBanWithExpirationDate()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var expirationDate = DateTime.UtcNow.AddDays(7);
        var player = new PlayerEntity
        {
            Id = playerId,
            PlayerUID = "player123",
            ServerId = Guid.NewGuid()
        };

        var @event = new PlayerBannedEvent
        {
            OriginServerId = player.ServerId,
            Data = new PlayerBannedEventData
            {
                PlayerUID = player.PlayerUID,
                Reason = "Spam",
                IssuedBy = "Mod",
                ExpirationDate = expirationDate
            }
        };

        _mockDataContext.Players.Add(player);
        _mockDataContext.SaveChanges();

        // Act
        await ((IEventHandler<PlayerBannedEvent>)_handler).Handle(@event);

        // Assert
        var updated = _mockDataContext.Players.Single(p => p.Id == playerId);
        updated.BanUntil.Should().Be(expirationDate);
    }

    [Fact]
    public async Task Handle_PlayerUnbannedEvent_RemovesBanAndClearsFields()
    {
        // Arrange
        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            PlayerUID = "player123",
            ServerId = Guid.NewGuid(),
            IsBanned = true,
            BanReason = "Cheating",
            BanBy = "Admin",
            BanUntil = DateTime.MaxValue
        };

        var @event = new PlayerUnbannedEvent
        {
            OriginServerId = player.ServerId,
            Data = new PlayerUnbannedEventData { PlayerUID = player.PlayerUID }
        };

        _mockDataContext.Players.Add(player);
        _mockDataContext.SaveChanges();

        // Act
        await ((IEventHandler<PlayerUnbannedEvent>)_handler).Handle(@event);

        // Assert
        var updated = _mockDataContext.Players.Single(p => p.Id == player.Id);
        updated.IsBanned.Should().BeFalse();
        updated.BanReason.Should().BeNull();
        updated.BanBy.Should().BeNull();
        updated.BanUntil.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PlayerWhitelistedEvent_UpdatesWhitelistState()
    {
        // Arrange
        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            PlayerUID = "player123",
            ServerId = Guid.NewGuid()
        };

        var @event = new PlayerWhitelistedEvent
        {
            OriginServerId = player.ServerId,
            Data = new PlayerWhitelistedEventData { PlayerUID = player.PlayerUID }
        };

        _mockDataContext.Players.Add(player);
        _mockDataContext.SaveChanges();

        // Act
        await ((IEventHandler<PlayerWhitelistedEvent>)_handler).Handle(@event);

        // Assert
        var updated = _mockDataContext.Players.Single(p => p.Id == player.Id);
        updated.IsWhitelisted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PlayerUnwhitelistedEvent_RemovesWhitelistAndClearsFields()
    {
        // Arrange
        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            PlayerUID = "player123",
            ServerId = Guid.NewGuid(),
            IsWhitelisted = true,
            WhitelistedReason = "Friend of admin",
            WhitelistedBy = "Admin"
        };

        var @event = new PlayerUnwhitelistedEvent
        {
            OriginServerId = player.ServerId,
            Data = new PlayerUnwhitelistedEventData { PlayerUID = player.PlayerUID }
        };

        _mockDataContext.Players.Add(player);
        _mockDataContext.SaveChanges();

        // Act
        await ((IEventHandler<PlayerUnwhitelistedEvent>)_handler).Handle(@event);

        // Assert
        var updated = _mockDataContext.Players.Single(p => p.Id == player.Id);
        updated.IsWhitelisted.Should().BeFalse();
        updated.WhitelistedReason.Should().BeNull();
        updated.WhitelistedBy.Should().BeNull();
    }

    // No helper needed with EF Core InMemory provider
}
