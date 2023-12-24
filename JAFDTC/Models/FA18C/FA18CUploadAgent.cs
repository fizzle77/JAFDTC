// ********************************************************************************************************************
//
// FA18CUploadAgent.cs -- fa-18c upload agent
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

using JAFDTC.Models.FA18C.CMS;
using JAFDTC.Models.FA18C.Upload;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.FA18C
{
    /// <summary>
    /// upload agent responsible for building a stream of commands for use by dcs to set up hornet avionics according
    /// to a configuration.
    /// </summary>
    public class FA18CUploadAgent : UploadAgentBase, IUploadAgent
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly FA18CConfiguration _cfg;
        private readonly FA18CCommands _dcsCmds;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public FA18CUploadAgent(FA18CConfiguration cfg) => (_cfg, _dcsCmds) = (cfg, new FA18CCommands());

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void BuildSystems(StringBuilder sb)
        {
            new RadioBuilder(_cfg, _dcsCmds, sb).Build();
            new CMSBuilder(_cfg, _dcsCmds, sb).Build();
            new WYPTBuilder(_cfg, _dcsCmds, sb).Build();
        }
    }
}
