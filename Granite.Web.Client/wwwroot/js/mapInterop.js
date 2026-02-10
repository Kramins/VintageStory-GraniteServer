window.mapInterop = {
    map: null,
    tileLayer: null,
    tileSource: null,
    extent: null,
    TILE_SIZE: 256,
    objectUrls: new Set(), // Track object URLs for cleanup
    playerMarkers: new Map(), // Track player markers: playerUID -> ol.Overlay

    initializeMap: function (elementId, baseUrl, center, authToken) {
        var self = this;
        const TILE_SIZE = this.TILE_SIZE;
        const CHUNKS_PER_GROUP = 8;

        const SPAWN_CHUNK_X = 15998;
        const SPAWN_CHUNK_Z = 16000;
        const SPAWN_GROUP_X = Math.floor(SPAWN_CHUNK_X / CHUNKS_PER_GROUP);
        const SPAWN_GROUP_Z = Math.floor(SPAWN_CHUNK_Z / CHUNKS_PER_GROUP);

        // Align extent to 256-block (group) boundaries
        // -1000192 = -3907 * 256 (start of group -3907)
        // 1000192 = 3907 * 256 (end of group 3906)
        const extent = [-1000192, -1000192, 1000192, 1000192];
        this.extent = extent;

        const projection = new ol.proj.Projection({
            code: 'VS-PIXEL',
            units: 'pixels',
            extent: extent
        });

        const tileGrid = new ol.tilegrid.TileGrid({
            origin: [extent[0], extent[3]],
            tileSize: TILE_SIZE,
            resolutions: [1] // This is only one resolution as we only have 256 sized map titles.
        });

        const tileSource = new ol.source.TileImage({
            projection: projection,
            tileGrid: tileGrid,
            wrapX: false,
            crossOrigin: 'anonymous',
            tileUrlFunction: function (tileCoord) {
                if (!tileCoord) return null;

                const z = tileCoord[0]; // zoom level
                const tileX = tileCoord[1];
                const tileY = tileCoord[2];

                // Convert OpenLayers tile coordinates to game group coordinates
                // This formula matches the working mapTesting/index.html
                const groupX = tileX + Math.floor(extent[0] / TILE_SIZE);
                const groupZ = tileY - Math.floor(extent[3] / TILE_SIZE);

                // Add cache-busting parameter if tile was invalidated
                const key = self.getTileKey(groupX, groupZ);
                const timestamp = self.invalidatedTiles?.get(key) || '';
                const cacheBuster = timestamp ? `?t=${timestamp}` : '';

                // Calculate what blocks this tile covers
                const tileBlockX = extent[0] + tileX * TILE_SIZE;
                const tileMapY = extent[3] - tileY * TILE_SIZE;
                const tileBlockZ = -tileMapY;
                
                // Calculate expected block range for this group
                const groupBlockX = groupX * TILE_SIZE;
                const groupBlockZ = groupZ * TILE_SIZE;
                
                console.log(`=== TILE DEBUG ===`);
                console.log(`Tile(${tileX}, ${tileY}) -> Group(${groupX}, ${groupZ})`);
                console.log(`Expected group blocks: X[${groupBlockX} to ${groupBlockX + 255}], Z[${groupBlockZ} to ${groupBlockZ + 255}]`);
                console.log(`Map places tile at: map[${tileBlockX}, ${tileMapY}] = block(${tileBlockX}, ${tileBlockZ})`);

                return `${baseUrl}/${groupX}/${groupZ}${cacheBuster}`;
            },
            tileLoadFunction: function (imageTile, src) {
                const img = imageTile.getImage();
                if (src.startsWith('data:')) {
                    img.src = src;
                    return;
                }
                
                fetch(src, {
                    headers: {
                        'Authorization': `Bearer ${authToken}`
                    }
                }).then(resp => {
                    if (!resp.ok) throw new Error(resp.statusText);
                    return resp.blob();
                }).then(blob => {
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
                }).catch(err => {
                    console.error('Tile load failed:', err);
                });
            }
        });

        this.tileSource = tileSource;
        this.invalidatedTiles = new Map(); // Track invalidated tiles with timestamps

        const map = new ol.Map({
            target: elementId,
            layers: [new ol.layer.Tile({ source: tileSource })],
            view: new ol.View({
                projection: projection,
                center: center, // Use the center passed from C# (in chunk coords converted to pixels)
                resolution: 1,
                minResolution: 0.25,
                maxResolution: 4,
                constrainResolution: false,
                enableRotation: false
            })
        });

        this.map = map;
        console.log(`Map initialized with center: [${center[0]}, ${center[1]}]`);
    },
    getTileKey: function(groupX, groupZ) {
        return `${groupX},${groupZ}`;
    },
    updateTileUrl: function (tileUrlTemplate) {
        if (this.tileLayer) {
            const newSource = new ol.source.XYZ({
                url: tileUrlTemplate,
                crossOrigin: 'anonymous',
                tileSize: [32, 32]
            });
            this.tileLayer.setSource(newSource);
            console.log('Tile URL updated:', tileUrlTemplate);
        }
    },

    setCenter: function (blockX, blockZ, zoom) {
        if (this.map) {
            const view = this.map.getView();
            // Convert block coords to map coords: [X, -Z]
            view.setCenter([blockX, -blockZ]);
            if (zoom !== undefined) {
                view.setZoom(zoom);
            }
            console.log(`Map centered at block (${blockX}, ${blockZ}), map coords [${blockX}, ${-blockZ}]`);
        }
    },

    getCenter: function () {
        if (this.map) {
            const view = this.map.getView();
            const center = view.getCenter();
            // Convert map coords back to block coords: [X, -Z] -> (X, Z)
            return { blockX: center[0], blockZ: -center[1], zoom: view.getZoom() };
        }
        return null;
    },
    invalidateTile: function (groupX, groupZ) {
        if (!this.tileSource) return;

        const key = this.getTileKey(groupX, groupZ);
        
        // Add timestamp to force cache busting on next load
        this.invalidatedTiles.set(key, Date.now());

        // Trigger re-render - tiles with timestamps will get new URLs
        this.tileSource.changed();

        console.log(`Invalidated tile ${key}`);
    },

    invalidateTiles: function (tiles) {
        if (!tiles || tiles.length === 0 || !this.tileSource) return;

        const timestamp = Date.now();

        // Mark all tiles as invalidated with timestamp for cache busting
        tiles.forEach(tile => {
            const tileX = tile.tileX ?? tile.TileX;
            const tileZ = tile.tileZ ?? tile.TileZ;
            const key = this.getTileKey(tileX, tileZ);
            
            // Mark as invalidated with timestamp for cache busting
            this.invalidatedTiles.set(key, timestamp);
        });

        // Trigger changed event once - OpenLayers will check visible tiles and reload those with new URLs
        this.tileSource.changed();

        console.log(`Invalidated ${tiles.length} tiles with cache-busting timestamps`);
    },

    dispose: function () {
        if (this.map) {
            // Clean up player markers
            this.playerMarkers.forEach(marker => {
                this.map.removeOverlay(marker);
            });
            this.playerMarkers.clear();
            
            this.map.setTarget(null);
            this.map = null;
            this.tileLayer = null;
            this.tileSource = null;
            
            // Clean up all object URLs to prevent memory leaks
            this.objectUrls.forEach(url => URL.revokeObjectURL(url));
            this.objectUrls.clear();
            
            if (this.invalidatedTiles) {
                this.invalidatedTiles.clear();
            }
            
            console.log('OpenLayers map disposed');
        }
    },

    // Player marker management
    updatePlayerMarker: function (playerUID, blockX, blockZ, playerName) {
        if (!this.map) {
            console.warn('Map not initialized, cannot update player marker');
            return;
        }

        // Convert VS block coordinates to OpenLayers map coordinates
        const mapCoords = [blockX, -blockZ];
        
        // Debug logging
        const view = this.map.getView();
        const currentCenter = view.getCenter();
        const currentZoom = view.getZoom();
        console.log(`=== Player Marker Update ===`);
        console.log(`Player: ${playerName} (${playerUID.substring(0,8)})`);
        console.log(`Block coords (VS): (${blockX}, ${blockZ})`);
        console.log(`Map coords (OL): [${mapCoords[0]}, ${mapCoords[1]}]`);
        console.log(`Current map center: [${currentCenter[0]}, ${currentCenter[1]}]`);
        console.log(`Current zoom: ${currentZoom}`);
        console.log(`Tile group: (${Math.floor(blockX/256)}, ${Math.floor(blockZ/256)})`);

        console.log('=== PLAYER DEBUG ===');
        console.log(`Player ${playerName}: Block(${blockX}, ${blockZ})`);
        console.log(`Map coords: [${blockX}, ${-blockZ}]`);
        console.log(`Expected chunk: (${Math.floor(blockX / 32)}, ${Math.floor(blockZ / 32)})`);
        console.log(`Expected group: (${Math.floor(blockX / 256)}, ${Math.floor(blockZ / 256)})`);
        console.log(`Map center: [${this.map.getView().getCenter()}]`);
        // console.log(`Extent: [${extent}]`);

        let overlay = this.playerMarkers.get(playerUID);
        
        if (!overlay) {
            // Create new marker
            const element = document.createElement('div');
            element.className = 'player-marker';
            element.innerHTML = `
                <div class="player-marker-icon">📍</div>
                <div class="player-marker-label">${playerName}</div>
            `;
            element.title = `${playerName} (${Math.floor(blockX)}, ${Math.floor(blockZ)})`;

            overlay = new ol.Overlay({
                position: mapCoords,
                positioning: 'top-left',  // Position from top-left, CSS handles centering
                element: element,
                stopEvent: false,
                offset: [0, 0]  // No offset - CSS translate handles positioning
            });

            this.map.addOverlay(overlay);
            this.playerMarkers.set(playerUID, overlay);
            console.log(`Added player marker for ${playerName} at (${blockX}, ${blockZ})`);
        } else {
            // Update existing marker position
            overlay.setPosition(mapCoords);
            
            // Update label if name changed
            const element = overlay.getElement();
            const label = element.querySelector('.player-marker-label');
            if (label && label.textContent !== playerName) {
                label.textContent = playerName;
            }
            element.title = `${playerName} (${Math.floor(blockX)}, ${Math.floor(blockZ)})`;
        }
    },

    removePlayerMarker: function (playerUID) {
        if (!this.map) return;

        const overlay = this.playerMarkers.get(playerUID);
        if (overlay) {
            this.map.removeOverlay(overlay);
            this.playerMarkers.delete(playerUID);
            console.log(`Removed player marker for ${playerUID}`);
        }
    },

    clearAllPlayerMarkers: function () {
        if (!this.map) return;

        this.playerMarkers.forEach(overlay => {
            this.map.removeOverlay(overlay);
        });
        this.playerMarkers.clear();
        console.log('Cleared all player markers');
    }
};
