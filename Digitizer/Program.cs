using Itinero;
using Itinero.Osm.Vehicles;
using Orion.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Digitizer
{
    public class Program
    {
        static void convert(string file)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            TripDataModel td = new TripDataModel();
            Binarizer.Binarize<TripAttributes>(file, file + ".dat", ',', td.ParseTokens);

            watch.Stop();
            var elapsedSec = watch.ElapsedMilliseconds / 1000f;

            Console.WriteLine("Execution time: " + elapsedSec.ToString() + "sec");
        }

        static TripDataModel import(string file)
        {
            Console.WriteLine("Import Start");
            TripDataModel td = new TripDataModel();
            Task task = Task.Run(() =>
            {
                //string file = @".\..\..\data\yellow_tripdata_2016-02.csv";
                FileStream fstream = new FileStream(file, FileMode.Open, FileAccess.Read);
                var watch = System.Diagnostics.Stopwatch.StartNew();

                td.Rows = Binarizer.ImportBinarizedFile<TripAttributes>(file + ".dat");

                watch.Stop();
                var elapsedSec = watch.ElapsedMilliseconds / 1000f;

                Console.WriteLine("Execution time: " + elapsedSec.ToString() + "sec");
            });
            task.Wait();
            Console.WriteLine("Import complete");
            return td;
        }

        static void TestRouting()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var index = 0;
            TripDataModel td = new TripDataModel();
            using (var stream = new FileInfo(@"C:\Users\ahmad\Documents\Visual Studio 2015\Projects\TaxiDataSimulator\data\NewYork.routerdb").OpenRead())
            {
                var routerDb = RouterDb.Deserialize(stream);
                var router = new Router(routerDb);
                foreach (var data in td.Rows)
                {
                    try
                    {
                        var route = router.Calculate(Vehicle.Car.Shortest(), (float)data.pickup_latitude, (float)data.pickup_longitude,
                                                                        (float)data.dropoff_latitude, (float)data.dropoff_longitude);

                        if (index % 100 == 0)
                            Console.WriteLine(index);
                        if (index++ > 10000)
                            break;
                        //if (Request.ContentType.Equals("application/vnd.geo+json"))
                        //    return getRoute(data).ToGeoJson();
                        //return getRoute(data).ToJson();
                    }
                    catch (Exception e)
                    {

                    }
                }

            }

            watch.Stop();
            var elapsedSec = watch.ElapsedMilliseconds / 1000f;

            Console.WriteLine("Execution time: " + elapsedSec.ToString() + "sec");
        }

        public static void Main(string[] args)
        {
            string file = @"C:\Users\seetam\Documents\TaxiData\yellow_tripdata_2016-01.csv";
            convert(file);

            TripDataModel td = import(file);
            TripAttributes att = Binarizer.Get<TripAttributes>(file + ".dat", 1000);

            //Console.WriteLine("Routing start");
            //TestRouting();
            //Console.WriteLine("Routing complete");
            Console.ReadKey();
        }
    }
}
