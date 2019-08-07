using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Orion.Util
{
    public class Constants
    {
        public static readonly string Root_Dir =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Orion");

        public const string LookupTableExtension = "lookup";
        public const string TaxiData_FileName = "data.dat";
        public const string TaxiLookup_FileName = "data.lookup";
    }
}
