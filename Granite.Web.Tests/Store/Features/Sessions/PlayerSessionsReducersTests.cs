using System.Collections.Immutable;
using Granite.Common.Dto;
using Granite.Web.Client.Store.Features.Sessions;
using Xunit;

namespace Granite.Web.Tests.Store.Features.Sessions;

public class PlayerSessionsReducersTests
{
    [Fact]
    public void ReduceLoadPlayerSessionsAction_ShouldSetLoadingTrue()
    {
        // Arrange
        var state = new PlayerSessionsState();
        var action = new LoadPlayerSessionsAction("server-id", "player-id", 1, 10, "-JoinDate", null);

        // Act
        var result = PlayerSessionsReducers.ReduceLoadPlayerSessionsAction(state, action);

        // Assert
        Assert.True(result.IsLoading);
        Assert.Null(result.ErrorMessage);
        Assert.Equal("server-id", result.CurrentServerId);
        Assert.Equal("player-id", result.CurrentPlayerId);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public void ReduceLoadPlayerSessionsSuccessAction_ShouldUpdateSessionsAndClearLoading()
    {
        // Arrange
        var state = new PlayerSessionsState { IsLoading = true };
        var sessions = new List<PlayerSessionDTO>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PlayerId = "player-1",
                PlayerName = "Player 1",
                ServerId = Guid.NewGuid(),
                JoinDate = DateTime.UtcNow.AddHours(-2),
                LeaveDate = DateTime.UtcNow.AddHours(-1),
                Duration = 3600,
                IpAddress = "192.168.1.1",
                IsActive = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                PlayerId = "player-1",
                PlayerName = "Player 1",
                ServerId = Guid.NewGuid(),
                JoinDate = DateTime.UtcNow.AddMinutes(-30),
                LeaveDate = null,
                Duration = null,
                IpAddress = "192.168.1.1",
                IsActive = true
            }
        };
        var action = new LoadPlayerSessionsSuccessAction(sessions, 25, 2, "server-id", "player-1");

        // Act
        var result = PlayerSessionsReducers.ReduceLoadPlayerSessionsSuccessAction(state, action);

        // Assert
        Assert.False(result.IsLoading);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.Sessions.Count);
        Assert.Equal(25, result.TotalItems);
        Assert.Equal(2, result.CurrentPage);
        Assert.Equal("server-id", result.CurrentServerId);
        Assert.Equal("player-1", result.CurrentPlayerId);
    }

    [Fact]
    public void ReduceLoadPlayerSessionsFailureAction_ShouldSetErrorAndClearLoading()
    {
        // Arrange
        var state = new PlayerSessionsState { IsLoading = true };
        var action = new LoadPlayerSessionsFailureAction("Failed to fetch sessions");

        // Act
        var result = PlayerSessionsReducers.ReduceLoadPlayerSessionsFailureAction(state, action);

        // Assert
        Assert.False(result.IsLoading);
        Assert.Equal("Failed to fetch sessions", result.ErrorMessage);
    }

    [Fact]
    public void ReduceClearPlayerSessionsAction_ShouldResetToInitialState()
    {
        // Arrange
        var state = new PlayerSessionsState
        {
            Sessions = ImmutableList.Create(new PlayerSessionDTO
            {
                Id = Guid.NewGuid(),
                PlayerId = "player-1",
                PlayerName = "Player 1"
            }),
            IsLoading = false,
            ErrorMessage = null,
            CurrentPage = 2,
            TotalItems = 50,
            CurrentServerId = "server-id",
            CurrentPlayerId = "player-id"
        };
        var action = new ClearPlayerSessionsAction();

        // Act
        var result = PlayerSessionsReducers.ReduceClearPlayerSessionsAction(state, action);

        // Assert
        Assert.Empty(result.Sessions);
        Assert.False(result.IsLoading);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(0, result.TotalItems);
        Assert.Null(result.CurrentServerId);
        Assert.Null(result.CurrentPlayerId);
    }

    [Fact]
    public void ReduceLoadPlayerSessionsSuccessAction_WithPaginationParameters_ShouldUpdateCorrectly()
    {
        // Arrange
        var state = new PlayerSessionsState
        {
            CurrentPage = 1,
            PageSize = 10
        };
        var sessions = new List<PlayerSessionDTO>();
        for (int i = 0; i < 10; i++)
        {
            sessions.Add(new PlayerSessionDTO
            {
                Id = Guid.NewGuid(),
                PlayerId = "player-1",
                PlayerName = "Player 1",
                JoinDate = DateTime.UtcNow.AddHours(-i),
                IsActive = i == 0
            });
        }
        var action = new LoadPlayerSessionsSuccessAction(sessions, 42, 3, "server-id", "player-1");

        // Act
        var result = PlayerSessionsReducers.ReduceLoadPlayerSessionsSuccessAction(state, action);

        // Assert
        Assert.Equal(10, result.Sessions.Count);
        Assert.Equal(42, result.TotalItems);
        Assert.Equal(3, result.CurrentPage);
        Assert.Equal(10, result.PageSize);
    }
}
