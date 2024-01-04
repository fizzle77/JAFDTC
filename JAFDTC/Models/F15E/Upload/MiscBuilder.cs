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
    /// command stream builder for the mudhen miscellaneous system that covers BINGO, CARA, TACAN, and ILS.
    /// </summary>
    internal class MiscBuilder : F15EBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MiscBuilder(F15EConfiguration cfg, F15ECommands dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure miscellaneous system (bingo, cara, tacan, ils) via the ufc according to the non-default
        /// programming settings (this function is safe to call with a configuration with default settings: defaults
        /// are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            Device ufc = _aircraft.GetDevice("UFC_PILOT");
            Device fltInst = _aircraft.GetDevice("FLTINST");

            BuildBingo(fltInst);
            BuildCARA(ufc);
            BuildTACAN(ufc); 
            BuildILS(ufc);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildBingo(Device fltInst)
        {
            if (!_cfg.Misc.IsBINGODefault)
            {
                int bingo = int.Parse(_cfg.Misc.Bingo);
                for (int i = 0; i < 140; i++)
                {
                    AppendCommand(fltInst.GetCommand("BingoDecrease"));
                }
                for (int i = 0; i < bingo / 100; i++)
                {
                    AppendCommand(fltInst.GetCommand("BingoIncrease"));
                }
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildCARA(Device ufc)
        {
            if (!_cfg.Misc.IsLowAltDefault)
            {
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("MENU"));

                AppendCommand(BuildDigits(ufc, _cfg.Misc.LowAltWarn));
                AppendCommand(ufc.GetCommand("PB1"));
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildTACAN(Device ufc)
        {
            if (!_cfg.Misc.IsTACANDefault)
            {
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("MENU"));

                AppendCommand(ufc.GetCommand("PB2"));

                AppendCommand(BuildDigits(ufc, _cfg.Misc.TACANChannel.ToString()));
                AppendCommand(ufc.GetCommand("PB1"));

                if (_cfg.Misc.TACANBandValue == TACANBands.X)
                {
                    AppendCommand(StartCondition("IsTACANBand", "Y"));
                    AppendCommand(ufc.GetCommand("PB1"));
                    AppendCommand(EndCondition("IsTACANBand"));
                }
                else
                {
                    AppendCommand(StartCondition("IsTACANBand", "X"));
                    AppendCommand(ufc.GetCommand("PB1"));
                    AppendCommand(EndCondition("IsTACANBand"));
                }

                AppendCommand(ufc.GetCommand("PB10"));
                AppendCommand(ufc.GetCommand("MENU"));
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildILS(Device ufc)
        {
            if (!_cfg.Misc.IsILSDefault)
            {
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("MENU"));
                AppendCommand(ufc.GetCommand("MENU"));

                AppendCommand(ufc.GetCommand("PB3"));

                AppendCommand(BuildDigits(ufc, RemoveSeparators(_cfg.Misc.ILSFrequency)));
                AppendCommand(ufc.GetCommand("PB3"));

                AppendCommand(ufc.GetCommand("MENU"));
            }
        }
    }
}
