using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Orion.Web.Models
{
    public class TrafficDataModel
    {
        [Key]
        public int Id { get; set; }
        public double Speed { get; set; }
        public int Travel_Time { get; set; }
        public DateTime Data_As_Of { get; set; }
        public int Link_Id { get; set; }
        public string Link_Points { get; set; }
    }
}
