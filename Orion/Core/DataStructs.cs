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
        public int Idx;
        public string UID;
        public double Latitude;
        public double Longitude;
        public double Area;             //Region area
        public double Length;           //Region length
        public int Population;          //Total population (Domegraphic)
        public int MedianIncome;        //Median household income USD (Econimic)
        public float BachelorHigher;      //Bachelor's degree or higher (Social)
        public float PopDensity;         //Population Density per acre (Census)
        public int Popularity;          //Number of user reviewed the location based on Google reviews (Social)
        public double Rating;           //Star rating  based on Google reviews (Social)
        public int Places;              //Number of places that contributed to the popularity value (max 20)
        public float Attraction;
        public string LandUse;          //Zoning & land use category of location (categorical)
        public double DistCityCenter;   //Distance of location to closest city center (Hub)

        public RegionData() { }
        public RegionData(int id, double lat, double lng, double area, double length, 
                            CensusData c, RatingData r, float attraction, string landuse)
        {
            Debug.Assert(c.id == r.id);
            Idx = id;
            UID = c.id;
            Latitude = lat;
            Longitude = lng;
            Area = area;
            Length = length;
            Population = c.Population;
            MedianIncome = c.MedianIncome;
            BachelorHigher = c.BachelorHigher;
            PopDensity = c.PopDensity;
            Popularity = r.TotalUsers;
            Rating = r.Rating;
            Places = r.PlacesCount;
            Attraction = attraction;
            LandUse = landuse;
        }
    }

    public struct Weather
    {
        public float TempAvg;
        public float Precipitation;
        public float SnowDepth;
    }

    public struct Edge
    {
        public int SenderID;
        public int ReceiverID;
        public double Distance;
    }
}
