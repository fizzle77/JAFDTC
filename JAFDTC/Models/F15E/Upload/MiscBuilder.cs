// ********************************************************************************************************************
//
// MiscBuilder.cs -- f-15e misc system command builder
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
using JAFDTC.Models.F15E.Misc;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F15E.Upload
{
    /// <summary>
    /// command builder for the miscellaneous systems (BINGO) in the mudhen. translates misc setup in F15EConfiguration
    /// into commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class MiscBuilder : F15EBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MiscBuilder(F15EConfiguration cfg, F15EDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure miscellaneous system (bingo) via the ufc according to the non-default programming settings (this
        /// function is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            AirframeDevice fltInst = _aircraft.GetDevice("FLTINST");

            if (_cfg.CrewMember == F15EConfiguration.CrewPositions.PILOT)
            {
                AddIfBlock("IsInFrontCockpit", true, null, delegate ()
                {
                    BuildBingo(fltInst);
                });
            }
        }

        /// <summary>
        /// configure the bingo setting based on the configuration.
        /// </summary>
        private void BuildBingo(AirframeDevice fltInst)
        {
            if (!_cfg.Misc.IsBINGODefault)
            {
                int bingo = int.Parse(_cfg.Misc.Bingo);
                // TODO: handle this through a dcs-side loop?
                for (int i = 0; i < 140; i++)
                {
                    AddAction(fltInst, "BingoDecrease");
                }
                for (int i = 0; i < bingo / 100; i++)
                {
                    AddAction(fltInst, "BingoIncrease");
                }
            }
        }
    }
}
