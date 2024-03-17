// ********************************************************************************************************************
//
// MPDSystem.cs -- f-15e mpd/mpcd system
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

using JAFDTC.Models.F15E.Misc;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JAFDTC.Models.F15E.MPD
{
    public class MPDSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:F15E:MPD";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties, computed

        /// <summary>
        /// returns a MPDSystem with the fields populated with the actual default values (note that usually the value
        /// "" implies default).
        ///
        /// defaults are as of DCS v2.9.0.47168.
        /// </summary>
        [JsonIgnore]
        public readonly static MPDSystem ExplicitDefaults = new()
        {
        };

        /// <summary>
        /// returns true if the instance indicates a default setup (all fields are "") or the object is in explicit
        /// form, false otherwise.
        /// </summary>
        [JsonIgnore]
        public bool IsDefault => true;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MPDSystem()
        {
            Reset();
        }

        public MPDSystem(MPDSystem other)
        {
        }

        public virtual object Clone() => new MPDSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the instance to defaults (by definition, field value of "" implies default).
        /// </summary>
        public void Reset()
        {
        }
    }
}
