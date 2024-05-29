// ********************************************************************************************************************
//
// MunitionSettings.cs -- munition settings for f-16c sms system
//
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JAFDTC.Models.F16C.SMS
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class MunitionSettings : BindableObject
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // Properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties post change and validation events

        public string Profile { get; set; }

        public string EmplMode { get; set; }

        public string Ripple { get; set; }

        public string RipplePulse { get; set; }
        
        public string RippleSpacing { get; set; }

        public string Fuse { get; set; }

        public string ArmDelay { get; set; }

        public string ArmDelay2 { get; set; }

        public string BurstAlt { get; set; }

        public string RelaseAng { get; set; }

        public string LADDPR { get; set; }

        public string LADDToF { get; set; }

        public string LADDMRA { get; set; }

        // ---- following properties are synthesized

        /// <summary>
        /// returns true if the instance indicates a default setup: either Settings is empty or it contains only
        /// default setups.
        /// </summary>
        [JsonIgnore]
        public bool IsDefault => true;

        // ------------------------------------------------------------------------------------------------------------
        //
        // Construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MunitionSettings()
        {
            Reset();
        }

        public MunitionSettings(MunitionSettings other)
        {
            // TODO
        }

        public virtual object Clone() => new MunitionSettings(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the instance to defaults.
        /// </summary>
        public void Reset()
        {
            // TODO
        }
    }
}
