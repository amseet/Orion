using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Orion.Cities.NYC;
using Orion.Core;
using Orion.DB;
using Orion.Geo;
using Orion.IO;
using Orion.Core.DataStructs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orion.Util
{
    public static class Methods
    {
        public struct TripStr
        {
            public int PickupZone;
            public int DropoffZone;
            public DateTime Date;
            public int PassengerCount;
            public int Hour;
            public int Minute;
        }

        public static void ExportNYCTrips_ByDay(DateTime from, DateTime to, DayOfWeek day)
        {
            Stopwatch stopwatch = new Stopwatch();
            SqlContext context = new SqlContext();
            City use;
            //use.Deserialize(@"C:\Users\seetam\Documents\TaxiData\Zoning\nyzd.shp");

            #region DataRetrival
            Console.WriteLine(">Data Retrival<");
            stopwatch.Restart();
            var Rows = context.TripData.AsNoTracking()
                                        .Where(t => t.Trip_Date >= from
                                                && t.Trip_Date < to
                                                && t.Trip_Day == day.ToString())
                                        .Select(t => new
                                        {
                                            t.Pickup_Latitude,
                                            t.Pickup_Longitude,
                                            t.Dropoff_Latitude,
                                            t.Dropoff_Longitude,
                                            t.Passenger_Count,
                                            t.Trip_Date,
                                            t.Trip_Hour,
                                            t.Trip_Minute
                                        });
            Console.WriteLine("Row Count: {0}", Rows.Count());
            Console.WriteLine("Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            #region DataProcessing
            Console.WriteLine(">Populating Trip List<");
            stopwatch.Restart();
            List<TripStr> Trips = new List<TripStr>(Rows.Count());

            Progress progress = new Progress(3000, Rows.Count());
            progress.Start();
            Rows.ForEachAsync(t =>
            {
                //int pZone = use.FindZone(t.Pickup_Latitude, t.Pickup_Longitude);
                //int dZone = use.FindZone(t.Dropoff_Latitude, t.Dropoff_Longitude);
                //if (pZone != -1 && dZone != -1)
                //    Trips.Add(new TripStr
                //    {
                //        PickupZone = pZone,
                //        DropoffZone = dZone,
                //        PassengerCount = t.Passenger_Count,
                //        Date = t.Trip_Date,
                //        Hour = t.Trip_Hour,
                //        Minute = t.Trip_Minute
                //    });

                progress.inc();
            }).Wait();
            progress.Stop();

            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            #region SavingData
            Console.WriteLine(">Saving Trips to binary file<");
            stopwatch.Restart();
            //TODO
            //ToBinary.CsvToBinary(Trips, string.Format(@".\{0}Trips-2015.dat", day.ToString()));
            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion
        }

        public static Trip.Attr[] ImportTripDataByHour(DateTime date, int hour)
        {
            Stopwatch stopwatch = new Stopwatch();
            SqlContext context = new SqlContext();

            #region DataRetrival
            Console.WriteLine(">Retriving Data<");
            stopwatch.Restart();
            var Rows = context.TripData.AsNoTracking()
                                   .Where(t => t.Trip_Date == date && t.Trip_Hour == hour)
                                   .OrderBy(t => t.Pickup_Datetime)
                                   .Select(t => new Trip.Attr
                                   {
                                       Pickup = new GeoLocation(t.Pickup_Latitude, t.Pickup_Longitude),
                                       Dropoff = new GeoLocation(t.Dropoff_Latitude, t.Dropoff_Longitude),
                                       Date = t.Pickup_Datetime,
                                       Hour = t.Trip_Hour,
                                       Minute = t.Trip_Minute
                                   });

            Console.WriteLine("Row Count: {0}", Rows.Count());
            Console.WriteLine("Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            return Rows.ToArray();
        }
        public static Trip.Attr[] ImportTripDataByDay(DateTime date)
        {
            Stopwatch stopwatch = new Stopwatch();
            SqlContext context = new SqlContext();

            #region DataRetrival
            Console.WriteLine(">Retriving Data<");
            stopwatch.Restart();
            var Rows = context.TripData.AsNoTracking()
                                   .Where(t => t.Trip_Date == date)
                                   .OrderBy(t => t.Pickup_Datetime)
                                   .Select(t => new Trip.Attr
                                   {
                                       Pickup = new GeoLocation(t.Pickup_Latitude, t.Pickup_Longitude),
                                       Dropoff = new GeoLocation(t.Dropoff_Latitude, t.Dropoff_Longitude),
                                       Date = t.Pickup_Datetime,
                                       Period = Trip.GetTimePeriod(t.Trip_Hour),
                                   });

            Console.WriteLine("Row Count: {0}", Rows.Count());
            Console.WriteLine("Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            return Rows.ToArray();
        }


        public static void TripComputation()
        {
            var stopwatch = new Stopwatch();

            // Data retrival from DB
            foreach (var dayOfWeek in (DayOfWeek[])Enum.GetValues(typeof(DayOfWeek)))
            {
                Console.WriteLine("=========== Day {0} ==========\n", dayOfWeek);

                ExportNYCTrips_ByDay(new DateTime(2015, 1, 1), new DateTime(2016, 1, 1), dayOfWeek);
                // var Trips = GetTrips(new DateTime(2015, 1, 1), new DateTime(2016, 1, 1), dayOfWeek);
                // TripStats.ProbDistribution(Trips, dayOfWeek);

                #region Stats
                //Console.WriteLine("# of Trips: {0}", Trips.Count());
                Console.WriteLine("=========== End Of Process ===========\n");
                #endregion
            }
        }

        public static void GetCensusData(City nyc)
        {
            NYCFactFinderAPI ffapi = new NYCFactFinderAPI();
            NYCFactFinderAPI.Record[] result = new NYCFactFinderAPI.Record[nyc.Count];

            //ffapi.GetData("2016300");

            TaskPool pool = new TaskPool(1, nyc.Count);
            pool.Run(i =>
            {
                result[i] = ffapi.GetData(nyc.GetRegion(i).id);
            });

            string file = Path.Combine(NYCConst.CensusData_Dir, "Census_Data.json");
            File.WriteAllText(file, JsonConvert.SerializeObject(result));
            //Task.Run(async () =>
            //{
            //    await places.GetPlacesAsync(shape.boro_ct201,
            //                                shape.Geometry.Centroid.Centroid.Y,
            //                                shape.Geometry.Centroid.Centroid.X,
            //                                shape.Length / 2);

            //}).Wait();
        }

        public static void GetPlacesData(City nyc)
        {
            GooglePlacesAPI api = new GooglePlacesAPI();
            string file = Path.Combine(NYCConst.Places_Dir, "nycplaces.json");
            var tracts = nyc.Select(t => new { id = t.id, lng = t.Longitude, lat = t.Latitude, radius = t.Length / 6.562 });
            List <RatingData> tractPopularity = new List<RatingData>();

            Progress progress = new Progress(1000, tracts.Count());
            progress.Start();
            do{
                Parallel.ForEach(tracts, tract => {
                    GooglePlacesAPI.Place[] places = null;
                    Task.Run(async () => {
                        places = await api.GetPlacesAsync(tract.lat, tract.lng, tract.radius);
                    }).Wait();

                    RatingData data = new RatingData();
                    float ratingTotal = 0;
                    int userTotal = 0;
                    int placesCount = 0;
                    foreach (var place in places)
                    {
                        if(place.User_ratings_total > 0)
                        {
                            ratingTotal += place.Rating * place.User_ratings_total;
                            userTotal += place.User_ratings_total;
                            placesCount++;
                        }
                        
                    }
                    data.id = tract.id;
                    data.Rating = (userTotal > 0) ? ratingTotal / userTotal : 0;
                    data.TotalUsers = userTotal;
                    data.PlacesCount = placesCount;
                    tractPopularity.Add(data);
                    progress.inc();
                });
                tracts = tracts.Where(x => !tractPopularity.Select(i => i.id).Contains(x.id));
            }while (tracts.Count() > 0);

            progress.Stop();
            Console.WriteLine("Processed {0} places", tractPopularity.Count());
            File.WriteAllTextAsync(file, JsonConvert.SerializeObject(tractPopularity));

        }

        //Source: https://codeblog.jonskeet.uk/2008/02/05/a-simple-extension-method-but-a-beautiful-one/
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
                                               TKey key)
        where TValue : new()
        {
            TValue ret;
            if (!dictionary.TryGetValue(key, out ret))
            {
                ret = new TValue();
                dictionary[key] = ret;
            }
            return ret;
        }

    }
}
