using Orion.DB;
using Orion.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Orion.DB.Models;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Orion.IO;
using Orion.Geo;

namespace Orion
{
    static  class Stub
    {

        static void printLn(string msg = "")
        {
            Console.WriteLine(msg);
        }

        static void print(string msg = "")
        {
            Console.Write(msg);
        }

        public enum EnumCommands
        {
            Exit,
            ComputeLattice,
            GetRoute,
            GetTrips,
            ComputeProbDist,
            SaveAsCSV,
            SaveAsBinary
        }

        static Dictionary<EnumCommands, string> Commands = new Dictionary<EnumCommands, string>()
        {
            {EnumCommands.Exit, "Exit." },
            {EnumCommands.ComputeLattice, "Compute Lattice." },
            {EnumCommands.GetRoute, "Get shortest path between two locations." },
            {EnumCommands.GetTrips, "Get Trips." },

            {EnumCommands.ComputeProbDist, "Compute probability distribution for trips." },
            {EnumCommands.SaveAsCSV, "Save Raw Trips as CSV file." },
            {EnumCommands.SaveAsBinary, "Save Trips as Binary file" },
            //{EnumCommands.GetTripsByDate, "Get shortest path between two locations." },
            //{EnumCommands.GetTripsByDayOfWeek, "Get shortest path between two locations." },
            //{EnumCommands.GetTripsByTimePeriod, "Get shortest path between two locations." },
        };

        static double[] ParseLocation(string str)
        {
            double[] latlong = new double[2];
            string[] tokens = str.Split(',');
            int i = 0;
            foreach (var token in tokens)
                latlong[i++] = double.Parse(token);
            return latlong;
        }

        static void PrintCommands()
        {
            foreach (var command in Commands)
                printLn((int)command.Key + ". " + command.Value);
        }

        static int GetCommand()
        {
            if (int.TryParse(Console.ReadLine(), out int result))
                return result;
            return -1;
        }

        static DateTime ParseDate(string str)
        {
            string[] d = str.Split('-');
            if (d.Length == 0)
                return DateTime.MinValue;

            int year = int.Parse(d[0]);
            int month = int.Parse(d[1]);
            int day = int.Parse(d[2]);

            return new DateTime(year, month, day);
        }

        static DayOfWeek ParseDay(string str)
        {
            return Enum.Parse<DayOfWeek>(str, true);
        }

        static DayOfWeek[] ParseDays(string str)
        {
            DayOfWeek[] all = { DayOfWeek.Friday, DayOfWeek.Monday, DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Thursday, DayOfWeek.Tuesday, DayOfWeek.Wednesday };

            if (string.IsNullOrEmpty(str))
                return all;

            str = str.Substring(0, (str.Length > 7) ? 7 : str.Length);

            char[] tokens = str.ToCharArray();
            List<DayOfWeek> days = new List<DayOfWeek>();
            try
            {
                for (int i = 0; i < tokens.Length; i++)
                    if (tokens[i] != '0')
                        days.Add((DayOfWeek)i);
            }
            catch (Exception e)
            {
                printLn(e.Message);
                return all;
            }

            return days.ToArray();
        }

        static void routing()
        {
            RoutingService.InitService(@"C:\Users\seetam\Documents\Visual Studio 2017\Projects\Orion\Orion\bin\Debug\netcoreapp2.1\itinero.routerdb");
            RoutingService.Service.BatchRouting();
            RoutingService.Service.bench();
            TripFunctions func = new TripFunctions();
            //func.GetByDayOfWeek(DayOfWeek.Sunday, new DateTime(2015, 1, 1), new DateTime(2016, 1, 1));
        }

        public static GeoLattice ComputeLattice()
        {
            Stopwatch stopwatch = new Stopwatch();

            printLn(">>Enter cell dimentions in meters (n x n):");
            int x = int.Parse(Console.ReadLine());
            GeoLattice geoLattice = GeoLattice.ComputeLattice(x);
            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            return geoLattice;
        }

        public static List<Trip.Attr> ImportTripDataFromFile(string filepath)
        {
            Stopwatch stopwatch = new Stopwatch();

            #region ImportDataFromFile
            stopwatch.Restart();
            Console.WriteLine(">Import Data<");
            var Trips = BinaryStream.ImportBinaryFile<Trip.Attr>(new StreamReader(filepath).BaseStream);
            Console.WriteLine("Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);

            return Trips.ToList();
            #endregion
        }

        public static List<Trip.Attr> GetTrips(Lattice lattice, DateTime from, DateTime to, DayOfWeek day, bool Save = true)
        {
            Stopwatch stopwatch = new Stopwatch();
            SqlContext context = new SqlContext();

            #region DataRetrival
            Console.WriteLine(">Data Retrival<");
            stopwatch.Restart();
            var Rows = context.TripData.AsNoTracking()
                                        .Where(t => t.Trip_Day == day.ToString()
                                                && t.Trip_Date >= from
                                                && t.Trip_Date < to)
                                        .Select(t => new {
                                            t.Pickup_Latitude,
                                            t.Pickup_Longitude,
                                            t.Dropoff_Latitude,
                                            t.Dropoff_Longitude,
                                            t.Pickup_Datetime,
                                            t.Passenger_Count
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
                        Pickup = new GeoLocation(trip.Pickup_Latitude, trip.Pickup_Longitude),
                        Dropoff = new GeoLocation(trip.Dropoff_Latitude, trip.Dropoff_Longitude),
                        PickupZone = pCell.ID,
                        DropoffZone = dCell.ID,
                        PassengerCount = trip.Passenger_Count,
                        Date = trip.Pickup_Datetime.Date,
                        Hour = trip.Pickup_Datetime.Hour,
                        Minute = trip.Pickup_Datetime.Minute,
                        //Direction = Trip.GetDirection(lattice, pCell, dCell)
                    });
            }).Wait();
            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            #region SavingData
            //if (Save)
            //{
            //    Console.WriteLine(">Saving Trip Data<");
            //    stopwatch.Restart();
            //    Binarizer.Binarize(Trips, string.Format(@".\{0}Trips-2015.dat", dayOfWeek.ToString()));
            //    Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            //}
            #endregion

            return Trips;

        }

        public static List<Trip.Attr> ComputeNearestCentroid(GeoLattice lattice, List<TripDataModel> Rows)
        {
            Stopwatch stopwatch = new Stopwatch();
            List<Trip.Attr> Trips = lattice.ComputeNearestCentroid(Rows);
            Console.WriteLine("Time Elapsed: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            return Trips;
        }

        public static void TripComputation()
        {
            var stopwatch = new Stopwatch();
            var lattice = ComputeLattice();

            // Data retrival from DB
            foreach (var dayOfWeek in (DayOfWeek[])Enum.GetValues(typeof(DayOfWeek)))
            {
                //var dayOfWeek = DayOfWeek.Friday;
                Console.WriteLine("=========== Day {0} ==========\n", dayOfWeek);

                //var Trips = GenerateTrips(lattice, dayOfWeek);
                var Trips = ImportTripDataFromFile(string.Format(@".\{0}Trips-2015.dat", dayOfWeek));
                TripStats.DistributionOfRequests(Trips, dayOfWeek.ToString());

                #region Stats
                Console.WriteLine("# of Trips: {0}", Trips.Count());
                Console.WriteLine("=========== End Of Process ===========\n");
                #endregion
            }
        }

        public static List<Trip.Attr> GetTrips(GeoLattice lattice)
        {
            printLn(">>Enter Start Date (format: YYYY-MM-DD):");
            DateTime from = ParseDate(Console.ReadLine());
            printLn(">You entered: " + from.ToString());

            printLn(">>Enter End Date (format: YYYY-MM-DD):");
            DateTime to = ParseDate(Console.ReadLine());
            printLn(">You entered: " + to.ToString());

            printLn(">>Enter Day of Week (e.g. Saturday):");
            DayOfWeek day = ParseDay(Console.ReadLine());
            printLn(">You entered: " + day);

            return GetTrips(lattice, from, to, day, false);
        }

        static List<TripDataModel> tripsDB = new List<TripDataModel>();
        static List<Trip.Attr> trips = new List<Trip.Attr>();
        static GeoLattice lattice = null;
        static string saveFile = null;

        public static void RunCommand(EnumCommands iCommand)
        {
            if (Commands.ContainsKey(iCommand))
            {
                switch (iCommand)
                {
                    case EnumCommands.ComputeLattice:
                        printLn(">>Compute Lattice");
                        lattice = ComputeLattice();
                        break;

                    case EnumCommands.GetRoute:
                        printLn(">>Get Route");
                        printLn(">>Source Location (lat, long):");
                        double[] source = ParseLocation(Console.ReadLine());

                        printLn(">>Destination Location (lat, long):");
                        double[] destination = ParseLocation(Console.ReadLine());

                        string result = RoutingService.Service.GetRoute(source, destination);
                        string path = Path.Combine(saveFile, "route.geojson");
                        File.WriteAllText(path, result);
                        printLn("Route saved to : " + path);
                        break;

                    case EnumCommands.GetTrips:
                        if (lattice == null)
                            RunCommand(EnumCommands.ComputeLattice);

                        printLn(">>Get Trips");
                        trips = GetTrips(lattice);
                        break;

                    case EnumCommands.ComputeProbDist:
                        if (trips.Count == 0)
                            RunCommand(EnumCommands.GetTrips);
                        if (trips.Count > 0)
                        {
                            printLn(">>Save distribution results As:");
                            string file = Console.ReadLine();
                            printLn(">>Compute Probability Distribution");
                            TripStats.DistributionOfRequests(trips, file);
                        }
                        else
                            printLn("No trips available. Please check parameters.");
                        break;

                    case EnumCommands.SaveAsBinary:
                        printLn(">>Enter File Name:");
                        string binfile = Console.ReadLine();
                        printLn(">Saving Trip Data<");
                        //TODO
                        //ToBinary.CsvToBinary(trips, Path.Combine(saveFile, binfile + ".dat"));
                        printLn(string.Format("{0} Trips Saved", trips.Count));
                        break;

                    case EnumCommands.SaveAsCSV:
                        printLn(">>Enter File Name:");
                        string csvfile = Console.ReadLine();
                        printLn(">Saving Raw Trip Data<");
                        TripFunctions.Save(tripsDB, Path.Combine(saveFile, csvfile + ".csv"));
                        printLn(string.Format("{0} Trips Saved", tripsDB.Count));
                        break;

                    default:
                        break;
                }
            }
        }

        public static void P1()
        {
            printLn(">>Enter save directory:");
            saveFile = Path.GetFullPath(Console.ReadLine());
            printLn(saveFile);
            printLn();

            EnumCommands iCommand;
            do
            {
                printLn(">>Command List:");
                PrintCommands();
                printLn();

                printLn(">>Enter Command");
                iCommand = (EnumCommands)GetCommand();
                printLn();

                RunCommand(iCommand);
                printLn();
            } while (iCommand != EnumCommands.Exit);
        }
    }
}
