using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Orion.IO;
using Orion.Util;
using System.Diagnostics;
using Orion.Core;
using Orion.Models;

namespace Orion.Cities.NYC
{
    public class NYCTaxiRecord : TripRecordModel
    {
        
        public NYCTaxiRecord()
        {
          
        }

        //Parser for Yellow Taxi Trips 2015 (All) & 2016 (first 6 months)
        public override TripRecord ParseTokens(Dictionary<string, string> row)
        {
            TripRecord record = new TripRecord();
            record.ID = Idx++;
            record.Distance = double.Parse(row["trip_distance"]);
            record.Pickup_Longitude = float.Parse(row["pickup_longitude"]);
            record.Pickup_Latitude = float.Parse(row["pickup_latitude"]);
            record.Dropoff_Longitude = float.Parse(row["dropoff_longitude"]);
            record.Dropoff_Latitude = float.Parse(row["dropoff_latitude"]);

            string key = row.Keys.Where(x => x.Contains("pickup_datetime"))?.First();
            record.TimeStamp = DateTime.Parse(row[key]);

            return record;
        }
    }
}
