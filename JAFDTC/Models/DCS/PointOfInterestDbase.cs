// ********************************************************************************************************************
//
// PointOfInterestDbase.cs -- point of interest "database" model
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

using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;

namespace JAFDTC.Models.DCS
{
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
    /// flags to control paramters of a query in the point of interest database via Find().
    /// </summary>
    [Flags]
    public enum PointOfInterestDbQueryFlags
    {
        NONE = 0,                                               // no flags
        NAME_PARTIAL_MATCH = 1 << 0,                            // allow partial match of name
        TAGS_ANY_MATCH = 1 << 1,                                // allow at least one tag match
        TAG_PARTIAL_MATCH = 1 << 2,                             // allow partial match of a tag
    }

    // ================================================================================================================

    /// <summary>
    /// parameters for a query of the point of interest database via Find(). for a poi to match a query,
    /// the following must hold:
    /// 
    ///     1) Types contains the point of interest Type
    ///     2) Theater matches the point of interest Theater exactly
    ///     3) Name matches the point of interest Name per Flags, given poi Name "abcdef"
    ///             NAME_PARTIAL_MATCH => Name "bcd" matches
    ///            !NAME_PARTIAL_MATCH => Name "bcd" does not match
    ///     4) Tags matches the point of interest tags per Flags, given poi Tags "aa ; bb"
    ///             TAGS_ANY_MATCH => to match, at least one tag in poi Tags must match a tag in poi Tags
    ///            !TAGS_ANY_MATCH => to match, all tags in poi Tags must match a tag in poi Tags
    ///             TAG_PARTIAL_MATCH => allows partial matches, "a" matches "aa"
    ///            !TAG_PARTIAL_MATCH => disallows partial matches, "a" does not match "aa"
    ///
    /// string comparisons are always case-insensitive.
    /// </summary>
    internal class PointOfInterestDbQuery
    {
        public readonly PointOfInterestTypeMask Types;          // types of points of interest to search

        public readonly string Theater;                         // theater (null => match any)

        public readonly string Name;                            // name (null => match any)

        public readonly string Tags;                            // tags (";"-separated list, null => match any)

        public readonly PointOfInterestDbQueryFlags Flags;      // query flags

        public PointOfInterestDbQuery(PointOfInterestTypeMask types = PointOfInterestTypeMask.ANY, string theater = null,
                                      string name = null, string tags = null,
                                      PointOfInterestDbQueryFlags flags = PointOfInterestDbQueryFlags.NONE)
            => (Types, Theater, Name, Tags, Flags) = (types, theater, name, tags, flags);
    }

    // ================================================================================================================

    /// <summary>
    /// point of interest (poi) database holds information (PointOfInterest instances) known to jafdtc. the database
    /// class is a singleton that supports find operations to query the known pois. the database is built from fixed
    /// dcs pois (such as airfields) as well as user-defined pois.
    /// </summary>
    internal class PointOfInterestDbase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // singleton
        //
        // ------------------------------------------------------------------------------------------------------------

        private static readonly Lazy<PointOfInterestDbase> lazy = new(() => new PointOfInterestDbase());

        public static PointOfInterestDbase Instance { get => lazy.Value; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // database is organized as a dictionary where keys are theater names and values are lists of PointOfInterest
        // instances for points of interest in that theater.

        private readonly Dictionary<string, List<PointOfInterest>> _dbase;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        private PointOfInterestDbase()
        {
            _dbase = new();
            Reset();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

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
            else if (InRange(10.0, lat, 23.0) && InRange(-149.0, lon, -137.0))
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

        /// <summary>
        /// return a list of strings for all of the theaters represented in the database.
        /// 
        /// TODO: consider allowing user-defined theaters.
        /// </summary>
        public static List<string> KnownTheaters()
        {
            return new()
            {
                "Caucasus",
                "Marianas",
                "Nevada",
                "Persian Gulf",
                "Sinai",
                "South Atlantic",
                "Syria",
                "Other"
            };
        }

        /// <summary>
        /// return list of points of interest matching the specified criteria in the query: theater name, tags,
        /// type, and poi name. using the default values for these parameters matches "any". database seraches
        /// are always case insensitive.
        /// </summary>
        public List<PointOfInterest> Find(PointOfInterestDbQuery query)
        {
            string theater = query.Theater?.ToLower();
            string tags = query.Tags?.ToLower();
            string name = query.Name?.ToLower();
            PointOfInterestDbQueryFlags flags = query.Flags;
            PointOfInterestTypeMask types = query.Types;

            List<PointOfInterest> results = new();
            foreach (KeyValuePair<string, List<PointOfInterest>> kvp in _dbase)
            {
                if (!string.IsNullOrEmpty(theater) && (theater != kvp.Key.ToLower()))
                {
                    continue;
                }

                foreach (PointOfInterest poi in kvp.Value)
                {
                    PointOfInterestTypeMask poiType = (PointOfInterestTypeMask)(1 << (int)poi.Type);
                    if (!types.HasFlag(poiType))
                    {
                        continue;
                    }

                    string poiName = poi.Name.ToLower();
                    if (!string.IsNullOrEmpty(name) &&
                        ((!flags.HasFlag(PointOfInterestDbQueryFlags.NAME_PARTIAL_MATCH) && (name != poiName)) ||
                         ( flags.HasFlag(PointOfInterestDbQueryFlags.NAME_PARTIAL_MATCH) && !poiName.Contains(name))))
                    {
                        continue;
                    }

                    bool isMatch = true;
                    if (!string.IsNullOrEmpty(tags))
                    {
                        List<string> tagVals = (string.IsNullOrEmpty(tags))
                            ? new() : tags.Split(';').ToList<string>();
                        List<string> poiTagVals = (string.IsNullOrEmpty(poi.Tags))
                            ? new() : poi.Tags.ToLower().Split(';').ToList<string>();

                        if (flags.HasFlag(PointOfInterestDbQueryFlags.TAGS_ANY_MATCH))
                        {
                            isMatch = false;
                            foreach (string tagVal in tagVals)
                            {
                                foreach (string poiTagVal in poiTagVals)
                                {
                                    if ((flags.HasFlag(PointOfInterestDbQueryFlags.TAG_PARTIAL_MATCH) &&
                                         (tagVal.Trim().Contains(poiTagVal.Trim()))) ||
                                        (tagVal.Trim() == poiTagVal.Trim()))
                                    {
                                        isMatch = true;
                                        break;
                                    }
                                }
                                if (isMatch)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            isMatch = true;
                            foreach (string tagVal in tagVals)
                            {
                                bool isFound = false;
                                foreach (string poiTagVal in poiTagVals)
                                {
                                    if ((flags.HasFlag(PointOfInterestDbQueryFlags.TAG_PARTIAL_MATCH) &&
                                         (tagVal.Trim().Contains(poiTagVal.Trim()))) ||
                                        (tagVal.Trim() == poiTagVal.Trim()))
                                    {
                                        isFound = true;
                                        break;
                                    }
                                }
                                if (!isFound)
                                {
                                    isMatch = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (isMatch)
                    {
                        results.Add(poi);
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// parse a multi-line tab-separated value string to build a list of points of interest. the format of each
        /// line is as follows
        ///
        ///     [Name]\t[tags]\t[latitude]\t[longitude]\t[elevation]
        ///
        /// where "\t" is a tab character.
        /// </summary>
        public static List<PointOfInterest> ParseTSV(string tsv)
        {
            List<PointOfInterest> pois = new();
            string[] lines = (string.IsNullOrEmpty(tsv)) ? Array.Empty<string>() : tsv.Replace("\r", "").Split('\n');
            foreach (string line in lines)
            {
                string[] cols = line.Split("\t");
                if (cols.Length == 5)
                {
                    PointOfInterest poi = new()
                    {
                        Name = cols[0].Trim(),
                        Tags = cols[1].Trim(),
                        Latitude = cols[2].Trim(),
                        Longitude = cols[3].Trim(),
                        Elevation = cols[4].Trim()
                    };
                    poi.Theater = TheaterForCoords(poi.Latitude, poi.Longitude);
                    if (poi.Theater != null )
                    {
                        pois.Add(poi);
                    }
                }
            }
            return pois;
        }

        /// <summary>
        /// reset the database by clearing its current contents and reloading from storage.
        /// </summary>
        public void Reset()
        {
            List<PointOfInterest> pois = FileManager.LoadPointsOfInterest();
            _dbase.Clear();
            foreach (PointOfInterest poi in pois)
            {
                Add(poi, false);
            }
        }

        /// <summary>
        /// add a point of interest to the database, persisting the database to storage if requested.
        /// </summary>
        public void Add(PointOfInterest poi, bool isPersist = true)
        {
            if (!_dbase.ContainsKey(poi.Theater))
            {
                _dbase[poi.Theater] = new List<PointOfInterest>();
            }
            _dbase[poi.Theater].Add(poi);
            if (isPersist)
            {
                Save();
            }
        }

        /// <summary>
        /// remove a point of interest to the database, persisting the database to storage if requested.
        /// </summary>
        public void Remove(PointOfInterest poi, bool isPersist = true)
        {
            _dbase[poi.Theater].Remove(poi);
            if (isPersist)
            {
                Save();
            }
        }

        /// <summary>
        /// persist the user points of interest to storage.
        /// </summary>
        public bool Save()
        {
            return FileManager.SaveUserPointsOfInterest(Find(new PointOfInterestDbQuery(PointOfInterestTypeMask.USER)));
        }
    }
}
