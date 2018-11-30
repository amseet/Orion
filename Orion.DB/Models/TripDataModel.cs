using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Orion.DB.Models
{
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

        public List<TripRoutesModel> TripRoutes { get; set; }
    }
}
