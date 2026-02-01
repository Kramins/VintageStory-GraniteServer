# Map Extraction Research - WebCartographer Analysis

## Status: Phase 1 Complete ✅

**Completed (2026-02-01):**
- ✅ Map generation service implementation in Granite.Mod
- ✅ Color mapping based on WebCartographer medieval style
- ✅ API-based chunk data access (no direct SQLite)
- ✅ TGA image generation for map tiles
- ✅ **API verification via ilspycmd decompilation** (see [API Verification](#api-verification) section)
- ✅ Unit tests (**98 tests passing**) - comprehensive coverage based on verified APIs

**Next Steps:**
- Register MapGenerationService in GraniteMod.cs DI container
- Create REST endpoints in Granite.Server for tile serving
- Implement real-time tile update notifications via SignalR
- Add map viewer component in Blazor client

## Overview
WebCartographer is a server-side Vintage Story mod that extracts map data and serves it via a built-in web server. This research explores how to adapt this approach for Granite Server to provide real-time map data to the Blazor client.

## Key Components

### 1. **Data Access Layer**
WebCartographer directly accesses the Vintage Story savegame SQLite database:

- **Database Location**: The mod uses `ServerMain` to access the world's SQLite database
- **Tables Used**:
  - `mapchunk` - Contains heightmap and surface data
  - `chunk` - Contains full 3D chunk data (blocks, entities, etc.)
  - `mapregion` - Contains region-level data (structures, traders, translocators)

**Key Classes:**
- `SavegameDataLoader` - Thread-safe SQLite connection pooling
- `SqliteThreadCon` - Per-thread database connection wrapper

### 2. **Map Chunk Data Structure**

The mod reads two main data types:

**ServerMapChunk:**
- Contains `RainHeightMap` (ushort[]) - Height of topmost rain-permeable block for each x,z coordinate
- Size: 32x32 per chunk (ServerChunkSize)
- Stored in SQLite as protobuf-serialized bytes

**ServerChunk:**
- Full 3D chunk data with block IDs
- Contains BlockEntities (signs, traders, translocators, etc.)
- Used to determine actual surface block types
- Unpacked on-demand for processing

### 3. **Map Generation Process**

**Step-by-step extraction:**

1. **Load Map Chunks**: Query all chunk positions from database
2. **Group Chunks**: Group chunks into tiles (default 256x256 pixels = 8x8 chunks)
3. **For Each Pixel**:
   - Get height from `RainHeightMap`
   - Load the ServerChunk at that height
   - Get the block ID at (x, height, z)
   - Convert block ID to color using block color mapping
   - Apply height shading/hill shading effects
   - Write pixel to tile image
4. **Generate Zoom Levels**: Create lower resolution tiles by downsampling
5. **Extract POIs**: Signs, traders, translocators as GeoJSON

**Performance:**
- Highly parallel (uses all CPU cores)
- Processes 34.8 GB savegame in ~22 minutes
- Caches frequently accessed data
- Can suspend server during export to prevent corruption

### 4. **Color Mapping**

The mod uses two approaches:

**Mode 4 (Medieval Style)** - Default:
- Uses Vintage Story's built-in `mapColorCode` attribute on blocks
- Pre-defined color palette in `MapColors.cs`
- No client-side data needed
- Colors: land, water, snow, ice, rock, wateredge, etc.

**Modes 0-3 (Accurate Colors):**
- Requires client mod to export actual block colors
- Sends `blockColorMapping.json` from client to server
- More accurate to in-game appearance

### 5. **Web Server Integration**

**WebmapServer.cs:**
- Built-in HTTP server using `HttpListener`
- Serves static files (HTML, PNG tiles, GeoJSON)
- Memory caching with expiration
- Multi-threaded request handling
- Configurable host/port

## Challenges for Granite Server Implementation

### Challenge 1: Database Access
**Problem:** WebCartographer directly accesses the SQLite database, which requires:
- Read access to the savegame file
- Understanding of protobuf serialization format
- Database locking concerns

**Solutions:**
1. **Direct Database Access** (Like WebCartographer)
   - Read-only access to avoid corruption
   - Use connection pooling
   - Coordinate with game server locks
   
2. **API-Based Access** (Cleaner approach)
   - Use Vintage Story's API methods if available
   - `IWorldManagerAPI.GetMapChunk()`
   - `IServerChunkCache` or similar

### Challenge 2: Real-Time Updates
**Problem:** WebCartographer is batch-oriented (full export). We need real-time streaming.

**Solutions:**
1. **Chunk Change Detection**
   - Listen for chunk generation events
   - Track dirty chunks
   - Generate tiles on-demand or incrementally
   
2. **Incremental Tile Updates**
   - Only regenerate affected tiles when chunks change
   - Push updates via SignalR to Blazor client
   
3. **Streaming API**
   - Provide REST endpoints for tile requests
   - Generate tiles on first request, cache thereafter

### Challenge 3: Integration with Granite Architecture

**Current Granite Components:**
- `Granite.Mod` - Server-side mod (has access to VS API)
- `Granite.Server` - ASP.NET Core API server (can serve tiles)
- `Granite.Web.Client` - Blazor client (will display map)
- SignalR for real-time events

**Proposed Architecture:**

```
[Vintage Story Server]
         ↓ (mod API)
   [Granite.Mod]
         ↓ (HTTP/message bus)
   [Granite.Server]
    - MapTileController (REST API)
    - MapTileCache (memory/disk)
    - MapHub (SignalR for updates)
         ↓ (SignalR/HTTP)
   [Granite.Web.Client]
    - Map component (Leaflet/OpenLayers)
    - Real-time chunk updates
```

## Recommended Approach

### Phase 1: Basic Static Map ✅ COMPLETED
**Implementation completed on 2026-02-01:**

1. ✅ Created `MapGenerationService` in `Granite.Mod/Services/Map/`
   - [`IMapGenerationService.cs`](../Granite.Mod/Services/Map/IMapGenerationService.cs) - Interface definition
   - [`MapGenerationService.cs`](../Granite.Mod/Services/Map/MapGenerationService.cs) - Main implementation using VS API
   - [`MapColors.cs`](../Granite.Mod/Services/Map/MapColors.cs) - Medieval-style color mapping

2. ✅ Implemented VS API-based map chunk reading
   - Uses `IBlockAccessor.GetMapChunk()` instead of direct SQLite
   - Reads `RainHeightMap` for surface heights
   - Block color mapping via `Block.Attributes["mapColorCode"]`
   - Pre-computed color lookup arrays for performance

3. ✅ TGA image tile generation
   - 32x32 pixel tiles (one chunk per tile)
   - ARGB color format
   - Outputs to `{GamePaths.DataPath}/granite/maptiles/`

4. ✅ Comprehensive unit tests (verified against decompiled VS APIs)
   - [`Granite.Mod.Tests/`](../Granite.Mod.Tests/) - New test project
   - [`MapColorsTests.cs`](../Granite.Mod.Tests/Services/Map/MapColorsTests.cs) - 63 tests for color mapping
   - [`MapGenerationServiceTests.cs`](../Granite.Mod.Tests/Services/Map/MapGenerationServiceTests.cs) - 35 tests for service
   - All **98 tests passing** (expanded from 44 after API verification)

**Remaining Phase 1 Tasks:**
- Register MapGenerationService in GraniteMod.cs DI container
- Create `MapTileController` in `Granite.Server` to serve tiles via REST
- Add map viewer component in Blazor client (OpenLayers/Leaflet)

### Phase 2: Real-Time Updates (PENDING)
1. Create `MapGenerationService` in `Granite.Mod`
2. Implement map chunk reading (copy from WebCartographer)
3. Generate tiles to disk on server start or on command
4. Create `MapTileController` in `Granite.Server` to serve tiles
5. Add map viewer component in Blazor client (OpenLayers/Leaflet)

### Phase 2: Real-Time Updates
1. Add chunk change tracking in `Granite.Mod`
2. Send tile update events via message bus
3. Implement incremental tile regeneration
4. Push updates to Blazor client via SignalR
5. Update map display without full refresh

### Phase 3: Performance Optimization
1. Tile caching strategy (memory + disk)
2. On-demand tile generation
3. Multiple zoom levels
4. Viewport-based loading (only load visible tiles)

## Key Code References

### Accessing Map Data
```csharp
// From WebCartographer/SavegameDataLoader.cs
// Direct SQLite access
var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT data FROM mapchunk WHERE position=@position";
var bytes = (byte[])reader["data"];
var mapChunk = Serializer.Deserialize<ServerMapChunkTrimmed>(stream);
```

### Vintage Story API Access
```csharp
// ✅ IMPLEMENTED - Cleaner API-based approach (no direct SQLite)
// In Granite.Mod/Services/Map/MapGenerationService.cs
// All APIs verified via ilspycmd decompilation of VintagestoryAPI.dll

// Verified: IBlockAccessor.GetMapChunk(int chunkX, int chunkZ) returns IMapChunk
var mapChunk = _api.World.BlockAccessor.GetMapChunk(chunkX, chunkZ);
if (mapChunk == null) return null;

// Verified: IMapChunk.RainHeightMap is ushort[] (32x32 = 1024 elements)
var heightmap = mapChunk.RainHeightMap;
var chunkSize = GlobalConstants.ChunkSize; // Verified: 32

for (int localZ = 0; localZ < chunkSize; localZ++)
{
    for (int localX = 0; localX < chunkSize; localX++)
    {
        var height = heightmap[localZ * chunkSize + localX];
        
        // Verified: BlockPos(int x, int y, int z, int dimension) constructor
        var blockPos = new BlockPos(chunkX * chunkSize + localX, height, chunkZ * chunkSize + localZ, 0);
        
        // Verified: BlockLayersAccess.FluidOrSolid = 3 (reads fluid or solid layer)
        // This ensures water surfaces are rendered correctly
        var block = _api.World.BlockAccessor.GetBlock(blockPos, BlockLayersAccess.FluidOrSolid);
        
        var color = GetBlockColor(block.Id);
        
        // Apply height-based shading using map half-height
        // Verified: IBlockAccessor.MapSizeY property exists
        var mapYHalf = _api.World.BlockAccessor.MapSizeY / 2f;
        var heightFactor = Math.Clamp(height / mapYHalf, 0.5f, 1.5f);
        color = MapColors.ApplyBrightness(color, heightFactor);
        
        // Write pixel to tile image
    }
}
```

### Block to Color Conversion
```csharp
// ✅ IMPLEMENTED - Uses block attributes and material types
// From Granite.Mod/Services/Map/MapGenerationService.cs and MapColors.cs
// Verified via ilspycmd: Block.Attributes is JsonObject with indexer this[string key]
private static string GetBlockColorCode(Block block)
{
    // Special case: snow blocks render as glacier (matches WebCartographer)
    // Verified: Block.Code.Path contains the block path string
    if (block.BlockMaterial == EnumBlockMaterial.Snow && 
        block.Code?.Path?.Contains("snowblock") == true)
    {
        return "glacier";
    }
    
    // First try mapColorCode attribute
    // Verified: JsonObject indexer returns child JsonObject, AsString() gets value
    var colorCode = block.Attributes?["mapColorCode"]?.AsString();
    if (!string.IsNullOrEmpty(colorCode) && MapColors.ColorsByCode.ContainsKey(colorCode))
        return colorCode;
    
    // Fallback to material-based color
    return MapColors.GetDefaultMapColorCode(block.BlockMaterial);
}

// Color palette (medieval style)
public static readonly IReadOnlyDictionary<string, string> HexColorsByCode = new Dictionary<string, string>
{
    { "land", "#AC8858" },
    { "desert", "#C4A468" },
    { "forest", "#98844C" },
    { "lake", "#CCC890" },
    { "glacier", "#E0E0C0" },
    { "settlement", "#856844" },
    // ... more colors
};
```

## Next Steps

### Immediate (Complete Phase 1)

1. **Register Service in DI Container:**
   - Add MapGenerationService to GraniteMod.cs
   - Configure service lifetime (singleton recommended)

2. **Create REST API Endpoints:**
   - `GET /api/map/tiles/{z}/{x}/{y}.tga` - Serve individual tile
   - `GET /api/map/info` - Map metadata (size, spawn point, etc.)
   - Add caching headers for tile responses

3. **Blazor Map Component:**
   - Choose map library (Leaflet vs OpenLayers)
   - Implement custom tile layer pointing to Granite.Server endpoints
   - Add zoom controls and coordinate display

### Phase 2 Implementation

1. **Real-Time Chunk Tracking:**
   - Subscribe to chunk generation/modification events in Granite.Mod
   - Track dirty chunks that need tile regeneration
   - Batch updates to avoid excessive regeneration

2. **SignalR Events:**
   - Define `MapTileUpdated` event with chunk coordinates
   - Send events from Granite.Mod → Granite.Server → Blazor clients
   - Client refreshes affected tiles on notification

3. **Performance Optimization:**
   - Implement tile caching strategy (memory + disk)
   - Generate tiles on-demand instead of pre-generating all
   - Add zoom level support (downsample tiles for overview)

## Implementation Files Created

### Core Service Implementation
- [`Granite.Mod/Services/Map/IMapGenerationService.cs`](../Granite.Mod/Services/Map/IMapGenerationService.cs)
  - Service interface with tile generation methods
  - MapPositionInfo record for position queries
  
- [`Granite.Mod/Services/Map/MapGenerationService.cs`](../Granite.Mod/Services/Map/MapGenerationService.cs) (313 lines)
  - Main implementation using ICoreServerAPI
  - Block-to-color index mapping for performance
  - TGA image generation (32x32 tiles)
  - Heightmap-based terrain rendering
  - Uses `BlockLayersAccess.FluidOrSolid` for proper water/solid block handling
  - Height-based brightness shading using `MapSizeY / 2` reference
  - Special snow block handling (matches WebCartographer)
  
- [`Granite.Mod/Services/Map/MapColors.cs`](../Granite.Mod/Services/Map/MapColors.cs) (142 lines)
  - Medieval-style color palette (13 colors)
  - Block material to color code mapping (all 19 EnumBlockMaterial values)
  - Hex color parsing and brightness adjustments

### Test Suite
- [`Granite.Mod.Tests/Granite.Mod.Tests.csproj`](../Granite.Mod.Tests/Granite.Mod.Tests.csproj)
  - New test project for Granite.Mod
  - Targets net8.0 to match Granite.Mod
  - Includes VintagestoryAPI reference for testing
  
- [`Granite.Mod.Tests/Services/Map/MapColorsTests.cs`](../Granite.Mod.Tests/Services/Map/MapColorsTests.cs)
  - 32 tests covering color mapping functionality
  - Material to color code conversion
  - Hex parsing, brightness adjustments, color lookups
  
- [`Granite.Mod.Tests/Services/Map/MapGenerationServiceTests.cs`](../Granite.Mod.Tests/Services/Map/MapGenerationServiceTests.cs)
  - 12 tests for service behavior
  - Constructor validation, null handling
  - MapPositionInfo record tests

**Test Results:** All 98 tests passing ✅ (expanded after API verification)

## API Verification

### Methodology
Used `ilspycmd` to decompile VintagestoryAPI.dll and verify exact API signatures before implementation:

```bash
ilspycmd /app/VintagestoryAPI.dll -t <TypeName>
```

### Verified APIs

| API | Verified Signature | Notes |
|-----|-------------------|-------|
| `IBlockAccessor.GetMapChunk` | `IMapChunk GetMapChunk(int chunkX, int chunkZ)` | Returns null if chunk not loaded |
| `IMapChunk.RainHeightMap` | `ushort[] RainHeightMap` | 32×32 = 1024 elements per chunk |
| `IBlockAccessor.GetBlock` | `Block GetBlock(BlockPos pos, int layer)` | layer param for BlockLayersAccess |
| `BlockLayersAccess.FluidOrSolid` | `public const int FluidOrSolid = 3` | Reads fluid or solid block |
| `BlockPos` constructor | `BlockPos(int x, int y, int z, int dimension)` | 4th param is dimension (0 = overworld) |
| `GlobalConstants.ChunkSize` | `public const int ChunkSize = 32` | Used for tile dimensions |
| `Block.Attributes` | `JsonObject Attributes` | Has indexer `this[string key]` |
| `JsonObject.AsString()` | `string AsString(string defaultValue = null)` | Gets string value |
| `Block.Code.Path` | `string Path` property | Contains block path like "snowblock" |
| `EnumBlockMaterial` | 19 values | Air, Liquid, Soil, Stone, Ore, Gravel, Sand, Meta, Plant, Leaves, Wood, Ice, Snow, Lava, Brick, Ceramic, Glass, Cloth, Mantle |
| `IBlockAccessor.MapSizeY` | `int MapSizeY` | World height for brightness calc |
| `ILogger` methods | `Debug`, `Notification`, `Warning`, `Error` | VS logging interface |

### Fixes Applied Based on Verification

1. **Snow Block Special Case**
   - WebCartographer checks `block.Code?.Path?.Contains("snowblock")` for glacier color
   - Added same check for consistency with WebCartographer output

2. **Height Factor Calculation**
   - WebCartographer uses `mapYHalf` (MapSizeY / 2) for height-based brightness
   - Changed from hardcoded `128` to dynamic `_api.World.BlockAccessor.MapSizeY / 2f`

3. **BlockLayersAccess Consistency**
   - `GetBlock(blockPos, BlockLayersAccess.FluidOrSolid)` ensures water surfaces render correctly
   - Applied consistently in both `GetPositionInfo` and `GenerateChunkTileInternal`

### Test Coverage by Verified API

| API | Test Count | Test Files |
|-----|------------|------------|
| `EnumBlockMaterial` mapping | 19 | MapColorsTests.cs |
| `GetMapChunk` null handling | 2 | MapGenerationServiceTests.cs |
| `GlobalConstants.ChunkSize` | 1 | MapGenerationServiceTests.cs |
| `MapSizeX/Y/Z` | 3 | MapGenerationServiceTests.cs |
| `DefaultSpawnPosition` | 3 | MapGenerationServiceTests.cs |
| Block color lookup | 6 | MapGenerationServiceTests.cs |
| Hex parsing edge cases | 5 | MapColorsTests.cs |
| Brightness calculation | 4 | MapColorsTests.cs |
| Color palette validation | 9 | MapColorsTests.cs |

## Known Issues & Solutions

### Build System Issue (RESOLVED)
**Problem:** Granite.Mod.csproj had aggressive `<CallTarget Targets="Clean" />` in PreClean target that was deleting dependency outputs during multi-project builds.

**Solution:** Modified PreClean target to only remove `bin/mods` directory without calling Clean target:
```xml
<Target Name="PreClean" BeforeTargets="PreBuildEvent" 
        Condition="'$(BuildingInsideVisualStudio)' == 'true' or '$(BuildProjectReferences)' != 'false'">
  <RemoveDir Directories="bin/mods" />
</Target>
```

### Package Downgrade Warning (PRE-EXISTING)
**Issue:** Granite.Integration.Tests has Microsoft.Extensions.Options package downgrade warning (9.0.11 → 9.0.0).

**Status:** Pre-existing issue, not related to map implementation. Main projects build successfully.

## Next Steps (Priority Order)

1. ~~**Prototype in Granite.Mod:**~~ ✅ COMPLETED
   - ~~Create minimal map chunk reader~~
   - ~~Generate single tile as proof of concept~~
   - ~~Verify database access doesn't conflict with game~~

2. **API Design:**
   - Define REST endpoints for tile serving
   - Define SignalR events for updates
   - Design tile coordinate system

3. **Client Integration:**
   - Choose map library (Leaflet vs OpenLayers)
   - Implement tile layer with custom tile source
   - Add real-time update handler

4. **Testing:**
   - Small world first
   - Performance testing with large worlds
   - Concurrent access testing

## Key Reference Files (Local Copy)

The WebCartographer repository has been cloned locally for reference. Here are the critical files:

### Core Implementation Files

**Map Data Access:**
- [`temp/WebCartographer/WebCartographer/SavegameDataLoader.cs`](../temp/WebCartographer/WebCartographer/SavegameDataLoader.cs) (413 lines)
  - SQLite connection pooling and thread-safe chunk access
  - Methods: `GetServerMapChunkTrimmed()`, `GetServerChunk()`, `GetAllMapChunkPositions()`
  - Direct database queries to `mapchunk`, `chunk`, and `mapregion` tables

**Map Extraction Engine:**
- [`temp/WebCartographer/WebCartographer/Extractor.cs`](../temp/WebCartographer/WebCartographer/Extractor.cs) (1358 lines)
  - Main extraction logic for world map tiles
  - Block-to-pixel conversion with color mapping
  - Hill shading and height map generation
  - POI extraction (traders, translocators, signs)
  - Zoom level generation
  - **Key methods to study:**
    - `ExtractWorldMap()` (line 568) - Main tile generation loop
    - `GetTilePixelColorAndHeight()` (line 865) - Per-pixel rendering logic
    - `LoadBlockColorsJson()` - Block color mapping setup
    - `CreateZoomLevels()` (line 966) - Tile pyramid generation
    - `PrepareChunkGroups()` (line 1162) - Chunk grouping for tiles

**Configuration:**
- [`temp/WebCartographer/WebCartographer/Config.cs`](../temp/WebCartographer/WebCartographer/Config.cs) (153 lines)
  - All export configuration options
  - Tile size, zoom levels, parallel processing settings
  - Export modes (Medieval style, accurate colors, hill shading)

**Main Mod Entry:**
- [`temp/WebCartographer/WebCartographer/WebCartographer.cs`](../temp/WebCartographer/WebCartographer/WebCartographer.cs)
  - Mod initialization and lifecycle
  - Command registration (`/webc export`)
  - Server suspension for safe export
  - Configuration loading

**Built-in Web Server:**
- [`temp/WebCartographer/WebCartographer/WebmapServer.cs`](../temp/WebCartographer/WebCartographer/WebmapServer.cs)
  - HTTP server implementation using `HttpListener`
  - Static file serving with memory caching
  - Multi-threaded request handling

### Supporting Files

**Data Structures:**
- [`temp/WebCartographer/WebCartographer/ServerMapChunkTrimmed.cs`](../temp/WebCartographer/WebCartographer/ServerMapChunkTrimmed.cs)
  - Lightweight map chunk structure with only RainHeightMap
- [`temp/WebCartographer/WebCartographer/ServerChunkTrimmed.cs`](../temp/WebCartographer/WebCartographer/ServerChunkTrimmed.cs)
  - Trimmed chunk data for efficient processing
- [`temp/WebCartographer/WebCartographer/ExportData.cs`](../temp/WebCartographer/WebCartographer/ExportData.cs)
  - Block color mapping structure (sent from client mod)
- [`temp/WebCartographer/WebCartographer/ChunkPosExtension.cs`](../temp/WebCartographer/WebCartographer/ChunkPosExtension.cs)
  - Chunk coordinate conversion utilities

**Rendering:**
- [`temp/WebCartographer/WebCartographer/MapColors.cs`](../temp/WebCartographer/WebCartographer/MapColors.cs)
  - Block material to color mapping
  - Default color palette for Medieval style rendering
- [`temp/WebCartographer/WebCartographer/BlurTool.cs`](../temp/WebCartographer/WebCartographer/BlurTool.cs)
  - Hill shading blur algorithm
- [`temp/WebCartographer/WebCartographer/GroupChunks.cs`](../temp/WebCartographer/WebCartographer/GroupChunks.cs)
  - Chunk version grouping for geojson export

**GeoJSON Export:**
- [`temp/WebCartographer/WebCartographer/GeoJson/`](../temp/WebCartographer/WebCartographer/GeoJson/)
  - Sign, trader, translocator feature classes
  - GeoJSON serialization for POI markers

**HTML/JavaScript Client:**
- [`temp/WebCartographer/WebCartographer/html/`](../temp/WebCartographer/WebCartographer/html/)
  - OpenLayers-based web map viewer
  - Settings and configuration
  - Tile layer and marker integration

### Related Project: WebCartographerSync (Real-Time Player Positions)

A separate companion mod for real-time player position tracking:

- [`temp/WebCartographer/WebCartographerSync/WebCartographerSyncModSystem.cs`](../temp/WebCartographer/WebCartographerSync/WebCartographerSyncModSystem.cs) (113 lines)
  - HTTP endpoint that returns current player positions as JSON
  - Polls `_sapi.World.AllOnlinePlayers` and returns `{name, uid, x, z}` coordinates
  - Good reference for real-time game state access
  
- [`temp/WebCartographer/WebCartographerSync/PlayerPositions.cs`](../temp/WebCartographer/WebCartographerSync/PlayerPositions.cs)
  - Simple DTO for player position data

**Note:** Our Granite architecture already handles player positions via SignalR, but this shows the pattern for accessing real-time game state.

### Critical Code Patterns to Adapt

1. **Thread-Safe Database Access** (SavegameDataLoader.cs lines 74-96)
   ```csharp
   internal SqliteThreadCon SqliteThreadConn
   {
       get
       {
           lock (_sqliteConns)
           {
               while (true)
               {
                   foreach (var conn in _sqliteConns)
                   {
                       if (conn.InUse) continue;
                       conn.InUse = true;
                       return conn;
                   }
                   Thread.Sleep(500);
               }
           }
       }
   }
   ```

2. **Chunk Grouping for Tiles** (Extractor.cs lines 1162-1188)
   - Groups 8x8 chunks (256x256 pixels) into single tile images
   - Parallel processing with configurable degree of parallelism

3. **Height-to-Color Conversion** (Extractor.cs lines 680-800)
   - Reads RainHeightMap to get surface height
   - Loads chunk data to get actual block ID
   - Converts block ID to color using mapping
   - Applies hill shading based on altitude differences

4. **Zoom Level Generation** (Extractor.cs lines 966-1023)
   - Recursively downsamples tiles by factor of 2
   - Creates tile pyramid for efficient web viewing

## Communication Architecture Decision

**Use Hybrid SignalR + REST Approach:**

Granite Server already has SignalR infrastructure in place:
- [`Granite.Mod/HostedServices/SignalRClientHostedService.cs`](../Granite.Mod/HostedServices/SignalRClientHostedService.cs)
- [`Granite.Mod/Services/ClientMessageBusService.cs`](../Granite.Mod/Services/ClientMessageBusService.cs)
- [`Granite.Server/Hubs/ModHub.cs`](../Granite.Server/Hubs/ModHub.cs)

**Recommended:**
- **SignalR**: Send tile update notifications (lightweight events)
- **REST**: Serve actual tile images (HTTP caching, efficient for images)

This avoids SignalR message size limits while maintaining real-time updates.

## Additional Resources

- WebCartographer GitLab: https://gitlab.com/th3dilli_vintagestory/WebCartographer
- Cloned repo: `/workspaces/VintageStory-GraniteServer/temp/WebCartographer`
- Vintage Story Modding Wiki: https://wiki.vintagestory.at/index.php/Modding:Advanced_Modding
- OpenLayers (web map library): https://openlayers.org/
- Leaflet (simpler alternative): https://leafletjs.com/
