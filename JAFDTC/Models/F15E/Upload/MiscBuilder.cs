// ********************************************************************************************************************
//
// MiscBuilder.cs -- f-15e misc system command builder
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023 ilominar/raven
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
    /// TODO: document
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
        /// configure radio system (com1/com2 uhf/vhf radios) via the icp/ded according to the non-default programming
        /// settings (this function is safe to call with a configuration with default settings: defaults are skipped as
        /// necessary).
        /// <summary>
        public override void Build()
        {
            BuildBingo();
            BuildCARA();
            BuildTACAN(); 
            BuildILS();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildBingo()
        {
            if (!_cfg.Misc.IsBINGODefault)
            {
                Device d = _aircraft.GetDevice("FLTINST");
                int bingo = int.Parse(_cfg.Misc.Bingo);

                for (int i = 0; i < 140; i++)
                {
                    AppendCommand(d.GetCommand("BingoDecrease"));
                }
                for (int i = 0; i < bingo / 100; i++)
                {
                    AppendCommand(d.GetCommand("BingoIncrease"));
                }
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildCARA()
        {
            if (!_cfg.Misc.IsLowAltDefault)
            {
                Device ufc = _aircraft.GetDevice("UFC_PILOT");
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
        private void BuildTACAN()
        {
            if (!_cfg.Misc.IsTACANDefault)
            {

                Device ufc = _aircraft.GetDevice("UFC_PILOT");
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
        private void BuildILS()
        {
            if (!_cfg.Misc.IsILSDefault)
            {
                Device ufc = _aircraft.GetDevice("UFC_PILOT");
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
