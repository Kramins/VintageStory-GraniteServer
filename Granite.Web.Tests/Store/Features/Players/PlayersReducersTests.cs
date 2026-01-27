using System.Collections.Immutable;
using Granite.Common.Dto;
using Granite.Web.Client.Store.Features.Players;
using Xunit;

namespace Granite.Web.Tests.Store.Features.Players;

public class PlayersReducersTests
{
    [Fact]
    public void OnFetchPlayers_ShouldSetLoadingTrue()
    {
        // Arrange
        var state = new PlayersState();
        var action = new FetchPlayersAction();

        // Act
        var result = PlayersReducers.OnFetchPlayers(state, action);

        // Assert
        Assert.True(result.IsLoading);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void OnFetchPlayersSuccess_ShouldUpdatePlayersAndClearLoading()
    {
        // Arrange
        var state = new PlayersState { IsLoading = true };
        var players = new List<PlayerDTO>
        {
            new() { PlayerUID = "uid1", Name = "Player1" },
            new() { PlayerUID = "uid2", Name = "Player2" }
        };
        var action = new FetchPlayersSuccessAction(players);

        // Act
        var result = PlayersReducers.OnFetchPlayersSuccess(state, action);

        // Assert
        Assert.False(result.IsLoading);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.Players.Count);
        Assert.Equal("Player1", result.Players[0].Name);
        Assert.Equal("Player2", result.Players[1].Name);
        Assert.NotNull(result.LastUpdated);
    }

    [Fact]
    public void OnFetchPlayersFailure_ShouldSetErrorAndClearLoading()
    {
        // Arrange
        var state = new PlayersState { IsLoading = true };
        var action = new FetchPlayersFailureAction("Network error");

        // Act
        var result = PlayersReducers.OnFetchPlayersFailure(state, action);

        // Assert
        Assert.False(result.IsLoading);
        Assert.Equal("Network error", result.ErrorMessage);
    }

    [Fact]
    public void OnSelectPlayer_ShouldSetLoadingTrue()
    {
        // Arrange
        var state = new PlayersState();
        var action = new SelectPlayerAction("player-123");

        // Act
        var result = PlayersReducers.OnSelectPlayer(state, action);

        // Assert
        Assert.True(result.IsLoading);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void OnSelectPlayerSuccess_ShouldSetSelectedPlayerAndClearLoading()
    {
        // Arrange
        var state = new PlayersState { IsLoading = true };
        var player = new PlayerDTO { PlayerUID = "uid1", Name = "Player1" };
        var action = new SelectPlayerSuccessAction(player);

        // Act
        var result = PlayersReducers.OnSelectPlayerSuccess(state, action);

        // Assert
        Assert.False(result.IsLoading);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.SelectedPlayer);
        Assert.Equal("Player1", result.SelectedPlayer.Name);
    }

    [Fact]
    public void OnSelectPlayerFailure_ShouldSetErrorAndClearLoading()
    {
        // Arrange
        var state = new PlayersState { IsLoading = true };
        var action = new SelectPlayerFailureAction("Player not found");

        // Act
        var result = PlayersReducers.OnSelectPlayerFailure(state, action);

        // Assert
        Assert.False(result.IsLoading);
        Assert.Equal("Player not found", result.ErrorMessage);
    }

    [Fact]
    public void OnClearPlayersError_ShouldClearErrorMessage()
    {
        // Arrange
        var state = new PlayersState { ErrorMessage = "Some error" };
        var action = new ClearPlayersErrorAction();

        // Act
        var result = PlayersReducers.OnClearPlayersError(state, action);

        // Assert
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void PlayersState_ShouldHaveImmutableList()
    {
        // Arrange
        var state = new PlayersState();

        // Act & Assert
        Assert.NotNull(state.Players);
        Assert.IsAssignableFrom<ImmutableList<PlayerDTO>>(state.Players);
        Assert.Empty(state.Players);
    }

    [Fact]
    public void PlayersState_WithModification_ShouldReturnNewInstance()
    {
        // Arrange
        var state1 = new PlayersState();
        var players = new List<PlayerDTO> { new() { PlayerUID = "uid1", Name = "Player1" } };

        // Act
        var state2 = state1 with { Players = players.ToImmutableList() };

        // Assert
        Assert.NotSame(state1, state2);
        Assert.Empty(state1.Players);
        Assert.Single(state2.Players);
    }
}
