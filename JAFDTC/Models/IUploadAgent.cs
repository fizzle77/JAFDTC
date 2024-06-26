﻿// ********************************************************************************************************************
//
// IUploadAgent.cs -- interface for airframe upload agent class
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

using JAFDTC.Models.DCS;
using System.Text;
using System.Threading.Tasks;

namespace JAFDTC.Models
{
    /// <summary>
    /// interface for classes that serve as an upload agent that generates an airframe-specific command stream for
    /// dcs to setup a configuration in the jet. instances of this class are specialized to an airframe.
    /// </summary>
    public interface IUploadAgent
    {
        /// <summary>
        /// create the stream of commands and state necessary to load a configuration on the jet, then send the
        /// commands to the jet for processing via the network connection to the dcs scripting engine. Load()
        /// uses SetupBuilder(), BuildSystems(), and TeardownBuilder() to create the command streams for the systems
        /// in the airframe. returns true on success, false on failure.
        /// </summary>
        public Task<bool> Load();

        /// <summary>
        /// create the stream of commands and state necessary to load a configuration on the jet using the given
        /// StringBuilder. Load() will add the command streams from SetupBuilder() and TeardownBuilder() to wrap
        /// the stream built by this method. implementations should clear the StringBuilder to indicate an error.
        /// 
        /// this function may use QueryBuilderBase derivatives along with UploadAgentBase:Query() to query dcs
        /// state in order to shape the command stream.
        ///
        /// this function will typically create multiple IBuilder instances for various systems and use them to
        /// assemble the command stream.
        /// </summary>
        public void BuildSystems(StringBuilder sb);

        /// <summary>
        /// returns an object that implements IBuilder to generate commands that appear at the start of a command
        /// stream. by default, the returned IBuilder generates a stream with a "start of upload" marker command.
        /// </summary>
        public IBuilder SetupBuilder(StringBuilder sb);

        /// <summary>
        /// returns an object that implements IBuilder to generate commands that appear at the end of a command
        /// stream. by default, the returned IBuilder generates a stream with an "end of upload" marker command.
        /// </summary>
        public IBuilder TeardownBuilder(StringBuilder sb);
    }
}
