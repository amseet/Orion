using System;
using System.Collections.Generic;
using System.Text;

namespace Orion.IO.Models
{
    public abstract class  BaseModel<T> where T : struct
    {
        public T[] Rows;
        public abstract T ParseTokens(string[] tokens);
    }
}
