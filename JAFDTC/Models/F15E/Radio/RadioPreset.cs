// ********************************************************************************************************************
//
// RadioPreset.cs -- f-15e radio system preset
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

using JAFDTC.Models.Base;
using System.Diagnostics;

namespace JAFDTC.Models.F15E.Radio
{
    /// <summary>
    /// radio preset for the mudhen radio system. use the abstract RadioPresetBase as-is since we don't need any
    /// more than that.
    /// </summary>
    public class RadioPreset : RadioPresetInfoBase
    {
        // preset values for the mudhen G/GV presets
        //
        public const int PRESET_G = -1;
        public const int PRESET_GV = -2;
    }
}
