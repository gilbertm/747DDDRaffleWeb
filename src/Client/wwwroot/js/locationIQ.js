/* map */
/* window.dotNetToJsMapInitialize = {
    updateAddressDtoFromJS: function (dotNetHelper) {
        var homeAddress = '';
        var homeCity = '';
        var homeCountry = '';
        var homeRegion = '';
        var latitude = '';
        var longitude = '';

        if (document.getElementById("HomeAddress"))
            homeAddress = document.getElementById("HomeAddress").value;

        if (document.getElementById("HomeCity"))
            homeCity = document.getElementById("HomeCity").value;

        if (document.getElementById("HomeCountry"))
            homeCountry = document.getElementById("HomeCountry").value;

        if (document.getElementById("HomeRegion"))
            homeRegion = document.getElementById("HomeRegion").value;

        if (document.getElementById("Latitude"))
            latitude = document.getElementById("Latitude").value;

        if (document.getElementById("Longitude"))
            longitude = document.getElementById("Longitude").value;

        dotNetHelper.invokeMethodAsync("ChangeAddressFromJS", homeAddress, homeCity, homeCountry, homeRegion, latitude, longitude);
    },
    displayInitMap: function (objectFromDotNet) {

        console.log(objectFromDotNet.key);
        console.log(objectFromDotNet.longitude);
        console.log(objectFromDotNet.latitude);


        if (document.getElementById(objectFromDotNet.mapContainer) != undefined) {
            //Add your LocationIQ Maps Access Token here (not the API token!)
            locationiq.key = objectFromDotNet.key;
            //Define the map and configure the map's theme
            var map = new mapboxgl.Map({
                container: objectFromDotNet.mapContainer,
                attributionControl: false, //need this to show a compact attribution icon (i) instead of the whole text
                zoom: parseInt(objectFromDotNet.zoom),
                style: 'https://tiles.locationiq.com/v3/streets/vector.json?key=' + locationiq.key,
                center: [parseFloat(objectFromDotNet.longitude), parseFloat(objectFromDotNet.latitude)]
            });

            var geolocate = new mapboxgl.GeolocateControl({
                positionOptions: {
                    enableHighAccuracy: true
                },
                showAccuracyCircle: true,
                trackUserLocation: true,
                showUserHeading: true,
                showUserLocation: true
            });

            map.addControl(geolocate);

            // First create DOM element for the marker
            var el = document.createElement('div');
            el.className = 'marker';
            // Set marker properties using JS
            el.style.backgroundImage = 'url(./img/marker50px.png)';
            el.style.width = '50px';
            el.style.height = '50px';


            map.on('load', () => {
                // automatic locate or go to exact location
                // disabled to ensure that the pin selected is always the main focal
                // geolocate.trigger();

                var marker = new mapboxgl.Marker(el)
                    .setLngLat([parseFloat(objectFromDotNet.longitude), parseFloat(objectFromDotNet.latitude)])
                    .addTo(map);

                var lngLat = marker.getLngLat();
                /* coordinates.style.display = 'block';
                coordinates.innerHTML =
                    'Latitude: ' + lngLat.lat + '<br />Longitude: ' + lngLat.lng; * /
            });

            geolocate.on('trackuserlocationstart', () => {
                console.log('A trackuserlocationstart event has occurred.');
            });

            map.on('styleimagemissing', (e) => {
                const id = e.id; // id of the missing image

                // Check if this missing icon is
                // one this function can generate.
                const prefix = 'square-rgb-';
                if (!id.includes(prefix)) return;

                // Get the color from the id.
                const rgb = id.replace(prefix, '').split(',').map(Number);

                const width = 64; // The image will be 64 pixels square.
                const bytesPerPixel = 4; // Each pixel is represented by 4 bytes: red, green, blue, and alpha.
                const data = new Uint8Array(width * width * bytesPerPixel);

                for (let x = 0; x < width; x++) {
                    for (let y = 0; y < width; y++) {
                        const offset = (y * width + x) * bytesPerPixel;
                        data[offset + 0] = rgb[0]; // red
                        data[offset + 1] = rgb[1]; // green
                        data[offset + 2] = rgb[2]; // blue
                        data[offset + 3] = 255; // alpha
                    }
                }

                map.addImage(id, { width: width, height: width, data: data });
            });

            //Define layers you want to add to the layer controls; the first element will be the default layer
            var layerStyles = {
                "Streets": "streets/vector",
                // "Satellite": "earth/raster",
                // "Hybrid": "hybrid/vector",
                // "Dark": "dark/vector",
                // "Light": "light/vector"
            };

            map.addControl(new mapboxgl.FullscreenControl());

            map.addControl(new locationiqLayerControl({
                key: locationiq.key,
                layerStyles: layerStyles
            }), 'top-left');


            map.on('click', function (e) {
                // e.point is the x, y coordinates of the mousemove event relative
                // to the top-left corner of the map
                // e.lngLat is the longitude, latitude geographical position of the event

                // Finally, create the marker where the mouse was clicked
                var marker = new mapboxgl.Marker(el)
                    .setLngLat(e.lngLat.wrap())
                    .addTo(map);

                var lngLat = marker.getLngLat();
                /* coordinates.style.display = 'block';
                coordinates.innerHTML =
                    'Latitude: ' + lngLat.lat + '<br />Longitude: ' + lngLat.lng; * /

                var url = "https://us1.locationiq.com/v1/reverse.php?key=" + locationiq.key + "&lat=" + lngLat.lat + "&lon=" + lngLat.lng + "&format=json";

                // Making our request
                fetch(url, { method: 'GET' })
                    .then(Result => Result.json())
                    .then(location => {

                        // Printing our response
                        console.log(location);
                        console.log(location.address.road);

                        if (document.getElementById("HomeAddress"))
                            document.getElementById("HomeAddress").value = location.address.road + ' ' + location.address.suburb;

                        if (document.getElementById("HomeCity"))
                            document.getElementById("HomeCity").value = location.address.city;

                        if (document.getElementById("HomeRegion"))
                            document.getElementById("HomeRegion").value = location.address.state;

                        if (document.getElementById("HomeCountry"))
                            document.getElementById("HomeCountry").value = location.address.country;

                        if (document.getElementById("Latitude"))
                            document.getElementById("Latitude").value = location.lat;

                        if (document.getElementById("Longitude"))
                            document.getElementById("Longitude").value = location.lon;

                        // Printing our field of our response
                        // console.log(`Title of our response :  ${string.title}`);
                    })
                    .catch(errorMsg => {
                        console.log(errorMsg);
                    });
            });
        }
    }
}; */