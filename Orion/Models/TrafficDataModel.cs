using System;
using System.Collections.Generic;
using System.Text;

namespace Orion.Models
{
    public struct TrafficAttributes
    {
        long ID;
    }

    public class TrafficDataModel : BaseModel<TrafficAttributes>
    {
        public override TrafficAttributes ParseTokens(string[] tokens)
        {
            throw new NotImplementedException();
        }
    }
}
