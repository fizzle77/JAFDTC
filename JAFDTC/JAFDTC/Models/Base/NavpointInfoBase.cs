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
using System.Collections.Generic;
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
        // known lat/lon coordinate formats for use with the conversion functions [Lat|Lon]RegexFor(),
        // ConvertFrom[Lat|Lon]DD(), and ConvertTo[Lat|Lon]DD().
        //
        public enum LLFormat
        {
            DD,             // decimal degrees
            DMS,            // degrees, minutes, seconds
            DDM_P3ZF,       // degrees, decimal minutes (to 3-digit precision), zero-fill degrees
            DDM_P2ZF,       // degrees, decimal minutes (to 2-digit precision), zero-fill degrees
            DDM_P2,         // degrees, decimal minutes (to 2-digit precision) 
        }

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
                                          ((string.IsNullOrEmpty(Lon)) ? "Unknown" : LonUI) + " / " +
                                          ((string.IsNullOrEmpty(Alt)) ? "Unknown" : Alt + "’");

        // ---- private read-only

        private static readonly Dictionary<LLFormat, Regex> _formatRegexLat = new()
        {
            [LLFormat.DD] = new(@"^([\-][0-9]\.[0-9]{6,12})|([\-][1-8][0-9]\.[0-9]{6,12})|([\-]90\.[0]{6,12})$"),
            [LLFormat.DMS] = new(@"^([NSns] [0-8][0-9]° [0-5][0-9]’ [0-5][0-9]’’)|([NSns] 90° 00’ 00’’)$"),
            [LLFormat.DDM_P3ZF] = new(@"^([NSns] [0-8][0-9]° [0-5][0-9]\.[0-9]{3}’)|([NSns] 90° 00\.000’)$"),
            [LLFormat.DDM_P2ZF] = new(@"^([NSns] [0-8][0-9]° [0-5][0-9]\.[0-9]{2}’)|([NSns] 90° 00\.00’)$"),
            [LLFormat.DDM_P2] = new(@"^([NSns] [0-8][0-9]° [0-5][0-9]\.[0-9]{2}’)|([NSns] 90° 00\.00’)$"),
        };

        private static readonly Dictionary<LLFormat, Regex> _formatRegexLon = new()
        {
            [LLFormat.DD] = new(@"^([\-][0-9]\.[0-9]{6,12})|([\-][1-9][0-9]\.[0-9]{6,12})|([\-]1[0-7][0-9]\.[0-9]{6,12})|([\-]180\.[0]{6,12})$"),
            [LLFormat.DMS] = new(@"^([EWew] 0[0-9]{2}° [0-5][0-9]’ [0-5][0-9]’’)|([EWew] 1[0-7][0-9]° [0-5][0-9]’ [0-5][0-9]’’)|([EWew] 180° 00’ 00’’)$"),
            [LLFormat.DDM_P3ZF] = new(@"^([EWew] 0[0-9]{2}° [0-5][0-9]\.[0-9]{3}’)|([EWew] 1[0-7][0-9]° [0-5][0-9]\.[0-9]{3}’)|([EWew] 180° 00\.000’)$"),
            [LLFormat.DDM_P2ZF] = new(@"^([EWew] 0[0-9]{2}° [0-5][0-9]\.[0-9]{2}’)|([EWew] 1[0-7][0-9]° [0-5][0-9]\.[0-9]{2}’)|([EWew] 180° 00\.00’)$"),
            [LLFormat.DDM_P2] = new(@"^([EWew] [0-9]° [0-5][0-9]\.[0-9]{2}’)|([EWew] [0-8][0-9]° [0-5][0-9]\.[0-9]{2}’)|([EWew] 180° 00\.00’)$"),
        };

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

        /// <summary>
        /// cleanup the steerpoint by adjusting content as necessary.
        /// </summary>
        public virtual void CleanUp() { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // format conversion
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns true regex for a latitude in the specified format.
        /// </summary>
        public static Regex LatRegexFor(LLFormat fmt) => _formatRegexLat[fmt];

        /// <summary>
        /// returns true regex for a latitude in the specified format.
        /// </summary>
        public static Regex LonRegexFor(LLFormat fmt) => _formatRegexLon[fmt];

        /// <summary>
        /// returns the string represented the specified format of a latitude in DD format.
        /// </summary>
        public static string ConvertFromLatDD(string latDD, LLFormat dstFmt)
            => dstFmt switch
            {
                LLFormat.DD => latDD,
                LLFormat.DMS => CoreDDtoDMS(latDD, 90.0, "N", "S"),
                LLFormat.DDM_P3ZF => CoreDDtoDDM(latDD, 90.0, "N", "S", 3, true),
                LLFormat.DDM_P2ZF => CoreDDtoDDM(latDD, 90.0, "N", "S", 2, true),
                LLFormat.DDM_P2 => CoreDDtoDDM(latDD, 90.0, "N", "S", 2, false),
                _ => "",
            };

        /// <summary>
        /// returns the string represented the specified format of a longitude in DD format.
        /// </summary>
        public static string ConvertFromLonDD(string lonDD, LLFormat dstFmt)
            => dstFmt switch
            {
                LLFormat.DD => lonDD,
                LLFormat.DMS => CoreDDtoDMS(lonDD, 180.0, "E", "W"),
                LLFormat.DDM_P3ZF => CoreDDtoDDM(lonDD, 180.0, "E", "W", 3, true),
                LLFormat.DDM_P2ZF => CoreDDtoDDM(lonDD, 180.0, "E", "W", 2, true),
                LLFormat.DDM_P2 => CoreDDtoDDM(lonDD, 180.0, "E", "W", 2, false),
                _ => "",
            };

        /// <summary>
        /// returns the string representing the DD format of a latitude in the specified format.
        /// </summary>
        public static string ConvertToLatDD(string latFmt, LLFormat fmt)
            => fmt switch
            {
                LLFormat.DD => latFmt,
                LLFormat.DMS => CoreDMStoDD(latFmt, _formatRegexLat[fmt], "N"),
                LLFormat.DDM_P3ZF => CoreDDMtoDD(latFmt, _formatRegexLat[fmt], "N"),
                LLFormat.DDM_P2ZF => CoreDDMtoDD(latFmt, _formatRegexLat[fmt], "N"),
                LLFormat.DDM_P2 => CoreDDMtoDD(latFmt, _formatRegexLat[fmt], "N"),
                _ => "",
            };

        /// <summary>
        /// returns the string representing the DD format of a longitude in the specified format.
        /// </summary>
        public static string ConvertToLonDD(string lonFmt, LLFormat fmt)
            => fmt switch
            {
                LLFormat.DD => lonFmt,
                LLFormat.DMS => CoreDMStoDD(lonFmt, _formatRegexLon[fmt], "E"),
                LLFormat.DDM_P3ZF => CoreDDMtoDD(lonFmt, _formatRegexLon[fmt], "E"),
                LLFormat.DDM_P2ZF => CoreDDMtoDD(lonFmt, _formatRegexLon[fmt], "E"),
                LLFormat.DDM_P2 => CoreDDMtoDD(lonFmt, _formatRegexLon[fmt], "E"),
                _ => "",
            };

        // ---- decimal degrees <--> degrees, decimal minutes ---------------------------------------------------------

        private static string CoreDDtoDDM(string ddValue, double max, string posDir, string negDir, int precision = 3, bool fillDeg = true)
        {
            if (double.TryParse(ddValue, out double dd) && (Math.Abs(dd) < max))
            {
                string dir = (dd >= 0.0) ? posDir : negDir;
                dd = Math.Abs(dd);
                int d = (int)Math.Truncate(dd);
                double dm = (dd - (double)d) * 60.0;
                string pad = (dm < 10.0) ? "0" : "";

                string dOut = $"{d}";
                if (fillDeg)
                {
                    dOut = (max > 100.0) ? $"{d,3:D3}" : $"{d,2:D2}";
                }
                string dmOut = string.Format(string.Format("{{0}}{{1:F{0}}}", precision), pad, dm);
                return $"{dir} {dOut}° {dmOut}’";
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
    }
}
