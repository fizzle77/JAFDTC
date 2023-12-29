﻿// ********************************************************************************************************************
//
// WaypointInfo.cs -- m-2000c waypoint base information
//
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

using JAFDTC.Models.Base;
using JAFDTC.Utilities;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.M2000C.WYPT
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class WaypointInfo : NavpointInfoBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties post change and validation events.

        [JsonIgnore]
        private string _latUI;                      // string, DDM "[N|S] 00° 00.000’"
        [JsonIgnore]
        public override string LatUI
        {
            get => Coord.ConvertFromLatDD(Lat, LLFormat.DDM_P3ZF);
            set
            {
                string error = "Invalid latitude DDM format";
                if (IsRegexFieldValid(value, Coord.LatRegexFor(LLFormat.DDM_P3ZF), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lat = Coord.ConvertToLatDD(value, LLFormat.DDM_P3ZF);
                SetProperty(ref _latUI, value, error);
            }
        }

        [JsonIgnore]
        private string _lonUI;                      // string, DDM "[E|W] 000° 00.000’"
        [JsonIgnore]
        public override string LonUI
        {
            get => Coord.ConvertFromLonDD(Lon, LLFormat.DDM_P3ZF);
            set
            {
                string error = "Invalid longitude DDM format";
                if (IsRegexFieldValid(value, Coord.LonRegexFor(LLFormat.DDM_P3ZF), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lon = Coord.ConvertToLonDD(value, LLFormat.DDM_P3ZF);
                SetProperty(ref _lonUI, value, error);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public WaypointInfo() => (Number) = (1);

        public WaypointInfo(WaypointInfo other)
        {
            Number = other.Number;
            Name = new(other.Name);
            Lat = new(other.Lat);
            Lon = new(other.Lon);
            Alt = new(other.Alt);
        }

        public virtual object Clone() => new WaypointInfo(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the waypoint to default values. the Number field is not changed.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
        }
    }
}
