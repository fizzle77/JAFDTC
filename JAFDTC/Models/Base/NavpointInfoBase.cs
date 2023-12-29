// ********************************************************************************************************************
//
// NavpointInfoBase.cs -- navigation point information abstract base class
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

using JAFDTC.Utilities;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// abstract base class for a navigation point description. this inclues number, name, lat, lon, and altitude.
    /// the lat and lon are always given in decimal degrees. derived classes are responsible for converting between
    /// dd and the airframe-appropriate format by over-riding LatUI, LonUI as necessary. the class provides functions
    /// to convert between common formats.
    /// </summary>
    public abstract class NavpointInfoBase : BindableObject, INavpointInfo
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- INotifyPropertyChanged, INotifyDataErrorInfo properties

        private int _number;                        // positive integer > 1
        public int Number
        {
            get => _number;
            set => SetProperty(ref _number, value, (value < 1) ? "Invalid number format" : null);
        }

        private string _name;                       // string
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value, null);
        }

        private string _lat;                        // string, decimal degrees
        public string Lat
        {
            get => _lat;
            set
            {
                string error = "Invalid latitude DD format";
                if (IsDecimalFieldValid(value, -90.0, 90.0, false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                SetProperty(ref _lat, value, error);
            }
        }

        public virtual string LatUI                 // string, decimal degrees
        {
            get => Lat;
            set => Lat = value;
        }

        private string _lon;                        // string, decimal degrees
        public string Lon
        {
            get => _lon;
            set
            {
                string error = "Invalid longitude DD format";
                if (IsDecimalFieldValid(value, -180.0, 180.0, false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                SetProperty(ref _lon, value, error);
            }
        }

        public virtual string LonUI                 // string, decimal degrees
        {
            get => Lon;
            set => Lon = value;
        }

        private string _alt;                        // positive integer, on [-1500, 80000]
        public string Alt
        {
            get => _alt;
            set
            {
                string error = "Invalid altitude format";
                if (IsIntegerFieldValid(value, -1500, 80000, false))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _alt, value, error);
            }
        }

        // ---- synthesized properties

        [JsonIgnore]
        public virtual bool IsValid => (IsIntegerFieldValid(_alt, -1500, 80000, false) &&
                                        IsDecimalFieldValid(_lat, -90.0, 90.0, false) &&
                                        IsDecimalFieldValid(_lon, -180.0, 180.0, false));

        [JsonIgnore]
        public virtual string Location => ((string.IsNullOrEmpty(Lat)) ? "Unknown" : Coord.RemoveLLDegZeroFill(LatUI)) + ", " +
                                          ((string.IsNullOrEmpty(Lon)) ? "Unknown" : Coord.RemoveLLDegZeroFill(LonUI)) + " / " +
                                          ((string.IsNullOrEmpty(Alt)) ? "Unknown" : Alt + "’");

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public NavpointInfoBase() => (Name, Lat, Lon, Alt) = ("", "", "", "");

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the steerpoint to default values. the Number field is not changed.
        /// </summary>
        public virtual void Reset() => (Name, Lat, Lon, Alt) = ("", "", "", "");
    }
}
