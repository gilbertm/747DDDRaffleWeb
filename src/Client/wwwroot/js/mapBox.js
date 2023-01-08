window.dotNetJSMapBox = {
    initMap: function (objectFromDotNet) {

        /* console.log(objectFromDotNet.key);
        console.log(objectFromDotNet.zoom);
        console.log(objectFromDotNet.style);
        console.log(objectFromDotNet.longitude);
        console.log(objectFromDotNet.latitude); */

        if (document.getElementById(objectFromDotNet.mapContainer) != undefined) {

            mapboxgl.accessToken = objectFromDotNet.key;
            window.MainMap = new mapboxgl.Map({
                container: objectFromDotNet.mapContainer,
                style: objectFromDotNet.style,
                center: [parseFloat(objectFromDotNet.longitude), parseFloat(objectFromDotNet.latitude)],
                zoom: parseInt(objectFromDotNet.zoom)
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

            window.MainMap.addControl(geolocate);

            // First create DOM element for the marker
            var el = document.createElement('div');
            el.className = 'marker';
            // Set marker properties using JS
            el.style.backgroundImage = 'url(./img/marker50px.png)';
            el.style.width = '50px';
            el.style.height = '50px';

            window.MainMap.on('load', () => {
                // automatic locate or go to exact location
                // disabled to ensure that the pin selected is always the main focal
                // geolocate.trigger();

                /* var marker = new mapboxgl.Marker(el)
                    .setLngLat([parseFloat(objectFromDotNet.longitude), parseFloat(objectFromDotNet.latitude)])
                    .addTo(window.MainMap);

                var lngLat = marker.getLngLat();

                console.log(lngLat); */

                // deprecated, using internal checks is more reliable
                window.dotNetJSMapBox.processLatLng(objectFromDotNet.longitude, objectFromDotNet.latitude, objectFromDotNet.key);

            });

            geolocate.on('trackuserlocationstart', () => {
                // console.log('A trackuserlocationstart event has occurred.');
            });

            window.MainMap.on('styleimagemissing', (e) => {
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

                window.MainMap.addImage(id, { width: width, height: width, data: data });
            });

            window.MainMap.addControl(new mapboxgl.FullscreenControl());

            window.MainMap.on('click', function (e) {
                // e.point is the x, y coordinates of the mousemove event relative
                // to the top-left corner of the map
                // e.lngLat is the longitude, latitude geographical position of the event

                // Finally, create the marker where the mouse was clicked
                var marker = new mapboxgl.Marker(el)
                    .setLngLat(e.lngLat.wrap())
                    .addTo(window.MainMap);

                var lngLat = marker.getLngLat();

                var url = "https://api.mapbox.com/geocoding/v5/mapbox.places/" + lngLat.lng + "," + lngLat.lat + ".json?access_token=" + objectFromDotNet.key;

                // Making our request
                fetch(url, { method: 'GET' })
                    .then(Result => Result.json())
                    .then(location => {

                        // Printing our response
                        // console.log(location.features);

                        for (let i = 0; i < location.features.length; i++) {
                            if (location.features[i].id.includes('country')) {
                                if (document.getElementById("HomeCountry"))
                                    document.getElementById("HomeCountry").value = location.features[i].text;
                            }
                            if (location.features[i].id.includes('region')) {
                                if (document.getElementById("HomeRegion"))
                                    document.getElementById("HomeRegion").value = location.features[i].text;
                            }
                            if (location.features[i].id.includes('postcode')) {
                                // postal
                            }
                            if (location.features[i].id.includes('place')) {
                                // city or town
                                if (document.getElementById("HomeCity"))
                                    document.getElementById("HomeCity").value = location.features[i].text;
                            }

                            if (location.features[i].id.includes('district')) {
                                // bigger than cities
                                if (document.getElementById("HomeAddress")) {
                                    if (location.features[i].text != "") {
                                        document.getElementById("HomeAddress").value = location.features[i].text;
                                    }
                                }
                            }

                            if (location.features[i].id.includes('poi')) {
                                // poi
                                if (document.getElementById("HomeAddress")) {
                                    if (location.features[i].text != "") {
                                        document.getElementById("HomeAddress").value = location.features[i].text;
                                    }
                                }
                            }
                            if (location.features[i].id.includes('neighborhood')) {
                                // neighborhood
                                if (document.getElementById("HomeAddress")) {
                                    if (location.features[i].text != "") {
                                        document.getElementById("HomeAddress").value = location.features[i].text;
                                    }
                                }

                            }
                            if (location.features[i].id.includes('locality')) {
                                // barangay
                                if (document.getElementById("HomeAddress")) {
                                    if (location.features[i].text != "") {
                                        document.getElementById("HomeAddress").value = location.features[i].text;
                                    }
                                }

                            }

                            if (location.features[i].id.includes('address')) {
                                // address
                                if (document.getElementById("HomeAddress")) {
                                    if (location.features[i].text != "") {
                                        document.getElementById("HomeAddress").value = location.features[i].text;
                                    }
                                }

                            }
                        }

                        if (location.query) {
                            // lat
                            if (document.getElementById("Latitude")) {
                                if (location.features[i].text != "") {
                                    document.getElementById("Latitude").value = lngLat.lat;
                                }
                            }
                            // long
                            if (document.getElementById("Longitude")) {
                                if (location.features[i].text != "") {
                                    document.getElementById("Longitude").value = lngLat.lng;
                                }
                            }
                        }
                    })
                    .catch(errorMsg => {
                        // console.log(errorMsg);
                    });
            });

        }
    },
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
    processLatLng: function (longitude, latitude, key) {

        /* console.log("load -----------------------------------");
        console.log(longitude);
        console.log(latitude); */

        var url = "https://api.mapbox.com/geocoding/v5/mapbox.places/" + longitude + "," + latitude + ".json?access_token=" + key;

        var homeCity = '';
        var homeCountry = '';

        // Making our request
        fetch(url, { method: 'GET' })
            .then(Result => Result.json())
            .then(location => {                

                for (let i = 0; i < location.features.length; i++) {

                    if (location.features[i].id.includes('country')) {                       

                        if (document.getElementById("HomeCountry"))
                        {
                            document.getElementById("HomeCountry").value = location.features[i].text;
                            homeCountry = location.features[i].text;

                            /* console.log("data--------------------country");
                            console.log(homeCountry); */
                        }

                        if (document.getElementById("HomeCountryAnon")) {
                            console.log("country anon");
                            console.log(document.getElementById("HomeCountryAnon"));
                            document.getElementById("HomeCountryAnon").innerHTML = location.features[i].text;
                        }

                    }
                    if (location.features[i].id.includes('place')) {
                        // city or town
                        if (document.getElementById("HomeCity"))
                        {
                            document.getElementById("HomeCity").value = location.features[i].text;
                            homeCity = location.features[i].text;

                            /* console.log("data--------------------city");
                            console.log(homeCity); */
                        }

                        // city or town
                        if (document.getElementById("HomeCityAnon")) {
                            document.getElementById("HomeCityAnon").innerHTML = location.features[i].text + ",&nbsp;";
                        }
                    }
                }
            })
            .catch(errorMsg => {
                // console.log(errorMsg);
            });

        /* console.log("-----------------------------------------------------------------------------------");
        console.log(homeCity);
        console.log(homeCountry); */


    }
};