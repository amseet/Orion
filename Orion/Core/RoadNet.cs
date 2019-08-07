using Itinero;
using Itinero.IO.Osm;
using Itinero.Profiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Orion.Core
{
    public class RoadNet
    {
        private RouterDb routerDb;
        private Router router;
        private int searchDistanceInMeter = 250;
        private IProfileInstance Car;

        public RoadNet(string routerDbFilePath)
        {
            //EnableLogging();
            routerDb = new RouterDb();
            if (File.Exists(routerDbFilePath))
            {
                using (var stream = File.OpenRead(routerDbFilePath))
                {
                    routerDb = RouterDb.Deserialize(stream);
                    router = new Router(routerDb);
                }
            }
            else
                throw new FileNotFoundException();
        }

        public RoadNet(string routerDbFilePath, string osmFilePath)
        {
            //EnableLogging();
            routerDb = new RouterDb();
            if (File.Exists(routerDbFilePath))
            {
                using (var stream = File.OpenRead(routerDbFilePath))
                {
                    routerDb = RouterDb.Deserialize(stream);
                    router = new Router(routerDb);
                }
            }
            else
                BuildRoutingDB(osmFilePath, routerDbFilePath);
        }

        private void EnableLogging()
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

        // build routerdb from raw OSM data.
        public void BuildRoutingDB(string osmFilePath, string routerDbFilePath, bool IsContracted = false)
        {
            string map_path = osmFilePath;
            string routerdb_path = routerDbFilePath;
            Profile car;

            EnableLogging();
            using (var sourceStream = File.OpenRead(map_path))
            {
                routerDb.LoadOsmData(sourceStream, Itinero.Osm.Vehicles.Vehicle.Car);

                // get the profile from the routerdb.
                // this is best-practice in Itinero, to prevent mis-matches.
                car = routerDb.GetSupportedProfile("car");

                // add a contraction hierarchy.
                if(IsContracted)
                    routerDb.AddContracted(car);
            }

            using (var stream = new FileInfo(routerdb_path).Open(FileMode.Create))
            {
                routerDb.Serialize(stream, false);
            }

            router = new Router(routerDb);
        }

        public string GetRoute(double[] source, double[] dest)
        {
            Result<Route> result = router.TryCalculate(routerDb.GetSupportedProfile("car"),
                                        (float)source[0], (float)source[1], (float)dest[0], (float)dest[1]);
            if (!result.IsError)
                return result.Value.ToGeoJson();
            
            return string.Empty;
        }

        public bool IsConnected(double lat, double lng, int searchRadiusInMeters = 50)
        {
            var result = router.TryResolve(routerDb.GetSupportedProfile("car"), (float)lat, (float)lng, searchRadiusInMeters);

            if (result.IsError)
                return false;

            return true;
        }
    }
}
