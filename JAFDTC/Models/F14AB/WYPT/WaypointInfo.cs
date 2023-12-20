// ********************************************************************************************************************
//
// WaypointInfo.cs -- f-14a/b waypoint base information
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
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F14AB.WYPT
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

        // TODO: double check coordinate format
        [JsonIgnore]
        private string _latUI;                      // string, DDM (DMTM) "[N|S] 00° 00.0’"
        [JsonIgnore]
        public override string LatUI
        {
            get => ConvertFromLatDD(Lat, LLFormat.DDM_P1);
            set
            {
                string error = "Invalid latitude DDM format";
                if (IsRegexFieldValid(value, LatRegexFor(LLFormat.DDM_P1), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lat = ConvertToLatDD(value, LLFormat.DDM_P1);
                SetProperty(ref _latUI, value, error);
            }
        }

        // TODO: double check coordinate format
        [JsonIgnore]
        private string _lonUI;                      // string, DDM (DMTM) "[E|W] 000° 00.0’"
        [JsonIgnore]
        public override string LonUI
        {
            get => ConvertFromLonDD(Lon, LLFormat.DDM_P1);
            set
            {
                string error = "Invalid longitude DDM format";
                if (IsRegexFieldValid(value, LonRegexFor(LLFormat.DDM_P1), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lon = ConvertToLonDD(value, LLFormat.DDM_P1);
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
