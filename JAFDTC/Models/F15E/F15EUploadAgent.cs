// ********************************************************************************************************************
//
// F15EUploadAgent.cs -- f-15e upload agent
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

using JAFDTC.Models.F15E.Upload;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F15E
{
    /// <summary>
    /// upload agent responsible for building a stream of commands for use by dcs to set up mudhen avionics according
    /// to a configuration.
    /// </summary>
    public class F15EUploadAgent : UploadAgentBase, IUploadAgent
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly F15EConfiguration _cfg;
        private readonly F15ECommands _dcsCmds;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F15EUploadAgent(F15EConfiguration cfg) => (_cfg, _dcsCmds) = (cfg, new F15ECommands());

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void BuildSystems(StringBuilder sb)
        {
            new RadioBuilder(_cfg, _dcsCmds, sb).Build();
            new MiscBuilder(_cfg, _dcsCmds, sb).Build();
        }
    }
}
