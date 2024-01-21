// ********************************************************************************************************************
//
// A10CUploadAgent.cs -- a-10c upload agent
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

using JAFDTC.Models.A10C.Upload;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.A10C
{
    /// <summary>
    /// upload agent responsible for building a stream of commands for use by dcs to set up warthog avionics according
    /// to a configuration.
    /// </summary>
    internal class A10CUploadAgent : UploadAgentBase, IUploadAgent
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly A10CConfiguration _cfg;
        private readonly A10CDeviceManager _dcsCmds;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public A10CUploadAgent(A10CConfiguration cfg) => (_cfg, _dcsCmds) = (cfg, new A10CDeviceManager());

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
