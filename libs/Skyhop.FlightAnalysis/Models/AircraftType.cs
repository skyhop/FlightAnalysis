using System;
using System.Collections.Generic;
using System.Text;

namespace Skyhop.FlightAnalysis.Models
{
    public enum AircraftType
    {
        Unknown = 0x0,
        Glider = 0x1,
        TowPlane = 0x2,
        Helicopter = 0x3,
        Skydiver = 0x4,
        DropPlane = 0x5,
        Hangglider = 0x6,
        Paraglider = 0x7,
        PoweredPiston = 0x8,
        PoweredJet = 0x9,
        Unknown2 = 0xA,
        Balloon = 0xB,
        Airship = 0xC,
        UAV = 0xD,
        Unknown3 = 0xE,
        Static = 0xF
    }
}
