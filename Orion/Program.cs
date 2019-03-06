using Microsoft.EntityFrameworkCore;
using Orion.DB;
using Orion.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Digitizer;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Orion
{
    class Program
    { 
        void routing()
        {
            RoutingService.InitService(@"C:\Users\seetam\Documents\Visual Studio 2017\Projects\Orion\Orion\bin\Debug\netcoreapp2.1\itinero.routerdb");
            RoutingService.Service.BatchRouting();
            RoutingService.Service.bench();
            TripFunctions func = new TripFunctions();
            //func.GetByDayOfWeek(DayOfWeek.Sunday, new DateTime(2015, 1, 1), new DateTime(2016, 1, 1));
        }

        public static GeoLattice ComputeLattice(int CellDimention = 200)
        {
            var stopwatch = new Stopwatch();
            //Computing Lattice
            #region ComputingLattice
            Console.WriteLine(">Computing Lattice<");
            stopwatch.Restart();

            GeoLattice lattice = new GeoLattice(40.6973f, -74.0200f, 40.8769f, -73.9013f, CellDimention); 
            Console.WriteLine("Lattice Dimentions: Rows {0}, Columns {1}, Size {2}.", lattice.Rows, lattice.Columns, lattice.Size);

            Console.WriteLine(">Save Lattice<");
            lattice.SaveLattice(string.Format(@".\Cells-{0}.csv", CellDimention));
            Console.WriteLine("Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);

            return lattice;
            #endregion
        }

        public static List<Trip.Attr> ImportTripDataFromFile(string filepath)
        {
            Stopwatch stopwatch = new Stopwatch();

            #region ImportDataFromFile
            stopwatch.Restart();
            Console.WriteLine(">Import Data<");
            var Trips = Binarizer.ImportBinarizedFile<Trip.Attr>(filepath);
            Console.WriteLine("Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);

            return Trips.ToList();
            #endregion
        }

        public static List<Trip.Attr> GenerateTrips(Lattice lattice, DayOfWeek dayOfWeek, bool Save = true)
        {
            Stopwatch stopwatch = new Stopwatch();
            SqlContext context = new SqlContext();
            
            #region DataRetrival
            Console.WriteLine(">Data Retrival<");
            stopwatch.Restart();
            var Rows = context.TripData.AsNoTracking()
                                        .Where(t => t.Trip_Day == dayOfWeek.ToString()
                                                && t.Trip_Date < new DateTime(2016, 1, 1))
                                        .Select(t => new {
                                            t.Pickup_Latitude,
                                            t.Pickup_Longitude,
                                            t.Dropoff_Latitude,
                                            t.Dropoff_Longitude,
                                            t.Trip_Date,
                                            t.Passenger_Count,
                                            t.Trip_Hour
                                        });
            Console.WriteLine("Row Count: {0}", Rows.Count());
            Console.WriteLine("Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            #region DataProcessing
            Console.WriteLine(">Populating Trip List<");
            stopwatch.Restart();
            List<Trip.Attr> Trips = new List<Trip.Attr>(Rows.Count());
            Rows.ForEachAsync(trip => {
                Cell pCell = lattice.GetCell(trip.Pickup_Latitude, trip.Pickup_Longitude);
                Cell dCell = lattice.GetCell(trip.Dropoff_Latitude, trip.Dropoff_Longitude);
                if (pCell != null && dCell != null)
                    Trips.Add(new Trip.Attr()
                    {
                        PickupCell = pCell.ID,
                        DropoffCell = dCell.ID,
                        PassengerCount = trip.Passenger_Count,
                        Date = trip.Trip_Date,
                        Hour = trip.Trip_Hour,
                        TimeOfDay = Trip.GetTimePeriod(trip.Trip_Hour),
                        Direction = Trip.GetDirection(lattice, pCell, dCell)
                    });
            }).Wait();
            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            #region SavingData
            if (Save)
            {
                Console.WriteLine(">Saving Trip Data<");
                stopwatch.Restart();
                Binarizer.Binarize(Trips, string.Format(@".\{0}Trips-2015.dat", dayOfWeek.ToString()));
                Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            }
            #endregion

            return Trips;
            
        }

        // "Pickup Cell", "Dropoff Cell", "Time of Day", "# of Requests", "# of Time Period With Requests", "Probability %", "Average # of Requests/Time Of Day"
        public static void GenTrips1(List<Trip.Attr> Trips, string filename)
        {
            Stopwatch stopwatch = new Stopwatch();
            #region SortingTrips
            Console.WriteLine(">sorting data<");
            stopwatch.Restart();
            var groupByTrip = Trips.OrderBy(i => i.PickupCell).ThenBy(i => i.DropoffCell)
                                    .GroupBy(t => new { t.PickupCell, t.DropoffCell });
            int maxUniqueTrips = groupByTrip.Count();
            var groupByDate = Trips.GroupBy(t => t.Date);
            int maxDays = groupByDate.Count();
            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);

            Console.WriteLine(">Saving Data<");
            stopwatch.Restart();
            using (StreamWriter writer = new StreamWriter(filename))
            {
                writer.AutoFlush = true;
                writer.WriteLine(string.Join(',', "Pickup Cell", "Dropoff Cell", "Time of Day",
                                            "# of Requests", "# of Time Period With Requests", "Probability %", "Average # of Requests/Time Of Day"));
                foreach (var trip in groupByTrip)
                {
                    var periods = trip.GroupBy(t => t.Hour); // Single Trip (O,D) per Time of Day
                    foreach (var period in periods)
                    {
                        int tripCount = period.Count();
                        int periodCount = period.GroupBy(t => t.Date).Count();

                        writer.WriteLine(string.Join(',', trip.Key.PickupCell, trip.Key.DropoffCell, period.Key,
                                                        tripCount, periodCount, (float)periodCount / maxDays * 100, (float)tripCount / maxDays));
                    }
                }
            }
            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

        }

        // "Pickup Cell", "Hour", "# of Requests", "Probability" (RequestCountForCellPerHour / AllRequestCountForHour)
        // "Dropoff Cell", "Hour", "# of Requests", "Probability" (RequestCountForCellPerHour  / AllRequestCountForHour)
        public static void DistributionOfRequests(List<Trip.Attr> Trips, DayOfWeek dayOfWeek)
        {
            Stopwatch stopwatch = new Stopwatch();

            #region SortingTrips
            Console.WriteLine(">sorting data<");
            stopwatch.Restart();
            var sortedTrips = Trips.OrderBy(i => i.PickupCell).ThenBy(i => i.DropoffCell);
            var groupByPickup = sortedTrips.GroupBy(t => new { t.PickupCell, t.Hour });
            var groupByDropoff = sortedTrips.GroupBy(t => new { t.DropoffCell, t.Hour });

            var groupedByPeriod = sortedTrips.GroupBy(t => t.Hour);
            int[] TimePeriodCount = new int[groupedByPeriod.Count()];
            foreach (var period in groupedByPeriod)
                TimePeriodCount[(int)period.Key] = period.Count();

            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            #region SaveData
            Console.WriteLine(">Saving Data<");
            stopwatch.Restart();

            using (StreamWriter pickupWriter = new StreamWriter(string.Format(@"{0}PickupTrips-2015.csv", dayOfWeek)))
            {
                pickupWriter.AutoFlush = true;
                pickupWriter.WriteLine(string.Join(',', "Pickup Cell", "Hour of Day", "# of Requests", "Distribution"));
                foreach (var trip in groupByPickup)
                {
                    int AllTripsCountForPeriod = TimePeriodCount[(int)trip.Key.Hour];
                    int TripCount = trip.Count();
                    pickupWriter.WriteLine(string.Join(',', trip.Key.PickupCell, trip.Key.Hour,
                        TripCount, (float)TripCount / AllTripsCountForPeriod * 100));

                }
            }

            using (StreamWriter dropoffWriter = new StreamWriter(string.Format(@"{0}DropoffTrips-2015.csv", dayOfWeek)))
            {
                dropoffWriter.AutoFlush = true;
                dropoffWriter.WriteLine(string.Join(',', "Dropoff Cell", "Hour of Day", "# of Requests", "Distribution"));
                foreach (var trip in groupByDropoff)
                {
                    int AllTripsCountForPeriod = TimePeriodCount[(int)trip.Key.Hour];
                    int PeriodTripCount = trip.Count();
                    dropoffWriter.WriteLine(string.Join(',', trip.Key.DropoffCell, trip.Key.Hour, 
                        PeriodTripCount, (float)PeriodTripCount / AllTripsCountForPeriod * 100));

                }
            }
            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion
        }

        // "Pickup Cell", "Dropoff Cell", "# of Requests", "Probability" (RequestCountForTrip / AllRequestCountForPickupCell)
        public static void GenTrips3(List<Trip.Attr> Trips, DayOfWeek dayOfWeek)
        {
            Stopwatch stopwatch = new Stopwatch();

            #region SortingTrips
            Console.WriteLine(">sorting data<");
            stopwatch.Restart();
            var sortedTrips = Trips.OrderBy(i => i.PickupCell).ThenBy(i => i.DropoffCell);
            var groupByTrip = sortedTrips.GroupBy(t => new { t.PickupCell, t.DropoffCell });

            var groupByPickup = sortedTrips.GroupBy(t =>  t.PickupCell);
            Dictionary<int,int> PickupCount = new Dictionary<int, int>();
            foreach (var pickup in groupByPickup)
                PickupCount.Add(pickup.Key, pickup.Count());

            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            #region SaveData
            Console.WriteLine(">Saving Data<");
            stopwatch.Restart();

            using (StreamWriter pickupWriter = new StreamWriter(string.Format(@"{0}PickupTrips-2015.csv", dayOfWeek)))
            {
                pickupWriter.AutoFlush = true;
                pickupWriter.WriteLine(string.Join(',', "Pickup Cell", "Dropoff Cell", "# of Requests", "Probability"));
                foreach (var trip in groupByTrip)
                {
                    int AllTripsCountForRegion = PickupCount[trip.Key.PickupCell];
                    int TripCount = trip.Count();
                    pickupWriter.WriteLine(string.Join(',', trip.Key.PickupCell, trip.Key.DropoffCell,
                         TripCount, (float)TripCount / AllTripsCountForRegion * 100));

                }
            }

            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion
        }

        public static void DistributionByDirection(List<Trip.Attr> Trips, DayOfWeek dayOfWeek)
        {
            Stopwatch stopwatch = new Stopwatch();

            #region SortingTrips
            Console.WriteLine(">sorting data<");
            stopwatch.Restart();
            var sortedTrips = Trips.OrderBy(i => i.PickupCell).ThenBy(i => i.DropoffCell);
            var groupByTrip = sortedTrips.GroupBy(t => t.PickupCell );

          
            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            #region SaveData
            Console.WriteLine(">Saving Data<");
            stopwatch.Restart();

            using (StreamWriter writer = new StreamWriter(string.Format(@"{0}PickupTrips-2015.csv", dayOfWeek)))
            {
                writer.AutoFlush = true;
                writer.WriteLine(string.Join(',', "Pickup Cell", "Direction", "Time Of Day", "# of Requests", "Distribution"));
                foreach (var trip in groupByTrip)
                {
                    int TotalTripCount = trip.Count();
                    var groupByDirection = trip.GroupBy(t => t.Direction);
                    foreach (var dir in groupByDirection)
                    {
                        var groupByTime = dir.GroupBy(t => t.Hour);
                        foreach(var time in groupByTime)
                        {
                            int TripCount = time.Count();
                            writer.WriteLine(string.Join(',', trip.Key, dir.Key, time.Key,
                                TripCount, (float)TripCount / TotalTripCount * 100));
                        }
                    }
                }
            }

            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion
        }

        public static void TripComputation()
        {   
            var stopwatch = new Stopwatch();
            var  lattice = ComputeLattice(200);

            // Data retrival from DB
            foreach (var dayOfWeek in (DayOfWeek[])Enum.GetValues(typeof(DayOfWeek)))
            {
                //var dayOfWeek = DayOfWeek.Friday;
                Console.WriteLine("=========== Day {0} ==========\n", dayOfWeek);

                //var Trips = GenerateTrips(lattice, dayOfWeek);
                var Trips = ImportTripDataFromFile(string.Format(@".\{0}Trips-2015.dat", dayOfWeek));
                DistributionOfRequests(Trips, dayOfWeek);

                #region Stats
                Console.WriteLine("# of Trips: {0}", Trips.Count());
                Console.WriteLine("=========== End Of Process ===========\n");
                #endregion
            }
        }

        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            TripComputation();

            stopwatch.Stop();
            Console.WriteLine("Total Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            Console.WriteLine("Press Any Key ...");
            Console.ReadKey();
        }
    }
}
