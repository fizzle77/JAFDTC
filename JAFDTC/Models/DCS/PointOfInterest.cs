// ********************************************************************************************************************
//
// PointOfInterest.cs -- point of interest model
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023-2025 ilominar/raven
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
using System.Collections.Generic;
using System.Linq;

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
        CAMPAIGN = 1 << PointOfInterestType.CAMPAIGN
    }

    // ================================================================================================================

    /// <summary>
    /// defines the boundary of a theater on the map in terms of a min/max latitude and longitude.
    /// </summary>
    public sealed class TheaterBounds
    {
        public double LatMin { get; }
        public double LatMax { get; }
        public double LonMin { get; }
        public double LonMax { get; }

        public TheaterBounds(double latMin, double latMax, double lonMin, double lonMax)
            => (LatMin, LatMax, LonMin, LonMax) = (latMin, latMax, lonMin, lonMax);

        public bool Contains(double lat, double lon)
            => ((LatMin <= lat) && (lat <= LatMax) && (LonMin <= lon) && (lon <= LonMax));
    }

    // ================================================================================================================

    /// <summary>
    /// defines the properties of a point of interest (poi) known to jafdtc. these instances are managed by the poi
    /// database (PointOfInterestDbase). pois include a theater (set based on lat/lon), optional campaign name,
    /// semicolon-separated list of tags, and a lat/lon/elev. the tuple (type, theater, name) must be unique across
    /// the database.
    /// </summary>
    public sealed class PointOfInterest
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public PointOfInterestType Type { get; set; }           // poi type (airfield, etc)

        // NOTE: Theater may not be null or empty and should be consistent with Latitude and Longitude.

        public string Theater { get; set; }                     // theater (general geographic area)

        public string Campaign { get; set; }                    // campaign name (null unless Type is CAMPAIGN)
        
        public string Name { get; set; }                        // name

        public string Tags { get; set; }                        // tags (";"-separated list)

        public string Latitude { get; set; }                    // latitude (decimal degrees)
        
        public string Longitude { get; set; }                   // longitude (decimal degrees)
        
        public string Elevation { get; set; }                   // elevation (feet)

        public static Dictionary<string, TheaterBounds> TheaterBounds => new()
        {
            ["Afghanistan"]     = new( 23.00,  38.75,   60.25,   73.25),
            ["Caucasus"]        = new( 40.00,  46.00,   33.00,   46.00),
            ["Germany"]         = new( 49.50,  54.50,    6.50,   16.00),
            ["Iraq"]            = new( 26.25,  37.00,   38.50,   52.00),
            ["Kola"]            = new( 64.00,  71.25,   12.00,   39.00),
            ["Marianas"]        = new( 11.75,  22.00,  141.00,  149.00),
            ["Nevada"]          = new( 34.50,  38.75, -118.50, -113.00),
            ["Persian Gulf"]    = new( 22.00,  30.75,   50.00,   59.00),
            ["Sinai"]           = new( 26.50,  32.00,   29.50,   35.75),
            ["South Atlantic"]  = new(-57.00, -48.00,  -77.00,  -55.00),
            ["Syria"]           = new( 31.25,  37.50,   30.75,   41.00)
        };
        public static List<string> Theaters => [.. TheaterBounds.Keys ];

        public string UniqueID => $"{(int)Type}:{Theater}:{Name}";

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
            => (Type, Theater, Campaign, Name, Tags, Latitude, Longitude, Elevation)
             = (PointOfInterestType.UNKNOWN, "", "", "", "", "", "", "");

        public PointOfInterest(PointOfInterestType type, string theater, string campaign, string name, string tags,
                               string lat, string lon, string elev)
            => (Type, Theater, Campaign, Name, Tags, Latitude, Longitude, Elevation)
             = (type, theater, campaign, name, tags, lat, lon, elev);

        public PointOfInterest(PointOfInterest poi)
            => (Type, Theater, Campaign, Name, Tags, Latitude, Longitude, Elevation)
             = (poi.Type, poi.Theater, poi.Campaign, poi.Name, poi.Tags, poi.Latitude, poi.Longitude, poi.Elevation);

        /// <summary>
        /// constructs a point of interest from a line of csv text. format of the line is,
        ///
        ///     [type],[campaign],[name],[tags],[latitude],[longitude],[elevation]
        ///     
        /// where the Theater is inferred from the decimal [latitude] and [longitude]. if the string is unable ot be
        /// parsed, the PointOfInterest is set to PointOfInterestType.UNKNOWN.
        /// </summary>
        public PointOfInterest(string csv)
        {
            Type = PointOfInterestType.UNKNOWN;
            Theater = "";
            Campaign = "";
            Name = "";
            Tags = "";
            Latitude = "";
            Longitude = "";
            Elevation = "";

            string[] cols = csv.Split(",");
            if (cols.Length >= 7)
            {
                string lat = cols[4].Trim();
                string lon = cols[5].Trim();
                string theater = TheaterForCoords(lat, lon);
                if (int.TryParse(cols[0].Trim(), out int type) && (theater != null))
                {
                    Type = (PointOfInterestType)type;
                    Theater = theater;
                    Campaign = cols[1].Trim();
                    Name = cols[2].Trim();
                    Tags = cols[3].Trim();
                    Latitude = lat;
                    Longitude = lon;
                    Elevation = cols[6].Trim();
                }
            }
        }

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
        /// returns a list of the names of the dcs theaters that contains the given coordinate, empty list if
        /// no theater matches the coordinates. the match is based on approximate lat/lon bounds of the theaters.
        /// note that a coordinate may appear in multiple theaters.
        /// </summary>
        public static List<string> TheatersForCoords(double lat, double lon)
        {
            List<string> theaters = [ ];
            foreach (KeyValuePair<string, TheaterBounds> kvp in TheaterBounds)
                if (kvp.Value.Contains(lat, lon))
                    theaters.Add(kvp.Key);
            return theaters;
        }

        /// <summary>
        /// returns a list of the names of the dcs theaters that contains the given coordinate, empty list if
        /// no theater matches the coordinates. the match is based on approximate lat/lon bounds of the theaters.
        /// note that a coordinate may appear in multiple theaters.
        /// </summary>
        public static List<string> TheatersForCoords(string lat, string lon)
            => TheatersForCoords(double.Parse(lat), double.Parse(lon));
        
// TODO: deprecate?
        /// <summary>
        /// return the name of the dcs theater that contains the given coordinate, null if no theater matches the
        /// coordinates. the match is based on approximate lat/lon bounds of the theaters.
        /// </summary>
        public static string TheaterForCoords(double lat, double lon)
        {
// TODO: this is kinda broken, theaters in dcs can overlap. use TheatersForCoords instead?
            foreach (KeyValuePair<string, TheaterBounds> kvp in TheaterBounds)
                if (kvp.Value.Contains(lat, lon))
                    return kvp.Key;
            return null;
        }

// TODO: deprecate?
        /// <summary>
        /// return the name of the dcs theater that contains the given coordinate, null if no theater matches the
        /// coordinates. the match is based on approximate lat/lon bounds of the theaters.
        /// </summary>
        public static string TheaterForCoords(string lat, string lon)
// TODO: this is kinda broken, theaters in dcs can overlap. use TheatersForCoords instead?
            => TheaterForCoords(double.Parse(lat), double.Parse(lon));

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
                        cleanTags += $"; {newValue}";
                }
                if (cleanTags.Length >= 3)
                    cleanTags = cleanTags[2..];
            }
            return cleanTags;
        }
    }
}
