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

        public WYPTBuilder(FA18CConfiguration cfg, FA18CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

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
            AirframeDevice ufc = _aircraft.GetDevice("UFC");
            AirframeDevice rmfd = _aircraft.GetDevice("RMFD");

            if (!_cfg.WYPT.IsDefault)
            {
                AddActions(rmfd, new() { "OSB-18", "OSB-18", "OSB-02" }); // MENU, MENU, HSI
                AddActions(rmfd, new() { "OSB-10", "OSB-07", "OSB-05" }); // DATA, WYPT, UFC

                SelectWp0(rmfd, 0);
                for (int i = 0; i < _cfg.WYPT.Points[0].Number; i++)
                {
                    AddAction(rmfd, "OSB-12");
                }

                for (int i = 0; i < _cfg.WYPT.Points.Count; i++)
                {
                    WaypointInfo wypt = _cfg.WYPT.Points[i];

                    if (wypt.IsValid)
                    {
                        // NOTE: coords are zero-filled in the ui, back that out here.

                        AddAction(ufc, "Opt1");
                        AddWait(WAIT_BASE);

                        AddActions(ufc, ActionsFor2864CoordinateString(Coord.RemoveLLDegZeroFill(wypt.LatUI)),  // DDM
                                   new() { "ENT" });
                        AddWait(WAIT_LONG);
                        AddActions(ufc, ActionsFor2864CoordinateString(Coord.RemoveLLDegZeroFill(wypt.LonUI)),  // DDM
                                   new() { "ENT" });
                        AddWait(WAIT_LONG);

                        AddActions(ufc, new() { "Opt3", "Opt1" });
                        AddActions(ufc, ActionsForString(wypt.Alt), new() { "ENT" });
                        AddWait(WAIT_BASE);
                    }
                    AddAction(rmfd, "OSB-12");   // Next Waypoint
                }

                for (var i = 0; i < _cfg.WYPT.Points.Count; i++)
                {
                    AddAction(rmfd, "OSB-13");   // Prev Waypoint
                }

                AddActions(rmfd, new() { "OSB-18", "OSB-18", "OSB-15" });
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void SelectWp0(AirframeDevice rmfd, int i)
        {
            if (i < 140) // It might not notice on the first pass, so we go around once more
            {
                AddIfBlock("NotAtWp0", null, delegate () { AddAction(rmfd, "OSB-13"); });
                SelectWp0(rmfd, i + 1);
            }
        }
    }
}
