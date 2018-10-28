using Itinero;
using Itinero.Attributes;
using Itinero.Data.Network;
using Itinero.IO.OpenLR;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using Itinero.Profiles;
using OpenLR;
using OpenLR.Osm;
using Orion.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Linq;

namespace Orion.Web
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

        private RoutingService(string routerDbFilePath, SqlContext contx)
        {
            context = contx;
            EnableLogging();
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

            Profile car;

            EnableLogging();
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

            router = new Router(routerDb);
        }

        public string simpleroute()
        {
            Profile car = routerDb.GetSupportedProfile("car");
            var route = router.Calculate(car, new Coordinate(40.733178f, -73.987169f),
               new Coordinate(40.724283f, -73.992555f));
            var routeGeoJson = route.ToGeoJson();

            /** original path **/
            return routeGeoJson;
        }

        void progress(int current, int max)
        {
            Timer timer = new Timer();
            //timer.Elapsed += action;
            timer.Interval = 1000;
            timer.Start();
            var action = new ElapsedEventHandler((s, e) => {
                Console.WriteLine("Proccesed {0}/{1} Rows - {2:P2}", current, max, (float)current / (float)max);
            });

            action.Invoke(null, null);
            timer.Stop();
            timer.Close();
        }

        public void RouteWithTraffic()
        {
            Profile car = routerDb.GetSupportedProfile("car");
            var x = context.TripData.Count();
            foreach (var trip in context.TripData.Take(100))
            {
                Result<Route> result = router.TryCalculate(car, new Coordinate((float)trip.Pickup_Latitude, (float)trip.Pickup_Longitude),
                                                            new Coordinate((float)trip.Dropoff_Latitude, (float)trip.Dropoff_Longitude));
                if(!result.IsError)
                {
                    Route route = result.Value;

                    TripRoutesModel tripRoutes = new TripRoutesModel();
                    tripRoutes.TripData = trip;
                    tripRoutes.withTraffic = false;
                    tripRoutes.Trip_Route = route.ToGeoJson();
                    tripRoutes.Trip_Distance = route.TotalDistance;
                    tripRoutes.Trip_Time = route.TotalTime;
                    tripRoutes.Provider = "Itinero";
                    context.TripRoutes.Add(tripRoutes);

                    /*
                     * Routing with traffic data from db 
                     */
                    IQueryable<TrafficDataModel> trafficDatas = context.TrafficData.Where(t => t.Data_As_Of >= trip.Pickup_Datetime.AddMinutes(-5)
                                                           && t.Data_As_Of <= trip.Pickup_Datetime);

                    if (trafficDatas.Count() > 0)
                    {
                        // create coder.
                        var coder = new Coder(routerDb, new OsmCoderProfile());

                        foreach (TrafficDataModel entry in trafficDatas)
                        {
                            // STAGING: create some test linestrings: encode edge(s) and pair them off with tags.
                            var edges = new Dictionary<string, IAttributeCollection>();
                            var attributes = new AttributeCollection();
                            attributes.AddOrReplace("maxspeed", entry.Speed + " mph");

                            //there's a much faster way of doing this using Dictionary class
                            string[] points = entry.Link_Points.Split(' ');
                            foreach (var point in points)
                            {
                                string[] coor = point.Split(',');
                                float _lat = float.Parse(coor[0]);
                                float _long = float.Parse(coor[1]);

                                //Encode edges and add attributes.
                                try
                                {
                                    edges[coder.EncodeClosestEdge(new Coordinate(_lat, _long))] = attributes;
                                }catch(Exception e)
                                {
                                    //skip edge if error
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
                        route = coder.Router.Calculate(car, new Coordinate((float)trip.Pickup_Latitude, (float)trip.Pickup_Longitude),
                        new Coordinate((float)trip.Dropoff_Latitude, (float)trip.Dropoff_Longitude));

                        tripRoutes = new TripRoutesModel();
                        tripRoutes.TripData = trip;
                        tripRoutes.withTraffic = false;
                        tripRoutes.Trip_Route = route.ToGeoJson();
                        tripRoutes.Trip_Distance = route.TotalDistance;
                        tripRoutes.Trip_Time = route.TotalTime;
                        tripRoutes.Provider = "Itinero";
                        context.TripRoutes.Add(tripRoutes);
                    }
                }

                context.SaveChanges();
            }

            //// get edge details.
            //var edge = routerDb.Network.GetEdge(router.Resolve(car, new Coordinate(40.733178f, -73.987169f)).EdgeIdDirected());
            //var oldattributes = routerDb.EdgeProfiles.Get(edge.Data.Profile);
            //var meta = routerDb.EdgeMeta.Get(edge.Data.MetaId);

        }
    }
}
