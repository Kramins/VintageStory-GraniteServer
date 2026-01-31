using System.Collections.Immutable;
using Fluxor;
using Granite.Common.Dto;

namespace Granite.Web.Client.Store.Features.Players;

[FeatureState]
public record PlayersState
{
    public ImmutableList<PlayerDTO> Players { get; init; } = ImmutableList<PlayerDTO>.Empty;
    public PlayerDTO? SelectedPlayer { get; init; }
    public PlayerDetailsDTO? SelectedPlayerDetails { get; init; }
    public bool IsLoading { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? LastUpdated { get; init; }
    public string? CurrentServerId { get; init; }
}
