﻿using Itinero;
using Itinero.Attributes;
using Itinero.Data.Network;
using Itinero.IO.OpenLR;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using Itinero.Profiles;
using OpenLR;
using OpenLR.Osm;
using System;
using System.Collections.Generic;
using System.IO;

namespace Orion
{
    class Program
    {
        static void EnableLogging()
        {
            // enable logging.
            OsmSharp.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                Console.WriteLine(string.Format("[{0}] {1} - {2}", o, level, message));
            };
            Itinero.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                Console.WriteLine(string.Format("[{0}] {1} - {2}", o, level, message));
            };
        }

        static void Main(string[] args)
        {
            string map_path = "new-york-latest.osm.pbf";
            string routerdb_path = "itinero.routerdb";
            string TaxiTripData_path = "yellow_tripdata_2016-01.csv";

            EnableLogging();

            // STAGING: setup a routerdb.
            Console.WriteLine("Downloading Map Data");
            Download.ToFile("https://download.geofabrik.de/north-america/us/new-york-latest.osm.pbf", map_path).Wait();

            //Console.WriteLine("Downloading Taxi Trip Data");
            //Download.ToFile("https://data.cityofnewyork.us/api/views/uacg-pexx/rows.csv", TaxiTripData_path).Wait();

            // build routerdb from raw OSM data.
            // check this for more info on RouterDb's: https://github.com/itinero/routing/wiki/RouterDb
            var routerDb = new RouterDb();
            Profile car;
            if (File.Exists(routerdb_path))
            {
                using (var stream = File.OpenRead(routerdb_path))
                {
                    routerDb = RouterDb.Deserialize(stream);
                    // get the profile from the routerdb.
                    // this is best-practice in Itinero, to prevent mis-matches.
                    car = routerDb.GetSupportedProfile("car");
                }
            }
            else
            {
                using (var sourceStream = File.OpenRead(map_path))
                {
                    routerDb.LoadOsmData(sourceStream, Itinero.Osm.Vehicles.Vehicle.Car);
                   
                    // get the profile from the routerdb.
                    // this is best-practice in Itinero, to prevent mis-matches.
                    car = routerDb.GetSupportedProfile("car");

                    // add a contraction hierarchy.
                    //routerDb.AddContracted(car);
                }

                using (var stream = new FileInfo(routerdb_path).Open(FileMode.Create))
                {
                    routerDb.Serialize(stream, false);
                }
            }

            // create router.
            var router = new Router(routerDb);

            // calculate route.
            var route = router.Calculate(car, new Coordinate(40.733178f, -73.987169f),
                new Coordinate(40.724283f, -73.992555f));
            var routeGeoJson = route.ToGeoJson();

            /** original path **/
            File.WriteAllText("route1.geojson", routeGeoJson);

            // get edge details.
            var edge = routerDb.Network.GetEdge(router.Resolve(car, new Coordinate(40.733178f, -73.987169f)).EdgeIdDirected());
            var oldattributes = routerDb.EdgeProfiles.Get(edge.Data.Profile);
            var meta = routerDb.EdgeMeta.Get(edge.Data.MetaId); 

            // create coder.
            var coder = new Coder(routerDb, new OsmCoderProfile());

            // STAGING: create some test linestrings: encode edge(s) and pair them off with tags.
            var attributes = new AttributeCollection();
            attributes.AddOrReplace("maxspeed", "5 mph");

            var testEdges = new Dictionary<string, IAttributeCollection>();
            testEdges.Add(coder.EncodeClosestEdge(new Coordinate(40.7326f, -73.98769f)), attributes);

            // TEST: decode edges and augment routerdb.
            foreach (var pair in testEdges)
            {
                var encodedLine = pair.Key;
                var attribute = pair.Value;

                coder.DecodeLine(encodedLine, attribute);
            }
            router = coder.Router;

            // calculate route.
            route = router.Calculate(car, new Coordinate(40.733178f, -73.987169f),
                 new Coordinate(40.724283f, -73.992555f));
            routeGeoJson = route.ToGeoJson();

            /** modified path **/
            File.WriteAllText("route2.geojson", routeGeoJson);
        }
    }
}
