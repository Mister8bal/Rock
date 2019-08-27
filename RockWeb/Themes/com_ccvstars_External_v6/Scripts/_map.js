(function(){

    window.CCV = window.CCV || {}

    function loadMap() {

    // Styles
    CCV.mapStyles = [{
            "stylers": [{
                "visibility": "simplified"
            }, {
                "saturation": -100
            }]
        }, {
            "featureType": "road.arterial",
            "elementType": "labels",
            "stylers": [{
                "visibility": "on"
            }, {
                "gamma": 3.05
            }]
        }, {
            "featureType": "poi",
            "stylers": [{
                "visibility": "off"
            }]
        }, {
            "featureType": "transit",
            "stylers": [{
                "visibility": "off"
            }]
        }, {
            "featureType": "administrative.country",
            "stylers": [{
                "visibility": "off"
            }]
        }, {
            "featureType": "administrative.locality",
            "stylers": [{
                "visibility": "off"
            }]
        }, {
            "featureType": "administrative.neighborhood",
            "stylers": [{
                "visibility": "off"
            }]
        }, {
            "featureType": "water",
            "stylers": [{
                "visibility": "off"
            }]
        },
    {}];

    // MapType used during map init
    CCV.mapType = new google.maps.StyledMapType(CCV.mapStyles);

    // Marker Styles
    CCV.marker = {
        url: '/Themes/com_ccvstars_External_v6/Assets/Images/icon/star-red.svg',
        size: new google.maps.Size(200,64),
        origin: new google.maps.Point(0,0),
        anchor: new google.maps.Point(100,30),
        scaledSize: new google.maps.Size(200,64)
    }



    // Begin map object

    CCV.baseMap = function (holder, points) {
        this.holder = holder
        this.points = points
        this.markers = []
        this.bounds = new google.maps.LatLngBounds()
        this.zoom = this.zoom || 12
        this.useZoom = this.useZoom || false
        this.useScrollZoom = this.useScrollZoom || true
        this.usePanControl = this.usePanControl || true
    }
    CCV.baseMap.prototype = {
        draw: function () {
            var options = {}

            if (this.useScrollZoom) {
                options = {
                    mapTypeId: 'CCV',
                    disableDefaultUI: true
                }
            } else {
                options = {
                    mapTypeId: 'CCV',
                    scrollwheel: false,
                    panControl: this.usePanControl,
                    zoomControl: true,
                    zoomControlOptions: {
                    style: google.maps.ZoomControlStyle.SMALL,
                    position: google.maps.ControlPosition.TOP_RIGHT
                    },
                    streetViewControl: false,
                    mapTypeControl: false
                }
            }
            this.mapOptions = this.mapOptions || options

            if (this.holder)
                this.map = new google.maps.Map(this.holder, this.mapOptions)
            else
                throw "Can't find map holder"
            this.map.mapTypes.set('CCV', CCV.mapType)
            this.dropMarkers()
            this.fitMarkers()
            this.bindUi()
        },
        dropMarkers: function () {
            for (var i = 0; i < this.points.length; i++) {
                var point = this.points[i]
                this.dropMarker(point)
            }
        },
        dropMarker: function (point) {
            var marker = new google.maps.Marker({
                position: new google.maps.LatLng(point.lat,point.lng),
                icon: CCV.marker,
                map: this.map,
                title: point.title,
                animation: google.maps.Animation.DROP
            })
            this.markers.push(marker)
            this.bounds.extend(marker.position)
            this.afterDropMarker.call(this, point, marker)
        },
        afterDropMarker: function (point, marker) {
        },
        fitMarkers: function () {
            this.map.setCenter(this.bounds.getCenter())
            if (this.useZoom || this.markers.length == 1)
                this.map.setZoom(this.zoom)
            else
                this.map.fitBounds(this.bounds)
        },
        bindUi: function () {
            var _this = this
            $(window).resize(function() {
                _this.fitMarkers()
            })
        },
        getInstanceName: function () {
            for (var name in window)
            if (window[name] == this)
                return name
        },
    }


    // Campus Map

    // Inherit baseMap and add campus details
    CCV.campusMap = function (holder, campusToDraw) {
        CCV.baseMap.call(this, holder, campusToDraw)
        this.campusToDraw = campusToDraw || 'all'
    }
    CCV.campusMap.prototype = new CCV.baseMap()
    CCV.campusMap.prototype.constructor = CCV.campusMap

    CCV.campusMap.prototype.dropMarkers = function () {
        if (this.campusToDraw == 'all') {
            for (var i = 0; i < CCV.locations.length; i++) {
                var campus = CCV.locations[i]
                this.dropMarker(campus)
            }
        }
        else {
            var campus = CCV.findCampusById(this.campusToDraw)
            this.dropMarker(campus)
            this.useZoom = true
        }
    }
    CCV.campusMap.prototype.dropMarker = function (campus) {
        var marker = new google.maps.Marker({
            position: new google.maps.LatLng(campus.geo.lat,campus.geo.lng),
            icon: CCV.marker,
            map: this.map,
            title: campus.name,
            campusid: campus.id,
            animation: google.maps.Animation.DROP
        })
        this.markers.push(marker)
        this.bounds.extend(marker.position)
        this.afterDropMarker.call(this, campus, marker)
    }


    // Infowindow map

    // Inherit campusMap and add initial infowindow
    CCV.campusInfoWindowMap = function (holder, campusToDraw) {
        CCV.campusMap.call(this, holder, campusToDraw)
        this.infowindow = new google.maps.InfoWindow({ content: 'Loading...' })
    }
    CCV.campusInfoWindowMap.prototype = new CCV.campusMap()
    CCV.campusInfoWindowMap.prototype.constructor = CCV.campusInfoWindowMap

    // Custom & override methods
    CCV.campusInfoWindowMap.prototype.afterDropMarker = function (campus, marker) {
        var _this = this
        google.maps.event.addListener(marker, 'click', function () {
            _this.infowindow.setContent(_this.buildInfoWindow(campus))
            _this.infowindow.open(_this.map, this)
        })
    }
    CCV.campusInfoWindowMap.prototype.buildInfoWindow = function(campus) {
        var result
        var campusSports = campus.sports
        var campusRoute = campus.name.replace(' ','-').toLowerCase()

        // create clickable map link
        var campusMapLink = ""
        var mapNewWindow = `target="_blank"`
        if ((navigator.platform.indexOf("iPhone") != -1) || (navigator.platform.indexOf("iPad") != -1) || (navigator.platform.indexOf("iPod") != -1)) {
            // ios - chrome ios doesnt like target blank so dont add it
            campusMapLink = "http://maps.apple.com/?q=" + campus.geo.lat + "," + campus.geo.lng
            mapNewWindow = ""
        } else {
            // everything else
            campusMapLink = "https://maps.google.com/maps?daddr=" + campus.geo.lat + "," + campus.geo.lng + "&amp;ll="
        }

        result  = '<div class="campus-info-window">'
            result += '<h6>' + campus.name + '</h6>'
            result += '<div class="sports-offered">'
                if ( campusSports.length > 0 ) {
                    if (campusSports.includes("baseball")) {
                        result += '<a href="/baseball/' + campusRoute + '"><img src="/Themes/com_ccvstars_External_v6/Assets/Images/icon/baseball-red.png"></a>'
                    }
                    if (campusSports.includes("basketball")) {
                        result += '<a href="/basketball/' + campusRoute + '"><img src="/Themes/com_ccvstars_External_v6/Assets/Images/icon/basketball-red.png"></a>'
                    }
                    if(campusSports.includes("football")) {
                        result += '<a href="/football/' + campusRoute + '"><img src="/Themes/com_ccvstars_External_v6/Assets/Images/icon/football-red.png"></a>'
                    }
                    if (campusSports.includes("soccer")) {
                        result += '<a href="/soccer/' + campusRoute + '"><img src="/Themes/com_ccvstars_External_v6/Assets/Images/icon/soccer-red.png"></a>'
                    }
                    if (campusSports.includes("exceptional-stars")) {
                        result += '<a class="exceptional-stars" href="/exceptional-stars/' + campusRoute + '"><img src="/Themes/com_ccvstars_External_v6/Assets/Images/icon/exceptional-stars-red.png"></a>'
                    }
                } else {
                    result += '<p>No sports currently offered</p>'
                }
            result += '</div>'
            result += '<div class="campus-contact">'
            result += '<a href="/stars-contact-us?Campus=' +  campusRoute +  '"><img src="/Themes/com_ccvstars_External_v6/Assets/Images/icon/email-red.png"></a>'
            result += `<a href="` + campusMapLink + `" ` + mapNewWindow + `><p>` + campus.street + '<br />'
            result += campus.city + ', ' + campus.state + ' ' + campus.zip + '</p></a>'
            result += '</div>'
            result += '<a href="tel:+1' + campus.starsContactNumber.replace(/[^A-Z0-9]/ig,'') + '">' + campus.starsContactNumber + '</a>'
            result += '<a href="https://ccv.church/' + campusRoute + '">ccv.church/'+ campusRoute +'</a>'
        result += '</div>'
        return result
    }


    // Infowindow map with geolocation

    // Inherit campusInfoWindowMap and add initial infowindow
    CCV.campusInfoWindowMapGeo = function (holder, campusToDraw) {
        CCV.campusInfoWindowMap.call(this, holder, campusToDraw)
        this.infowindow = new google.maps.InfoWindow({ content: 'Loading...' })
    }
    CCV.campusInfoWindowMapGeo.prototype = new CCV.campusInfoWindowMap()
    CCV.campusInfoWindowMapGeo.prototype.constructor = CCV.campusInfoWindowMapGeo

    // Custom & override methods
    CCV.campusInfoWindowMapGeo.prototype.findNearestCampus = function (callback, trigger) {
        var _this = this,
            $trigger = $(trigger),
            userLocation,
            service = new google.maps.DistanceMatrixService(),
            result

        if(navigator.geolocation) {
            $trigger.addClass('is-loading')
            navigator.geolocation.getCurrentPosition(function(p) {
                userLocation = new google.maps.LatLng(p.coords.latitude, p.coords.longitude)
                service.getDistanceMatrix(
                    {
                    origins: [userLocation],
                    destinations: _this.allLocationsGeoArray(),
                    travelMode: google.maps.TravelMode.DRIVING,
                    }, parseResults);
            })
        }

        function parseResults(response, status) {
            if (status == google.maps.DistanceMatrixStatus.OK) {

            var lowest = Number.POSITIVE_INFINITY,
                lowestArrayIndex,
                tmp,
                responseArray = response.rows[0].elements

            for (var i = 0; i < responseArray.length; i++) {
                tmp = responseArray[i].duration.value
                if (tmp < lowest) {
                    lowest = tmp
                    lowestArrayIndex = i
                }
            }

            $trigger.removeClass('is-loading')
            _this.nearestCampus = CCV.locations[lowestArrayIndex]
            typeof callback === 'function' && callback.call(_this, _this.nearestCampus)
            }
        }
    }
    CCV.campusInfoWindowMapGeo.prototype.allLocationsGeoArray = function() {
        var r = []
        for (var i = 0; i < CCV.locations.length; i++) {
            var location = CCV.locations[i].geo
            var geo = new google.maps.LatLng(location.lat,location.lng)
            r.push(geo)
        }
        return r
    }
    CCV.campusInfoWindowMapGeo.prototype.openInfoWindow = function (campus) {
      var marker = this.markers.filter(function (marker) {
        return marker.campusid == campus.id
      })[0]
      this.infowindow.setContent(this.buildInfoWindow(campus))
      this.infowindow.open(this.map, marker)
    }

}
    // Load google maps api dynamically to avoid double loading
    if (typeof google == 'object' && typeof google.maps == 'object')
       loadMap()
    else {
        $(window).on('googleMapsIsLoaded', function(){
            loadMap()
        })
    }
})();
