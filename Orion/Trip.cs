using Orion.Geo;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orion
{
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
            Night,          // 0am - 5am
            EarlyMorning,   // 5am - 7am
            MorningRush,    // 7am - 9am 
            Morning,        // 9am - 12am
            Lunch,          // 12pm - 1pm
            Afternoon,      // 1pm - 4pm
            AfternoonRush,  // 4pm - 6pm
            Evening,        // 6pm - 10am
            Midnight,       // 10am - 12am
            Other
        }

        public struct Attr
        {
            public GeoLocation Pickup;
            public GeoLocation Dropoff;
            public int PickupZone;
            public int DropoffZone;
            public DateTime Date;
            public int PassengerCount;
            public int Hour;
            public int Minute;
            public TimePeriod Period;
            //public Cardinal Direction;
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
                {TimePeriod.Night,  new[] {0, 5} },
                {TimePeriod.EarlyMorning,  new[] {5, 7} },
                {TimePeriod.MorningRush,  new[] {7, 9} },
                {TimePeriod.Morning,  new[] {9, 12} },
                {TimePeriod.Lunch,  new[] {12, 13} },
                {TimePeriod.Afternoon,  new[] {13, 16} },
                {TimePeriod.AfternoonRush,  new[] {16, 18} },
                {TimePeriod.Evening,  new[] {18, 22} },
                {TimePeriod.Midnight,  new[] {22, 24} },
            };

            foreach (var per in TimePeriond)
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
}
