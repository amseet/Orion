using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Diagnostics;
using Orion.Util;
using System.IO;
using Orion.Core.DataStructs;
using Orion.Geo;
using Orion.Cities.NYC;
using Orion.Models;
using System.Collections;
using GeoAPI.Geometries;
using System.Threading.Tasks;
using static Orion.Util.Methods;

namespace Orion.Core
{
    public class City : ICollection<RegionData>
    {
        private string[] removed = {"1000100", "5008900", "5990100" };
        private Layer<LandUseModel> LandUse;
        private Layer<CensusTractModel> CensusTract;
        private Layer<TaxiZoneModel> TaxiZone;
        
        private Lattice Lattice;
        private Dictionary<int, int> PrimaryLookup;       // lookup table from cellID to ObjectID
        private Dictionary<int, RegionData> DataLookup;   // lookup table from ObjectID to Region Data
        private Dictionary<string, Edge> AdjacencyMatrix;

        const string LatticeFile = "LatticeFile";
        const string PrimaryLookupFile = "PrimaryLookupFile";
        const string DataLookupFile = "DataLookupFile";
        const string AdjacencyMatrixFile = "AdjacencyMatrix";
        public int Count => DataLookup.Count;

        public bool IsReadOnly => true;

        private City()
        {
            PrimaryLookup = new Dictionary<int, int>();
            DataLookup = new Dictionary<int, RegionData>();
            AdjacencyMatrix = new Dictionary<string, Edge>();
        }

        public RegionData FindRegion(double lat, double lng)
        {
            Cell cell = Lattice.GetCell(lat, lng);
            if (cell != null && PrimaryLookup.TryGetValue(cell.ID, out int ObjectID))
                return DataLookup[ObjectID];
            return null;
        }

        public double Distance(RegionData from, RegionData to)
        {
            Tuple<int, int> idx = null;
            if (from.Idx <= to.Idx)
                idx = new Tuple<int, int>(from.Idx, to.Idx);
            else
                idx = new Tuple<int, int>(to.Idx, from.Idx);
            return AdjacencyMatrix[idx.ToString()].Distance;
        }

        public double Distance(int fromId, int toId)
        {
            Tuple<int, int> idx = null;
            if (fromId <= toId)
                idx = new Tuple<int, int>(fromId, toId);
            else
                idx = new Tuple<int, int>(toId, fromId);
            return AdjacencyMatrix[idx.ToString()].Distance;
        }

        public Edge GetEdge(RegionData from, RegionData to)
        {
            Tuple<int, int> idx = null;
            if (from.Idx <= to.Idx)
                idx = new Tuple<int, int>(from.Idx, to.Idx);
            else
                idx = new Tuple<int, int>(to.Idx, from.Idx);
            return AdjacencyMatrix[idx.ToString()];
        }

        public Edge[] GetEdges()
        {
            return AdjacencyMatrix.Values.ToArray();
        }

        public RegionData GetRegion(int index)
        {
            return DataLookup[index];
        }

        public Lattice GetLattice() => Lattice;

        public static City LoadCity(string ConfigFile)
        {
            City city = new City();
            var text = File.ReadAllText(ConfigFile);
            var files = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);

            city.LandUse = Layer<LandUseModel>.Deserialize(NYCConst.LandUse_Layer);
            city.CensusTract = Layer<CensusTractModel>.Deserialize(NYCConst.CensusTracts_Layer);
            city.TaxiZone = Layer<TaxiZoneModel>.Deserialize(NYCConst.TaxiZones_Layer);

            // load lattice file
            city.Lattice = GeoLattice.Load(files[City.LatticeFile]);

            // load primary file
            text = File.ReadAllText(files[City.PrimaryLookupFile]);
            city.PrimaryLookup = JsonConvert.DeserializeObject<Dictionary<int, int>>(text);

            // load data file
            text = File.ReadAllText(files[City.DataLookupFile]);
            city.DataLookup = JsonConvert.DeserializeObject<Dictionary<int, RegionData>>(text);

            // load adjacency matrix file
            text = File.ReadAllText(files[City.AdjacencyMatrixFile]);
            city.AdjacencyMatrix = JsonConvert.DeserializeObject <Dictionary<string, Edge>> (text);

            return city;
        }

        // City Specific
        public static City Factory(int cellLength)
        {
            City city = new City();
            Stopwatch stopwatch = new Stopwatch();
            Progress progress;

            #region LoadLayerModels&CensusData
            Console.WriteLine(">Extracting Shapefile data & build city map<");
            stopwatch.Restart();

            city.LandUse = Layer<LandUseModel>.Deserialize(NYCConst.LandUse_Layer);
            city.CensusTract = Layer<CensusTractModel>.Deserialize(NYCConst.CensusTracts_Layer);
            city.TaxiZone = Layer<TaxiZoneModel>.Deserialize(NYCConst.TaxiZones_Layer);

            int index = 0;
            List<CensusTractModel> toremove = new List<CensusTractModel>();
            foreach (var tract in city.CensusTract)
            {
                if (city.removed.Contains(tract.boro_ct201))
                    toremove.Add(tract);
                else
                    tract.Object_ID = index++;
            }
            foreach (var rem in toremove)
                city.CensusTract.Remove(rem);

            Dictionary<string, CensusData> censusDatas = JsonConvert.DeserializeObject<CensusData[]>(File.ReadAllText(NYCConst.CensusData))
                                                            .ToDictionary(p => p.id);
            Dictionary<string, RatingData> ratingDatas = JsonConvert.DeserializeObject<RatingData[]>(File.ReadAllText(NYCConst.PlacesData))
                                                            .ToDictionary(p => p.id);
            Dictionary<string, float> attraction = JsonConvert.DeserializeObject<Dictionary<string, float>>(File.ReadAllText(Path.Combine(NYCConst.Base_Dir, "Attraction.json")));
            Console.WriteLine("Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            #region BuildDataTable
            // Create Data lookup table by LandUse
            Console.WriteLine(">Computing City Data<");
            stopwatch.Restart();
            progress = new Progress(1000, city.CensusTract.Count);
            progress.Start();
            foreach (var tract in city.CensusTract)
            {
                var censusData = censusDatas[tract.boro_ct201];
                var ratingData = ratingDatas[tract.boro_ct201];

                var landTier = LandUseModel.AssessLandTierV3(city.LandUse.Interects(tract, 0.20f),
                                                            censusData.Population, tract.ntaname);

                //calculate distance to the closest predefined city center
                var attr = attraction[tract.boro_ct201];

                city.DataLookup[tract.Object_ID] = new RegionData(tract.Object_ID, tract.Latitude, tract.Longitude,
                                                                    tract.Geometry.Area, tract.Length,
                                                                    censusData, ratingData, attr,
                                                                    landTier.ToString());

                progress.inc();
            }
            progress.Stop();
            foreach(LandUseModel.LandTier type in Enum.GetValues(typeof(LandUseModel.LandTier)))
                Console.WriteLine("{0}: {1}", type, city.DataLookup.Where(i => i.Value.LandUse == type.ToString()).Count());
            Console.WriteLine("Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            #region Lattice
            Console.WriteLine(">Computing Lattice<");
            stopwatch.Restart();

            var minX = city.CensusTract.Min(z => z.Geometry.Coordinates.Min(c => c.X));
            var minY = city.CensusTract.Min(z => z.Geometry.Coordinates.Min(c => c.Y));
            var maxX = city.CensusTract.Max(z => z.Geometry.Coordinates.Max(c => c.X));
            var maxY = city.CensusTract.Max(z => z.Geometry.Coordinates.Max(c => c.Y));

            var min = CoordinateSystems.Convert(CoordinateSystems.COORSYSTEM.NAD83, CoordinateSystems.COORSYSTEM.WGS84, minY, minX);
            var max = CoordinateSystems.Convert(CoordinateSystems.COORSYSTEM.NAD83, CoordinateSystems.COORSYSTEM.WGS84, maxY, maxX);
            city.Lattice = new GeoLattice(min[1], min[0], max[1], max[0], cellLength);

            Console.WriteLine("Min Coordinates: {0},{1}", min[1], min[0]);
            Console.WriteLine("Max Coordinates: {0},{1}", max[1], max[0]);
            Console.WriteLine("Lattice Dimentions: Rows {0}, Columns {1}, Size {2}.", city.Lattice.Rows, city.Lattice.Columns, city.Lattice.Size);
            Console.WriteLine("Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion       

            #region Mapping
            Console.WriteLine(">Maping Lattice cells to layers<");
            stopwatch.Restart();

            progress = new Progress(1000, city.Lattice.Size);
            progress.Start();
            if (File.Exists(Path.Combine(NYCConst.Base_Dir, "PrimaryLookupFile.json")))
            {
                var text = File.ReadAllText(Path.Combine(NYCConst.Base_Dir, "PrimaryLookupFile.json"));
                city.PrimaryLookup = JsonConvert.DeserializeObject<Dictionary<int, int>>(text);
            }
            else
                city.Lattice.ForEachCell((i, j, cell) =>
                {
                    ShapeModel shape = city.CensusTract.Contains(cell.Center.x, cell.Center.y);
                    if (shape != null)
                        city.PrimaryLookup.Add(cell.ID, shape.Object_ID);
                    progress.inc();

                });
            progress.Stop();


            Console.WriteLine("Map size: {0}", city.PrimaryLookup.Count());
            Console.WriteLine("Distinct # of Objects: {0}", city.PrimaryLookup.Values.Distinct().Count());
            Console.WriteLine("Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            #region BuildAdjacencyMatrix
            Console.WriteLine(">Building Adjacency Matrix<");
            progress = new Progress(1000, city.Count * city.Count);
            progress.Start();
            
            Parallel.ForEach(city.CensusTract, t1 => {
                foreach(var t2 in city.CensusTract)
                {
                    Tuple<int, int> idx = null;
                    if (t1.Object_ID <= t2.Object_ID)
                        idx = new Tuple<int, int>(t1.Object_ID, t2.Object_ID);
                    else
                        idx = new Tuple<int, int>(t2.Object_ID, t1.Object_ID);

                    lock (city.AdjacencyMatrix)
                    {
                        if (!city.AdjacencyMatrix.Keys.Contains(idx.ToString()))
                        {
                            var dist = t1.Geometry.Centroid.Distance(t2.Geometry.Centroid);
                            city.AdjacencyMatrix.Add(idx.ToString(), new Edge()
                            {
                                SenderID = t1.Object_ID,
                                ReceiverID = t2.Object_ID,
                                Distance = dist
                            });
                        }
                    }
                    progress.inc();
                }
            });
            progress.Stop();
            #endregion

            #region SaveToFile
            Console.WriteLine(">Saving Data<");
            stopwatch.Restart();

            string latticefile = Path.Combine(NYCConst.Base_Dir, string.Format(@"Lattice-{0}.json", cellLength));
            string primarylookupfile = Path.Combine(NYCConst.Base_Dir, City.PrimaryLookupFile + ".json");
            string datalookupfile = Path.Combine(NYCConst.Base_Dir, City.DataLookupFile + ".json");
            string adjacencymatrixfile = Path.Combine(NYCConst.Base_Dir, City.AdjacencyMatrixFile + ".json");

            city.Lattice.Save(latticefile);
            File.WriteAllText(primarylookupfile, JsonConvert.SerializeObject(city.PrimaryLookup));
            File.WriteAllText(datalookupfile, JsonConvert.SerializeObject(city.DataLookup));
            File.WriteAllText(adjacencymatrixfile, JsonConvert.SerializeObject(city.AdjacencyMatrix));
            File.WriteAllText(NYCConst.NYCConfigFile, JsonConvert.SerializeObject(new Dictionary<string, string>{
                {City.LatticeFile, latticefile},
                {City.PrimaryLookupFile, primarylookupfile},
                {City.DataLookupFile, datalookupfile},
                {City.AdjacencyMatrixFile,  adjacencymatrixfile}
            }));
            Console.WriteLine("Execution Time: {0} Seconds\n", (float)stopwatch.ElapsedMilliseconds / 1000);
            #endregion

            return city;
        }

        public void Add(RegionData item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(RegionData item)
        {
            return DataLookup.ContainsValue(item);
        }

        public void CopyTo(RegionData[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(RegionData item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<RegionData> GetEnumerator()
        {
            return DataLookup.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return DataLookup.Values.GetEnumerator();
        }
    }
}
