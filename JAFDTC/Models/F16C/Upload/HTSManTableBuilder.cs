// ********************************************************************************************************************
//
// HTSBuilder.cs -- f-16c hts threat command builder
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// command builder for the hts system in the viper. translates cmds setup in F16CConfiguration into commands
    /// that drive the dcs clickable cockpit.
    /// </summary>
    internal class HTSBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public HTSBuilder(F16CConfiguration cfg, F16CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure hts system via the icp/ded according to the non-default programming settings (this function
        /// is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            AirframeDevice ufc = _aircraft.GetDevice("UFC");

            if (!_cfg.HTS.IsDefault)
            {
                AddActions(ufc, new() { "RTN", "RTN", "LIST", "8" }, null, WAIT_BASE);
                AddIfBlock("IsInAAMode", true, null, delegate ()
                {
                    AddAction(ufc, "SEQ");
                    AddIfBlock("IsInAGMode", true, null, delegate () { BuildHTSManualTable(ufc); });
                    AddActions(ufc, new() { "RTN", "RTN", "LIST", "8", "SEQ" });
                });
                AddAction(ufc, "RTN");
            }
        }

        /// <summary>
        /// configure hts manual table via the icp/ded according to the non-default programming settings. the manual
        /// table is only populated if it is non-default.
        /// <summary>
        private void BuildHTSManualTable(AirframeDevice ufc)
        {
            if (_cfg.HTS.IsMANTablePopulated)
            {
                AddActions(ufc, new() { "RTN", "RTN", "LIST", "0" }, null, WAIT_BASE);

                AddIfBlock("IsHTSOnDED", true, null, delegate ()
                {
                    AddAction(ufc, "ENTR");
                    for (int row = 0; row < _cfg.HTS.MANTable.Count; row++)
                    {
                        List<string> actions = PredActionsForNumAndEnter(_cfg.HTS.MANTable[row].Code);
                        AddActions(ufc, actions);
                        if (actions.Count == 0)
                        {
                            AddAction(ufc, "DOWN");
                        }
                    }
                });

                AddAction(ufc, "RTN");
            }
        }
    }
}