// ********************************************************************************************************************
//
// PointOfInterestDbase.cs -- point of interest "database" model
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

using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace JAFDTC.Models.DCS
{
    [Flags]
    public enum PointOfInterestMask
    {
        NONE = 0,
        ANY = -1,
        DCS_AIRBASE = 1 << PointOfInterestType.DCS_AIRBASE,
        USER = 1 << PointOfInterestType.USER,
    }

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

        private readonly Dictionary<string, List<PointOfInterest>> _dbase;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        private PointOfInterestDbase()
        {
            List<PointOfInterest> pois = FileManager.LoadPointsOfInterest();
            _dbase = new();
            foreach (PointOfInterest poi in pois)
            {
                Add(poi, false);
            }
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
        /// return list of points of interest matching the specified criteria: theater name, type, and poi name. using
        /// the default values for these parameters matches "any".
        /// </summary>
        public List<PointOfInterest> Find(string theater = null, PointOfInterestMask types = PointOfInterestMask.ANY,
                                          string name = null)
        {
            List<PointOfInterest> results = new();
            foreach (KeyValuePair<string, List<PointOfInterest>> kvp in _dbase)
            {
                if ((theater == null) || (theater == kvp.Key))
                {
                    foreach (PointOfInterest point in kvp.Value)
                    {
                        PointOfInterestMask mask = (PointOfInterestMask)(1 << (int)point.Type);
                        if (((types == PointOfInterestMask.ANY) || ((mask & types) != 0)) &&
                            ((name == null) || (name == point.Name)))
                        {
                            results.Add(point);
                        }
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// TODO: document
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
        /// TODO: document
        /// </summary>
        public void Remove(PointOfInterest point)
        {
            // TODO: implement
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public bool Save()
        {
            return FileManager.SaveUserPointsOfInterest(Find(null, PointOfInterestMask.USER, null));
        }
    }
}
