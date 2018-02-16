using System;
using Boerman.AprsClient.Models;

namespace SkyHop.Core
{
    public static class ExtensionMethods
    {
        public static Boerman.FlightAnalysis.Models.PositionUpdate ToPositionUpdate(this AprsMessage aprsMessage)
        {
            if (aprsMessage.DataType == Boerman.AprsClient.Enums.DataType.Status)
                return null;

            try
            {
                string serialType;
                string serial;

                if (aprsMessage.Callsign.Length == 9)
                {
                    serialType = aprsMessage.Callsign.Substring(0, 3);
                    serial = aprsMessage.Callsign.Substring(3, 6);
                }
                else return null;    // We're probably dealing with a reveiver sending it's update. Just ignore it for now.

                //switch (serialType)
                //{
                //    case "ICA":
                        
                //        break;
                //    case "FLR":
                        
                //        break;
                //    default:
                //        return null;
                //}

                return new Boerman.FlightAnalysis.Models.PositionUpdate(
                    serial,
                    aprsMessage.ReceivedDate,
                    aprsMessage.Latitude.AbsoluteValue,
                    aprsMessage.Longitude.AbsoluteValue,
                    aprsMessage.Altitude.FeetAboveSeaLevel,
                    aprsMessage.Speed.Knots,
                    (double)aprsMessage.Direction.Degrees);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        //public static SkyHop.Core.Models.PositionUpdate ToDbPositionUpdate(this AprsMessage aprsMessage)
        //{
        //    if (aprsMessage.DataType == Boerman.AprsClient.Enums.DataType.Status)
        //        return null;

        //    try
        //    {
        //        return new SkyHop.Core.Models.PositionUpdate
        //        {
        //            Altitude = aprsMessage.Altitude.FeetAboveSeaLevel,
        //            ClimbRate = aprsMessage.ClimbRate,
        //            FlarmId = aprsMessage.Callsign,                                 // ToDo: Check if this one is okay
        //            Latitude = aprsMessage.Latitude.AbsoluteValue,
        //            Longitude = aprsMessage.Longitude.AbsoluteValue,
        //            TimeStamp = aprsMessage.ReceivedDate,
        //            TurnRate = aprsMessage.TurnRate
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}    
    }
}
