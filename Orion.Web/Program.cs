using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orion.Web.Models;

namespace Orion.Web
{
    public class Program
    {

        public static void Main(string[] args)
        {
            SqlContext context = new SqlContext();
            
            //RoutingService.InitService(@"C:\Users\seetam\Documents\Visual Studio 2017\Projects\Orion\Orion\bin\Debug\netcoreapp2.1\itinero.routerdb", context);
            //RoutingService.Service.RouteWithTraffic();
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
