// ********************************************************************************************************************
//
// INavpointInfo.cs -- interface for a navigation point
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

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// interface for a basic navigation point that consists of a number, name, latitude, longitude, and
    /// altitude. lat/lon are always in decimal degrees format while latUI/lonUI are suitable for display
    /// in the user interface (typically, this matches the avionics format: DMS, DDM, and so on).
    /// </summary>
    public interface INavpointInfo
    {
        /// <summary>
        /// navpoint number
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// navpoint name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// navpoint latitude in decimal degrees
        /// </summary>
        public string Lat { get; set; }

        /// <summary>
        /// navpoint longitude in decimal degrees
        /// </summary>
        public string Lon { get; set; }

        /// <summary>
        /// navpoint latitude in format appropriate for interface or avionics
        /// </summary>
        public string LatUI { get; set; }

        /// <summary>
        /// navpoint longitude in format appropriate for interface or avionics
        /// </summary>
        public string LonUI { get; set; }

        /// <summary>
        /// navpoint altitude in feet
        /// </summary>
        public string Alt { get; set; }
    }
}
