using Itinero;
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
using System.Timers;
using System.Linq;
using Orion.DB.Models;
using Orion.DB;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace Orion
{
    public class RoutingService
    {
        private RouterDb routerDb;
        private Router router;
        public RouterDb RouterDb { get { return routerDb; } }
        public Router Router { get { return router; } }
        public static RoutingService Service { get; private set; }
        private SqlContext context;
        private List<int> ProcessedTrafficPoints;
        private int searchDistanceInMeter = 250;

        private RoutingService(string routerDbFilePath, SqlContext contx)
        {
            context = contx;
            //EnableLogging();
            routerDb = new RouterDb();
            if (File.Exists(routerDbFilePath))
            {
                using (var stream = File.OpenRead(routerDbFilePath))
                {
                    routerDb = RouterDb.Deserialize(stream);

                }
            }
            router = new Router(routerDb);
            ProcessedTrafficPoints = new List<int>();
        }

        public static void InitService(string routerDbFilePath, SqlContext context)
        {
            
            Service =  new RoutingService(routerDbFilePath, context);
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
        public void BuildRoutingService(string osmFilePath, string routerDbFilePath)
        {
            string map_path = osmFilePath;
            string routerdb_path = routerDbFilePath;
            EnableLogging();
            Profile car;

            EnableLogging();
            using (var sourceStream = File.OpenRead(map_path))
            {
                routerDb.LoadOsmData(sourceStream, Itinero.Osm.Vehicles.Vehicle.Car);

                // get the profile from the routerdb.
                // this is best-practice in Itinero, to prevent mis-matches.
                car = routerDb.GetSupportedProfile("car");

                // add a contraction hierarchy.
                routerDb.AddContracted(car);
            }

            using (var stream = new FileInfo(routerdb_path).Open(FileMode.Create))
            {
                routerDb.Serialize(stream, false);
            }

            router = new Router(routerDb);
        }
   
        public void AddRouteDb(SqlContext sql, Router router, TripDataModel trip, Profile profile, bool withTraffic = false)
        {
            Result<Route> result = router.TryCalculate(profile, (float)trip.Pickup_Latitude, (float)trip.Pickup_Longitude,
                                                            (float)trip.Dropoff_Latitude, (float)trip.Dropoff_Longitude);
            if (!result.IsError)
            {
                Route route = result.Value;
                sql.TripRoutes.Add(new TripRoutesModel() {
                    TripData = trip,
                    //withTraffic = withTraffic,
                    Trip_Route = route.ToGeoJson(),
                    Trip_Distance = route.TotalDistance,
                    Trip_Time = route.TotalTime,
                    Provider = "Itinero",
                    Route_Method = profile.FullName
                });
            }
        }

        public void SaveDb()
        {
           context.SaveChanges();
        }

        public void bench()
        {
            int x = 1000;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            //Batch(0, x);
            stopwatch.Stop();
            Console.WriteLine("Bench 1 :- {0}", stopwatch.ElapsedMilliseconds);
        }
       
        //List<Task> tasks = new List<Task>();
        Util.Progress progress = new Util.Progress(1000);

        public void BatchRouting()
        {
            int _BatchSize = 1000;
            int _MaxCount = context.TripData.Count();
            ThreadPool.GetMinThreads(out int _PoolSize, out int minPorts);
            ThreadPool.SetMaxThreads(_PoolSize, _PoolSize);
            int TaskCount = 0;

            int numberOfBatches = (int)((double)_MaxCount / (double)_BatchSize + 1.0f);
            Console.WriteLine("No. of Trips: {0}\t" +
                                "No. of Batchs: {1} of size {2}", _MaxCount, numberOfBatches, _BatchSize);

            Util.Progress progress = new Util.Progress(5000);
            progress.MaxValue = _MaxCount;
            progress.Start();
            int BatchCount = 0;
            while (BatchCount < numberOfBatches)
            {
                ThreadPool.QueueUserWorkItem((obj) =>
                {
                    Batch((int)obj, _BatchSize, progress);
                }, BatchCount++);
                Thread.Sleep(500);
            }
        }

        public void Batch(int batchIdx, int batchSize, Util.Progress progress)
        {
            int i = 0;
            SqlContext sql = new SqlContext();
            foreach (var trip in sql.TripData.Skip(batchIdx * batchSize).Take(batchSize))
            {
                if (sql.TripRoutes.Where(o => o.TripData.TripId == trip.TripId).Count() == 0)
                    AddRouteDb(sql, router, trip, Itinero.Osm.Vehicles.Vehicle.Car.Fastest(),true);
                progress.inc();
            }
            sql.SaveChanges();
        }

        public void BuildTraffic(Coder coder, IQueryable<TrafficDataModel> trafficDatas)
        {
            var edges = new Dictionary<string, IAttributeCollection>();
            var attributes = new AttributeCollection();

            // STAGING: create some linestrings: encode edge(s) and pair them off with tags.
            foreach (TrafficDataModel entry in trafficDatas)
            {
                attributes.AddOrReplace("maxspeed", entry.Speed + " mph");

                //there's a much faster way of doing this using Dictionary class
                string[] points = entry.Link_Points.Split(' ');
                foreach (var point in points)
                {
                    string[] coor = point.Split(',');
                    if (coor.Length == 2)
                    {
                        float.TryParse(coor[0], out float _lat);
                        float.TryParse(coor[1], out float _long);

                        //Encode edges and add attributes.
                        try
                        {
                            edges[coder.EncodeClosestEdge(new Coordinate(_lat, _long), searchDistanceInMeter)] = attributes;
                        }
                        catch (Exception e)
                        {
                            //skip edge if error
                        }
                    }
                }
            }
            // decode edges and augment routerdb.
            foreach (var pair in edges)
            {
                var encodedLine = pair.Key;
                var attribute = pair.Value;
                coder.DecodeLine(encodedLine, attribute);
            }
        }

        public void RouteWithTraffic()
        {
            var x = context.TripData.Count();
            foreach (var trip in context.TripData.Take(100))
            {
                AddRouteDb(context, router, trip, Itinero.Osm.Vehicles.Vehicle.Car.Shortest());
               
                /*
                * Routing with traffic data from db 
                */
                IQueryable<TrafficDataModel> trafficDatas = context.TrafficData.Where(t => t.Data_As_Of >= trip.Pickup_Datetime.AddMinutes(-5)
                                                        && t.Data_As_Of <= trip.Pickup_Datetime);

                // create coder.
                Coder coder = new Coder(routerDb, new OsmCoderProfile());
                BuildTraffic(coder, trafficDatas);

                AddRouteDb(context, coder.Router, trip, Itinero.Osm.Vehicles.Vehicle.Car.Shortest(), true);

                SaveDb();
                
            }

            //// get edge details.
            //var edge = routerDb.Network.GetEdge(router.Resolve(car, new Coordinate(40.733178f, -73.987169f)).EdgeIdDirected());
            //var oldattributes = routerDb.EdgeProfiles.Get(edge.Data.Profile);
            //var meta = routerDb.EdgeMeta.Get(edge.Data.MetaId);

        }

        public void RouteTrafficStops()
        {
            /*
            * Routing traffic stops from db and save to a geojson file
            */
            List<TrafficDataModel> trafficDatas = context.TrafficData.ToList();

            foreach (TrafficDataModel entry in trafficDatas)
            {
                // create coder.
                var coder = new Coder(routerDb, new OsmCoderProfile());

                // STAGING: create some test linestrings: encode edge(s) and pair them off with tags.
                var edges = new Dictionary<string, IAttributeCollection>();
                var attributes = new AttributeCollection();
                attributes.AddOrReplace("maxspeed", entry.Speed + " mph");

                //there's a much faster way of doing this using Dictionary class
                string[] points = entry.Link_Points.Split(' ');
                foreach (var point in points)
                {
                    string[] coor = point.Split(',');
                    if (coor.Length == 2)
                    {
                        float.TryParse(coor[0], out float _lat);
                        float.TryParse(coor[1], out float _long);

                        //Encode edges and add attributes.
                        try
                        {
                            edges[coder.EncodeClosestEdge(new Coordinate(_lat, _long))] = attributes;
                        }
                        catch (Exception e)
                        {
                            //skip edge if error
                        }
                    }
                }

                // decode edges and augment routerdb.
                foreach (var pair in edges)
                {
                    var encodedLine = pair.Key;
                    var attribute = pair.Value;
                    coder.DecodeLine(encodedLine, attribute);
                }

                //route = coder.Router.Calculate(car, new Coordinate((float)trip.Pickup_Latitude, (float)trip.Pickup_Longitude),
                //new Coordinate((float)trip.Dropoff_Latitude, (float)trip.Dropoff_Longitude));

                //tripRoutes = new TripRoutesModel();
                //tripRoutes.TripData = trip;
                //tripRoutes.withTraffic = true;
                //tripRoutes.Trip_Route = route.ToGeoJson();
                //tripRoutes.Trip_Distance = route.TotalDistance;
                //tripRoutes.Trip_Time = route.TotalTime;
                //tripRoutes.Provider = "Itinero";
                //context.TripRoutes.Add(tripRoutes);
                //tripRoutes.Route_Method = "Shortest";
            }

            context.SaveChanges();
        }
    }
}
