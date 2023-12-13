// ********************************************************************************************************************
//
// WYPTBuilder.cs -- av-8b waypoint command builder
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

using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.AV8B.Upload
{
    /// <summary>
    /// command builder for the waypoint system in the harrier. translates cmds setup in F16CConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class WYPTBuilder : AV8BBuilderBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public WYPTBuilder(AV8BConfiguration cfg, AV8BCommands av8b, StringBuilder sb) : base(cfg, av8b, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        public override void Build()
        {
            // TODO: implement
        }
    }
}
