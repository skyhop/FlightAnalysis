using Boerman.FlightAnalysis.Helpers;
using Boerman.FlightAnalysis.Models;
using SharpKml.Base;
using SharpKml.Dom;
using System.Collections.Generic;
using System.Linq;

namespace Boerman.FlightAnalysis
{
    public static class Extensions
    {
        public static Kml AsKml(this IList<PositionUpdate> positionUpdates)
        {
            var updates = positionUpdates.OrderBy(q => q.TimeStamp).ToList();

            var top = updates.Max(q => q.Altitude);

            var kml = new Kml
            {
                Feature = new Document()
            };
           
            for (var i = 0; i < updates.Count() - 1; i++)
            {
                LineString ls = new LineString
                {
                    AltitudeMode = AltitudeMode.Absolute,
                    Extrude = false
                };

                ls.Coordinates = new CoordinateCollection();

                // I guess KML (and Google Earth) works with an altitude in meters. So therefore the 0.3048.
                ls.Coordinates.Add(new Vector(updates[i].Latitude, updates[i].Longitude, updates[i].Altitude * 0.3048));
                ls.Coordinates.Add(new Vector(updates[i+1].Latitude, updates[i+1].Longitude, updates[i+1].Altitude * 0.3048));

                var placemark = new Placemark
                {
                    Geometry = ls
                };

                placemark.AddStyle(new Style
                {
                    Line = new LineStyle
                    {
                        Color = GetColor(updates[i].Altitude/top)
                    }
                });

                kml.Feature.AddChild(placemark);
            }
            
            return kml;
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
            Colors.HlsToRgb(1 - (value * 120), .5, .75, out int r, out int g, out int b);
            return new Color32(255, (byte)b, (byte)g, (byte)r);
        }
    }
}
