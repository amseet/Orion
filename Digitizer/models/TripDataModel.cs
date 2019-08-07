using System;
using System.Collections.Generic;
using System.Text;

namespace Orion.IO.Models
{
    public struct Location
    {
        float longitude;
        float latitude;
    }
    public struct TripAttributes
    {
        public DateTime pickup_datetime, dropoff_datetime;
        public int passenger_count;
        public float trip_distance;
        public double pickup_longitude, pickup_latitude;
        public double dropoff_longitude, dropoff_latitude;
        public float fare_amount;
    }

    public class TripDataModel : BaseModel<TripAttributes>
    {
        public override TripAttributes ParseTokens(string[] tokens)
        {
            int idx = 0;
            TripAttributes att = new TripAttributes();
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
}
