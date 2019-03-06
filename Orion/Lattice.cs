using Itinero;
using Itinero.Osm.Vehicles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Orion
{
    public class Location
    {
        public float x, y;
    }

    public class Cell
    {
        public Location Center { get; private set; }
        public float LimitLeft { get; private set; }
        public float LimitRight { get; private set; }
        public float LimitTop { get; private set; }
        public float LimitBottom { get; private set; }
        
        public int ID { get; private set; }
        public int XIndex { get; private set; }
        public int YIndex { get; private set; }

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
        public float CellWidth { get; private set; }
        public float CellHeight { get; private set; }
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public int Size { get { return Rows * Columns; } }
        public Location Start { get; private set; }
        public Location End { get; private set; }
        public Cell[][] Cells;

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
    }

    public class Trip
    {
        public enum Cardinal
        {
            East,
            NorthEast,
            North,
            NorthWast,
            West,
            SouthWest,
            South,
            SouthEast,
            Center
        }
        public enum TimePeriod
        {
            MorningRush,    // 7am - 10am 
            Midday,         // 10am - 3pm 
            EveningRush,    // 3pm - 6pm
            Evening,        // 6pm - 12am
            Midnight,       // 12am - 4am
            Morning,         // 4am - 7am
            Other
        }

        public struct Attr
        {
            public int PickupCell;
            public int DropoffCell;
            public DateTime Date;
            public int PassengerCount;
            public int Hour;
            public TimePeriod TimeOfDay;
            public Cardinal Direction;
        }

        const double angle = Math.PI / 4;
        static readonly double th1 = Math.Atan(angle), th2 = Math.Atan(2 * angle),
            th3 = Math.Atan(3 * angle), th4 = Math.Atan(4 * angle);

        static Dictionary<Cardinal, double[]> Directions = new Dictionary<Cardinal, double[]>() {
                {Cardinal.NorthEast,  new[] { 0, th2} },
                {Cardinal.NorthWast,  new[] { th2, th4} },
                {Cardinal.SouthWest,  new[] { -th4, -th2} },
                {Cardinal.SouthEast,  new[] { -th2, 0} },
                {Cardinal.North,  new[] { th1, th3} },
                {Cardinal.East,  new[] { -th1, th1 } },
                {Cardinal.South,  new[] { -th3, -th1} },
                {Cardinal.West,  new[] { th3, -th3} },
            };

        public static TimePeriod GetTimePeriod(int time)
        {
            Dictionary<TimePeriod, int[]> TimePeriond = new Dictionary<TimePeriod, int[]>() {
                {TimePeriod.MorningRush,  new[] {7, 10} },
                {TimePeriod.Midday,  new[] {10, 15} },
                {TimePeriod.EveningRush,  new[] {15, 18} },
                {TimePeriod.Evening,  new[] {18, 24} },
                {TimePeriod.Midnight,  new[] {0, 4} },
                {TimePeriod.Morning,  new[] {4, 7} }
            };

            //TimeSpan time = dt.TimeOfDay;
            foreach(var per in TimePeriond)
                if (time >= per.Value[0] && time < per.Value[1])
                    return per.Key;
            return TimePeriod.Other;
        }
        public static Cardinal GetDirection(Lattice lattice, int Cell1ID, int Cell2ID)
        {
            var Cell1 = lattice.GetCell(Cell1ID);
            var Cell2 = lattice.GetCell(Cell2ID);
            return GetDirection(lattice, Cell1, Cell2);
        }
        public static Cardinal GetDirection(Lattice lattice, Cell Cell1, Cell Cell2)
        {
            double x = Math.Abs(Cell2.XIndex - Cell1.XIndex) * 1.0f;
            double y = Math.Abs(Cell2.YIndex - Cell1.YIndex) * 1.0f;
            if (x + y == 0)
                return Cardinal.Center;

            var th = Math.Atan2(y, x);
            foreach (var dir in Directions)
                if (th >= dir.Value[0] && th < dir.Value[1])
                    return dir.Key;
            return Cardinal.West;
        }
    }

    public class GeoLattice : Lattice
    {
        const float GeoScaleToMeters = 111111; 
        public GeoLattice(float startX, float startY, float endX, float endY, float cellDimensionInMeters) 
            : base(startX,  startY,  endX,  endY,  cellDimensionInMeters / GeoScaleToMeters, cellDimensionInMeters / GeoScaleToMeters)
        { }

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
    }
}
