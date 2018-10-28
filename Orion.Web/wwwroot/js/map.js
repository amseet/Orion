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

class Taxi {
    constructor(data) {
        this.data = {
            id: {},
            uid: {},
            vendorID: {},
            pickup_datetime: {},
            dropoff_datetime: {},
            passenger_count: {},
            trip_distance: {},
            pickup_longitude: {},
            pickup_latitude: {},
            RatecodeID: {},
            store_and_fwd_flag: {},
            dropoff_longitude: {},
            dropoff_latitude: {},
            payment_type: {},
            fare_amount: {},
            extra: {},
            mta_tax: {},
            tip_amount: {},
            tolls_amount: {},
            improvement_surcharge: {},
            total_amount: {}
        }; //container for taxi information from DB
        this.routeItinero = {};
        this.routeGoogle = {};
        this.currentPos = {}; //current position in the route sequance
        this.currentLatLng = {}; //current position on map
        this.currentTime = {}; //current time according to taxi instance

        if (data !== undefined)
            this.data = data;
    }

    init(data) {
        if (data === undefined)
            throw "Taxi Data is undefined";
        this.data = data;
    }

    setRouteItinero(route) {
        //flip lnglat => latlng 
        flipArray(route.Shape);

        this.routeItinero = route;
        this.currentPos = 0;
        this.currentLatLng = route.Shape[this.currentPos];
    }

    onClick(event, taxi) {
        console.log(taxi);
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
            if (this._map._layers[i]._path != undefined) {
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

    dropPin(taxi) {
        var l = L.marker(taxi.currentLatLng);
        l.addTo(this._map);
        //l.addEventListener('click', taxi.onClick);
    }
}

class TaxiDataWrapper {
    constructor() {

    }

    static genId(year, month, index) {
        return parseInt(String(year) + month.pad(2) + index.pad(9));
    }

    static getTaxiById(id, callback) {
        $.ajax(`/api/TaxiData/${id}`, {
            method: 'GET',
            contentType: 'application/json',
            dataType: 'json'
        }).success(function (data) {
            callback(data);
        }).fail(function () {
            callback(undefined);
        });
    }

    static getTaxiRouteById(id, callback) {
        $.ajax(`/api/Router/${id}`, {
            method: 'GET',
            contentType: 'application/json',
            dataType: 'json'
        }).success(function (data) {
            callback(data);
        }).fail(function () {
            callback(undefined);
        });
    }

    static getTaxiRoute(taxi, callback) {
        var id = taxi.data.id;
        $.ajax(`/api/Router/${id}`, {
            method: 'GET',
            contentType: 'application/json',
            dataType: 'json'
        }).success(function (data) {
            callback(taxi, data);
        }).fail(function () {
            callback(taxi, undefined);
        });
    }

    static getTaxiByDateTime(startTime, endTime, callback) {
        let requestData = {
            startTime: startTime,
            endTime: endTime,
            size: 10,
            offset: 0
        };

        $.ajax('/api/TaxiData/TimeRange', {
            method: 'GET',
            contentType: 'application/json',
            dataType: 'json',
            data: requestData
        }).success(function (multidata) {
            callback(multidata);
        });
    }
}

class googleMapApiWrapper {
    constructor() {

    }

    static getRoute(taxi, callback) {
        var id = taxi.data.id;

        var requestData = {
            type: "Google"
        };
            
        $.ajax(`/api/Router/${id}`, {
            method: 'GET',
            dataType: "json",
            data: requestData,
        }).success(function (data) {
            callback(taxi, data);
        });
    }
}

$(function () {
    $('#datetime_from').datetimepicker({
        defaultDate: "2016-02-01T12:00:00"
    });
    $('#datetime_to').datetimepicker({
        defaultDate: "2016-02-01T01:10:00"
    });
});




let mymap = new Map("mapid");
mymap.init(40.729912, -73.980782);

let taxi = new Taxi();
let taxis = [];

$("#requestTimeRange").click(function () {
    var startTime = new Date($("#startTime").val());
    var endTime = new Date($("#endTime").val());

    TaxiDataWrapper.getTaxiByDateTime(startTime.toJSON(), endTime.toJSON(),
        function (multidata) {
            mymap.clearMap();
            for (var i = 0; i < multidata.length; i++) {
                taxis[i] = new Taxi(multidata[i]);
                let taxi = taxis[i];

                // Plot Itinero Route
                TaxiDataWrapper.getTaxiRoute(taxi, function (taxi, route) {
                    taxi.setRouteItinero(route);
                    mymap.drawRoute(taxi.routeItinero.Shape, 'red');
                    mymap.dropPin(taxi);
                });

                // Plot Google Maps Api Route
                googleMapApiWrapper.getRoute(taxi, function (taxi, result) {
                    taxi.routeGoogle = result;

                    let route = L.Polyline.fromEncoded(result.routes[0].overview_polyline.points);
                    mymap.drawRoute(route._latlngs, 'blue');
                    //mymap.dropPin(taxi.currentLatLng);

                });
            }
        });
})