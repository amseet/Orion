using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OsmSharp.API;
using OsmSharp.Streams;
using System.Threading;
using Newtonsoft.Json;

namespace Orion.temp
{
    class Cell
    {
        public double centerLat, centerLong, limitLeft, limitRight, limitTop, limitBottom;
        public bool outOfBounds = false;
        public int xIndex, yIndex;
        public int id;

        public Cell(double latitude, double longitude, double hght, double wdth, int x, int y)
        {
            centerLat = latitude;
            centerLong = longitude;
            limitLeft = centerLat - wdth / 2;
            limitRight = centerLat + wdth / 2;
            limitTop = centerLong + hght / 2;
            limitBottom = centerLong - hght / 2;
            xIndex = x;
            yIndex = y;
        }

        public bool isInBounds(double lat, double lng) {
            return lat >= limitLeft && lat <= limitRight && lng >= limitBottom && lng <= limitTop;
        }

        public void fixCenter(Router router, int meterLimit)
        {
            try
            {
                var center = router.TryResolve(Vehicle.Car.Fastest(), Convert.ToSingle(centerLat), Convert.ToSingle(centerLong), meterLimit);
                centerLat = Convert.ToDouble(center.Value.Latitude);
                centerLong = Convert.ToDouble(center.Value.Longitude);

                if (centerLat < limitLeft || centerLat > limitRight || centerLong < limitBottom || centerLong > limitTop)
                    outOfBounds = true;
            }
            catch
            {
                outOfBounds = true;
            }

            if (outOfBounds)
            {
                centerLat = 0;
                centerLong = 0;
            }
        }

        public void printCenter()
        {
            Console.Write("(" + centerLat + "," + centerLong + ") ");
        }

        public double getCenterLat()
        {
            return centerLat;
        }

        public double getCenterLong()
        {
            return centerLong;
        }
        public void updateCenter(String newCenter)
        {
            int index = newCenter.IndexOf('*');
            centerLat = Convert.ToDouble(newCenter.Substring(0, index));
            centerLong = Convert.ToDouble(newCenter.Substring(index + 1));
        }

    }

    class Trip
    {
        public double startLat;
        public double startLong;
        public double destLat;
        public double destLong;
        public DateTime pickupTime, dropOffTime;
        public Trip(double stLat, double stLon, double desLat, double desLong, DateTime pTime, DateTime dTime)
        {
            startLat = stLat;
            startLong = stLon;
            destLat = desLat;
            destLong = desLong;
            pickupTime = pTime;
            dropOffTime = dTime;
        }
        public void print()
        {
            Console.WriteLine("Pickup: " + pickupTime + " Drop off: " + dropOffTime + " Lat: " + startLat + " Lon: " + startLong + " dLat: " + destLat + " dLon: " + destLong);
        }
    }

    class Lattice
    {
        public double cellWidth, cellHeight;
        public int gridWidth, gridHeight;
        public Cell [][] Cells;
        public List<Cell> Regions;
        private double startLat, startLong, endLat, endLong; 

        public Lattice(double nLat, double nLong, double xLat, double xLong, float cellDimensionInMeters)
        {
            cellHeight = cellDimensionInMeters / 111111;
            cellWidth = cellDimensionInMeters / 111111;
            startLat = nLat;
            startLong = nLong;
            endLat = xLat;
            endLong = xLong;
            
            Regions = new List<Cell>();

            double latDiff = Math.Abs(xLat - nLat);
            double lonDiff = Math.Abs(xLong - nLong);

            //not used
            int latDiffMeters = Convert.ToInt32(Math.Ceiling(latDiff * 111111));
            int lonDiffMeters = Convert.ToInt32(Math.Ceiling(lonDiff * 111111));

            gridHeight = Convert.ToInt32(Math.Ceiling(latDiff / cellHeight));
            gridWidth = Convert.ToInt32(Math.Ceiling(lonDiff / cellWidth));

            Cells = new Cell[gridHeight][] ;

            Console.WriteLine(nLat + "," + xLat + "," + nLong + "," + xLong);
            Console.WriteLine(cellWidth + "," + latDiff + "," + lonDiff + "," + latDiffMeters + "," + lonDiffMeters + "," + gridWidth + "," + gridHeight);
            Console.WriteLine(nLat + gridHeight * cellHeight);
            Console.WriteLine(nLong + gridWidth * cellWidth);


            for (int i = 0; i < gridHeight ; i++)
            {
                double curHeight = nLat + i * cellHeight + cellHeight / 2;
                //List<Cell> newList = new List<Cell>();
                Cells[i] = new Cell[gridWidth];

                for (int j = 0; j < gridWidth; j++)
                {
                    double curWidth = nLong + j * cellWidth + cellWidth / 2; 
                    Cells[i][j] = new Cell(curHeight, curWidth, cellHeight, cellWidth, i, j);
                    Cells[i][j].id = j + i * gridWidth;
                }

                //Cells.Add(newList);
            }
        }

        public Cell getCell(double lat, double lng)
        {
            int i = (int) ((lat - startLat - cellHeight / 2) / cellHeight);
            int j = (int) ((lng - startLong - cellWidth / 2) / cellWidth);

            if (i >= 0 && j >= 0 && i < Cells.Length && j < Cells[i].Length)
                return Cells[i][j];
            else
                return null;
        }

        public void fixCenters(Router router)
        {
            int limit = Convert.ToInt32(Math.Ceiling(Math.Sqrt((cellWidth * 111111 / 2) * (cellWidth * 111111 / 2) + (cellHeight * 111111 / 2) * (cellHeight * 111111 / 2))));
            ThreadPool.GetMinThreads(out int _PoolSize, out int minPorts);
            ThreadPool.SetMaxThreads(_PoolSize, _PoolSize);

            ThreadPool.GetAvailableThreads(out int workers, out int complete);
            //Console.WriteLine(workers + ";" + complete);
            

            for (int i = 0; i < Cells.Length; i++)
            {
                for (int j = 0; j < Cells[i].Length; )
                {
                    var cell = Cells[i][j];
                    ThreadPool.GetAvailableThreads(out workers, out complete);

                    if (workers > 0)
                    {
                        ThreadPool.QueueUserWorkItem((obj) =>
                        {
                            var c = obj as Cell;
                            c.fixCenter(router, limit);
                            lock (Regions)
                            {
                                if(!c.outOfBounds)
                                    Regions.Add(c);
                            }
                        }, cell);

                        j++;
                    }
                    else
                        Thread.Sleep(100);
                }
                Console.WriteLine("Row Fixed " + i);
            }
        }

        public void updateCenters(List<List<String>> newCenters)
        {
            for (int i = 0; i < newCenters.Count(); i++)
                for (int j = 0; j < newCenters[0].Count(); j++)
                    Cells[i][j].updateCenter(newCenters[i][j]);
        }

        public void printCenters(int limit)
        {
            if (limit > gridHeight)
                limit = gridHeight;

            for (int i = 0; i < limit; i++)
            {
                Console.WriteLine();
                for (int j = 0; j < gridWidth; j++)
                {
                    Cells[i][j].printCenter();
                }
            }
            Console.WriteLine();
        }

        public void outputCenters()
        {
            string filePath = @".\centers.csv";
            string delimiter = ",";

            int toOutput = Cells.Count();

            string[][] outputs = new string[toOutput][];

            for (int i = 0; i < toOutput; i++)
            {
               Cell[] c = Cells[i];
                string[] output = new string[c.Count()];
                for (int j = 0; j < c.Count(); j++)
                    output[j] = Convert.ToString(c[j].getCenterLat()) + " " + Convert.ToString(c[j].getCenterLong());
                outputs[i] = output;
            }

            int length = outputs.GetLength(0);
            StringBuilder sb = new StringBuilder();
            for (int index = 0; index < length; index++)
                sb.AppendLine(string.Join(delimiter, outputs[index]));

            File.WriteAllText(filePath, sb.ToString());
        }

        public void SaveRegions()
        {
            string json = JsonConvert.SerializeObject(Regions);
            File.WriteAllText(@".\Regions.json", json);
        }

        public void LoadRegions()
        {
            string json = File.ReadAllText(@".\Regions.json");
            Regions = JsonConvert.DeserializeObject<List<Cell>>(json);
        }
    }

    class Grid
    {
        private Router router;
        private Itinero.Profiles.Profile profile;
        private List<Trip> trips;
        public Lattice lattice;

        public Grid(Router rt)
        {
            router = rt;
            profile = Vehicle.Car.Fastest(); // the default OSM car profile.

            createLattice(100);
            Console.WriteLine("Lattice created.");

            //Console.WriteLine("about to fix centers");
            //lattice.fixCenters(router);
            //lattice.outputCenters();

            //Console.WriteLine("Saving regions.");
            //lattice.SaveRegions();

            //Console.WriteLine("Loading regions.");
            //lattice.LoadRegions();

            Console.WriteLine("Done.");
        }

        public void readCenters()
        {
            List<List<String>> centers = new List<List<String>>();

            using (var reader = new StreamReader(@".\centers.csv"))
            {
                List<string> listA = new List<string>();
                List<string> listB = new List<string>();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    List<String> output = new List<String>();

                    for (int i = 0; i < values.Count(); i++)
                        output.Add(values[i]);

                    centers.Add(output);
                }
                Console.WriteLine("Count: " + centers.Count() + "," + centers[0].Count());
            }
            lattice.updateCenters(centers);
            //lattice.printCenters(1);
        }

        private void createLattice(int CellDimention)
        {
            lattice = new Lattice(40.6973, -74.0200, 40.8769, -73.9013, CellDimention);
        }
    }
}
