using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Orion.DB.Models
{
    public class TripRouteModel
    {
        [Key]
        public int RouteId { get; set; }

        [Required]
        public double Trip_Time { get; set; }

        [Required]
        public double Trip_Distance { get; set; }

        [ForeignKey("TripId")]
        public TripDataModel TripData { get; set; }
    }
}
