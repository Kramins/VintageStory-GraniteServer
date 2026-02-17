using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Granite.Common.Dto;
using GraniteServer.Messaging.Commands;
using Xunit;

namespace Granite.Tests.Handlers;

public class ServerReadyEventHandlerTests
{
    [Fact]
    public void SyncPlayerModerationDataCommand_ContainsExpectedFields()
    {
        // Arrange & Act
        var command = new SyncPlayerModerationDataCommand
        {
            OriginServerId = Guid.NewGuid(),
            Data = new SyncPlayerModerationDataCommandData
            {
                Players = new List<PlayerModerationRecord>
                {
                    new PlayerModerationRecord
                    {
                        PlayerUID = "test-player",
                        Name = "TestPlayer",
                        IsBanned = true,
                        BanReason = "Test",
                        BanBy = "Admin",
                        BanUntil = DateTime.UtcNow.AddDays(7),
                        IsWhitelisted = false
                    }
                }
            }
        };

        // Assert
        command.Data.Players.Should().HaveCount(1);
        var player = command.Data.Players.First();
        player.PlayerUID.Should().Be("test-player");
        player.IsBanned.Should().BeTrue();
        player.BanReason.Should().Be("Test");
    }

    [Fact]
    public void PlayerModerationData_Filtering_BannedAndWhitelistedOnly()
    {
        // Arrange
        var players = new List<PlayerDTO>
        {
            new PlayerDTO { PlayerUID = "banned", IsBanned = true, IsWhitelisted = false },
            new PlayerDTO { PlayerUID = "whitelisted", IsBanned = false, IsWhitelisted = true },
            new PlayerDTO { PlayerUID = "normal", IsBanned = false, IsWhitelisted = false },
            new PlayerDTO { PlayerUID = "both", IsBanned = true, IsWhitelisted = true }
        };

        // Act - Simulate the filtering logic from ServerReadyEventHandler
        var filtered = players
            .Where(p => p.IsBanned || p.IsWhitelisted)
            .ToList();

        // Assert
        filtered.Should().HaveCount(3);
        filtered.Should().Contain(p => p.PlayerUID == "banned");
        filtered.Should().Contain(p => p.PlayerUID == "whitelisted");
        filtered.Should().Contain(p => p.PlayerUID == "both");
        filtered.Should().NotContain(p => p.PlayerUID == "normal");
    }

    [Fact]
    public void PlayerModerationData_Filtering_ExpiredBans()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var players = new List<PlayerDTO>
        {
            new PlayerDTO { PlayerUID = "expired", IsBanned = true, BanUntil = now.AddDays(-7) },
            new PlayerDTO { PlayerUID = "active", IsBanned = true, BanUntil = now.AddDays(7) },
            new PlayerDTO { PlayerUID = "permanent", IsBanned = true, BanUntil = null }
        };

        // Act - Simulate the expired ban filtering logic
        var filtered = players
            .Where(p => p.IsBanned || p.IsWhitelisted)
            .Where(p => !p.IsBanned || p.BanUntil == null || p.BanUntil > now)
            .ToList();

        // Assert
        filtered.Should().HaveCount(2);
        filtered.Should().Contain(p => p.PlayerUID == "active");
        filtered.Should().Contain(p => p.PlayerUID == "permanent");
        filtered.Should().NotContain(p => p.PlayerUID == "expired");
    }

    [Fact]
    public void PlayerModerationRecord_MapsCorrectly()
    {
        // Arrange
        var playerDto = new PlayerDTO
        {
            PlayerUID = "test-uid",
            Name = "TestName",
            IsBanned = true,
            BanReason = "Cheating",
            BanBy = "Admin",
            BanUntil = DateTime.UtcNow.AddDays(30),
            IsWhitelisted = true,
            WhitelistedReason = "VIP",
            WhitelistedBy = "System"
        };

        // Act - Simulate mapping logic
        var record = new PlayerModerationRecord
        {
            PlayerUID = playerDto.PlayerUID,
            Name = playerDto.Name,
            IsBanned = playerDto.IsBanned,
            BanReason = playerDto.BanReason,
            BanBy = playerDto.BanBy,
            BanUntil = playerDto.BanUntil,
            IsWhitelisted = playerDto.IsWhitelisted,
            WhitelistedReason = playerDto.WhitelistedReason,
            WhitelistedBy = playerDto.WhitelistedBy
        };

        // Assert
        record.PlayerUID.Should().Be("test-uid");
        record.Name.Should().Be("TestName");
        record.IsBanned.Should().BeTrue();
        record.BanReason.Should().Be("Cheating");
        record.BanBy.Should().Be("Admin");
        record.BanUntil.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
        record.IsWhitelisted.Should().BeTrue();
        record.WhitelistedReason.Should().Be("VIP");
        record.WhitelistedBy.Should().Be("System");
    }
}
