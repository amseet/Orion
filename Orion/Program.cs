using Microsoft.EntityFrameworkCore;
using Orion.DB;
using Orion.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Orion.Core;
using Orion.Geo;
using Orion.Models;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using Orion.Cities.NYC;
using Orion.IO;
using System.Threading;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Orion.Core.DataStructs;

namespace Orion
{
    class Program
    {
        static TripRecordContext GetTrips(bool isNew = false)
        {
            if(File.Exists(NYCConst.TaxiData) && File.Exists(NYCConst.TaxiLookup) && !isNew)
                return TripRecordFactory.Load(NYCConst.TaxiData, NYCConst.TaxiLookup);
            else
                return TripRecordFactory.GenerateBinaryFile<NYCTaxiRecord>(Directory.GetFiles(NYCConst.TripsRaw_Dir), NYCConst.TaxiData);

        }

        static void ProcessTrips(TripRecordContext context, City nyc, Dictionary<DateTime, Weather> WeatherData)
        {
            bool processEdges = true;
            bool processGlobals = true;
            bool processNodes = true;
            StreamWriter wNode = null;
            StreamWriter wGlobal = null;
            StreamWriter wEdge = null;

            Progress progress;
            Stopwatch stopwatch = new Stopwatch();
            int edgeCounter = 0;
            #region ProcessingTrips
            Console.WriteLine(">Starting Process<");
            stopwatch.Restart();

            if (processNodes)
            {
                wNode = new StreamWriter(Path.Combine(Constants.Root_Dir, "node.csv"));
                wNode.WriteLine(string.Join(',',
                                "ID", "LandUse",
                                "Population", "MedianIncome", "PopDensity", "BachelorHigher", 
                                "Popularity", "Rating", "Places",
                                "OutFlow"));
            }

            if (processGlobals)
            {
                wGlobal = new StreamWriter(Path.Combine(Constants.Root_Dir, "global.csv"));
                wGlobal.WriteLine(string.Join(',', "Month", "DayofWeek", "Period", "TempAvg", "Precipitation", "Snow"));
            }

            foreach (DateTime date in context)
            {
                var trips = context.Get(date);
                var tripsByPeriod = trips.GroupBy(t => t.Period);
                var weather = WeatherData[date];

                Console.WriteLine(">Processing {0}", date);
                progress = new Progress(1000, trips.Count());
                progress.Start();
                foreach (var period in tripsByPeriod)
                {
                    //Count the number of pickups and dropoffs per zone
                    Dictionary<string, int> Pickups = new Dictionary<string, int>();
                    //Dictionary<string, int> Dropoffs = new Dictionary<string, int>();              
                    Dictionary<Tuple<int,int>, int> EdgeFlow = new Dictionary<Tuple<int, int>, int>();

                    if (processEdges)
                    {
                        wEdge = new StreamWriter(Path.Combine(Constants.Root_Dir,
                                               string.Format("Edges/edges_{0}.csv", edgeCounter++)));
                        wEdge.WriteLine(string.Join(',', "SenderID", "ReceiverID", "Distance", "InFlow"));
                        wEdge.AutoFlush = true;
                    }

                    #region FindContainingRegion
                    var sortedTrips = period.OrderBy(t => t.TimeStamp).ToArray();
                    
                    //Find containing region for every trip record 
                    foreach (var trip in sortedTrips)
                    {
                        if (trip.IsValid)
                        {
                            var pickupRegion= nyc.FindRegion(trip.Pickup_Latitude, trip.Pickup_Longitude);
                            var dropoffRegion = nyc.FindRegion(trip.Dropoff_Latitude, trip.Dropoff_Longitude);

                            //aggrigate pickup & dropoffcounts per region
                            if (pickupRegion != null && dropoffRegion != null)
                            {
                                Pickups[pickupRegion.UID] = Pickups.GetOrCreate(pickupRegion.UID) + 1;
                                //Dropoffs[dropoffRegion.UID] = Dropoffs.GetOrCreate(dropoffRegion.UID) + 1;

                                var edge = new Tuple<int, int>(pickupRegion.Idx, dropoffRegion.Idx); //nyc.GetEdge(pickupRegion, dropoffRegion);
                                EdgeFlow[edge] = EdgeFlow.GetOrCreate(edge) + 1;
                            }
                        }
                        progress.inc();
                    }
                    #endregion

                    #region SaveDataToFile
                    //Save Node results to file
                    if (processNodes)
                    {
                        foreach (var region in nyc)
                        {
                            /** 
                             * NODE FIELDS:
                                "ID", "LandUse",
                                "Population", "MedianIncome", "PopDensity", "BachelorHigher", "Popularity", "Rating", "Places"
                                "OutFlow"
                             */
                            wNode.WriteLine(string.Join(',',
                                                region.Idx, region.LandUse,
                                                region.Population / NYCConst.TotalPopulation, region.MedianIncome, region.PopDensity, region.BachelorHigher,
                                                region.Popularity, region.Rating, region.Places,
                                                Pickups.GetOrCreate(region.UID)));
                        }
                        wNode.Flush();
                    }


                    //Save Edge results to file
                    if (processEdges)
                    {
                        foreach (var edge in EdgeFlow)
                        {
                            /** 
                             * EDGE FIELD:
                                "SenderID", "ReceiverID", "Distance", "InFlow"
                             */
                            var dist = nyc.Distance(edge.Key.Item1, edge.Key.Item2) / 3280.84;
                            wEdge.WriteLine(string.Join(',', edge.Key.Item1, edge.Key.Item2, dist, edge.Value));
                        }
                        wEdge.Flush();
                        wEdge.Close();
                    }

                    //Save Global results to file
                    /**
                     *  GLOBAL FIELD:
                        "Month", "DayofWeek", "Period", "Temp Avg", "Precipitation", "Snow"
                     */
                    if (processGlobals)
                    {
                        wGlobal.WriteLine(string.Join(',', date.Month, (int)date.DayOfWeek, period.Key,
                                            weather.TempAvg, weather.Precipitation, weather.SnowDepth));
                        wGlobal.Flush();
                    }

                    #endregion
                }
                progress.Stop();
            }
            if (processGlobals)
                wGlobal.Close();
            if (processNodes)
                wNode.Close();

            Console.WriteLine("Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion
        }

        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            TripRecordContext context = GetTrips();
            var Weather = NOAAWeatherAPI.ParseFile(NYCConst.WeatherData);
            City nyc = (File.Exists(NYCConst.NYCConfigFile)) ?  City.LoadCity(NYCConst.NYCConfigFile) : City.Factory(50);

            ProcessTrips(context, nyc, Weather);

            stopwatch.Stop();
            Console.WriteLine("Total Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            Console.WriteLine("Press Any Key ...");
            //Console.ReadKey();
        }
    }
}
