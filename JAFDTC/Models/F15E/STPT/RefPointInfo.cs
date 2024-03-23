// ********************************************************************************************************************
//
// RefPointInfo.cs -- f-15e reference point
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

using JAFDTC.Utilities;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F15E.STPT
{
    /// <summary>
    /// mudhen steerpoint system reference point. reference points are associated with steerpoints and are located
    /// via lat/lon/alt coordinates. the mudhen supports up to 7 reference points per steertpoint. as in base
    /// navpoints, the ui views of the lat/lon (LatUI/LonUI) are layered on top of the persisted DD format lat/lon
    /// (Lat/Lon).
    /// 
    /// TODO: support rng/brng specifications, not just lat/lon
    /// </summary>
    public class RefPointInfo : BindableObject
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties, posts change/validation events

        private int _number;                        // positive integer on [1, 7]
        public int Number
        {
            get => _number;
            //
            // NOTE: number should only be set by code, so no need for validation here...
            //
            set => SetProperty(ref _number, value);
        }

        private string _name;                       // string
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value, null);
        }

        private string _lat;                        // string, decimal degrees (raw, no units)
        public string Lat
        {
            get => _lat;
            set
            {
                string error = "Invalid latitude DD format";
                if (IsDecimalFieldValid(value, -90.0, 90.0))
                {
                    value = value.ToUpper();
                    error = null;
                }
                SetProperty(ref _lat, value, error);
            }
        }

        [JsonIgnore]
        private string _latUI;                      // string, DDM "[N|S] 00° 00.000’"
        [JsonIgnore]
        public string LatUI
        {
            get => Coord.ConvertFromLatDD(Lat, LLFormat.DDM_P3ZF);
            set
            {
                string error = "Invalid latitude DDM format";
                if (IsRegexFieldValid(value, Coord.LatRegexFor(LLFormat.DDM_P3ZF)))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lat = Coord.ConvertToLatDD(value, LLFormat.DDM_P3ZF);
                SetProperty(ref _latUI, value, error);
            }
        }

        private string _lon;                        // string, decimal degrees (raw, no units)
        public string Lon
        {
            get => _lon;
            set
            {
                string error = "Invalid longitude DD format";
                if (IsDecimalFieldValid(value, -180.0, 180.0))
                {
                    value = value.ToUpper();
                    error = null;
                }
                SetProperty(ref _lon, value, error);
            }
        }

        [JsonIgnore]
        private string _lonUI;                      // string, DDM "[E|W] 000° 00.000’"
        [JsonIgnore]
        public string LonUI
        {
            get => Coord.ConvertFromLonDD(Lon, LLFormat.DDM_P3ZF);
            set
            {
                string error = "Invalid longitude DDM format";
                if (IsRegexFieldValid(value, Coord.LonRegexFor(LLFormat.DDM_P3ZF)))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lon = Coord.ConvertToLonDD(value, LLFormat.DDM_P3ZF);
                SetProperty(ref _lonUI, value, error);
            }
        }

        private string _alt;                        // integer, on [1, 59999] or [-59999, -1]
        public string Alt
        {
            get => _alt;
            set
            {
                string error = "Invalid altitude format";
                if (IsIntegerFieldValid(value, 1, 59999) || IsIntegerFieldValid(value, -59999, -1))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _alt, value, error);
            }
        }

        // ---- public properties, computed

        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrEmpty(_name) && string.IsNullOrEmpty(_lat) &&
                               string.IsNullOrEmpty(_lon) && string.IsNullOrEmpty(_alt);

        [JsonIgnore]
        public virtual bool IsValid => ((IsIntegerFieldValid(_alt, 1, 59999, false) ||
                                         IsIntegerFieldValid(_alt, -59999, -1, false)) &&
                                        IsDecimalFieldValid(_lat, -90.0, 90.0, false) &&
                                        IsDecimalFieldValid(_lon, -180.0, 180.0, false));

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public RefPointInfo() => (Number) = (1);

        public RefPointInfo(int number = 1) => (Number) = (number);

        public RefPointInfo(RefPointInfo other)
        {
            Number = other.Number;
            Name = new(other.Name);
            Lat = new(other.Lat);
            Lon = new(other.Lon);
            Alt = new(other.Alt);
        }

        public virtual object Clone() => new RefPointInfo(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the reference point to default values. the Number field is not changed.
        /// </summary>
        public void Reset()
        {
            Name = "";
            Lat = "";
            Lon = "";
            Alt = "";
        }
    }
}
