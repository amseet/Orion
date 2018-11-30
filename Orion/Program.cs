using Itinero;
using Itinero.Attributes;
using Itinero.Data.Network;
using Itinero.IO.OpenLR;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using Itinero.Profiles;
using OpenLR;
using OpenLR.Osm;
using Orion.DB;
using System;
using System.Collections.Generic;
using System.IO;

namespace Orion
{
    class Program
    {
        static void Main(string[] args)
        {
            SqlContext context = new SqlContext();

            RoutingService.InitService(@"C:\Users\seetam\Documents\Visual Studio 2017\Projects\Orion\Orion\bin\Debug\netcoreapp2.1\itinero.routerdb", context);
            RoutingService.Service.BatchRouting();
            //RoutingService.Service.bench();
            Console.ReadKey();
        }
    }
}
