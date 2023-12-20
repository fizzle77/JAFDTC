// ********************************************************************************************************************
//
// WYPTBuilder.cs -- m-2000c waypoint command builder
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

using JAFDTC.Models.DCS;
using JAFDTC.Models.M2000C.WYPT;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.M2000C.Upload
{
    /// <summary>
    /// TODO: document
    /// </summary>
    internal class WYPTBuilder : M2000CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public WYPTBuilder(M2000CConfiguration cfg, M2000CCommands dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure waypoint system via the pcn according to the non-default programming settings (this function is
        /// safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// </summary>
        public override void Build()
        {
            ObservableCollection<WaypointInfo> wypts = _cfg.WYPT.Points;
            Device pcn = _aircraft.GetDevice("PCN");

            if (wypts.Count > 0)
            {
                // NOTE: should be at most 10 steerpoints, 1-10.

                for (var i = 0; i < wypts.Count; i++)
                {
                    if (wypts[i].IsValid)
                    {
                        // TODO: Set UNI Parameter Selector Switch to L/G

                        AppendCommand(pcn.GetCommand("INS_PREP_SW"));
                        AppendCommand(pcn.GetCommand("0"));
                        AppendCommand(pcn.GetCommand($"{wypts[i].Number - 1}"));

                        AppendCommand(pcn.GetCommand("1"));
                        AppendCommand(Build2864Coordinate(pcn, wypts[i].LatUI));
                        AppendCommand(pcn.GetCommand("INS_ENTER_BTN"));

                        AppendCommand(pcn.GetCommand("3"));
                        AppendCommand(Build2864Coordinate(pcn, wypts[i].LonUI));
                        AppendCommand(pcn.GetCommand("INS_ENTER_BTN"));

                        // TODO: Set UNI Parameter Selector Switch to ALT
                        // TODO: 1 to set feet, 3 to set m
                        // TODO: 1 to set +ive, 7 to set -ive
                        // TODO: enter elevation
                        // TODO: INS_ENTER_BTN
                    }
                }
            }
        }
    }
}
