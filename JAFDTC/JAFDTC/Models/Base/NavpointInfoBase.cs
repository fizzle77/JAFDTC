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
using System;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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
        // regular expressions for the DDM and DMS strings that the class supports. conversion to/from DDM/DMS
        // expect or produce strings in these formats.
        //
        protected static readonly Regex DDMlatRegex = new(@"^[NSns] [0-8][0-9]° [0-5][0-9]\.[0-9]{3}’$");
        protected static readonly Regex DDMlonRegex = new(@"^([EWew] 0[0-9]{2}° [0-5][0-9]\.[0-9]{3}’)|([EWew] 1[0-7][0-9]° [0-5][0-9]\.[0-9]{3}’)$");

        protected static readonly Regex DMSlatRegex = new(@"^[NSns] [0-8][0-9]° [0-5][0-9]’ [0-5][0-9]’’$");
        protected static readonly Regex DMSlonRegex = new(@"^([EWew] 0[0-9]{2}° [0-5][0-9]’ [0-5][0-9]’’)|([EWew] 1[0-7][0-9]° [0-5][0-9]’ [0-5][0-9]’’)$");

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
        public virtual string Location => ((string.IsNullOrEmpty(Lat)) ? "Unknown" : LatUI) + ", " +
                                          ((string.IsNullOrEmpty(Lon)) ? "Unknown" : LonUI) + ", " +
                                          ((string.IsNullOrEmpty(Alt)) ? "Unknown" : Alt);

        // ------------------------------------------------------------------------------------------------------------
        //
        // consturction
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

        /// <summary>
        /// cleanup the steerpoint by adjusting content as necessary.
        /// </summary>
        public virtual void CleanUp() { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // format conversion
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- decimal degrees <--> degrees, decimal minutes ---------------------------------------------------------

        private static string CoreDDtoDDM(string ddValue, double max, string posDir, string negDir)
        {
            if (double.TryParse(ddValue, out double dd) && (Math.Abs(dd) < max))
            {
                string dir = (dd >= 0.0) ? posDir : negDir;
                dd = Math.Abs(dd);
                int d = (int)Math.Truncate(dd);
                double dm = (dd - (double)d) * 60.0;
                string pad = (dm < 10.0) ? "0" : "";
                return (max >= 100.0) ? $"{dir} {d,3:D3}° {pad}{dm:F3}’" : $"{dir} {d,2:D2}° {pad}{dm:F3}’";
            }
            return "";
        }

        private static string CoreDDMtoDD(string ddmValue, Regex regex, string posDir)
        {
            if (regex.IsMatch(ddmValue))
            {
                string[] parts = ddmValue.Replace("°", "").Replace("’", "").Split(' ');
                if ((parts.Length == 3) && double.TryParse(parts[1], out double d) &&
                                           double.TryParse(parts[2], out double dm))
                {
                    double dd = (d + (dm / 60.0)) * ((parts[0] == posDir) ? 1.0 : -1.0);
                    return $"{dd:F8}";
                }
            }
            return "";
        }

        public static string ConvertLatDDtoDDM(string ddValue) => CoreDDtoDDM(ddValue, 90.0, "N", "S");

        public static string ConvertLatDDMtoDD(string ddValue) => CoreDDMtoDD(ddValue, DDMlatRegex, "N");

        public static string ConvertLonDDtoDDM(string ddValue) => CoreDDtoDDM(ddValue, 180.0, "E", "W");

        public static string ConvertLonDDMtoDD(string ddValue) => CoreDDMtoDD(ddValue, DDMlonRegex, "E");

        // ---- decimal degrees <--> degrees, minutes, seconds --------------------------------------------------------

        private static string CoreDDtoDMS(string ddValue, double max, string posDir, string negDir)
        {
            if (double.TryParse(ddValue, out double dd) && (Math.Abs(dd) < max))
            {
                string dir = (dd >= 0.0) ? posDir : negDir;
                dd = Math.Abs(dd);
                int d = (int)Math.Truncate(dd);
                double dm = (dd - (double)d) * 60.0;
                int m = (int)Math.Truncate(dm);
                int s = (int)((dm - (double)m) * 60.0);
                return (max >= 100.0) ? $"{dir} {d,3:D3}° {m,2:D2}’ {s,2:D2}’’" : $"{dir} {d,2:D2}° {m,2:D2}’ {s,2:D2}’’";
            }
            return "";
        }

        private static string CoreDMStoDD(string ddmValue, Regex regex, string posDir)
        {
            if (regex.IsMatch(ddmValue))
            {
                string[] parts = ddmValue.Replace("°", "").Replace("’", "").Split(' ');
                if ((parts.Length == 4) && int.TryParse(parts[1], out int d) &&
                                           int.TryParse(parts[2], out int m) &&
                                           int.TryParse(parts[3], out int s))
                {
                    double dd = (d + (m / 60.0) + (s / 3600.0)) * ((parts[0] == posDir) ? 1.0 : -1.0);
                    return $"{dd:F8}";
                }
            }
            return "";
        }

        public static string ConvertLatDDtoDMS(string ddValue) => CoreDDtoDMS(ddValue, 90.0, "N", "S");

        public static string ConvertLatDMStoDD(string ddValue) => CoreDMStoDD(ddValue, DMSlatRegex, "N");

        public static string ConvertLonDDtoDMS(string ddValue) => CoreDDtoDMS(ddValue, 180.0, "E", "W");

        public static string ConvertLonDMStoDD(string ddValue) => CoreDMStoDD(ddValue, DMSlonRegex, "E");
    }
}
