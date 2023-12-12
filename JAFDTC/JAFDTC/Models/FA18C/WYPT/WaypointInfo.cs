// ********************************************************************************************************************
//
// WaypointInfo.cs -- fa-18c waypoint base information
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

namespace JAFDTC.Models.FA18C.WYPT
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
        private string _latUI;                      // string, DDM "[N|S] 00° 00.00’"
        [JsonIgnore]
        public override string LatUI
        {
            // NOTE: avionics are actually DDM_P2 (no zero-fill), but zero-fill is necessary to make the ui work
            // NOTE: correctly. upload will back out the zero-fill to address this.

            get => ConvertFromLatDD(Lat, LLFormat.DDM_P2ZF);
            set
            {
                string error = "Invalid latitude DMS format";
                if (IsRegexFieldValid(value, LatRegexFor(LLFormat.DDM_P2ZF), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lat = ConvertToLatDD(value, LLFormat.DDM_P2ZF);
                SetProperty(ref _latUI, value, error);
            }
        }

        [JsonIgnore]
        private string _lonUI;                      // string, DDM "[E|W] 000° 00.00’"
        [JsonIgnore]
        public override string LonUI
        {
            // NOTE: avionics are actually DDM_P2 (no zero-fill), but zero-fill is necessary to make the ui work
            // NOTE: correctly. upload will back out the zero-fill to address this.

            get => ConvertFromLonDD(Lon, LLFormat.DDM_P2ZF);
            set
            {
                string error = "Invalid longitude DMS format";
                if (IsRegexFieldValid(value, LonRegexFor(LLFormat.DDM_P2ZF), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lon = ConvertToLonDD(value, LLFormat.DDM_P2ZF);
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

        // reset the steerpoint to default values. the Number field is not changed.
        //
        public override void Reset()
        {
            base.Reset();
        }
    }
}
