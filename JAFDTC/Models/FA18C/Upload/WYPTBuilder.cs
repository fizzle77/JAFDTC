// ********************************************************************************************************************
//
// WYPTBuilder.cs -- fa-18c waypoint command builder
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

using JAFDTC.Models.DCS;
using JAFDTC.Models.FA18C.WYPT;
using System.Text;
using JAFDTC.Utilities;

namespace JAFDTC.Models.FA18C.Upload
{
    /// <summary>
    /// TODO: document
    /// </summary>
    internal class WYPTBuilder : FA18CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public WYPTBuilder(FA18CConfiguration cfg, FA18CCommands dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure steerpoint system via the ufc/rmfd according to the non-default programming settings (this
        /// function is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            Device ufc = _aircraft.GetDevice("UFC");
            Device rmfd = _aircraft.GetDevice("RMFD");

            if (!_cfg.WYPT.IsDefault)
            {
                AppendCommand(rmfd.GetCommand("OSB-18"));    // MENU
                AppendCommand(rmfd.GetCommand("OSB-18"));    // MENU
                AppendCommand(rmfd.GetCommand("OSB-02"));    // HSI

                AppendCommand(rmfd.GetCommand("OSB-10"));    // DATA
                AppendCommand(rmfd.GetCommand("OSB-07"));    // WYPT
                AppendCommand(rmfd.GetCommand("OSB-05"));    // UFC

                SelectWp0(rmfd, 0);
                for (int i = 0; i < _cfg.WYPT.Points[0].Number; i++)
                {
                    AppendCommand(rmfd.GetCommand("OSB-12"));
                }

                for (int i = 0; i < _cfg.WYPT.Points.Count; i++)
                {
                    WaypointInfo wypt = _cfg.WYPT.Points[i];

                    if (wypt.IsValid)
                    {
                        // NOTE: coords are zero-filled in the ui, back that out here.

                        AppendCommand(ufc.GetCommand("Opt1"));
                        AppendCommand(Wait());
                        AppendCommand(Build2864Coordinate(ufc, Coord.RemoveLLDegZeroFill(wypt.LatUI)));    // DDM
                        AppendCommand(ufc.GetCommand("ENT"));
                        AppendCommand(WaitLong());

                        AppendCommand(Build2864Coordinate(ufc, Coord.RemoveLLDegZeroFill(wypt.LonUI)));    // DDM
                        AppendCommand(ufc.GetCommand("ENT"));
                        AppendCommand(WaitLong());

                        AppendCommand(ufc.GetCommand("Opt3"));
                        AppendCommand(ufc.GetCommand("Opt1"));
                        AppendCommand(BuildDigits(ufc, wypt.Alt));
                        AppendCommand(ufc.GetCommand("ENT"));
                        AppendCommand(Wait());
                    }
                    AppendCommand(rmfd.GetCommand("OSB-12"));   // Next Waypoint
                }

                for (var i = 0; i < _cfg.WYPT.Points.Count; i++)
                {
                    AppendCommand(rmfd.GetCommand("OSB-13"));   // Prev Waypoint
                }

                AppendCommand(rmfd.GetCommand("OSB-18"));
                AppendCommand(rmfd.GetCommand("OSB-18"));
                AppendCommand(rmfd.GetCommand("OSB-15"));
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void SelectWp0(Device rmfd, int i)
        {
            if (i < 140) // It might not notice on the first pass, so we go around once more
            {
                AppendCommand(StartCondition("NotAtWp0"));
                AppendCommand(rmfd.GetCommand("OSB-13"));
                AppendCommand(EndCondition("NotAtWp0"));
                SelectWp0(rmfd, i + 1);
            }
        }
    }
}
