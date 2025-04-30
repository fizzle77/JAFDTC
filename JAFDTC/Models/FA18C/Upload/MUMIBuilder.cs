// ********************************************************************************************************************
//
// DTEBuilder.cs -- f-16c dte command builder
//
// Copyright(C) 2025 ilominar/raven
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
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.FA18C.Upload
{
    /// <summary>
    /// command builder for the mumi system in the hornet. translates mumi (dtc) setup in FA18CConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class MUMIBuilder : FA18CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MUMIBuilder(FA18CConfiguration cfg, FA18CDeviceManager dm, StringBuilder sb) : base(cfg, dm, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure mumi system via the ddi according to the non-default programming settings (this function is
        /// safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.MUMI.IsDefault)
                return;

            AddExecFunction("NOP", new() { "==== MUMIBuilder:Build()" });

            if (!_cfg.MUMI.EnableLoadValue)
                return;

            AirframeDevice rddi = _aircraft.GetDevice("RDDI");

            SwitchDDIToPage(rddi, "IsRDDISUPT", "OSB-10");                                  // MUMI page

            AddAction(rddi, "OSB-13");                                                      // COMM
            AddWhileBlock("IsRDDMUMILoadedCOMM", false, null, delegate ()
            {
                AddWait(500);
            }, 4);

            AddAction(rddi, "OSB-16");                                                      // ALR-67
            AddWhileBlock("IsRDDMUMILoadedALR", false, null, delegate ()
            {
                AddWait(500);
            }, 4);

            AddWait(WAIT_LONG);
        }
    }
}
