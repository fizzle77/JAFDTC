// ********************************************************************************************************************
//
// PointOfInterest.cs -- point of interest model
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023 ilominar/raven
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
// option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along with this program.  If not, see
// <https://www.gnu.org/licenses/>.
//
// ********************************************************************************************************************

namespace JAFDTC.Models.DCS
{
    public enum PointOfInterestType
    {
        UNKNOWN = -1,
        AIRBASE = 0,
        USER = 1
    }

    /// <summary>
    /// defines the properties of a point of interest (poi) known to jafdtc. these instances are managed by the poi
    /// database (PointOfInterestDbase).
    /// </summary>
    public sealed class PointOfInterest
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public PointOfInterestType Type { get; set; }           // poi type (airfield, etc)

        public string Theater { get; set; }                     // theater (general geographic area)
        
        public string Name { get; set; }                        // name
        
        public string Latitude { get; set; }                    // latitude (decimal degrees)
        
        public string Longitude { get; set; }                   // longitude (decimal degrees)
        
        public int Elevation { get; set; }                      // elevation (feet)

        public override string ToString()
        {
            return ((Theater != null) && (Name != null)) ? $"{Theater} - {Name}" : "";
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public PointOfInterest()
            => (Type, Theater, Name, Latitude, Longitude, Elevation) = (PointOfInterestType.UNKNOWN, "", "", "", "", 0);

        public PointOfInterest(PointOfInterestType type, string theater, string name, string lat, string lon, int elev)
            => (Type, Theater, Name, Latitude, Longitude, Elevation) = (type, theater, name, lat, lon, elev);
    }
}
