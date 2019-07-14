using Boerman.FlightAnalysis.Models;
using GeoAPI.Geometries;
using SharpKml.Base;
using SharpKml.Dom;
using System.Collections.Generic;
using System.Linq;

namespace Boerman.FlightAnalysis
{
    public static class KmlExtensions
    {
        public static Kml AsKml(this IList<Coordinate> coordinates)
        {
            var top = coordinates.Max(q => q.Z);

            var kml = new Kml
            {
                Feature = new Document()
            };

            for (var i = 0; i < coordinates.Count() - 1; i++)
            {
                LineString ls = new LineString
                {
                    AltitudeMode = AltitudeMode.Absolute,
                    Extrude = false
                };

                ls.Coordinates = new CoordinateCollection();

                // I guess KML (and Google Earth) works with an altitude in meters. So therefore the 0.3048.
                ls.Coordinates.Add(new Vector(coordinates[i].Y, coordinates[i].X, coordinates[i].Z * 0.3048));
                ls.Coordinates.Add(new Vector(coordinates[i + 1].Y, coordinates[i + 1].X, coordinates[i + 1].Z * 0.3048));

                var placemark = new Placemark
                {
                    Geometry = ls
                };

                placemark.AddStyle(new Style
                {
                    Line = new LineStyle
                    {
                        Color = GetColor(coordinates[i].Z / top)
                    }
                });

                kml.Feature.AddChild(placemark);
            }

            return kml;
        }

        public static Kml AsKml(this IList<PositionUpdate> positionUpdates)
        {
            var updates = positionUpdates
                .OrderBy(q => q.TimeStamp)
                .ToList();

            return updates
                .Select(q => new Coordinate(
                    q.Latitude,
                    q.Longitude,
                    q.Altitude))
                .ToList()
                .AsKml();
        }

        public static string AsKmlXml(this IList<Coordinate> coordinates)
        {
            var kml = coordinates.AsKml();

            Serializer serializer = new Serializer();
            serializer.Serialize(kml);
            return serializer.Xml;
        }

        public static string AsKmlXml(this IList<PositionUpdate> positionUpdates)
        {
            var kml = positionUpdates.AsKml();

            Serializer serializer = new Serializer();
            serializer.Serialize(kml);
            return serializer.Xml;
        }

        private static Color32 GetColor(double value)
        {
            var rgb = new Hls
            {
                H = 1 - (value * 120),
                L = .5,
                S = .75
            }.ToRgb();

            return new Color32(255, rgb.B, rgb.G, rgb.R);
        }
    }
}
