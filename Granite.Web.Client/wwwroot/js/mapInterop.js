window.mapInterop = {
    map: null,
    tileLayer: null,

    initializeMap: function (elementId, baseUrl, center, authToken) {
        const TILE_SIZE = 256;
        const CHUNKS_PER_GROUP = 8;

        const SPAWN_CHUNK_X = 15998;
        const SPAWN_CHUNK_Z = 16000;
        const SPAWN_GROUP_X = Math.floor(SPAWN_CHUNK_X / CHUNKS_PER_GROUP);
        const SPAWN_GROUP_Z = Math.floor(SPAWN_CHUNK_Z / CHUNKS_PER_GROUP);

        // Tile cache: track which tiles exist and which don't
        const tileCache = {
            loaded: new Set(),    // Successfully loaded tiles
            failed: new Set(),    // Tiles that returned 404
            pending: new Map(),   // Currently loading tiles
        };


        const extent = [-1000000, -1000000, 1000000, 1000000];

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

                const tileX = tileCoord[1];
                const tileY = tileCoord[2];

                const groupX = tileX + Math.floor(extent[0] / TILE_SIZE);
                const groupZ = tileY - Math.floor(extent[3] / TILE_SIZE);

                const key = window.mapInterop.getTileKey(groupX, groupZ);

                // If we know this tile failed before, return a data URL for empty tile
                if (tileCache.failed.has(key)) {
                    // Return a 1x1 transparent PNG to avoid the request
                    return 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==';
                }

                const url = `${baseUrl}/${groupX}/${groupZ}`;

               

                // Mark as pending
                if (!tileCache.loaded.has(key) && !tileCache.pending.has(key)) {
                    tileCache.pending.set(key, Date.now());
                }

                return url;
            },
            tileLoadFunction: function (imageTile, src) {
                const img = imageTile.getImage();
                if (src.startsWith('data:')) {
                    img.src = src;
                    return;
                }
                // imageTile.setState(ol.TileState.LOADING);
                fetch(src, {
                    headers: {
                        'Authorization': `Bearer ${authToken}`
                    }
                }).then(resp => {
                    if (!resp.ok) throw new Error(resp.statusText);
                    return resp.blob();
                }).then(blob => {
                    const objectUrl = URL.createObjectURL(blob);
                    img.src = objectUrl;
                    // imageTile.setState(ol.TileState.LOADED);
                }).catch(err => {
                    console.error('Tile load failed', err);
                    // imageTile.setState(ol.TileState.ERROR);
                });
            }
        });

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

    dispose: function () {
        if (this.map) {
            this.map.setTarget(null);
            this.map = null;
            this.tileLayer = null;
            console.log('OpenLayers map disposed');
        }
    }
};
