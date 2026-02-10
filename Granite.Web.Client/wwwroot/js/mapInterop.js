window.mapInterop = {
    map: null,
    tileLayer: null,
    tileSource: null,
    extent: null,
    TILE_SIZE: 256,
    objectUrls: new Set(), // Track object URLs for cleanup

    initializeMap: function (elementId, baseUrl, center, authToken) {
        var self = this;
        const TILE_SIZE = this.TILE_SIZE;
        const CHUNKS_PER_GROUP = 8;

        const SPAWN_CHUNK_X = 15998;
        const SPAWN_CHUNK_Z = 16000;
        const SPAWN_GROUP_X = Math.floor(SPAWN_CHUNK_X / CHUNKS_PER_GROUP);
        const SPAWN_GROUP_Z = Math.floor(SPAWN_CHUNK_Z / CHUNKS_PER_GROUP);

        const extent = [-1000000, -1000000, 1000000, 1000000];
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

                const groupX = tileX + Math.floor(extent[0] / TILE_SIZE);
                const groupZ = tileY - Math.floor(extent[3] / TILE_SIZE);

                // Add cache-busting parameter if tile was invalidated
                const key = self.getTileKey(groupX, groupZ);
                const timestamp = self.invalidatedTiles?.get(key) || '';
                const cacheBuster = timestamp ? `?t=${timestamp}` : '';

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
                center: [
                    SPAWN_GROUP_X * TILE_SIZE,
                    -SPAWN_GROUP_Z * TILE_SIZE
                ],
                resolution: 1,
                minResolution: 0.25,
                maxResolution: 4,
                constrainResolution: false,
                enableRotation: false
            })
        });

        this.map = map;
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

    setCenter: function (x, z, zoom) {
        if (this.map) {
            const view = this.map.getView();
            view.setCenter([x, z]); // Direct chunk coordinates
            if (zoom !== undefined) {
                view.setZoom(zoom);
            }
        }
    },

    getCenter: function () {
        if (this.map) {
            const view = this.map.getView();
            const center = view.getCenter();
            return { x: center[0], z: center[1], zoom: view.getZoom() };
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
    }
};
