// ********************************************************************************************************************
//
// UFCBuilder.cs -- f-15e ufc system command builder
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
using JAFDTC.Models.F15E.UFC;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F15E.Upload
{
    /// <summary>
    /// command builder for the ufc-managed systems (CARA, TACAN, ILS) in the mudhen. translates ufc setup in
    /// F15EConfiguration into commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class UFCBuilder : F15EBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public UFCBuilder(F15EConfiguration cfg, F15EDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure ufc systems (cara, tacan, ils) via the ufc according to the non-default programming settings
        /// (this function is safe to call with a configuration with default settings: defaults are skipped as
        /// necessary).
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            AirframeDevice ufcPilot = _aircraft.GetDevice("UFC_PILOT");
            AirframeDevice ufcWizzo = _aircraft.GetDevice("UFC_WSO");

            if ((_cfg.CrewMember == F15EConfiguration.CrewPositions.PILOT) && !_cfg.UFC.IsDefault)
            {
                AddIfBlock("IsInFrontCockpit", true, null, delegate ()
                {
                    BuildUFCCore(ufcPilot);
                });
            }
            if ((_cfg.CrewMember == F15EConfiguration.CrewPositions.WSO) && !_cfg.UFC.IsDefault)
            {
                AddIfBlock("IsInRearCockpit", true, null, delegate ()
                {
                    BuildUFCCore(ufcWizzo);
                });
            }
        }

        /// <summary>
        /// core cockpit independent steerpoint setup.
        /// </summary>
        private void BuildUFCCore(AirframeDevice ufc)
        {
            BuildCARA(ufc);
            BuildTACAN(ufc);
            BuildILS(ufc);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildCARA(AirframeDevice ufc)
        {
            if (!_cfg.UFC.IsLowAltDefault)
            {
                AddActions(ufc, new() { "CLR", "CLR", "MENU" });
                AddActions(ufc, ActionsForString(_cfg.UFC.LowAltWarn), new() { "PB1" });
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildTACAN(AirframeDevice ufc)
        {
            if (!_cfg.UFC.IsTACANDefault)
            {
                AddActions(ufc, new() { "CLR", "CLR", "MENU", "PB2" });

                AddActions(ufc, ActionsForString(_cfg.UFC.TACANChannel.ToString()), new() { "PB1" });

                string band = (_cfg.UFC.TACANBandValue == TACANBands.X) ? "Y" : "X";
                AddIfBlock("IsTACANBand", true, new() { ufc.Name, band }, delegate () { AddAction(ufc, "PB1"); });

                string modeButton = _cfg.UFC.TACANModeValue switch
                {
                    TACANModes.A2A => "PB2",
                    TACANModes.TR => "PB3",
                    TACANModes.REC => "PB4",
                    _ => "PB2"
                };

                AddActions(ufc, new() { modeButton, "PB10", "MENU" });
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildILS(AirframeDevice ufc)
        {
            if (!_cfg.UFC.IsILSDefault)
            {
                AddActions(ufc, new() { "CLR", "CLR", "MENU", "MENU", "PB3" });

                AddActions(ufc, ActionsForString(AdjustNoSeparators(_cfg.UFC.ILSFrequency)), new() { "PB3" });

                AddAction(ufc, "MENU");
            }
        }
    }
}
