using Itinero;
using Itinero.Osm.Vehicles;
using Newtonsoft.Json;
using Orion.DB.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Orion.Geo
{
    public struct Location
    {
        public double x, y;
    }

    public struct GeoLocation
    {
        public double latitude, longitude;
        public GeoLocation(double lat, double lng) { latitude = lat; longitude = lng; }
    }

    public class Cell
    {
        public Location Center;
        public float LimitLeft;
        public float LimitRight;
        public float LimitTop;
        public float LimitBottom;
        public int ID;
        public int XIndex;
        public int YIndex;

        public Cell(float x, float y, float height, float width)
        {
            Center = new Location();
            Center.x = x;
            Center.y = y;

            LimitLeft = x - width / 2;
            LimitRight = x + width / 2;

            LimitTop = y + height / 2;
            LimitBottom = y - height / 2;
        }

        public void SetIndex(int xIndex, int yIndex, int Id)
        {
            XIndex = xIndex;
            YIndex = yIndex;
            ID = Id;
        }

        public bool IsInBounds(double x, double y)
        {
            return x >= LimitLeft && x <= LimitRight && y >= LimitBottom && y <= LimitTop;
        }

        public override string ToString()
        {
            return Center.x + ":" + Center.y;
        }
    }

    public class Lattice
    {
        public float CellWidth;
        public float CellHeight;
        public int Columns;
        public int Rows;
        public int Size { get { return Rows * Columns; } }
        public Location Start;
        public Location End;
        public Cell[][] Cells;

        protected Lattice() { }

        protected Lattice(Lattice copy)
        {
            this.CellHeight = copy.CellHeight;
            this.CellWidth = copy.CellWidth;
            this.Columns = copy.Columns;
            this.Rows = copy.Rows;
            this.Start = copy.Start;
            this.End = copy.End;
            this.Cells = copy.Cells;
        }

        public Lattice(float startX, float startY, float endX, float endY, float cellHeight, float cellWidth)
        {
            Start = new Location();
            End = new Location();

            CellHeight = cellHeight;
            CellWidth = cellWidth;
            Start.x = startX;
            Start.y = startY;
            End.x = endX;
            End.y = endY;

            Rows = (int)((endX - startX) / CellHeight + 1);
            Columns = (int)((endY - startY) / CellWidth + 1);

            Cells = new Cell[Rows][];
            for (int i = 0; i < Rows; i++)
            {
                float curHeight = startX + i * CellHeight + CellHeight / 2;
                Cells[i] = new Cell[Columns];

                for (int j = 0; j < Columns; j++)
                {
                    float curWidth = startY + j * CellWidth + CellWidth / 2;
                    Cells[i][j] = new Cell(curHeight, curWidth, CellHeight, CellWidth);
                    Cells[i][j].SetIndex(i, j,  j + i * Columns);
                }
            }
        }

        public Cell GetCell(double x, double y)
        {
            int i = (int)((x - Start.x - CellHeight / 2) / CellHeight);
            int j = (int)((y - Start.y - CellWidth / 2) / CellWidth);

            if (i >= 0 && j >= 0 && i < Cells.Length && j < Cells[i].Length)
                return Cells[i][j];
            else
                return null;
        }

        public Cell GetCell(int CellID)
        {
            int i, j;
            i = CellID / Columns;
            j = CellID - (i * Columns);

            return Cells[i][j];
        }

        public void ForEachCell(Action<int, int, Cell> action)
        {
            for (int i = 0; i < Cells.Length; i++)
                for (int j = 0; j < Cells[i].Length; j++)
                    action(i, j, Cells[i][j]);
        }

        public struct CellPair
        {
            public Cell PickupCell;
            public Cell DropoffCell;
        }

        public int[,] GetCellMatrix()
        {
            int[,] matrix = new int[Size, Size];
            ForEachCell((i, j, cell1) => {
                ForEachCell((x, y, cell2) => {
                    matrix[cell1.ID, cell2.ID] = 0;
                });
            });
            return matrix;
        }

        public CellPair[] GetCellPairs()
        {
            CellPair[] matrix = new CellPair[Size * Size];
            ForEachCell((i, j, cell1) => {
                ForEachCell((x, y, cell2) => {
                    int idx = cell1.ID * Size + cell2.ID;
                    matrix[idx] = new CellPair()
                    {
                        PickupCell = cell1,
                        DropoffCell = cell2,
                    };
                });
            });
            return matrix;
        }

        public void Save(string path)
        {
            string json = JsonConvert.SerializeObject(this);
            File.WriteAllText(path, json);
        }

        public static Lattice Load(string path)
        {
            string file = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Lattice>(file);
        }
    }

    public class GeoLattice : Lattice
    {
        const float GeoScaleToMeters = 111319.9f;

        public GeoLattice()
        {

        }

        public GeoLattice(GeoLattice lattice) : base(lattice)
        {

        }

        public GeoLattice(float startLat, float startLng, float endLat, float endLng, float cellDimensionInMeters) 
            : base(startLat, startLng, endLat, endLng,  cellDimensionInMeters / GeoScaleToMeters, cellDimensionInMeters / GeoScaleToMeters)
        { }

        public GeoLattice(double startLat, double startLng, double endLat, double endLng, float cellDimensionInMeters)
            : base((float)startLat, (float)startLng, (float)endLat, (float)endLng, cellDimensionInMeters / GeoScaleToMeters, cellDimensionInMeters / GeoScaleToMeters)

        { }

        public static GeoLattice Load(string path)
        {
            string file = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<GeoLattice>(file);
        }

        public void SaveLattice(string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                writer.AutoFlush = true;
                writer.WriteLine(string.Join(',', "Cell ID", "Latitude", "Longitude"));
                ForEachCell((i, j, cell) =>
                {
                    writer.WriteLine(string.Join(',', cell.ID, cell.Center.x, cell.Center.y));
                });
            }
        }

        public static GeoLattice ComputeLattice(int CellDimention = 200)
        {
            //Computing Lattice
            #region ComputingLattice
            Console.WriteLine(">Computing Lattice<");

            GeoLattice lattice = new GeoLattice(40.6973f, -74.0200f, 40.8769f, -73.9013f, CellDimention);
            Console.WriteLine("Lattice Dimentions: Rows {0}, Columns {1}, Size {2}.", lattice.Rows, lattice.Columns, lattice.Size);

            Console.WriteLine(">Save Lattice<");
            lattice.SaveLattice(string.Format(@".\Cells-{0}.csv", CellDimention));

            return lattice;
            #endregion
        }

        public List<Trip.Attr> ComputeNearestCentroid(List<TripDataModel> Rows)
        {
            #region DataProcessing
            Console.WriteLine(">Populating Trip List<");
            List<Trip.Attr> Trips = new List<Trip.Attr>(Rows.Count());
            Rows.ForEach(trip => {
                Cell pCell = GetCell(trip.Pickup_Latitude, trip.Pickup_Longitude);
                Cell dCell = GetCell(trip.Dropoff_Latitude, trip.Dropoff_Longitude);
                if (pCell != null && dCell != null)
                    Trips.Add(new Trip.Attr()
                    {
                        Pickup = new GeoLocation(trip.Pickup_Latitude, trip.Pickup_Longitude),
                        Dropoff = new GeoLocation(trip.Dropoff_Latitude, trip.Dropoff_Longitude),
                        PickupZone = pCell.ID,
                        DropoffZone = dCell.ID,
                        PassengerCount = trip.Passenger_Count,
                        Date = trip.Pickup_Datetime.Date,
                        Hour = trip.Pickup_Datetime.Hour,
                        Minute = trip.Pickup_Datetime.Minute,
                        //Direction = Trip.GetDirection(this, pCell, dCell)
                    });
            });
            #endregion
            return Trips;
        }

    }
}
