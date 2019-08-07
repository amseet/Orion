using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Itinero;
using Itinero.Osm.Vehicles;
using Microsoft.AspNetCore.Mvc;
using Orion.DB;
using Orion.DB.Models;

namespace Orion.Web.Controllers.api.v1
{
    [Route("api/v1/[controller]")]
    public class RouteController : Controller
    {
        private readonly SqlContext _context;
        private readonly Router _router;
        public RouteController(SqlContext context)
        {
            _context = context;
            _router = RoutingService.Service.Router;
        }

        public string getItineroRoute(TripDataModel data)
        {
            try
            {
                var route = _router.Calculate(Vehicle.Car.Shortest(), (float)data.Pickup_Latitude, (float)data.Pickup_Longitude,
                                                                (float)data.Dropoff_Latitude, (float)data.Dropoff_Longitude);
                return route.ToJson();

                //if (Request.ContentType.Equals("application/vnd.geo+json"))
                //    return getRoute(data).ToGeoJson();
                //return getRoute(data).ToJson();
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string getGoogleMapRoute(TripDataModel data)
        {
            var origin = data.Pickup_Latitude.ToString() + ',' + data.Pickup_Longitude.ToString();
            var destination = data.Dropoff_Latitude.ToString() + ',' + data.Dropoff_Longitude.ToString();
            var key = "AIzaSyCh0xL_-zPlZRpJtB5-W3O5zu9vJFNywb0";//"AIzaSyBJHn6rSjKhXQs8WpgSOnuAa3uaTzPsFIo";

            string url = String.Format("https://maps.googleapis.com/maps/api/directions/json?origin={0}&destination={1}&key={2}",
                origin, destination, key);
            using (var httpClient = new HttpClient())
            {
                var response = httpClient.GetAsync(url).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id, string type = "Itinero")
        {
            TripDataModel data = _context.TripData.Find(id);

            if (type.Equals("Google"))
            {
                return getGoogleMapRoute(data);
            }

            return getItineroRoute(data);
        }

    }
}