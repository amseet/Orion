using Digitizer.models;
using System;
using System.IO;
using System.Reflection;

namespace Digitizer
{
    class Program
    {
        static void Main(string[] args)
        {
            TripDataModel nyc = new TripDataModel();
            string data = @"C:\Users\seetam\Documents\TaxiData\yellow_tripdata_2016-01.csv";

            using (var ostream = new StreamReader(File.OpenRead(data)))
            {
                ostream.ReadLine();
                using (BinaryWriter writer = new BinaryWriter(File.Open("data.dat", FileMode.Create)))
                {
                    //int idx = 0;
                    string[] tokens = ostream.ReadLine().Split(',');
                    //int VendorID = int.Parse(tokens[idx++]); ;

                    
                    CSVAttribute.GetColumnPosition<TripDataModel>(nameof(nyc.Pickup_datetime));

                    DateTime Pickup_Time = DateTime.Parse(tokens[0]);

                    //DateTime Dropoff_Time = DateTime.Parse(tokens[idx++]);
                    //int PassengerCount = int.Parse(tokens[idx++]);
                    //float TripDistance = float.Parse(tokens[idx++]);
                    //float Pickup_Long = float.Parse(tokens[idx++]);
                    //float Pickup_Lat = float.Parse(tokens[idx++]);
                    //int RatecodeID = int.Parse(tokens[idx++]);
                    //char StoreNFwd = char.Parse(tokens[idx++]);
                    //float Dropoff_Long = float.Parse(tokens[idx++]);
                    //float Dropoff_Lat = float.Parse(tokens[idx++]);

                    writer.Write(Pickup_Time.Ticks);
                    //writer.Write(Dropoff_Time.Ticks);
                    //writer.Write(TripDistance);
                    //writer.Write(Pickup_Long);
                    //writer.Write(Pickup_Lat);
                    //writer.Write(Dropoff_Long);
                    //writer.Write(Dropoff_Lat);
                }

                using(BinaryReader reader = new BinaryReader(File.Open("data.dat", FileMode.Open)))
                {
                    
                }
            }
        }
    }
}
