using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Orion.Util;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Orion.IO
{
    public class GooglePlacesAPI
    {
        HttpClient client;

        public struct Place
        {
            public string Name;
            public string Place_id;
            public float Rating;
            public int User_ratings_total;
            public float Popularity { get
                {
                    return Rating * User_ratings_total;
                } }
            public string[] Types;
        }

        private string Apikey = "-xQLPo";
        private string base_url = "https://maps.googleapis.com/maps/api/place/nearbysearch/json?";
        private string alt_url = "https://maps.googleapis.com/maps/api/place/findplacefromtext/json?";

        public GooglePlacesAPI()
        {
            client = new HttpClient();
        }

        public async Task<Place[]> GetPlacesAsync(double lat, double lng, double rad)
        {
            string parameters = string.Format("location={0},{1}&radius={2}&rankby={3}&type={4}&key={5}",
                                                lat, lng, rad, "prominence", "point_of_interest", Apikey);

            string alt_param = string.Format("input={0}&inputtype={1}&fields={2}&locationbias=circular:{3}@{4},{5}&key={6}",
                                                "place", "textquery", "name,place_id,rating,user_ratings_total", rad, lat, lng, Apikey);

            var message = await client.GetAsync(base_url + parameters);
            if (message.IsSuccessStatusCode)
            {
                JObject response = JsonConvert.DeserializeObject<JObject>(await message.Content.ReadAsStringAsync());
                //cache responses
                Place [] results = response["results"].ToObject<Place[]>();

                return results;
            }
            else
            {
                Console.Error.WriteLine(message);
            }
            Console.Error.WriteLine("Empty Place");
            return new Place[0];
        }
    }
}
