# World Map Feature

## Overview
The World Map feature allows users to view an interactive map of their Vintage Story world server directly in the web interface. The map is rendered using OpenLayers 10 and displays map tiles extracted from the game server.

## Architecture

### Backend (Server)
- **Controller**: `ServerWorldMapController` handles 3 endpoints:
  - `GET /api/worldmap/{serverId}/bounds` - Returns world boundary information
  - `GET /api/worldmap/{serverId}/tiles/{x}/{y}` - Returns a PNG tile image
  - `GET /api/worldmap/{serverId}/tiles/{x}/{y}/metadata` - Returns tile metadata

- **Service**: `ServerWorldMapService` orchestrates data retrieval from:
  - `MapDataStorageService` - Manages chunk storage with compression and hashing
  - `MapRenderingService` - Renders map chunks to PNG images
  - `GraniteDataContext` - EF Core database access

- **Coordinate System**: The server handles coordinate transformation:
  - OpenLayers uses `(x, y)` with y increasing downward
  - Game uses `(chunkX, chunkZ)` with z increasing northward
  - Transformation: `chunkX = x; chunkZ = -y;`

### Frontend (Blazor Client)
- **Page**: `WorldMap.razor` - Main map display component
- **Service**: `WorldMapService` - API client for map data
- **JavaScript Interop**: `mapInterop.js` - OpenLayers integration
- **Styling**: `map.css` - Map-specific styles

## Features
- Interactive pan and zoom
- Tile-based rendering (32x32 blocks per tile)
- ETag-based caching for optimal performance
- World boundary display showing X/Z ranges
- Error tile for missing chunks (gray placeholder)
- Responsive design using MudBlazor

## Usage

### Accessing the Map
Navigate to: `/world-map` or `/{serverId}/world-map`

### Navigation
- **Pan**: Click and drag the map
- **Zoom**: Use mouse wheel or the +/- controls
- **Map Information**: View world bounds and total chunks above the map

## Technical Details

### Tile Format
- Size: 32×32 pixels (1 pixel = 1 block)
- Format: PNG with transparency support
- Caching: ETag-based HTTP caching

### Performance
- Tiles are cached on both client and server
- Missing tiles return a gray placeholder (no error)
- Map data is loaded asynchronously

### Dependencies
- **OpenLayers**: v10.3.1 (loaded via CDN)
- **MudBlazor**: UI framework
- **Fluxor**: State management (planned for future enhancements)

## Development

### Server Endpoint Testing
Use Bruno collection in `Bruno/granet-server/world/`:
- `GetWorldBounds.bru`
- `GetTileImage.bru`
- `GetTileMetadata.bru`

### Unit Tests
Run server tests:
```bash
dotnet test Granite.Tests/Granite.Tests.csproj --filter "FullyQualifiedName~ServerWorldMapControllerTests"
```

All 19 tests verify:
- Coordinate transformation accuracy
- HTTP response codes and headers
- DTO serialization
- Error handling
- Edge cases

## Future Enhancements
- [ ] Click to view tile details
- [ ] Player position overlays
- [ ] Real-time updates via SignalR
- [ ] Export map to image
- [ ] Configurable tile refresh intervals
- [ ] Multiple zoom levels with different detail
- [ ] Coordinate display on hover
- [ ] Waypoint/marker support
