using System;
using System.IO;

namespace Digitizer
{
    class Program
    {
        struct TaxiTripData{
            DateTime Pickup_Time;
            DateTime Dropoff_Time;
            float TripDistance;
            float Pickup_Long;
            float Pickup_Lat;
            float Dropoff_Long;
            float Dropoff_Lat;
        }

        static void Main(string[] args)
        {
            string data = "yellow_tripdata_2016-01.csv";

            using (var ostream = new StreamReader(File.OpenRead(data)))
            {
                ostream.ReadLine();
                using (BinaryWriter writer = new BinaryWriter(File.Open("data.dat", FileMode.Create)))
                {
                    int idx = 0;
                    string[] tokens = ostream.ReadLine().Split(',');
                    int VendorID = int.Parse(tokens[idx++]); ;
                    DateTime Pickup_Time = DateTime.Parse(tokens[idx++]);
                    DateTime Dropoff_Time = DateTime.Parse(tokens[idx++]);
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
                }

                using(BinaryReader reader = new BinaryReader(File.Open("data.dat", FileMode.Open)))
                {
                    
                }
            }
        }
    }
}
