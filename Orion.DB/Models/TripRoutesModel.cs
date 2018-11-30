using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Orion.DB.Models
{
    public class TripRoutesModel
    {
        [Key]
        public int RouteId { get; set; }

        [Required]
        public string Trip_Route { get; set; }

        [Required]
        public bool withTraffic { get; set; }

        [Required]
        public double Trip_Time { get; set; }

        [Required]
        public double Trip_Distance { get; set; }

        public string Provider { get; set; }

        public string Route_Method { get; set; }

        [ForeignKey("TripId")]
        public TripDataModel TripData { get; set; }
    }
}
