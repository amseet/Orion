Number.prototype.pad = function (size) {
    var str = String(this);
    while (str.length < (size || 2)) { str = "0" + str; }
    return str;
};

function flip(data) {
    var tmp = data[0];
    data[0] = data[1];
    data[1] = tmp;
}

function flipArray(array) {
    for (var i = 0; i < array.length; i++)
        flip(array[i]);
}

class Trip {
    constructor(data) {
        this.data = {}; //container for trip information from DB
        this.routeItinero = {};
        this.routeGoogle = {};
        this.currentPos = {}; //current position in the route sequance
        this.currentLatLng = {}; //current position on map
        this.currentTime = {}; //current time according to trip instance

        if (data !== undefined)
            this.data = data;
    }

    init(data) {
        if (data === undefined)
            throw "Trip Data is undefined";
        this.data = data;
    }

    setRouteItinero(route) {
        //flip lnglat => latlng 
        flipArray(route.Shape);

        this.routeItinero = route;
        this.currentPos = 0;
        this.currentLatLng = route.Shape[this.currentPos];
    }

    onClick(event, trip) {
        console.log(trip);
    }
    timeStep() {

    }
}

class Map {
    constructor(domId) {
        this.domId = domId;
        this._map = {};
    }

    /*
        Create and initialize a map instance.
        example: init(40.734695, -73.99037)
    */
    init(lat, lng) {
   
        this._map = L.map(this.domId).setView([lat, lng], 13);
        L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token={accessToken}', {
            attribution: 'Map data &copy; <a href="http://openstreetmap.org">OpenStreetMap</a> contributors, <a href="http://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, Imagery © <a href="http://mapbox.com">Mapbox</a>',
            maxZoom: 18,
            id: 'mapbox.streets',
            accessToken: 'pk.eyJ1IjoiYWhtYWRzZWV0IiwiYSI6ImNqNXN3Y3gxbjAza3YycW8wNHBpNjlkYmcifQ.PcYk9H2jddIorXB93rMT6A'
        }).addTo(this._map);
    }

    clearMap() {
        for (var i in this._map._layers) {
            if (this._map._layers[i]._path !== undefined) {
                try {
                    this._map.removeLayer(this._map._layers[i]);
                }
                catch(e) {
                    console.log("problem with " + e + this._map._layers[i]);
                }
            }
        }
    }

    drawRoute(route, color) {
        var polyline = L.polyline(route, { color: color }).addTo(this._map);

        // zoom the map to the polyline
        this._map.fitBounds(polyline.getBounds());
    }

    //dropPin(pos) {
    //    L.marker([pos[0], pos[1]]).addTo(this._map);
    //}

    dropPin(trip) {
        var l = L.marker(trip.currentLatLng);
        l.addTo(this._map);
        //l.addEventListener('click', trip.onClick);
    }
}

class TripDataWrapper {
    constructor() {

    }

    static genId(year, month, index) {
        return parseInt(String(year) + month.pad(2) + index.pad(9));
    }

    static getTripById(id, callback) {
        $.ajax(`/api/v1/TripData/${id}`, {
            method: 'GET',
            contentType: 'application/json',
			dataType: 'json',
			success: function (data) {
				callback(data);
			},
			fail: function () {
				callback(undefined);
			}
        });
    }

    static getTripRouteById(id, callback) {
        $.ajax(`/api/v1/Route/${id}`, {
            method: 'GET',
            contentType: 'application/json',
            dataType: 'json',
			success: function (data) {
				callback(data);
			},
			fail: function () {
				callback(undefined);
			}
        });
    }

    static getTripRoute(trip, callback) {
		var id = trip.data.TripId;
		$.ajax(`/api/v1/Route/${id}`, {
			method: 'GET',
			contentType: 'application/json',
			dataType: 'json',
			success: function (data) {
				callback(trip, data);
			},
			fail: function () {
				callback(trip, undefined);
			}
		});
    }

    static getTripByDateTime(startTime, endTime, callback) {
        let requestData = {
            startTime: startTime,
            endTime: endTime,
            size: 10,
            offset: 0
        };

        $.ajax('/api/v1/TripData/TimeRange', {
            method: 'GET',
            contentType: 'application/json',
            dataType: 'json',
            data: requestData,
			success: function (multidata) {
				callback(multidata);
			}
        });
    }
}

class googleMapApiWrapper {
    constructor() {

    }

    static getRoute(trip, callback) {
        var id = trip.data.TripId;

        var requestData = {
            type: "Google"
        };
            
        $.ajax(`/api/v1/Route/${id}`, {
            method: 'GET',
            dataType: "json",
            data: requestData,
			success: function (data) {
				callback(trip, data);
			}
        });
    }
}

$(function () {
    $('#datetime_from').datetimepicker({
        defaultDate: "2016-02-01T12:00:00"
    });
    $('#datetime_to').datetimepicker({
        defaultDate: "2016-02-01T12:10:00"
    });
});




let mymap = new Map("mapid");
mymap.init(40.729912, -73.980782);

let trip = new Trip();
let trips = [];

$("#requestTimeRange").click(function () {
    var startTime = new Date($("#startTime").val());
    var endTime = new Date($("#endTime").val());

    TripDataWrapper.getTripByDateTime(startTime.toJSON(), endTime.toJSON(),
		function (multidata) {
            mymap.clearMap();
            for (var i = 0; i < multidata.length; i++) {
                trips[i] = new Trip(multidata[i]);
                let trip = trips[i];

                // Plot Itinero Route
                TripDataWrapper.getTripRoute(trip, function (trip, route) {
                    trip.setRouteItinero(route);
                    mymap.drawRoute(trip.routeItinero.Shape, 'red');
                    mymap.dropPin(trip);
                });

                // Plot Google Maps Api Route
                //googleMapApiWrapper.getRoute(trip, function (trip, result) {
                //    trip.routeGoogle = result;

                //    let route = L.Polyline.fromEncoded(result.routes[0].overview_polyline.points);
                //    mymap.drawRoute(route._latlngs, 'blue');
                //    //mymap.dropPin(trip.currentLatLng);

                //});
            }
        });
})