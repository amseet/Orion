using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Orion.Core.DataStructs
{
    public struct CensusData
    {
        public string id;
        public int Population;          //Total population (Domegraphic)
        public int MedianIncome;        //Median household income USD (Econimic)
        public float BachelorHigher;      //Bachelor's degree or higher (Social)
        public float PopDensity;          //Population Density per acre (Census)
    }

    public struct RatingData
    {
        public string id;
        public float Rating;
        public int TotalUsers;
        public int PlacesCount;
    }

    public class RegionData
    {
        public string id;
        public double Latitude;
        public double Longitude;
        public double Area;             //Region area
        public double Length;           //Region length
        public int Population;          //Total population (Domegraphic)
        public int MedianIncome;        //Median household income USD (Econimic)
        public float BachelorHigher;      //Bachelor's degree or higher (Social)
        public float PopDensity;         //Population Density per acre (Census)
        public float Popularity;        //Location user rating based on Google reviews (Social)
        public int Places;              //Number of places that contributed to the popularity value (max 20)
        public string LandUse;          //Zoning & land use category of location (categorical)
        public double DistCityCenter;   //Distance of location to closest city center (Hub)

        public RegionData() { }
        public RegionData(double lat, double lng, double area, double length, 
                            CensusData c, RatingData r, string landuse, double distance)
        {
            Debug.Assert(c.id == r.id);
            id = c.id;
            Latitude = lat;
            Longitude = lng;
            Area = area;
            Length = length;
            Population = c.Population;
            MedianIncome = c.MedianIncome;
            BachelorHigher = c.BachelorHigher;
            PopDensity = c.PopDensity;
            Popularity = r.Rating * r.TotalUsers;
            Places = r.PlacesCount;
            LandUse = landuse;
            DistCityCenter = distance;
        }
    }

    public struct Weather
    {
        public float TempAvg;
        public float Precipitation;
        public float SnowDepth;
    }

}
