using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Orion.DB.Models
{
    [Table("tripdataByTripDate")]
    public class TripDataModel
    {
        [Key]
        public int TripId { get; set; }
        public DateTime Pickup_Datetime { get; set; }
        public DateTime Dropoff_Datetime { get; set; }
        public int Passenger_Count { get; set; }
        public double Trip_Distance { get; set; }
        public double Pickup_Longitude { get; set; }
        public double Pickup_Latitude { get; set; }
        public double Dropoff_Longitude { get; set; }
        public double Dropoff_Latitude { get; set; }
        public double Fare_Amount { get; set; }

        public DateTime Trip_Date { get; set; }
        public string Trip_Day { get; set; }
        public int Trip_Hour { get; set; }
        public int Trip_Minute { get; set; }
        // public List<TripRouteModel> TripRoutes { get; set; }

        override public string ToString()
        {
            return string.Join(',', TripId, Pickup_Datetime, Dropoff_Datetime, Passenger_Count);
        }
    }
}
