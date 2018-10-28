using System;
using System.Collections.Generic;
using System.Text;

namespace Orion.Models
{
    public struct TrafficAttributes
    {
        public int id;
        public float speed;
        public int travel_timel;
         
    }

    public class TrafficDataModel : BaseModel<TrafficAttributes>
    {
        public override TrafficAttributes ParseTokens(string[] tokens)
        {
            throw new NotImplementedException();
        }
    }
}
