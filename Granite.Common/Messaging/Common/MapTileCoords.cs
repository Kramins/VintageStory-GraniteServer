using System;

namespace Granite.Common.Messaging.Common;

public class MapTileCoords
{
    public MapTileCoords(int tileX, int tileZ)
    {
        TileX = tileX;
        TileZ = tileZ;
    }

    public int TileX { get; }
    public int TileZ { get; }
}
