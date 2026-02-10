using System.Collections.Immutable;
using Fluxor;

namespace Granite.Web.Client.Store.Features.Map;

[FeatureState]
public record MapState
{
    public ImmutableDictionary<string, PlayerMapPosition> PlayerPositions { get; init; } =
        ImmutableDictionary<string, PlayerMapPosition>.Empty;

    public DateTime? LastUpdated { get; init; }
}

public record PlayerMapPosition
{
    public required string PlayerUID { get; init; }
    public required float BlockX { get; init; }
    public required float BlockZ { get; init; }
    public required string PlayerName { get; init; }
    public required DateTime LastUpdated { get; init; }

    // Computed property for OpenLayers coordinates
    public (double MapX, double MapY) MapCoords => (BlockX, -BlockZ);
}
