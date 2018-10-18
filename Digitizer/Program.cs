using Digitizer.models;
using Itinero;
using Itinero.Osm.Vehicles;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;

namespace Digitizer
{
    class Binarize
    {
        /// <summary>
        /// Source: https://stackoverflow.com/questions/2871/reading-a-c-c-data-structure-in-c-sharp-from-a-byte-array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns>T</returns>

        public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var t = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();
            return t;
        }

        public static byte[] StructureToByteArray<T>(T t) where T : struct
        {
            byte[] bytes = new byte[Marshal.SizeOf<T>()];
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            Marshal.StructureToPtr<T>(t, handle.AddrOfPinnedObject(), false);
            handle.Free();
            return bytes;
        }

        public static T ByteArrayToClass<T>(byte[] bytes) where T : class
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var t = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();
            return t;
        }

        public static byte[] ClassToByteArray<T>(T t) where T : class
        {
            byte[] bytes = new byte[Marshal.SizeOf<T>()];
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            Marshal.StructureToPtr<T>(t, handle.AddrOfPinnedObject(), false);
            handle.Free();
            return bytes;
        }
    }

    public abstract class DSVAttributes
    {    }
    public abstract class DelimiterSeprable<T> where T : DSVAttributes, new()
    {
        public T[] Rows;
        public abstract T ParseTokens(string[] tokens);
         // Methods
        public void ConvertCsvToBinary(string file)
        {
            using (FileStream fstream = new FileStream(file, FileMode.Open, FileAccess.Read),
                    dstream = new FileStream(file + ".dat", FileMode.Create, FileAccess.Write))
            {
                BinaryWriter bwriter = new BinaryWriter(dstream);
                StreamReader sreader = new StreamReader(fstream);
                T att = new T();
                int rowcount = 0;
                int currentrow = 0;

                // Remove first line
                sreader.ReadLine();
                while (!sreader.EndOfStream)
                {
                    sreader.ReadLine();
                    rowcount++;
                }
                Console.WriteLine("Row Count = {0}", rowcount);

                // Reset stream
                sreader.BaseStream.Position = 0;
                // Remove first line
                sreader.ReadLine();

                var action = new ElapsedEventHandler((s, e) => {
                    Console.WriteLine("Proccesed {0}/{1} Rows", currentrow, rowcount);
                });
                Timer timer = new Timer();
                timer.Elapsed += action;
                timer.Interval = 1000;
                timer.Start();
                while (!sreader.EndOfStream)
                {


                    // Read entitre row as string and tokenize
                    string[] tokens = sreader.ReadLine().Split(',');

                    //Parse tokens
                    att = ParseTokens(tokens);

                    // Convert attributes into bytes
                    byte[] bytes = Binarize.ClassToByteArray<T>(att);

                    // Write bytes to file
                    bwriter.Write(bytes, 0, bytes.Length);

                    currentrow++;
                }
                action.Invoke(null, null);
                timer.Stop();
                timer.Close();
            }
        }

        public void ImportDatFile(string file)
        {
            using (FileStream fstream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                byte[] bytes;
                System.IO.BinaryReader breader = new System.IO.BinaryReader(fstream);
                long rowcount = fstream.Length / Marshal.SizeOf<T>();

                Rows = new T[rowcount];

                for (var i = 0; i < rowcount; i++)
                {
                    bytes = breader.ReadBytes(Marshal.SizeOf<T>());
                    Rows[i] = Binarize.ByteArrayToClass<T>(bytes);
                }
            }
        }

        public static T get(string file, long row)
        {
            BinaryReader reader = new BinaryReader(new FileStream(file, FileMode.Open, FileAccess.Read));
            long pos = row * Marshal.SizeOf<T>();
            reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf<T>());
            return Binarize.ByteArrayToClass<T>(bytes);
        }

    }

    public class Attributes : DSVAttributes
    {
        public DateTime pickup_datetime, dropoff_datetime;
        public int passenger_count;
        public float trip_distance;
        public double pickup_longitude, pickup_latitude;
        public double dropoff_longitude, dropoff_latitude;
        public float fare_amount;
    }
    public class NewYorkTaxiData : DelimiterSeprable<Attributes>
    {
        public override Attributes ParseTokens(string[] tokens)
        {
            int idx = 0;
            Attributes att = new Attributes();
            // Parse tokens
            int vendorID = int.Parse(tokens[idx++]); //discarded
            att.pickup_datetime = DateTime.Parse(tokens[idx++]);
            att.dropoff_datetime = DateTime.Parse(tokens[idx++]);
            att.passenger_count = int.Parse(tokens[idx++]);
            att.trip_distance = float.Parse(tokens[idx++]);
            att.pickup_longitude = double.Parse(tokens[idx++]);
            att.pickup_latitude = double.Parse(tokens[idx++]);
            int RatecodeID = int.Parse(tokens[idx++]); //discarded
            char store_and_fwd_flag = char.Parse(tokens[idx++]); //discarded
            att.dropoff_longitude = double.Parse(tokens[idx++]);
            att.dropoff_latitude = double.Parse(tokens[idx++]);
            int payment_type = int.Parse(tokens[idx++]); //discarded
            att.fare_amount = float.Parse(tokens[idx++]);

            return att;
        }
       
    }

    public class Program
    {
        static void convert(string file)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            NewYorkTaxiData td = new NewYorkTaxiData();
            td.ConvertCsvToBinary(file);

            watch.Stop();
            var elapsedSec = watch.ElapsedMilliseconds / 1000f;

            Console.WriteLine("Execution time: " + elapsedSec.ToString() + "sec");
        }

        static NewYorkTaxiData import(string file)
        {
            Console.WriteLine("Import Start");
            NewYorkTaxiData td = new NewYorkTaxiData();
            Task task = Task.Run(() =>
            {
                //string file = @".\..\..\data\yellow_tripdata_2016-02.csv";
                FileStream fstream = new FileStream(file, FileMode.Open, FileAccess.Read);
                var watch = System.Diagnostics.Stopwatch.StartNew();

                td.ImportDatFile(file + ".dat");

                watch.Stop();
                var elapsedSec = watch.ElapsedMilliseconds / 1000f;

                Console.WriteLine("Execution time: " + elapsedSec.ToString() + "sec");
            });
            task.Wait();
            Console.WriteLine("import complete");
            return td;
        }

        static void TestRouting()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var index = 0;
            NewYorkTaxiData td = new NewYorkTaxiData();
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

            NewYorkTaxiData td = import(file);
            Attributes att = NewYorkTaxiData.get(file + ".dat", 1000);
            //Console.WriteLine("Routing start");
            //TestRouting();
            //Console.WriteLine("Routing complete");
            Console.ReadKey();
        }
    }

    class Program2
    {
        struct TripData
        {
            long Dropoff_Time;
            long Pickup_Time;
            float TripDistance;
            float Pickup_Long;
            float Pickup_Lat;
            float Dropoff_Long;
            float Dropoff_Lat;
        }
        static void digitize()
        {
            string data = @"C:\Users\seetam\Documents\TaxiData\yellow_tripdata_2016-01.csv";

            using (var ostream = new StreamReader(File.OpenRead(data)))
            {
                ostream.ReadLine(); //drop header row
                int rowcount = 0;
                int currentrow = 0;

                while (!ostream.EndOfStream)
                {
                    ostream.ReadLine();
                    rowcount++;
                }
                Console.WriteLine("Row Count = {0}", rowcount);
                ostream.BaseStream.Position = 0;

                Timer timer = new Timer();
                timer.Elapsed += new ElapsedEventHandler((s, e) => {
                    Console.WriteLine("Proccesed {0}/{1} Rows", currentrow, rowcount);
                });
                timer.Interval = 1000;

                using (BinaryWriter writer = new BinaryWriter(File.Open("data.dat", FileMode.Create)))
                {
                    timer.Start();
                    ostream.ReadLine(); //drop header row
                    while (!ostream.EndOfStream)
                    {
                        int idx = 0;
                        string[] tokens = ostream.ReadLine().Split(',');
                        int VendorID = int.Parse(tokens[idx++]); ;
                        DateTime Dropoff_Time = DateTime.Parse(tokens[idx++]);
                        DateTime Pickup_Time = DateTime.Parse(tokens[idx++]);
                        int PassengerCount = int.Parse(tokens[idx++]);
                        float TripDistance = float.Parse(tokens[idx++]);
                        float Pickup_Long = float.Parse(tokens[idx++]);
                        float Pickup_Lat = float.Parse(tokens[idx++]);
                        int RatecodeID = int.Parse(tokens[idx++]);
                        char StoreNFwd = char.Parse(tokens[idx++]);
                        float Dropoff_Long = float.Parse(tokens[idx++]);
                        float Dropoff_Lat = float.Parse(tokens[idx++]);


                        writer.Write(Pickup_Time.Ticks);
                        writer.Write(Dropoff_Time.Ticks);
                        writer.Write(TripDistance);
                        writer.Write(Pickup_Long);
                        writer.Write(Pickup_Lat);
                        writer.Write(Dropoff_Long);
                        writer.Write(Dropoff_Lat);

                        currentrow++;
                    }
                    timer.Stop();
                }
            }
        }
        static void binreader()
        {
            using (BinaryReader reader = new BinaryReader(File.Open("data.dat", FileMode.Open)))
            {
                //reader.
            }
        }
        static void Main2(string[] args)
        {
            
        }
    }
}
