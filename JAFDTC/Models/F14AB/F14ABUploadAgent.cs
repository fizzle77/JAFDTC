// ********************************************************************************************************************
//
// F14ABUploadAgent.cs -- f-14a/b upload agent
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

using JAFDTC.Models.F14AB.Upload;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F14AB
{
    /// <summary>
    /// upload agent responsible for building a stream of commands for use by dcs to set up warthog avionics according
    /// to a configuration.
    /// </summary>
    internal class F14ABUploadAgent : UploadAgentBase, IUploadAgent
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly F14ABConfiguration _cfg;
        private readonly F14ABCommands _dcsCmds;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F14ABUploadAgent(F14ABConfiguration cfg) => (_cfg, _dcsCmds) = (cfg, new F14ABCommands());

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
