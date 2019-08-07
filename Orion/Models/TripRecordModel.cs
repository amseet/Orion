using System;
using System.Collections.Generic;
using System.Text;

namespace Orion.Models
{
    public abstract class TripRecordModel
    {
        protected long Idx = 0;

        public enum TimePeriod
        {
            Night,          // 0am - 5am
            EarlyMorning,   // 5am - 7am
            MorningRush,    // 7am - 9am 
            Morning,        // 9am - 12am
            Lunch,          // 12pm - 1pm
            Afternoon,      // 1pm - 4pm
            AfternoonRush,  // 4pm - 6pm
            Evening,        // 6pm - 10am
            Midnight,       // 10am - 12am
            Other
        }

        public static TimePeriod GetTimePeriod(int time)
        {
            Dictionary<TimePeriod, int[]> TimePeriond = new Dictionary<TimePeriod, int[]>() {
                {TimePeriod.Night,  new[] {0, 5} },
                {TimePeriod.EarlyMorning,  new[] {5, 7} },
                {TimePeriod.MorningRush,  new[] {7, 9} },
                {TimePeriod.Morning,  new[] {9, 12} },
                {TimePeriod.Lunch,  new[] {12, 13} },
                {TimePeriod.Afternoon,  new[] {13, 16} },
                {TimePeriod.AfternoonRush,  new[] {16, 18} },
                {TimePeriod.Evening,  new[] {18, 22} },
                {TimePeriod.Midnight,  new[] {22, 24} },
            };

            foreach (var per in TimePeriond)
                if (time >= per.Value[0] && time < per.Value[1])
                    return per.Key;
            return TimePeriod.Other;
        }

        public struct TripRecord
        {
            public long ID;
            public float Pickup_Latitude;
            public float Pickup_Longitude;
            public DateTime TimeStamp;

            public float Dropoff_Latitude;
            public float Dropoff_Longitude;

            public TimePeriod Period { get { return GetTimePeriod(TimeStamp.Hour); } }
            public bool IsValid { get { return Pickup_Latitude != 0 && Pickup_Longitude != 0 && Dropoff_Latitude != 0 && Dropoff_Longitude != 0; } }
            //public DateTime Dropoff_Time;

            //public int PassengerCount;
            public double Distance;
            //public TimeSpan Duration { get { return Dropoff_Time - Pickup_Time; } }
        }

        public abstract TripRecord ParseTokens(Dictionary<string,string> row);

    }
}
