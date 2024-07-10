// ********************************************************************************************************************
//
// PPCoordinateInfo.cs -- fa-18c pre-planned system coordinate info
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2024 ilominar/raven
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

namespace JAFDTC.Models.FA18C.PP
{
    /// <summary>
    /// pre-planned program coordinate information. a coordinate in the hornet pre-planned system can be located
    /// at a given lat/lon/elev or tied to a waypoint set through the navigation system. the WaypointNumber field
    /// distinguishes these cases (0 => position, >0 => waypoint number). this class is used both for target points
    /// (pp) as well as slam-er steerpionts (stp)
    ///
    /// a PPCoordinateInfo can only be set to valid coordinates, it can be reset to an invalid/empty state using the
    /// Reset() method.
    /// </summary>
    public class PPCoordinateInfo : NavpointInfoBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change or validation events.

        // this class uses Number from the base class to hold the waypoint number that is mapped to the pre-programmed
        // coordiate. to keep Number legal, we'll adjust by 1 here. WaypointNumber is the number of the waypoint (this
        // is index+1), 0 if the coordinate is a "position" coordinate.

        [JsonIgnore]
        public int WaypointNumber
        {
            get => Number - 1;
            set => Number = value + 1;
        }

        // ---- following properties post change and validation events.

        [JsonIgnore]
        private string _latUI;                      // string, DMDS "[N|S] 00° 00’ 00.00’’"
        [JsonIgnore]
        public override string LatUI
        {
            // NOTE: avionics are actually DMDS_P2 (no zero-fill), but zero-fill is necessary to make the ui work
            // NOTE: correctly. upload will back out the zero-fill to address this.

            get => Coord.ConvertFromLatDD(Lat, LLFormat.DMDS_P2ZF);
            set
            {
                string error = "Invalid latitude DMS format";
                if (IsRegexFieldValid(value, Coord.LatRegexFor(LLFormat.DMDS_P2ZF), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lat = Coord.ConvertToLatDD(value, LLFormat.DMDS_P2ZF);
                SetProperty(ref _latUI, value, error);
            }
        }

        [JsonIgnore]
        private string _lonUI;                      // string, DMDS "[E|W] 000° 00’ 00.00’’"
        [JsonIgnore]
        public override string LonUI
        {
            // NOTE: avionics are actually DMDS_P2 (no zero-fill), but zero-fill is necessary to make the ui work
            // NOTE: correctly. upload will back out the zero-fill to address this.

            get => Coord.ConvertFromLonDD(Lon, LLFormat.DMDS_P2ZF);
            set
            {
                string error = "Invalid longitude DMS format";
                if (IsRegexFieldValid(value, Coord.LonRegexFor(LLFormat.DMDS_P2ZF), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lon = Coord.ConvertToLonDD(value, LLFormat.DMDS_P2ZF);
                SetProperty(ref _lonUI, value, error);
            }
        }

        // ---- public properties, computed

        [JsonIgnore]
        public override bool IsValid => (base.IsValid || (WaypointNumber > 0));

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public PPCoordinateInfo() => (WaypointNumber) = (0);

        public PPCoordinateInfo(PPCoordinateInfo other)
        {
            WaypointNumber = other.WaypointNumber;
            Name = new(other.Name);
            Lat = new(other.Lat);
            Lon = new(other.Lon);
            Alt = new(other.Alt);
        }

        public virtual object Clone() => new PPCoordinateInfo(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // reset the steerpoint to default values.
        //
        public override void Reset()
        {
            base.Reset();
            WaypointNumber = 0;

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
