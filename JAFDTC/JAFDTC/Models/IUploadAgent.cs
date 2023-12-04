// ********************************************************************************************************************
//
// IUploadAgent.cs -- interface for airframe upload agent class
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

namespace JAFDTC.Models
{
    /// <summary>
    /// interface for classes that serve as an upload agent that generates a command stream for dcs to setup a
    /// configuration in the jet.
    /// </summary>
    public interface IUploadAgent
    {
        /// <summary>
        /// create the set of commands and state necessary to load a configuration on the jet, then send the
        /// commands to the jet for processing. returns true on success, false on failure.
        /// </summary>
        public bool Load();
    }
}
