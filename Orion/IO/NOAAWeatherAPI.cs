using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Orion.Core.DataStructs;

namespace Orion.IO
{
    class NOAAWeatherAPI
    {
        class BaseObj
        {
            public WeatherEntry[] data;
            public JObject meta;
        }
        struct WeatherEntry
        {
            public string date;
            public JArray tempmax;
            public JArray tempmin;
            public JArray tempavg;
            public JArray tempdep;
            public JArray hdd;
            public JArray cdd;
            public JArray precip;
            public JArray snowfall;
            public JArray snowdepth;
        }

        public static Dictionary<DateTime, Weather> ParseFile(string file)
        {
            var json = File.ReadAllText(file);
            JObject temp = JsonConvert.DeserializeObject<JObject>(json);

            Dictionary<DateTime, Weather> weathers = new Dictionary<DateTime, Weather>();
            
            foreach(var child in temp["data"])
            {
                float.TryParse((string)child[3][0], out float tempavg);
                float.TryParse((string)child[7][0], out float precip);
                float.TryParse((string)child[8][0], out float snowfall);
                float.TryParse((string)child[9][0], out float snowdepth);

                weathers.Add(DateTime.Parse((string)child[0]), new Weather()
                {
                    TempAvg = tempavg,
                    Precipitation = precip,
                    SnowDepth = snowfall + snowdepth
                });
            }
            return weathers;
        }

    }
}
