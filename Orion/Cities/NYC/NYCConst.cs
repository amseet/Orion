using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Orion.Util;
namespace Orion.Cities.NYC
{
    public class NYCConst : Constants
    {
        public const float TotalPopulation = 8175133.0f;
        public readonly static string Base_Dir = Path.Combine(Root_Dir, "NYC");
        public readonly static string Trips_Dir = Path.Combine(Base_Dir, "TripRecords");
        public readonly static string TripsRaw_Dir = Path.Combine(Trips_Dir, "Raw");
        public readonly static string TripsBin_Dir = Path.Combine(Trips_Dir, "Bin");
        public readonly static string Layers_Dir = Path.Combine(Base_Dir, "Layers");
        public readonly static string LandUse_Dir = Path.Combine(Layers_Dir, "Land_Use");
        public readonly static string CensusBlocks_Dir = Path.Combine(Layers_Dir, "Census_Blocks");
        public readonly static string CensusTracts_Dir = Path.Combine(Layers_Dir, "Census_Tracts");
        public readonly static string TaxiZones_Dir = Path.Combine(Layers_Dir, "taxi_zones");
        public readonly static string Places_Dir = Path.Combine(Base_Dir, "Places");
        public readonly static string CensusData_Dir = Path.Combine(Base_Dir, "Census");
        public readonly static string WeatherData_Dir = Path.Combine(Base_Dir, "Weather");
        public readonly static string RoadNetwork_Dir = Path.Combine(Base_Dir, "Itinero");

        public readonly static string LandUse_Layer = Path.Combine(LandUse_Dir, "nyzd.shp");
        public readonly static string CensusBlocks_Layer = Path.Combine(CensusBlocks_Dir, "Census_Blocks.shp");
        public readonly static string CensusTracts_Layer = Path.Combine(CensusTracts_Dir, "Census_Tracts.shp");
        public readonly static string TaxiZones_Layer = Path.Combine(TaxiZones_Dir, "taxi_zones.shp");

        public readonly static string NYCConfigFile = Path.Combine(Base_Dir, "NYC.config");
        public readonly static string TaxiData = Path.Combine(TripsBin_Dir, TaxiData_FileName);
        public readonly static string TaxiLookup = Path.Combine(TripsBin_Dir, TaxiLookup_FileName);
        public readonly static string PlacesData = Path.Combine(Places_Dir, "nyc_places_data.json");
        public readonly static string CensusData = Path.Combine(CensusData_Dir, "nyc_census_data.json");
        public readonly static string WeatherData = Path.Combine(WeatherData_Dir, "nyc_weather_data.json");
        public readonly static string RoutingDB = Path.Combine(RoadNetwork_Dir, "itinero.routerdb");
        public readonly static string OpenStreetMap = Path.Combine(RoadNetwork_Dir, "new-york-latest.osm.pbf");
        public readonly static string CarProfile = Path.Combine(RoadNetwork_Dir, "car.lua");
    }
}
