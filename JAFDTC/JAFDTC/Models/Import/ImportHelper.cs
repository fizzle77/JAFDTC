// ********************************************************************************************************************
//
// ImportHelper.cs -- base class for helpers to import steerpoints
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

using System.Collections.Generic;
using System.Diagnostics;

namespace JAFDTC.Models.Import
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public abstract class ImportHelper : IImportHelper
    {
        public virtual List<string> Flights() { return null; }
        public virtual List<Dictionary<string, string>> Waypoints(string flightName) { return null; }

        /// <summary>
        /// convert a decimal degrees string to degrees, decimal minutes format: latitude, "[N|S] 00° 00.000’",
        /// longitude: "[E|W] 000° 00.000’"
        /// </summary>
        public static string ConvertDDtoDDM(string value, bool isLat)
        {
            string[] parts = value.Split(".");

            int degrees = int.Parse(parts[0]);
            bool isNegative = false;
            if (degrees < 0)
            {
                degrees = -degrees;
                isNegative = true;
            }
            if ((isLat && (degrees > 90)) || (!isLat && (degrees > 180)))
            {
                return null;
            }

            double decimalDegrees = double.Parse("0." + parts[1]);
            double decimalMinutes = decimalDegrees * 60.0;

            string lead = (isLat) ? ((isNegative ? "S " : "N ") + degrees.ToString("00"))
                                  : ((isNegative ? "W " : "E ") + degrees.ToString("000"));
            return lead + "° " + decimalMinutes.ToString("00.000") + "’";
        }
    }
}
