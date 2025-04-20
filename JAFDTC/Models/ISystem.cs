// ********************************************************************************************************************
//
// ISystem.cs -- interface for airframe system configuration class
//
// Copyright(C) 2023-2025 ilominar/raven
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

using System.Text.Json.Nodes;

namespace JAFDTC.Models
{
    /// <summary>
    /// interface for classes that hold configuration information for a particular avionics system in the jet.
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// returns true if the system is in a default setup (i.e., state is unchanged from avionics defaults,
        /// false otherwise.
        /// </summary>
        public bool IsDefault { get; }

        /// <summary>
        /// merge the system configuration into a dcs dtc configuration. the dcs dtc configuration is presented
        /// as a JsonObject for the root of the "data" object in the dtc file that encodes configuration data.
        /// this method will update the JsonObject and/or its children as necessary to complete the merge.
        /// </summary>
        public void MergeIntoSimDTC(JsonNode dataRoot);

        /// <summary>
        /// reset the setup of the system to avionics defaults.
        /// </summary>
        public void Reset();
    }
}
