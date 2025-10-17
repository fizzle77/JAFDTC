// ********************************************************************************************************************
//
// WaypointInfo.cs -- f-14a/b waypoint base information
//
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

using JAFDTC.Models.Base;
using JAFDTC.Utilities;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F14AB.WYPT
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public partial class WaypointInfo : NavpointInfoBase
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
            // NOTE: avionics are actually DDM_P1 (no zero-fill), but zero-fill is necessary to make the ui work
            // NOTE: correctly. upload will back out the zero-fill to address this.

            get => Coord.ConvertFromLatDD(Lat, LLFormat.DDM_P1ZF);
            set
            {
                string error = "Invalid latitude DDM format";
                if (IsRegexFieldValid(value, Coord.LatRegexFor(LLFormat.DDM_P1ZF), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lat = Coord.ConvertToLatDD(value, LLFormat.DDM_P1ZF);
                SetProperty(ref _latUI, value, error);
            }
        }

// TODO: double check coordinate format
        [JsonIgnore]
        private string _lonUI;                      // string, DDM (DMTM) "[E|W] 000° 00.0’"
        [JsonIgnore]
        public override string LonUI
        {
            // NOTE: avionics are actually DDM_P1 (no zero-fill), but zero-fill is necessary to make the ui work
            // NOTE: correctly. upload will back out the zero-fill to address this.

            get => Coord.ConvertFromLonDD(Lon, LLFormat.DDM_P1ZF);
            set
            {
                string error = "Invalid longitude DDM format";
                if (IsRegexFieldValid(value, Coord.LonRegexFor(LLFormat.DDM_P1ZF), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lon = Coord.ConvertToLonDD(value, LLFormat.DDM_P1ZF);
                SetProperty(ref _lonUI, value, error);
            }
        }

        // NOTE: F14AB WYPT system doesn't use the altitude, allow it to be empty without flagging an error.

        public override string Alt
        {
            get => _alt;
            set
            {
                string error = "Invalid altitude format";
                if (IsIntegerFieldValid(value, -80000, 80000))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _alt, value, error);
                //
                // NOTE: force a change notification for LocationUI too when changing this property.
                //
                LocationUI = null;
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

            // set accessors treat "" as illegal. to work around the way SetProperty() handles updating backing store
            // and error state, first set the fields to "" with no error to set backing store. then, use the set
            // accessor with a known bad to set error state (which will not update backing store).
            //
            SetProperty(ref _latUI, "", null, nameof(LatUI));
            SetProperty(ref _lonUI, "", null, nameof(LonUI));

            LatUI = null;
            LonUI = null;
        }
    }
}
