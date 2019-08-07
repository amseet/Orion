using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Orion
{
    public class TripStats
    {

        // "Pickup Cell", "Dropoff Cell", "Time of Day", "# of Requests", "# of Time Period With Requests", "Probability %", "Average # of Requests/Time Of Day"
        static void GenTrips1(List<Trip.Attr> Trips, string filename)
        {
            Stopwatch stopwatch = new Stopwatch();
            #region SortingTrips
            Console.WriteLine(">sorting data<");
            stopwatch.Restart();
            var groupByTrip = Trips.OrderBy(i => i.PickupZone).ThenBy(i => i.DropoffZone)
                                    .GroupBy(t => new { t.PickupZone, t.DropoffZone });
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

                        writer.WriteLine(string.Join(',', trip.Key.PickupZone, trip.Key.DropoffZone, period.Key,
                                                        tripCount, periodCount, (float)periodCount / maxDays * 100, (float)tripCount / maxDays));
                    }
                }
            }
            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

        }

        // "Pickup Cell", "Hour", "# of Requests", "Probability" (RequestCountForCellPerHour / AllRequestCountForHour)
        // "Dropoff Cell", "Hour", "# of Requests", "Probability" (RequestCountForCellPerHour  / AllRequestCountForHour)
        public static void DistributionOfRequests(List<Trip.Attr> Trips, string filename)
        {
            Stopwatch stopwatch = new Stopwatch();

            #region SortingTrips
            Console.WriteLine(">Sorting & Grouping Data<");
            stopwatch.Restart();
            var sortedTrips = Trips.OrderBy(i => i.PickupZone).ThenBy(i => i.DropoffZone);
            var groupByPickup = sortedTrips.GroupBy(t => new { t.PickupZone, t.Hour });
            var groupByDropoff = sortedTrips.GroupBy(t => new { t.DropoffZone, t.Hour });

            var groupedByPeriod = sortedTrips.GroupBy(t => t.Hour);
            int[] TimePeriodCount = new int[groupedByPeriod.Count()];
            foreach (var period in groupedByPeriod)
                TimePeriodCount[(int)period.Key] = period.Count();

            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            #region SaveData
            Console.WriteLine(">Saving Data<");
            stopwatch.Restart();

            using (StreamWriter pickupWriter = new StreamWriter(string.Format(@"{0}_PickupTrips-2015.csv", filename)))
            {
                pickupWriter.AutoFlush = true;
                pickupWriter.WriteLine(string.Join(',', "Pickup Cell", "Hour of Day", "# of Requests", "Distribution"));
                foreach (var trip in groupByPickup)
                {
                    int AllTripsCountForPeriod = TimePeriodCount[(int)trip.Key.Hour];
                    int TripCount = trip.Count();
                    pickupWriter.WriteLine(string.Join(',', trip.Key.PickupZone, trip.Key.Hour,
                        TripCount, (float)TripCount / AllTripsCountForPeriod * 100));

                }
            }

            using (StreamWriter dropoffWriter = new StreamWriter(string.Format(@"{0}_DropoffTrips-2015.csv", filename)))
            {
                dropoffWriter.AutoFlush = true;
                dropoffWriter.WriteLine(string.Join(',', "Dropoff Cell", "Hour of Day", "# of Requests", "Distribution"));
                foreach (var trip in groupByDropoff)
                {
                    int AllTripsCountForPeriod = TimePeriodCount[(int)trip.Key.Hour];
                    int PeriodTripCount = trip.Count();
                    dropoffWriter.WriteLine(string.Join(',', trip.Key.DropoffZone, trip.Key.Hour,
                        PeriodTripCount, (float)PeriodTripCount / AllTripsCountForPeriod * 100));

                }
            }
            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion
        }

        // "Pickup Cell", "Dropoff Cell", "# of Requests", "Probability" (RequestCountForTrip / AllRequestCountForPickupCell)
        public static void ProbDistribution(List<Trip.Attr> Trips, DayOfWeek dayOfWeek)
        {
            Stopwatch stopwatch = new Stopwatch();

            #region SortingTrips
            Console.WriteLine(">sorting data<");
            stopwatch.Restart();
            var sortedTrips = Trips.OrderBy(i => i.PickupZone).ThenBy(i => i.DropoffZone);
            var groupByPickup = sortedTrips.GroupBy(t => t.PickupZone);
            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            #region SaveData
            Console.WriteLine(">Saving Data<");
            stopwatch.Restart();

            using (StreamWriter writer = new StreamWriter(string.Format(@".\{0}TripsProbDist-2015.csv", dayOfWeek)))
            {
                writer.AutoFlush = true;
                writer.WriteLine(string.Join(',', "Pickup Cell", "Dropoff Cell", "# of Requests", "Prob. Dist."));

                foreach (var pickup in groupByPickup)
                {
                    int PickupCount = pickup.Count();
                    var groupByDropoff = pickup.GroupBy(t => t.DropoffZone);
                    foreach(var dropoff in groupByDropoff)
                    {
                        int DropoffCount = dropoff.Count();
                        writer.WriteLine(string.Join(',', pickup.Key, dropoff.Key,
                                            DropoffCount, (float)DropoffCount / (float)PickupCount * 100));
                    }
                }
            }

            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion
        }

        //public static void DistributionByDirection(List<Trip.Attr> Trips, DayOfWeek dayOfWeek)
        //{
        //    Stopwatch stopwatch = new Stopwatch();

        //    #region SortingTrips
        //    Console.WriteLine(">sorting data<");
        //    stopwatch.Restart();
        //    var sortedTrips = Trips.OrderBy(i => i.PickupZone).ThenBy(i => i.DropoffZone);
        //    var groupByTrip = sortedTrips.GroupBy(t => t.PickupZone);


        //    Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
        //    #endregion

        //    #region SaveData
        //    Console.WriteLine(">Saving Data<");
        //    stopwatch.Restart();

        //    using (StreamWriter writer = new StreamWriter(string.Format(@"{0}PickupTrips-2015.csv", dayOfWeek)))
        //    {
        //        writer.AutoFlush = true;
        //        writer.WriteLine(string.Join(',', "Pickup Cell", "Direction", "Time Of Day", "# of Requests", "Distribution"));
        //        foreach (var trip in groupByTrip)
        //        {
        //            int TotalTripCount = trip.Count();
        //            var groupByDirection = trip.GroupBy(t => t.Direction);
        //            foreach (var dir in groupByDirection)
        //            {
        //                var groupByTime = dir.GroupBy(t => t.Hour);
        //                foreach (var time in groupByTime)
        //                {
        //                    int TripCount = time.Count();
        //                    writer.WriteLine(string.Join(',', trip.Key, dir.Key, time.Key,
        //                        TripCount, (float)TripCount / TotalTripCount * 100));
        //                }
        //            }
        //        }
        //    }

        //    Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
        //    #endregion
        //}

    }
}
