using Orion.DB;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Orion.DB.Models;
using System.IO;

namespace Orion
{
    class TripFunctions
    {
        SqlContext context;

        public TripFunctions()
        {
            context = new SqlContext();
        }      

        public void GetByDayOfWeek(List<DayOfWeek> days, DateTime start, DateTime end)
        {
            var trips = context.TripData.Where(t => t.Pickup_Datetime >= start 
                                                && t.Pickup_Datetime < end 
                                                && days.Contains(t.Pickup_Datetime.DayOfWeek));
            List<string> data = new List<string>();
            //foreach(var trip in trips)
            //{
            //    data.Add(string.Join(',', trip.))
            //}
            //Save(@"",trips.ToList());
        }

        void Save(string file, List<string> data)
        {
           // File.WriteAllText(file, string.Join('\n', );
        }

    }
}
