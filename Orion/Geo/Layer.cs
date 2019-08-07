using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Orion.Cities.NYC;
using Orion.Models;


namespace Orion.Geo
{
    public class Layer<T> : ICollection<T> where T : ShapeModel, new()
    {
        private List<T> Shapes;
        public CoordinateSystems.COORSYSTEM coordinateSystem;

        public int Count => ((ICollection<T>)Shapes).Count;

        public bool IsReadOnly => ((ICollection<T>)Shapes).IsReadOnly;

        public Layer() 
        {
            coordinateSystem = CoordinateSystems.COORSYSTEM.NAD83;
            Shapes = new List<T>(); 
        }

        public static Layer<T> Deserialize(string file)
        {
            Layer<T> layer = new Layer<T>();
            int validGeoms = 0;

            using (var reader = new ShapefileDataReader(file, GeometryFactory.Default))
            {
                var projectionFile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".prj");
                var projectionInfo = File.ReadAllText(projectionFile);
                CoordinateSystems.COORSYSTEM detected = CoordinateSystems.DetectCoordinateSystems(projectionInfo);
                bool convert = (detected != layer.coordinateSystem);

                while (reader.Read())
                {
                    T t = new T();
                    
                    //import all fields by variable name
                    t.Geometry = reader.Geometry;
                    foreach (var field in t.GetType().GetFields())
                        if(reader.DbaseHeader.Fields.Select(x=>x.Name.ToLower()).Contains(field.Name.ToLower()))
                            t.GetType().GetField(field.Name).SetValue(t, reader[field.Name]);

                    //convert from geodecial degrees to NAD83
                    if (convert)
                    {
                        t.Longitude = t.Geometry.Centroid.X;
                        t.Latitude = t.Geometry.Centroid.Y;
                        t.Geometry = CoordinateSystems.Convert(detected, layer.coordinateSystem, reader.Geometry); 
                    }
                    else
                    {
                        var point = CoordinateSystems.Convert(layer.coordinateSystem, CoordinateSystems.COORSYSTEM.WGS84,
                                                    t.Geometry.Centroid.Y, t.Geometry.Centroid.X);
                        t.Longitude = point[0];
                        t.Latitude = point[1];
                    }
                    //Calculate max length of polygon
                    #region PolyLength
                    var hull = t.Geometry.ConvexHull();
                    Coordinate left = hull.Coordinate,
                               right = hull.Coordinate,
                               top = hull.Coordinate,
                               bottom = hull.Coordinate;

                    //Source: https://msi.nga.mil/msisitecontent/staticfiles/calculators/degree.html
                    //Source: https://gis.stackexchange.com/questions/142326/calculating-longitude-length-in-miles
                    //double lngToMeterScale = 111320f;
                    //double latToMeterScale = 110574f;
                    //double leftX = 0, rightX = 0, topY = 0, bottomY = 0;

                    foreach (var point in hull.Coordinates)
                    {
                        //double pointX = point.X * lngToMeterScale;
                        //double pointY = point.Y * latToMeterScale;
                        //leftX = left.X * lngToMeterScale;
                        //rightX = right.X * lngToMeterScale;
                        //topY = top.Y * latToMeterScale;
                        //bottomY = bottom.Y * latToMeterScale;

                        if (point.X < left.X)
                            left = point;
                        if (point.X > right.X)
                            right = point;
                        if (point.Y > top.Y)
                            top = point;
                        if (point.Y < bottom.Y)
                            bottom = point;
                    }

                    if (Math.Abs(right.X - left.X) > Math.Abs(top.Y - bottom.Y))
                        t.Length = Math.Abs(right.X - left.X);
                    else
                        t.Length = Math.Abs(top.Y - bottom.Y);

                    #endregion

                    t.Object_ID = layer.Shapes.Count + 1;
                    layer.Shapes.Add(t);

                    if (t.Geometry.IsValid)
                        validGeoms++;
                }

                Debug.Assert(layer.Shapes.Count == reader.RecordCount);
                Console.WriteLine("{0}/{1} Valid Geometries in file {2} (converted: {3})", validGeoms, layer.Shapes.Count, Path.GetFileNameWithoutExtension(file), convert);
            }
            return layer;
        }

        public T Contains(Coordinate coor)
        {
            IGeometry geo = GeometryFactory.Default.CreatePoint(coor);
            return Shapes.Where(z => z.Geometry.Contains(geo)).FirstOrDefault();
        }

        public T Contains(double lat, double lng)
        {
            var point =  CoordinateSystems.Convert(CoordinateSystems.COORSYSTEM.WGS84, coordinateSystem, lat, lng);
            IGeometry geo = GeometryFactory.Default.CreatePoint(new Coordinate(point[0], point[1]));
            return Shapes.Where(z => z.Geometry.Contains(geo)).FirstOrDefault();
        }

        public T[] Interects(ShapeModel shape, float margin = 0.1f)
        {
            var intersections = Shapes.Where(i => i.Geometry.Intersects(shape.Geometry));
            var validGeoms = new List<T>();
            foreach (var geo in intersections)
                if (geo.Geometry.IsValid)
                {
                    if (geo.Geometry.Covers(shape.Geometry) || shape.Geometry.Covers(geo.Geometry))
                        validGeoms.Add(geo);
                    else
                    {
                        var inter = geo.Geometry.Intersection(shape.Geometry);
                        if (!inter.IsEmpty)
                            if (inter.Area >= Math.Min(shape.Geometry.Area, geo.Geometry.Area) * margin)
                                validGeoms.Add(geo);
                    }
                }
                else
                    validGeoms.Add(geo);
            return validGeoms.ToArray();
        }


        public void Add(T item)
        {
            ((ICollection<T>)Shapes).Add(item);
        }

        public void Clear()
        {
            ((ICollection<T>)Shapes).Clear();
        }

        public bool Contains(T item)
        {
            return ((ICollection<T>)Shapes).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((ICollection<T>)Shapes).CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return ((ICollection<T>)Shapes).Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((ICollection<T>)Shapes).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ICollection<T>)Shapes).GetEnumerator();
        }

        public static implicit operator Layer<ShapeModel>(Layer<T> v)
        {
            return new Layer<ShapeModel>() { Shapes = new List<ShapeModel>(v.Shapes), coordinateSystem = v.coordinateSystem };
        }
    }
}
