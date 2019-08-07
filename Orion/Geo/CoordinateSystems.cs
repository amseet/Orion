using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace Orion.Geo
{
    public class CoordinateSystems
    {
        public enum COORSYSTEM
        {
            WGS84,
            NAD83,
        }

        private CoordinateTransformationFactory ctfac;
        private ICoordinateTransformation DegToFoot;
        private ICoordinateTransformation FootToDeg;
        private static IProjectedCoordinateSystem NAD83
        {
            get
            {
                var cFac = new ProjNet.CoordinateSystems.CoordinateSystemFactory();

                //Create GCS_North_American_1983 geographic coordinate system
                IEllipsoid ellipsoid = cFac.CreateFlattenedSphere("North American 1983", 6378137.0, 298.257222101, LinearUnit.Metre);
                IHorizontalDatum datum = cFac.CreateHorizontalDatum("North American 1983", DatumType.HD_Classic, ellipsoid, null);
                IGeographicCoordinateSystem gcs = cFac.CreateGeographicCoordinateSystem("North American 1983", AngularUnit.Degrees, datum,
                    PrimeMeridian.Greenwich, new AxisInfo("Lon", AxisOrientationEnum.East),
                    new AxisInfo("Lat", AxisOrientationEnum.North));

                //Create NAD_1983_StatePlane_New_York_Long_Island_FIPS_3104_Feet projected coordinate system
                List<ProjectionParameter> parameters = new List<ProjectionParameter>(6);
                parameters.Add(new ProjectionParameter("latitude_of_origin", 40.16666667d));
                parameters.Add(new ProjectionParameter("central_meridian", -74.00000000d));
                parameters.Add(new ProjectionParameter("false_easting", 984250.00000000d));
                parameters.Add(new ProjectionParameter("false_northing", 0));
                parameters.Add(new ProjectionParameter("Standard_Parallel_1", 40.66666667d));
                parameters.Add(new ProjectionParameter("Standard_Parallel_2", 41.03333333d));
                IProjection projection = cFac.CreateProjection("Lambert_Conformal_Conic", "Lambert_Conformal_Conic", parameters);

                return cFac.CreateProjectedCoordinateSystem("NAD_1983_StatePlane_New_York_Long_Island_FIPS_3104_Feet",
                    gcs, projection, LinearUnit.Foot, new AxisInfo("Top", AxisOrientationEnum.North), new AxisInfo("Right", AxisOrientationEnum.East));
            }
        }

        public CoordinateSystems()
        {
            ctfac = new CoordinateTransformationFactory();
            DegToFoot = ctfac.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, NAD83);
            FootToDeg = ctfac.CreateFromCoordinateSystems(NAD83, GeographicCoordinateSystem.WGS84);
        }

        public static COORSYSTEM DetectCoordinateSystems(string ProjectionInfo)
        {
            string pattern = @"(DATUM\[\"")(.+?)(\"")";
            Regex regex = new Regex(pattern);
            var result = regex.Match(ProjectionInfo).Value;
            var datum = result.Substring(result.IndexOf('"') + 1, result.LastIndexOf('"') - (result.IndexOf('"') + 1)).Replace('_', ' ');
            if (datum.Contains("North American 1983"))
                return COORSYSTEM.NAD83;
            return COORSYSTEM.WGS84;
        }

        public static IGeometry Convert(COORSYSTEM from, COORSYSTEM to, IGeometry geometry)
        {
            IGeometry geo = null;
            switch (geometry.GeometryType)
            {
                case "Polygon":
                    var poly = geometry as Polygon;
                    geo = Convert(from, to, poly);
                    break;
                case "MultiPolygon":
                    var multipoly = geometry as MultiPolygon;
                    geo = Convert(from, to, multipoly);
                    break;
            }
            return geo; 
        }

        public static IMultiPolygon Convert(COORSYSTEM from, COORSYSTEM to, IMultiPolygon multipoly)
        {
            IPolygon [] polys = new Polygon [multipoly.Count];
            for(int i = 0; i < multipoly.Count; i++)
                polys[i] = Convert(from, to, multipoly[i] as IPolygon);
            return GeometryFactory.Default.CreateMultiPolygon(polys);
        }

        public static IPolygon Convert(COORSYSTEM from, COORSYSTEM to, IPolygon poly)
        {
            var shell = GeometryFactory.Default.CreateLinearRing(Convert(from, to, poly.Shell.Coordinates));
            ILinearRing[] holes = new LinearRing[poly.Holes.Length];
            for (int i = 0; i < poly.Holes.Length; i++)
                holes[i] = GeometryFactory.Default.CreateLinearRing(Convert(from, to, poly.Holes[i].Coordinates));
            return GeometryFactory.Default.CreatePolygon(shell, holes);
        }

        public static Coordinate[] Convert(COORSYSTEM from, COORSYSTEM to, Coordinate[] coor)
        {
            Coordinate[] tmp = new Coordinate[coor.Length];
            for (int i = 0; i < coor.Length; i++)
                tmp[i] = Convert(from, to, coor[i]);
            return tmp;
        }

        public static Coordinate Convert(COORSYSTEM from, COORSYSTEM to, Coordinate coor)
        {
            ICoordinateTransformation cs = GetTransformation(from, to);
            return cs.MathTransform.Transform(coor);
        }

        public static double[] Convert(COORSYSTEM from, COORSYSTEM to, double lat, double lng)
        {
            ICoordinateTransformation cs = GetTransformation(from, to);
            return cs.MathTransform.Transform(new double[] { lng, lat });
        }

        private static ICoordinateTransformation GetTransformation(COORSYSTEM from, COORSYSTEM to)
        {
            CoordinateSystems cs = new CoordinateSystems();
            if (from == COORSYSTEM.NAD83 && to == COORSYSTEM.WGS84)
                return cs.FootToDeg;
            else if (from == COORSYSTEM.WGS84 && to == COORSYSTEM.NAD83)
                return cs.DegToFoot;
            return null;
        }
    }
}
