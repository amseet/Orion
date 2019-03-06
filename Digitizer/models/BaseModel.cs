using System;
using System.Collections.Generic;
using System.Text;

namespace Digitizer.Models
{
    public abstract class  BaseModel<T> where T : struct
    {
        public T[] Rows;
        public abstract T ParseTokens(string[] tokens);
    }
}
