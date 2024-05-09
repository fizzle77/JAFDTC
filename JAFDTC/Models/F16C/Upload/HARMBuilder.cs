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
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// command builder for the harm alic system in the viper. translates harm alic setup in F16CConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class HARMBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public HARMBuilder(F16CConfiguration cfg, F16CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure harm alic system via the icp/ded according to the non-default programming settings (this function
        /// is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            AirframeDevice ufc = _aircraft.GetDevice("UFC");

            if (!_cfg.HARM.IsDefault)
            {
                AddActions(ufc, new() { "RTN", "RTN", "LIST", "8" }, null, WAIT_BASE);
                AddIfBlock("IsInAAMode", true, null, delegate ()
                {
                    AddAction(ufc, "SEQ");
                    AddIfBlock("IsInAGMode", true, null, delegate () { BuildHARM(ufc); });
                    AddActions(ufc, new() { "RTN", "RTN", "LIST", "8", "SEQ" });
                });
                AddAction(ufc, "RTN");
            }
        }

        /// <summary>
        /// configure harm alic tables via the icp/ded according to the non-default programming settings. tables are
        /// only updated if they are non-default.
        /// <summary>
        private void BuildHARM(AirframeDevice ufc)
        {
            // TODO: check/force NAV assumption here
            AddActions(ufc, new() { "RTN", "RTN", "LIST", "0", "AG" });
            AddIfBlock("IsHARMOnDED", true, null, delegate ()
            {
                AddAction(ufc, "0");
                foreach (ALICTable table in _cfg.HARM.Tables)
                {
                    if (!table.IsDefault)
                    {
                        for (int i = 0; i < table.Table.Count; i++)
                        {
                            AddActions(ufc, PredActionsForNumAndEnter(table.Table[i].Code), new() { "DOWN" });
                        }
                    }
                    AddAction(ufc, "INC");
                }
            });
            AddActions(ufc, new() { "AG", "RTN" });
        }
    }
}