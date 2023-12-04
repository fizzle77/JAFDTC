// ********************************************************************************************************************
//
// MFDConfiguration.cs -- f-16c mfd configuration for a particular master mode
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

namespace JAFDTC.Models.F16C.MFD
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class MFDModeConfiguration : BindableObject
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events.

        public MFDConfiguration LeftMFD { get; set; }
        public MFDConfiguration RightMFD { get; set; }

        // ---- following properties are synthesized.

        // returns true if the instance indicates a default setup (all fields are ""), false otherwise.
        //
        [JsonIgnore]
        public bool IsDefault
        {
            get => (LeftMFD.IsDefault && RightMFD.IsDefault);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MFDModeConfiguration()
        {
            LeftMFD = new MFDConfiguration();
            RightMFD = new MFDConfiguration();
        }

        public MFDModeConfiguration(MFDModeConfiguration other)
        {
            LeftMFD = new(other.LeftMFD);
            RightMFD = new(other.RightMFD);
        }

        public MFDModeConfiguration(MFDConfiguration leftMFD, MFDConfiguration rightMFD)
        {
            LeftMFD = leftMFD;
            RightMFD = rightMFD;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // reset the instance to defaults (by definition, field value of "" implies default).
        //
        public void Reset()
        {
            LeftMFD.Reset();
            RightMFD.Reset();
        }

        public void CleanUp()
        {
            LeftMFD.CleanUp();
            RightMFD.CleanUp();
        }
    }
}
