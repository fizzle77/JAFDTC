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

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// point of interest (poi) database holds information (PointOfInterest instances) known to jafdtc. the database
    /// class is a singleton that supports find operations to query the known pois.
    /// 
    /// TODO: at some point, generalize this out to allow user-defined pois.
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

        private Dictionary<string, List<PointOfInterest>> Dbase { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        private PointOfInterestDbase()
        {
            // TODO: may want to add ability to load files other than base poi list?
            Dbase = FileManager.LoadPointsOfInterest();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document 
        /// </summary>
        public List<PointOfInterest> Find()
        {
            // TODO: currently only query supported is "find all".
            List<PointOfInterest> results = new();
            foreach (KeyValuePair<string, List<PointOfInterest>> kvp in Dbase)
            {
                foreach (PointOfInterest point in kvp.Value)
                {
                    results.Add(point);
                }
            }
            return results;
        }
    }
}
