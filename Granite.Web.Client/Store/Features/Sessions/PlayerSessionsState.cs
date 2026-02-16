using System.Collections.Immutable;
using Fluxor;
using Granite.Common.Dto;

namespace Granite.Web.Client.Store.Features.Sessions;

[FeatureState]
public record PlayerSessionsState
{
    public ImmutableList<PlayerSessionDTO> Sessions { get; init; } = ImmutableList<PlayerSessionDTO>.Empty;
    public bool IsLoading { get; init; }
    public string? ErrorMessage { get; init; }
    public int CurrentPage { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public string? CurrentServerId { get; init; }
    public string? CurrentPlayerId { get; init; }
}
