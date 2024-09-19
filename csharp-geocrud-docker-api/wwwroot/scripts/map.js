document.addEventListener('DOMContentLoaded', function () {
    // Initialize the map
    var map = new ol.Map({
        target: 'map',
        layers: [
            new ol.layer.Tile({
                title: 'Street View',
                type: 'base',
                visible: true,
                source: new ol.source.OSM()
            }),
            new ol.layer.Tile({
                title: 'Satellite View',
                type: 'base',
                source: new ol.source.XYZ({
                    url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}'
                })
            })
        ],
        view: new ol.View({
            center: ol.proj.fromLonLat([-0.09, 51.505]),
            zoom: 13
        })
    });

    // Add layer switcher control
    var layerSwitcher = new ol.control.LayerSwitcher({
        tipLabel: 'Layers' // Tooltip for the layer switcher
    });
    map.addControl(layerSwitcher);

    // Popup element
    var popupElement = document.getElementById('popup');
    const content = document.getElementById('popup-content');
    var popup = new ol.Overlay({
        element: popupElement,
        autoPan: {
            autoPanAnimation: {
                duration: 250,
            }
        }
    });
    map.addOverlay(popup);

    // Search control and function
    document.getElementById('search').addEventListener('keyup', function(event) {
        if (event.key === 'Enter') {
            var query = event.target.value;
            searchCity(query);
        }
    });

    function searchCity(query) {
        fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}`)
            .then(response => response.json())
            .then(data => {
                if (data.length > 0) {
                    var firstResult = data[0];
                    var lonLat = ol.proj.fromLonLat([parseFloat(firstResult.lon), parseFloat(firstResult.lat)]);
                    map.getView().setCenter(lonLat);
                    map.getView().setZoom(13);

                    var marker = new ol.Feature({
                        geometry: new ol.geom.Point(lonLat)
                    });

                    var markerVectorSource = new ol.source.Vector({
                        features: [marker]
                    });

                    var markerVectorLayer = new ol.layer.Vector({
                        source: markerVectorSource
                    });

                    // Clear previous markers
                    map.getLayers().forEach(layer => {
                        if (layer instanceof ol.layer.Vector) {
                            map.removeLayer(layer);
                        }
                    });

                    map.addLayer(markerVectorLayer);
                } else {
                    alert('No results found.');
                }
            })
            .catch(error => console.error('Error fetching search results:', error));
    }

    function getMarkerStyle(item) {
        return new ol.style.Style({
            image: new ol.style.Circle({
                radius: 10, // Size of the pins
                fill: new ol.style.Fill({ color: item.colour || 'black' }), // Use the color from item
                stroke: new ol.style.Stroke({ color: 'white', width: 2 })
            })
        });
    }

    // Function to add markers with custom style
    function addMarkers(data) {
        // Create features from the data
        var features = data.map(item => {
            var lonLat = ol.proj.fromLonLat([item.longitude, item.latitude]);
            var marker = new ol.Feature({
                geometry: new ol.geom.Point(lonLat),
                description: item.value || 'N/A'
            });
            marker.setStyle(getMarkerStyle(item));
            return marker;
        });

        var markerVectorSource = new ol.source.Vector({
            features: features
        });

        var markerVectorLayer = new ol.layer.Vector({
            source: markerVectorSource
        });

        // Clear previous markers
        map.getLayers().forEach(layer => {
            if (layer instanceof ol.layer.Vector) {
                map.removeLayer(layer);
            }
        });

        map.addLayer(markerVectorLayer);

        // Click event to show popup with description
        map.on('singleclick', function(event) {
            var feature = map.getFeaturesAtPixel(event.pixel)[0];
            if (feature) {
                var description = feature.get('description') || 'No description';
                content.innerHTML = description;
                popup.setPosition(event.coordinate);
                document.getElementById('popup-closer').addEventListener('click', function(evt) {
                    evt.preventDefault();
                    popup.setPosition(undefined); // Hide the popup
                });
            } else {
                popup.setPosition(undefined); // Hide popup if no marker clicked
            }
        });
    }

    // Call addMarkers with the fetched data
    document.getElementById('submit').addEventListener('click', function() {
        var colour = document.getElementById('colour').value;

        var apiUrl = `api/geo/colour?colour=${colour}`;
        console.log('URL: ', apiUrl);

        fetch(apiUrl)
            .then(response => response.json())
            .then(data => {
                if (Array.isArray(data)) {
                    document.getElementById('recordCount').textContent = `Records: ${data.length}`;
                    markerPostProcess(data);
                    addMarkers(data); // Add markers to the map
                } else {
                    document.getElementById('recordCount').textContent = 'Records: 0';
                    console.error('Received data is not an array:', data);
                }
            })
            .catch(error => {
                document.getElementById('recordCount').textContent = 'Records: N/A';
                console.error('Error:', error);
            });
    });
});
