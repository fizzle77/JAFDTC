// ********************************************************************************************************************
//
// M2000CUploadAgent.cs -- m-2000c upload agent
//
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

using JAFDTC.Models.M2000C.Upload;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.M2000C
{
    /// <summary>
    /// upload agent responsible for building a stream of commands for use by dcs to set up warthog avionics according
    /// to a configuration.
    /// </summary>
    internal class M2000CUploadAgent : UploadAgentBase, IUploadAgent
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly M2000CConfiguration _cfg;
        private readonly M2000CCommands _dcsCmds;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public M2000CUploadAgent(M2000CConfiguration cfg) => (_cfg, _dcsCmds) = (cfg, new M2000CCommands());

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void BuildSystems(StringBuilder sb)
        {
            new WYPTBuilder(_cfg, _dcsCmds, sb).Build();
        }
    }
}
