// ********************************************************************************************************************
//
// AV8BUploadAgent.cs -- av-8b upload agent
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

using JAFDTC.Models.AV8B.Upload;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.AV8B
{
    /// <summary>
    /// upload agent responsible for building a stream of commands for use by dcs to set up harrier avionics according
    /// to an AV8BConfiguration configuration.
    /// </summary>
    internal class AV8BUploadAgent : UploadAgentBase, IUploadAgent
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly AV8BConfiguration _cfg;
        private readonly AV8BDeviceManager _dcsCmds;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public AV8BUploadAgent(AV8BConfiguration cfg) => (_cfg, _dcsCmds) = (cfg, new AV8BDeviceManager());

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// build the dcs commands necessary to configure the systems as the configuration associated with this
        /// instance describes.
        /// </summary>
        public override void BuildSystems(StringBuilder sb)
        {
            new WYPTBuilder(_cfg, _dcsCmds, sb).Build();
        }
    }
}
