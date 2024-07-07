// ********************************************************************************************************************
//
// PPSystem.cs -- fa-18c pre-planned system configuration
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

using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.FA18C.PP
{
    /// <summary>
    /// enum encoding weapons that support pre-planned programming.
    /// </summary>
    public enum Weapons
    {
        NONE = 0,
        GBU_38 = 1,
        GBU_32 = 2,
        GBU_31_V12 = 3,
        GBU_31_V34 = 4,
        JSOW_A = 5,
        JSOW_C = 6,
        SLAM = 7,
        SLAM_ER = 8
    };

    /// <summary>
    /// pre-planned system for the hornet. this system carries the weapon programs for smart weapons that can be
    /// carried on stations 2, 3, 7, and 8.
    /// </summary>
    public class PPSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:FA18C:PP";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change or validation events.

        public Dictionary<int, PPStation> Stations { get; set; }

        // ---- following properties are synthesized.

        /// <summary>
        /// returns true if the instance indicates a default setup, false otherwise.
        /// </summary>
        [JsonIgnore]
        public bool IsDefault
        {
            get
            {
                foreach (PPStation station in Stations.Values)
                    if (!station.IsDefault)
                        return false;
                return true;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public PPSystem()
        {
            Stations = new Dictionary<int, PPStation>
            {
                { 2, new PPStation(2) },
                { 3, new PPStation(3) },
                { 7, new PPStation(7) },
                { 8, new PPStation(8) }
            };
        }

        public PPSystem(PPSystem other)
        {
            Stations = new Dictionary<int, PPStation>
            {
                { 2, new PPStation(other.Stations[2]) },
                { 3, new PPStation(other.Stations[3]) },
                { 7, new PPStation(other.Stations[7]) },
                { 8, new PPStation(other.Stations[8]) }
            };
        }

        public virtual object Clone() => new PPSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the instance to defaults.
        /// </summary>
        public void Reset()
        {
            foreach (PPStation station in Stations.Values)
                station.Reset();
        }
    }
}
