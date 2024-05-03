// ********************************************************************************************************************
//
// Coord.cs : coordinate transformation and conversion functions
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace JAFDTC.Utilities
{
    /// <summary>
    /// known lat/lon coordiante formats for use wth the conversion functions Coord provides.
    /// </summary>
    public enum LLFormat
    {
        DD,             // decimal degrees (raw number only)
        DDU,            // decimal degrees (with degree units)
        DMS,            // degrees, minutes, seconds
        DMDS_P2ZF,      // degrees, minutes, decimal seconds (to 2-digit precision), zero-fill degrees
        DDM_P3ZF,       // degrees, decimal minutes (to 3-digit precision), zero-fill degrees
        DDM_P2ZF,       // degrees, decimal minutes (to 2-digit precision), zero-fill degrees
        DDM_P1ZF,       // degrees, decimal minutes (to 1-digit precision), zero-fill degrees
        DDM_P2,         // degrees, decimal minutes (to 2-digit precision)
        DDM_P1,         // degrees, decimal minutes (to 1-digit precision)
    }

    /// <summary>
    /// TODO: document
    /// </summary>
    public class Coord
    {
        // ---- private read-only

        private static readonly Regex _regexRemoveDegZeroPad = new(@"^([NSEWnsew] )([0]*)([1-9].*)$");

        private static readonly Dictionary<LLFormat, Regex> _regexFormatLat = new()
        {
            [LLFormat.DD] = new(@"^([\-]{0,1}[0-9]\.[0-9]{6,12})|([\-]{0,1}[1-8][0-9]\.[0-9]{6,12})|([\-]{0,1}90\.[0]{6,12})$"),
            [LLFormat.DDU] = new(@"^([\-]{0,1}[0-9]\.[0-9]{6,12}°)|([\-]{0,1}[1-8][0-9]\.[0-9]{6,12}°)|([\-]{0,1}90\.[0]{6,12}°)$"),
            [LLFormat.DMS] = new(@"^([NSns] [0-8][0-9]° [0-5][0-9]’ [0-5][0-9]’’)|([NSns] 90° 00’ 00’’)$"),
            [LLFormat.DMDS_P2ZF] = new(@"^([NSns] [0-8][0-9]° [0-5][0-9]’ [0-5][0-9]\.[0-9]{2}’’)|([NSns] 90° 00’ 00\.00’’)$"),
            [LLFormat.DDM_P3ZF] = new(@"^([NSns] [0-8][0-9]° [0-5][0-9]\.[0-9]{3}’)|([NSns] 90° 00\.000’)$"),
            [LLFormat.DDM_P2ZF] = new(@"^([NSns] [0-8][0-9]° [0-5][0-9]\.[0-9]{2}’)|([NSns] 90° 00\.00’)$"),
            [LLFormat.DDM_P1ZF] = new(@"^([NSns] [0-8][0-9]° [0-5][0-9]\.[0-9]{1}’)|([NSns] 90° 00\.0’)$"),
            [LLFormat.DDM_P2] = new(@"^([NSns] [0-8][0-9]° [0-5][0-9]\.[0-9]{2}’)|([NSns] 90° 00\.00’)$"),
            [LLFormat.DDM_P1] = new(@"^([NSns] [0-8][0-9]° [0-5][0-9]\.[0-9]{1}’)|([NSns] 90° 00\.0’)$"),
        };

        private static readonly Dictionary<LLFormat, Regex> _regexFormatLon = new()
        {
            [LLFormat.DD] = new(@"^([\-]{0,1}[0-9]\.[0-9]{6,12})|([\-]{0,1}[1-9][0-9]\.[0-9]{6,12})|([\-]{0,1}1[0-7][0-9]\.[0-9]{6,12})|([\-]{0,1}180\.[0]{6,12})$"),
            [LLFormat.DDU] = new(@"^([\-]{0,1}[0-9]\.[0-9]{6,12}°)|([\-]{0,1}[1-9][0-9]\.[0-9]{6,12}°)|([\-]{0,1}1[0-7][0-9]\.[0-9]{6,12}°)|([\-]{0,1}180\.[0]{6,12}°)$"),
            [LLFormat.DMS] = new(@"^([EWew] 0[0-9]{2}° [0-5][0-9]’ [0-5][0-9]’’)|([EWew] 1[0-7][0-9]° [0-5][0-9]’ [0-5][0-9]’’)|([EWew] 180° 00’ 00’’)$"),
            [LLFormat.DMDS_P2ZF] = new(@"^([EWew] 0[0-9]{2}° [0-5][0-9]’ [0-5][0-9]\.[0-9]{2}’’)|([EWew] 1[0-7][0-9]° [0-5][0-9]’ [0-5][0-9]\.[0-9]{2}’’)|([EWew] 180° 00’ 00.00’’)$"),
            [LLFormat.DDM_P3ZF] = new(@"^([EWew] 0[0-9]{2}° [0-5][0-9]\.[0-9]{3}’)|([EWew] 1[0-7][0-9]° [0-5][0-9]\.[0-9]{3}’)|([EWew] 180° 00\.000’)$"),
            [LLFormat.DDM_P2ZF] = new(@"^([EWew] 0[0-9]{2}° [0-5][0-9]\.[0-9]{2}’)|([EWew] 1[0-7][0-9]° [0-5][0-9]\.[0-9]{2}’)|([EWew] 180° 00\.00’)$"),
            [LLFormat.DDM_P1ZF] = new(@"^([EWew] 0[0-9]{2}° [0-5][0-9]\.[0-9]{1}’)|([EWew] 1[0-7][0-9]° [0-5][0-9]\.[0-9]{1}’)|([EWew] 180° 00\.0’)$"),
            [LLFormat.DDM_P2] = new(@"^([EWew] [0-9]° [0-5][0-9]\.[0-9]{2}’)|([EWew] [0-8][0-9]° [0-5][0-9]\.[0-9]{2}’)|([EWew] 180° 00\.00’)$"),
            [LLFormat.DDM_P1] = new(@"^([EWew] [0-9]° [0-5][0-9]\.[0-9]{1}’)|([EWew] [0-8][0-9]° [0-5][0-9]\.[0-9]’)|([EWew] 180° 00\.0’)$"),
        };

        // ------------------------------------------------------------------------------------------------------------
        //
        // format conversion
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns regex for a latitude in the specified format.
        /// </summary>
        public static Regex LatRegexFor(LLFormat fmt) => _regexFormatLat[fmt];

        /// <summary>
        /// returns regex for a latitude in the specified format.
        /// </summary>
        public static Regex LonRegexFor(LLFormat fmt) => _regexFormatLon[fmt];

        /// <summary>
        /// converts a DD format (decimal, no units) latitude into a string representation in a destination format.
        /// returns "" on error.
        /// </summary>
        public static string ConvertFromLatDD(string latDD, LLFormat dstFmt)
            => dstFmt switch
            {
                LLFormat.DD => latDD,
                LLFormat.DDU => $"{latDD}°",
                LLFormat.DMS => CoreDDtoDMS(latDD, 90.0, "N", "S"),
                LLFormat.DMDS_P2ZF => CoreDDtoDMS(latDD, 90.0, "N", "S", 2),
                LLFormat.DDM_P3ZF => CoreDDtoDDM(latDD, 90.0, "N", "S", 3, true),
                LLFormat.DDM_P2ZF => CoreDDtoDDM(latDD, 90.0, "N", "S", 2, true),
                LLFormat.DDM_P1ZF => CoreDDtoDDM(latDD, 90.0, "N", "S", 1, true),
                LLFormat.DDM_P2 => CoreDDtoDDM(latDD, 90.0, "N", "S", 2, false),
                LLFormat.DDM_P1 => CoreDDtoDDM(latDD, 90.0, "N", "S", 1, false),
                _ => "",
            };

        /// <summary>
        /// converts a DD format (decimal, no units) longitude into a string representation in a destination format.
        /// returns "" on error.
        /// </summary>
        public static string ConvertFromLonDD(string lonDD, LLFormat dstFmt)
            => dstFmt switch
            {
                LLFormat.DD => lonDD,
                LLFormat.DDU => $"{lonDD}°",
                LLFormat.DMS => CoreDDtoDMS(lonDD, 180.0, "E", "W"),
                LLFormat.DMDS_P2ZF => CoreDDtoDMS(lonDD, 180.0, "E", "W", 2, true),
                LLFormat.DDM_P3ZF => CoreDDtoDDM(lonDD, 180.0, "E", "W", 3, true),
                LLFormat.DDM_P2ZF => CoreDDtoDDM(lonDD, 180.0, "E", "W", 2, true),
                LLFormat.DDM_P1ZF => CoreDDtoDDM(lonDD, 180.0, "E", "W", 1, true),
                LLFormat.DDM_P2 => CoreDDtoDDM(lonDD, 180.0, "E", "W", 2, false),
                LLFormat.DDM_P1 => CoreDDtoDDM(lonDD, 180.0, "E", "W", 1, false),
                _ => "",
            };

        /// <summary>
        /// converts a latitude in a given format into a string representation in a DD format (decimal, no units).
        /// returns "" on error.
        /// </summary>
        public static string ConvertToLatDD(string latFmt, LLFormat fmt)
            => fmt switch
            {
                LLFormat.DD => latFmt,
                LLFormat.DDU => latFmt.Replace("°", ""),
                LLFormat.DMS => CoreDMStoDD(latFmt, _regexFormatLat[fmt], "N"),
                LLFormat.DMDS_P2ZF => CoreDMStoDD(latFmt, _regexFormatLat[fmt], "N"),
                LLFormat.DDM_P3ZF => CoreDDMtoDD(latFmt, _regexFormatLat[fmt], "N"),
                LLFormat.DDM_P2ZF => CoreDDMtoDD(latFmt, _regexFormatLat[fmt], "N"),
                LLFormat.DDM_P1ZF => CoreDDMtoDD(latFmt, _regexFormatLat[fmt], "N"),
                LLFormat.DDM_P2 => CoreDDMtoDD(latFmt, _regexFormatLat[fmt], "N"),
                LLFormat.DDM_P1 => CoreDDMtoDD(latFmt, _regexFormatLat[fmt], "N"),
                _ => "",
            };

        /// <summary>
        /// converts a longitude in a given format into a string representation in a DD format (decimal, no units).
        /// returns "" on error.
        /// </summary>
        public static string ConvertToLonDD(string lonFmt, LLFormat fmt)
            => fmt switch
            {
                LLFormat.DD => lonFmt,
                LLFormat.DDU => lonFmt.Replace("°", ""),
                LLFormat.DMS => CoreDMStoDD(lonFmt, _regexFormatLon[fmt], "E"),
                LLFormat.DMDS_P2ZF => CoreDMStoDD(lonFmt, _regexFormatLon[fmt], "E"),
                LLFormat.DDM_P3ZF => CoreDDMtoDD(lonFmt, _regexFormatLon[fmt], "E"),
                LLFormat.DDM_P2ZF => CoreDDMtoDD(lonFmt, _regexFormatLon[fmt], "E"),
                LLFormat.DDM_P1ZF => CoreDDMtoDD(lonFmt, _regexFormatLon[fmt], "E"),
                LLFormat.DDM_P2 => CoreDDMtoDD(lonFmt, _regexFormatLon[fmt], "E"),
                LLFormat.DDM_P1 => CoreDDMtoDD(lonFmt, _regexFormatLon[fmt], "E"),
                _ => "",
            };

        /// <summary>
        /// returns a string with any zero-fill to the degrees field removed. assumes the coordinate string is
        /// not DD formatted (as DD formats should not have zero-fill, generally).
        /// </summary>
        public static string RemoveLLDegZeroFill(string coord) => _regexRemoveDegZeroPad.Replace(coord, "$1$3");

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

        private static string CoreDDtoDMS(string ddValue, double max, string posDir, string negDir, int precision = 0, bool fillDeg = true)
        {
            string dms = "";
            if (double.TryParse(ddValue, out double dd) && (Math.Abs(dd) < max))
            {
                string dir = (dd >= 0.0) ? posDir : negDir;
                dd = Math.Abs(dd);
                int d = (int)Math.Truncate(dd);
                double dm = (dd - (double)d) * 60.0;
                int m = (int)Math.Truncate(dm);

                string dOut = $"{d}";
                if (fillDeg)
                {
                    dOut = (max > 100.0) ? $"{d,3:D3}" : $"{d,2:D2}";
                }

                if (precision == 0)
                {
                    int s = (int)((dm - (double)m) * 60.0);
                    dms = $"{dir} {dOut}° {m,2:D2}’ {s,2:D2}’’";
                }
                else
                {
                    double ds = (dm - (double)m) * 60.0;
                    string pad = (ds < 10.0) ? "0" : "";
                    string dsOut = string.Format(string.Format("{{0}}{{1:F{0}}}", precision), pad, ds);
                    dms = $"{dir} {dOut}° {m,2:D2}’ {dsOut}’’";
                }
            }
            return dms;
        }

        private static string CoreDMStoDD(string ddmValue, Regex regex, string posDir)
        {
            if (regex.IsMatch(ddmValue))
            {
                string[] parts = ddmValue.Replace("°", "").Replace("’", "").Split(' ');
                if ((parts.Length == 4) && int.TryParse(parts[1], out int d) &&
                                           int.TryParse(parts[2], out int m) &&
                                           double.TryParse(parts[3], out double s))
                {
                    double dd = (d + (m / 60.0) + (s / 3600.0)) * ((parts[0] == posDir) ? 1.0 : -1.0);
                    return $"{dd:F8}";
                }
            }
            return "";
        }
    }
}
