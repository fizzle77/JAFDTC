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
using System.Collections.Generic;

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
        /// configure steerpoint system via the ufc/rddi according to the non-default programming settings (this
        /// function is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.WYPT.IsDefault)
                return;

            AddExecFunction("NOP", new() { "==== RadioBuilder:Build()" });

            AirframeDevice ufc = _aircraft.GetDevice("UFC");
            AirframeDevice rddi = _aircraft.GetDevice("RDDI");

            AddWhileBlock("IsRDDISUPT", false, null, delegate()
            {
                AddAction(rddi, "OSB-18");                                                  // MENU (SUPT)
            });   
            AddActions(rddi, new() { "OSB-02", "OSB-10", "OSB-07", "OSB-05" });             // HSI, DATA, WYPT, UFC

            AddWhileBlock("IsAtWYPTn", false, new() { $"{_cfg.WYPT.Points[0].Number - 1}" }, delegate()
            {
                AddAction(rddi, "OSB-12", WAIT_BASE);                                       // WYPT ++
            }, 150);
            for (int i = 0; i < _cfg.WYPT.Points.Count; i++)
            {
                AddAction(rddi, "OSB-12", WAIT_BASE);                                       // WYPT ++

                WaypointInfo wypt = _cfg.WYPT.Points[i];
                if (wypt.IsValid)
                {
                    // NOTE: coords are zero-filled in the ui, back that out here.

                    AddAction(ufc, "Opt1", WAIT_BASE);                                      // POSN

                    AddActions(ufc, ActionsFor2864CoordinateString(Coord.RemoveLLDegZeroFill(wypt.LatUI)),  // DDM
                                new() { "ENT" }, WAIT_LONG);
                    AddActions(ufc, ActionsFor2864CoordinateString(Coord.RemoveLLDegZeroFill(wypt.LonUI)),  // DDM
                                new() { "ENT" }, WAIT_LONG);

                    AddAction(ufc, "Opt3", WAIT_BASE);                                      // ALT
                    AddAction(ufc, "Opt1", WAIT_BASE);                                      // FEET
                    AddActions(ufc, ActionsForString(wypt.Alt), new() { "ENT" }, WAIT_BASE);
                }
            }
            AddWhileBlock("IsAtWYPTn", false, new() { $"{_cfg.WYPT.Points[0].Number}" }, delegate ()
            {
                AddAction(rddi, "OSB-13", WAIT_BASE);                                       // WYPT --
            }, 150);

            AddWhileBlock("IsRDDISUPT", false, null, delegate()
            {
                AddAction(rddi, "OSB-18");                                                  // MENU (SUPT)
            });
            AddAction(rddi, "OSB-15");                                                      // FCS
        }
    }
}
