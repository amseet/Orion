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
            Progress progress;
            Stopwatch stopwatch = new Stopwatch();

            #region ProcessingTrips
            Console.WriteLine(">Starting Process<");
            stopwatch.Restart();

            StreamWriter writer = new StreamWriter(Path.Combine(Constants.Root_Dir, "result.csv"));
            writer.AutoFlush = true;
            writer.WriteLine(string.Join(',', 
                                "ID", 
                                "DayofWeek", "Date", "Period", /*Global Temprol Data*/
                                "Population", "MedianIncome", "PopDensity", "BachelorHigher", /*Census Data*/
                                "Popularity", "LandUse",     /*Attraction and land use*/
                                "Temp Avg", "Precipitation", "Snow", /*Global Weather Data*/
                                "OutFlow", "InFlow"));
            
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
                    Dictionary<string, int> Dropoffs = new Dictionary<string, int>();
                    

                    #region FindContainingRegion
                    var sortedTrips = period.OrderBy(t => t.TimeStamp).ToArray();
                    
                    //Find containing region for every trip record 
                    foreach (var trip in sortedTrips)
                    {
                        if (trip.IsValid)
                        {
                            var pickupRegion= nyc.FindRegion(trip.Pickup_Latitude, trip.Pickup_Longitude);
                            var dropoffRegion = nyc.FindRegion(trip.Dropoff_Latitude, trip.Dropoff_Longitude);

                            //aggrigate pickup counts per region
                            if (pickupRegion != null)
                                Pickups[pickupRegion.id] = Pickups.GetOrCreate(pickupRegion.id) + 1;

                            //aggrigate dropoff counts per region
                            if (dropoffRegion != null)
                                Dropoffs[dropoffRegion.id] = Dropoffs.GetOrCreate(dropoffRegion.id) + 1;    
                        }
                        progress.inc();
                    }
                    
                    #endregion

                    #region SaveDataToFile
                    //Save results to file
                    foreach (var region in nyc)
                    {
                        var outflow = Pickups.GetOrCreate(region.id);
                        var inflow = Dropoffs.GetOrCreate(region.id);

                        //"ID", 
                        //"DayofWeek", "Date", "Period",                                    /*Global Temprol Data*/
                        //"Population", "MedianIncome", "Density", "BachelorHigher",        /*Census Data*/
                        //"Popularity", "LandUse",                                          /*Attraction and land use*/
                        //"Temp Avg", "Precipitation", "Snow",                              /*Global Weather Data*/
                        //"OutFlow", "InFlow"

                        writer.WriteLine(string.Join(',',
                                            region.id,
                                            date.DayOfWeek, date, period.Key,
                                            region.Population, region.MedianIncome, region.PopDensity, region.BachelorHigher,
                                            region.Popularity, region.LandUse,
                                            weather.TempAvg, weather.Precipitation, weather.SnowDepth,
                                            outflow, inflow));
                    }
                    #endregion
                }
                progress.Stop();
                writer.Flush();
            }
            writer.Close();
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
