window.mapInterop = {
    map: null,
    tileLayer: null,

    initializeMap: function (elementId, tileUrlTemplate, minZoom, maxZoom, center, zoom) {
        console.log('Initializing map with:', { elementId, tileUrlTemplate, center, zoom });

        // Define resolutions matching WebCartographer approach
        const resolutions = [256, 128, 64, 32, 16, 8, 4, 2, 1, 0.5, 0.25, 0.125];

        // Create tile grid with origin at (0,0) so tile coords match chunk coords
        const tileGrid = new ol.tilegrid.TileGrid({
            extent: [-100000, -100000, 100000, 100000],
            origin: [0, 0], // Origin at center so tile indices = chunk coordinates
            resolutions: resolutions,
            tileSize: 32 // Our tiles are 32x32 pixels
        });

        // Create XYZ tile source with custom URL function
        const tileSource = new ol.source.XYZ({
            interpolate: false,
            wrapX: false,
            tileGrid: tileGrid,
            crossOrigin: 'anonymous',
            tileUrlFunction: function(tileCoord) {
                if (!tileCoord) return undefined;
                
                const z = tileCoord[0];
                const x = tileCoord[1];
                const y = tileCoord[2];
                
                // Convert tile grid coordinates to chunk coordinates
                const resolution = resolutions[z];
                const chunkX = Math.floor(x * resolution);
                const chunkZ = Math.floor(-y * resolution); // Negate Y
                
                const url = tileUrlTemplate
                    .replace('{z}', z)
                    .replace('{x}', chunkX)
                    .replace('{y}', chunkZ);
                
                console.log('Tile request: z=' + z + ', tile=(' + x + ',' + y + '), chunk=(' + chunkX + ',' + chunkZ + ')');
                return url;
            }
        });

        // Create tile layer
        this.tileLayer = new ol.layer.Tile({
            name: "World",
            source: tileSource
        });

        // Create view with the passed-in center coordinates
        const view = new ol.View({
            center: center, // Use the center passed from C# (chunk coordinates)
            constrainResolution: true,
            zoom: zoom,
            resolutions: resolutions,
        });

        // Create map
        this.map = new ol.Map({
            target: elementId,
            layers: [this.tileLayer],
            view: view
        });

        console.log('OpenLayers map initialized with center:', center, 'zoom:', zoom);
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
