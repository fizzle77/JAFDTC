// ********************************************************************************************************************
//
// PointOfInterest.cs -- point of interest model
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023-2024 ilominar/raven
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

using System.Linq;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.DCS
{
    public enum PointOfInterestType
    {
        UNKNOWN = -1,
        DCS_CORE = 0,
        USER = 1,
        CAMPAIGN = 2
    }

    /// <summary>
    /// defines the properties of a point of interest (poi) known to jafdtc. these instances are managed by the poi
    /// database (PointOfInterestDbase). pois include a theater (set based on lat/lon), name, comma-separated list
    /// of tags, and a lat/lon/elev.
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

        public string Tags { get; set; }                        // tags (";"-separated list)

        public string Latitude { get; set; }                    // latitude (decimal degrees)
        
        public string Longitude { get; set; }                   // longitude (decimal degrees)
        
        public string Elevation { get; set; }                   // elevation (feet)

        [JsonIgnore]
        public string SourceFile { get; set; }                  // source file name

        public override string ToString()
        {
            return (Name != null) ? $"{Name}" : "";
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public PointOfInterest()
            => (Type, Theater, Name, Tags, Latitude, Longitude, Elevation) = (PointOfInterestType.UNKNOWN, "", "", "", "", "", "");

        public PointOfInterest(PointOfInterestType type, string theater, string name, string tags, string lat, string lon, string elev)
            => (Type, Theater, Name, Tags, Latitude, Longitude, Elevation) = (type, theater, name, tags, lat, lon, elev);

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return sanitized tag string with empty tags removed, extra spaces removed, etc.
        /// </summary>
        public static string SanitizedTags(string tags)
        {
            string cleanTags = tags;
            if (!string.IsNullOrEmpty(tags))
            {
                cleanTags = "";
                foreach (string value in tags.Split(';').ToList<string>())
                {
                    string newValue = value.Trim();
                    if (newValue.Length > 0)
                    {
                        cleanTags += $"; {newValue}";
                    }
                }
                if (cleanTags.Length >= 3)
                {
                    cleanTags = cleanTags[2..];
                }
            }
            return cleanTags;
        }
    }
}
