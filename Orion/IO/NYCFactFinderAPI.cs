using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Linq;
using Orion.Util;

namespace Orion.IO
{
    public class NYCFactFinderAPI
    {
        const string baseURL = "https://factfinder-api.planninglabs.nyc";
        readonly string selectionURL = baseURL + '/' + "selection";
        readonly string profileURL = baseURL + '/' + "profile";
        const string demographic = "demographic";
        const string social = "social";
        const string economic = "economic";

        HttpClient client;

        struct Selection
        {
            public string status;
            public int id;
        }

        public struct Record
        {
            public int Population;         //Total population
            public int MedianIncome;       //Median household income (dollars)
            public int BachelorHigher;     //Bachelor's degree or higher
            public int PublicTrans;        //Public transportation

            public override string ToString()
            {
                return string.Format("Population: {0}\nMedian Income: {1}\nBachelor's degree or higher: {2}\nUse of Public Transportation: {3}",
                    Population, MedianIncome, BachelorHigher, PublicTrans);
            }
        }

        public NYCFactFinderAPI()
        {
            client = new HttpClient();
            client.Timeout = new TimeSpan(0, 2, 0);
        }

        string BuildURL(int id, string censusType)
        {
            return profileURL + '/' + id + '/' + censusType;
        }

        public Record GetData(string boro_ct201)
        {
            List<string> list = new List<string>();
            list.Add(boro_ct201);
            Record record = new Record();
            Task.Run(async () =>
            {
                record = await GetDataAsync(list.ToArray());
            }).Wait();

            return record;
        }

        public async Task<Record> GetDataAsync(string [] boro_ct201)
        {
            Record record = new Record();
            var param = new {
               type = "tracts" ,
               geoids = boro_ct201 
            };
            string json = JsonConvert.SerializeObject(param);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage message = new HttpResponseMessage();
            try {
                message = await client.PostAsync(selectionURL, content);
                if (message.IsSuccessStatusCode)
                {
                    var response = JsonConvert.DeserializeObject<Selection>(await message.Content.ReadAsStringAsync());

                    var url = BuildURL(response.id, demographic);
                    message = await client.GetAsync(url);
                    if (message.IsSuccessStatusCode)
                    {
                        JArray array = JsonConvert.DeserializeObject<JArray>(await message.Content.ReadAsStringAsync());
                        record.Population = array.Where(x => (string)x["variable"] == "mdpop_3").Select(x => (int)x["sum"]).First();
                    }

                    url = BuildURL(response.id, social);
                    message = await client.GetAsync(url);
                    if (message.IsSuccessStatusCode)
                    {
                        JArray array = JsonConvert.DeserializeObject<JArray>(await message.Content.ReadAsStringAsync());
                        record.BachelorHigher = array.Where(x => (string)x["variable"] == "ea_bchdh").Select(x => (int)x["sum"]).First();
                    }

                    url = BuildURL(response.id, economic);
                    message = await client.GetAsync(url);
                    if (message.IsSuccessStatusCode)
                    {
                        JArray array = JsonConvert.DeserializeObject<JArray>(await message.Content.ReadAsStringAsync());
                        record.MedianIncome = array.Where(x => (string)x["variable"] == "mdhhinc").Select(x => (int)x["sum"]).First();
                        record.PublicTrans = array.Where(x => (string)x["variable"] == "cw_pbtrns").Select(x => (int)x["sum"]).First();
                    }
                }
            }
            catch (Exception e){
                Console.Error.WriteLine("Stack:" + e.Message);
                Console.Error.WriteLine("Content:" + json);
            }
           
            return record;
        }

    }
}
