// ********************************************************************************************************************
//
// IRadioPresetInfo.cs -- interface for a radio preset
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

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// interface for a basic radio preset that consists of a preset number, description, frequency, and modulation.
    /// </summary>
    public interface IRadioPresetInfo
    {
        /// <summary>
        /// preset number
        /// </summary>
        public int Preset { get; set; }

        /// <summary>
        /// preset frequency
        /// </summary>
        public string Frequency { get; set; }

        /// <summary>
        /// preset modulation, if programmable
        /// </summary>
        public string Modulation { get; set; }

        /// <summary>
        /// preset description or name
        /// </summary>
        public string Description { get; set; }
    }
}
