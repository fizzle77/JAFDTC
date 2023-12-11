// ********************************************************************************************************************
//
// WaypointInfo.cs -- av-8b waypoint base information
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

namespace JAFDTC.Models.AV8B.WYPT
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
        private string _latUI;                      // string, DMS "[N|S] 00° 00’ 00’’"
        [JsonIgnore]
        public override string LatUI
        {
            get => ConvertLatDDtoDMS(Lat);
            set
            {
                string error = "Invalid latitude DMS format";
                if (IsRegexFieldValid(value, DMSlatRegex, false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lat = ConvertLatDMStoDD(value);
                SetProperty(ref _latUI, value, error);
            }
        }

        [JsonIgnore]
        private string _lonUI;                      // string, DMS "[E|W] 000° 00’ 00’’"
        [JsonIgnore]
        public override string LonUI
        {
            get => ConvertLonDDtoDMS(Lon);
            set
            {
                string error = "Invalid longitude DMS format";
                if (IsRegexFieldValid(value, DMSlonRegex, false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lon = ConvertLonDMStoDD(value);
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

        // cleanup the steerpoint by adjusting content as necessary.
        //
        public override void CleanUp()
        {
            base.CleanUp();
        }
    }
}
