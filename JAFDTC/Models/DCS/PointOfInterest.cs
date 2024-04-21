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

using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// types for points of interest.
    /// </summary>
    public enum PointOfInterestType
    {
        UNKNOWN = -1,
        DCS_CORE = 0,
        USER = 1,
        CAMPAIGN = 2
    }

    /// <summary>
    /// type mask for PointOfInterestType enum.
    /// </summary>
    [Flags]
    public enum PointOfInterestTypeMask
    {
        NONE = 0,
        ANY = -1,
        DCS_CORE = 1 << PointOfInterestType.DCS_CORE,
        USER = 1 << PointOfInterestType.USER,
        CAMPAIGN = 1 << PointOfInterestType.CAMPAIGN,
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
        /// return true if the type of the poi matches a type mask, false otherwise.
        /// </summary>
        public bool IsMatchTypeMask(PointOfInterestTypeMask mask)
        {
            return mask.HasFlag((PointOfInterestTypeMask)(1 << (int)Type));
        }

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

        /// <summary>
        /// return true if a value is on [min, max]; false otherwise.
        /// </summary>
        private static bool InRange(double min, double val, double max) => ((min <= val) && (val <= max));

        /// <summary>
        /// return the name of the dcs theater that contains the given coordinate, null if no theater matches the
        /// coordinates. the match is based on approximate lat/lon bounds of the theaters.
        /// </summary>
        public static string TheaterForCoords(double lat, double lon)
        {
            if (InRange(40.0, lat, 46.0) && InRange(33.0, lon, 46.0))
            {
                return "Caucasus";
            }
            else if (InRange(10.0, lat, 23.0) && InRange(137.0, lon, 149.0))
            {
                return "Marianas";
            }
            else if (InRange(34.0, lat, 40.0) && InRange(-119.0, lon, -112.0))
            {
                return "Nevada";
            }
            else if (InRange(23.0, lat, 33.0) && InRange(47.0, lon, 60.0))
            {
                return "Persian Gulf";
            }
            else if (InRange(26.0, lat, 32.0) && InRange(28.0, lon, 37.0))
            {
                return "Sinai";
            }
            else if (InRange(-57.0, lat, -48.0) && InRange(-86.0, lon, -45.0))
            {
                return "South Atlantic";
            }
            else if (InRange(32.0, lat, 38.0) && InRange(30.0, lon, 41.0))
            {
                return "Syria";
            }
            return null;
        }

        /// <summary>
        /// return the name of the dcs theater that contains the given coordinate, null if no theater matches the
        /// coordinates. the match is based on approximate lat/lon bounds of the theaters.
        /// </summary>
        public static string TheaterForCoords(string lat, string lon)
            => TheaterForCoords(double.Parse(lat), double.Parse(lon));
    }
}
