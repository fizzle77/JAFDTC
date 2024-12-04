// ********************************************************************************************************************
//
// HARMBuilder.cs -- f-16c harm alic command builder
//
// Copyright(C) 2021-2023 the-paid-actor & others
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
using JAFDTC.Models.F16C.HARM;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// builder to generate the command stream to configure the harm alic system through the ded/ufc according to an
    /// F16CConfiguration. the stream returns the ded to its default page. the builder does not require any state to
    /// function.
    /// </summary>
    internal class HARMBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public HARMBuilder(F16CConfiguration cfg, F16CDeviceManager dm, StringBuilder sb) : base(cfg, dm, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure harm alic system via the ded/ufc according to the non-default programming settings (this function
        /// is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.HARM.IsDefault)
                return;

            AddExecFunction("NOP", new() { "==== HARMBuilder:Build()" });

            AirframeDevice ufc = _aircraft.GetDevice("UFC");

            // NOTE: alic is only shown on ded list in a-g master mode

            SwitchMasterModeAG(ufc, true);                      // nav to a-g
            SelectDEDPage(ufc, "0");
            AddIfBlock("IsHARMOnDED", true, null, delegate ()
            {
                AddAction(ufc, "0", WAIT_BASE);
                foreach (ALICTable table in _cfg.HARM.Tables)
                {
                    if (!table.IsDefault)
                        for (int i = 0; i < table.Table.Count; i++)
                            AddActions(ufc, PredActionsForNumAndEnter(table.Table[i].Code), new() { "DOWN" });
                    AddAction(ufc, "INC", WAIT_BASE);
                }
            });
            SwitchMasterModeAG(ufc, false);                     // a-g to nav, ded to default
        }
    }
}