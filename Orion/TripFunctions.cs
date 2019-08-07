using Orion.DB;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Orion.DB.Models;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Orion
{
    class TripFunctions
    {
        SqlContext context;

        public TripFunctions()
        {
            context = new SqlContext();
        }      

        public static List<TripDataModel> GetTrips(DateTime from, DateTime to, DayOfWeek [] days)
        {
            Console.WriteLine(">Retriving data<");
            List<TripDataModel> lst;
            SqlContext context = new SqlContext();
            if(days.Length >= 0)
            {
                var q = context.TripData
                    .Where(t => t.Trip_Day == days[0].ToString()
                            && t.Trip_Date >= from
                            && t.Trip_Date < to)
                    .AsNoTracking();
                lst = q.ToList();
            }
            else
            {
                var q = context.TripData
                    .AsNoTracking()
                    .Where(t => t.Trip_Date >= from && t.Trip_Date < to);
                lst = q.ToList();
            }
            return lst;
        }

        public static void Save(List<TripDataModel> trips, string file)
        {
            using (StreamWriter writer = new StreamWriter(file))
            {
                writer.AutoFlush = true;
                foreach (var trip in trips)
                {
                    writer.WriteLine(string.Join(',', trip.TripId,
                        trip.Pickup_Datetime, trip.Dropoff_Datetime,
                        trip.Pickup_Latitude + ";" + trip.Pickup_Longitude,
                        trip.Dropoff_Latitude + ";" + trip.Dropoff_Longitude,
                        trip.Passenger_Count,
                        trip.Trip_Day,
                        trip.Trip_Distance,
                        trip.Fare_Amount));
                }
            }
        }

    }
}
