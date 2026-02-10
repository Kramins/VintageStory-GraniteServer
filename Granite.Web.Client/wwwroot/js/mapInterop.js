/**
 * OpenLayers map interop for Vintage Story Granite Server
 * Handles map initialization, tile management, and player marker display
 */
window.mapInterop = {
    // ===== Constants =====
    TILE_SIZE: 256, // Size of each map tile in blocks (one group = 256 blocks)
    BLOCKS_PER_CHUNK: 32,
    CHUNKS_PER_GROUP: 8, // 8 chunks per group = 256 blocks
    
    // World extent aligned to 256-block (group) boundaries
    // Covers groups from -3907 to 3906 in both X and Z
    // -1000192 = -3907 * 256, 1000192 = 3907 * 256
    WORLD_EXTENT: [-1000192, -1000192, 1000192, 1000192],
    
    DEBUG_ENABLED: false, // Set to true to enable verbose logging
    
    // ===== State =====
    map: null,
    tileSource: null,
    extent: null,
    invalidatedTiles: null,
    objectUrls: new Set(), // Track object URLs for cleanup
    playerMarkers: new Map(), // Track player markers: playerUID -> ol.Overlay

    // ===== Coordinate Conversion Helpers =====
    
    /**
     * Convert Vintage Story block coordinates to OpenLayers map coordinates
     * @param {number} blockX - Block X coordinate (east/west)
     * @param {number} blockZ - Block Z coordinate (north/south)
     * @returns {Array<number>} [mapX, mapY] coordinates for OpenLayers
     */
    blockToMapCoords: function(blockX, blockZ) {
        return [blockX, -blockZ];
    },
    
    /**
     * Convert OpenLayers map coordinates to Vintage Story block coordinates
     * @param {Array<number>} mapCoords - [mapX, mapY] from OpenLayers
     * @returns {Object} {blockX, blockZ} in Vintage Story coordinates
     */
    mapToBlockCoords: function(mapCoords) {
        return { blockX: mapCoords[0], blockZ: -mapCoords[1] };
    },
    
    /**
     * Convert OpenLayers tile coordinates to game group coordinates
     * @param {number} tileX - OpenLayers tile X
     * @param {number} tileY - OpenLayers tile Y
     * @param {Array<number>} extent - Map extent [minX, minY, maxX, maxY]
     * @returns {Object} {groupX, groupZ} in game coordinates
     */
    tileToGroupCoords: function(tileX, tileY, extent) {
        const groupX = tileX + Math.floor(extent[0] / this.TILE_SIZE);
        const groupZ = tileY - Math.floor(extent[3] / this.TILE_SIZE);
        return { groupX, groupZ };
    },
    
    /**
     * Create a unique key for tile caching
     * @param {number} groupX - Group X coordinate
     * @param {number} groupZ - Group Z coordinate
     * @returns {string} Unique tile key
     */
    getTileKey: function(groupX, groupZ) {
        return `${groupX},${groupZ}`;
    },

    // ===== Map Initialization =====
    
    /**
     * Initialize the OpenLayers map with Vintage Story tile source
     * @param {string} elementId - DOM element ID for the map container
     * @param {string} baseUrl - Base URL for tile requests
     * @param {Array<number>} center - Initial map center [x, y] in map coordinates
     * @param {string} authToken - Bearer token for authenticated tile requests
     */
    initializeMap: function (elementId, baseUrl, center, authToken) {
        const self = this;
        const extent = this.WORLD_EXTENT;
        this.extent = extent;
        this.invalidatedTiles = new Map();

        const projection = new ol.proj.Projection({
            code: 'VS-PIXEL',
            units: 'pixels',
            extent: extent
        });

        const tileGrid = new ol.tilegrid.TileGrid({
            origin: [extent[0], extent[3]],
            tileSize: this.TILE_SIZE,
            resolutions: [1] // Single resolution - tiles are 256x256 blocks
        });

        const tileSource = this._createTileSource(projection, tileGrid, extent, baseUrl, authToken);
        this.tileSource = tileSource;

        const map = new ol.Map({
            target: elementId,
            layers: [new ol.layer.Tile({ source: tileSource })],
            view: new ol.View({
                projection: projection,
                center: center,
                resolution: 1,
                minResolution: 0.25,
                maxResolution: 4,
                constrainResolution: false,
                enableRotation: false
            })
        });

        this.map = map;
        
        if (this.DEBUG_ENABLED) {
            console.log(`Map initialized with center: [${center[0]}, ${center[1]}]`);
        }
    },
    
    /**
     * Create the tile source for loading map tiles
     * @private
     */
    _createTileSource: function(projection, tileGrid, extent, baseUrl, authToken) {
        const self = this;
        
        return new ol.source.TileImage({
            projection: projection,
            tileGrid: tileGrid,
            wrapX: false,
            crossOrigin: 'anonymous',
            tileUrlFunction: function (tileCoord) {
                if (!tileCoord) return null;

                const tileX = tileCoord[1];
                const tileY = tileCoord[2];

                // Convert OpenLayers tile coordinates to game group coordinates
                const { groupX, groupZ } = self.tileToGroupCoords(tileX, tileY, extent);

                // Add cache-busting parameter if tile was invalidated
                const key = self.getTileKey(groupX, groupZ);
                const timestamp = self.invalidatedTiles?.get(key) || '';
                const cacheBuster = timestamp ? `?t=${timestamp}` : '';
                
                if (self.DEBUG_ENABLED) {
                    console.log(`Tile(${tileX}, ${tileY}) -> Group(${groupX}, ${groupZ})`);
                }

                return `${baseUrl}/${groupX}/${groupZ}${cacheBuster}`;
            },
            tileLoadFunction: function (imageTile, src) {
                self._loadTileWithAuth(imageTile, src, authToken);
            }
        });
    },
    
    /**
     * Load a tile image with authentication
     * @private
     */
    _loadTileWithAuth: function(imageTile, src, authToken) {
        const self = this;
        const img = imageTile.getImage();
        
        if (src.startsWith('data:')) {
            img.src = src;
            return;
        }
        
        fetch(src, {
            headers: { 'Authorization': `Bearer ${authToken}` }
        })
        .then(resp => {
            if (!resp.ok) throw new Error(resp.statusText);
            return resp.blob();
        })
        .then(blob => {
            const objectUrl = URL.createObjectURL(blob);
            self.objectUrls.add(objectUrl);
            img.src = objectUrl;
            
            // Clean up old object URL when image loads
            img.onload = function() {
                if (img.previousObjectUrl && img.previousObjectUrl !== objectUrl) {
                    URL.revokeObjectURL(img.previousObjectUrl);
                    self.objectUrls.delete(img.previousObjectUrl);
                }
                img.previousObjectUrl = objectUrl;
            };
        })
        .catch(err => {
            console.error('Tile load failed:', err);
        });
    },
    
    // ===== Map View Control =====
    
    /**
     * Set the map center to a specific block location
     * @param {number} blockX - Block X coordinate
     * @param {number} blockZ - Block Z coordinate
     * @param {number} [zoom] - Optional zoom level
     */
    setCenter: function (blockX, blockZ, zoom) {
        if (!this.map) return;
        
        const view = this.map.getView();
        const mapCoords = this.blockToMapCoords(blockX, blockZ);
        view.setCenter(mapCoords);
        
        if (zoom !== undefined) {
            view.setZoom(zoom);
        }
        
        if (this.DEBUG_ENABLED) {
            console.log(`Map centered at block (${blockX}, ${blockZ})`);
        }
    },
    
    /**
     * Get the current map center in block coordinates
     * @returns {Object|null} {blockX, blockZ, zoom} or null if map not initialized
     */
    getCenter: function () {
        if (!this.map) return null;
        
        const view = this.map.getView();
        const center = view.getCenter();
        const blockCoords = this.mapToBlockCoords(center);
        
        return { ...blockCoords, zoom: view.getZoom() };
    },
    
    // ===== Tile Cache Management =====
    
    /**
     * Invalidate a single tile to force reload on next render
     * @param {number} groupX - Group X coordinate
     * @param {number} groupZ - Group Z coordinate
     */
    invalidateTile: function (groupX, groupZ) {
        if (!this.tileSource) return;

        const key = this.getTileKey(groupX, groupZ);
        this.invalidatedTiles.set(key, Date.now());
        this.tileSource.changed();

        if (this.DEBUG_ENABLED) {
            console.log(`Invalidated tile ${key}`);
        }
    },
    
    /**
     * Invalidate multiple tiles to force reload
     * @param {Array<Object>} tiles - Array of tile objects with tileX/TileX and tileZ/TileZ properties
     */
    invalidateTiles: function (tiles) {
        if (!tiles || tiles.length === 0 || !this.tileSource) return;

        const timestamp = Date.now();

        // Mark all tiles as invalidated with timestamp for cache busting
        tiles.forEach(tile => {
            // Handle both camelCase and PascalCase property names from C#
            const tileX = tile.tileX ?? tile.TileX;
            const tileZ = tile.tileZ ?? tile.TileZ;
            const key = this.getTileKey(tileX, tileZ);
            this.invalidatedTiles.set(key, timestamp);
        });

        // Trigger changed event once - OpenLayers will reload visible tiles with new cache-busted URLs
        this.tileSource.changed();

        if (this.DEBUG_ENABLED) {
            console.log(`Invalidated ${tiles.length} tiles`);
        }
    },
    
    // ===== Lifecycle Management =====
    
    /**
     * Dispose of the map and clean up all resources
     */
    dispose: function () {
        if (!this.map) return;
        
        // Clean up player markers
        this.clearAllPlayerMarkers();
        
        // Dispose map
        this.map.setTarget(null);
        this.map = null;
        this.tileSource = null;
        
        // Clean up all object URLs to prevent memory leaks
        this.objectUrls.forEach(url => URL.revokeObjectURL(url));
        this.objectUrls.clear();
        
        // Clean up tile cache
        if (this.invalidatedTiles) {
            this.invalidatedTiles.clear();
        }
        
        if (this.DEBUG_ENABLED) {
            console.log('OpenLayers map disposed');
        }
    },

    // ===== Player Marker Management =====
    
    /**
     * Update or create a player marker at the specified location
     * @param {string} playerUID - Unique player identifier
     * @param {number} blockX - Block X coordinate
     * @param {number} blockZ - Block Z coordinate
     * @param {string} playerName - Display name for the player
     */
    updatePlayerMarker: function (playerUID, blockX, blockZ, playerName) {
        if (!this.map) {
            console.warn('Map not initialized, cannot update player marker');
            return;
        }

        const mapCoords = this.blockToMapCoords(blockX, blockZ);
        
        if (this.DEBUG_ENABLED) {
            console.log(`Player ${playerName}: Block(${blockX}, ${blockZ}) Group(${Math.floor(blockX/this.TILE_SIZE)}, ${Math.floor(blockZ/this.TILE_SIZE)})`);
        }

        let overlay = this.playerMarkers.get(playerUID);
        
        if (!overlay) {
            overlay = this._createPlayerMarkerOverlay(playerUID, mapCoords, blockX, blockZ, playerName);
            if (this.DEBUG_ENABLED) {
                console.log(`Added player marker for ${playerName}`);
            }
        } else {
            this._updatePlayerMarkerOverlay(overlay, mapCoords, blockX, blockZ, playerName);
        }
    },
    
    /**
     * Create a new player marker overlay
     * @private
     */
    _createPlayerMarkerOverlay: function(playerUID, mapCoords, blockX, blockZ, playerName) {
        const element = document.createElement('div');
        element.className = 'player-marker';
        element.innerHTML = `
            <div class="player-marker-icon">📍</div>
            <div class="player-marker-label">${playerName}</div>
        `;
        element.title = `${playerName} (${Math.floor(blockX)}, ${Math.floor(blockZ)})`;

        const overlay = new ol.Overlay({
            position: mapCoords,
            positioning: 'top-left',
            element: element,
            stopEvent: false,
            offset: [0, 0]
        });

        this.map.addOverlay(overlay);
        this.playerMarkers.set(playerUID, overlay);
        
        return overlay;
    },
    
    /**
     * Update an existing player marker overlay
     * @private
     */
    _updatePlayerMarkerOverlay: function(overlay, mapCoords, blockX, blockZ, playerName) {
        overlay.setPosition(mapCoords);
        
        const element = overlay.getElement();
        const label = element.querySelector('.player-marker-label');
        if (label && label.textContent !== playerName) {
            label.textContent = playerName;
        }
        element.title = `${playerName} (${Math.floor(blockX)}, ${Math.floor(blockZ)})`;
    },
    
    /**
     * Remove a player marker from the map
     * @param {string} playerUID - Unique player identifier
     */
    removePlayerMarker: function (playerUID) {
        if (!this.map) return;

        const overlay = this.playerMarkers.get(playerUID);
        if (overlay) {
            this.map.removeOverlay(overlay);
            this.playerMarkers.delete(playerUID);
            
            if (this.DEBUG_ENABLED) {
                console.log(`Removed player marker for ${playerUID}`);
            }
        }
    },
    
    /**
     * Remove all player markers from the map
     */
    clearAllPlayerMarkers: function () {
        if (!this.map) return;

        this.playerMarkers.forEach(overlay => {
            this.map.removeOverlay(overlay);
        });
        this.playerMarkers.clear();
        
        if (this.DEBUG_ENABLED) {
            console.log('Cleared all player markers');
        }
    }
};
