// ********************************************************************************************************************
//
// CMDSBuilder.cs -- f-16c cmds command builder
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
using JAFDTC.Models.F16C.CMDS;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// command builder for the cmds system in the viper. translates cmds setup in F16CConfiguration into commands
    /// that drive the dcs clickable cockpit.
    /// </summary>
    internal class CMDSBuilder : F16CBuilderBase
	{
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public CMDSBuilder(F16CConfiguration cfg, F16CCommands dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure cmds system via the icp/ded according to the non-default programming settings (this function
        /// is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            Device ufc = _aircraft.GetDevice("UFC");

            if (!_cfg.CMDS.IsDefault)
            {
                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("RTN"));

                AppendCommand(ufc.GetCommand("LIST"));
                AppendCommand(ufc.GetCommand("7"));

                // ---- chaff bingo

                PredAppendDigitsWithEnter(ufc, _cfg.CMDS.BingoChaff);
                AppendCommand(ufc.GetCommand("DOWN"));

                // ---- flare bingo

                PredAppendDigitsWithEnter(ufc, _cfg.CMDS.BingoFlare);
                AppendCommand(ufc.GetCommand("UP"));

                // ---- move to chaff program 1 and enter chaff programs 1-6

                AppendCommand(ufc.GetCommand("SEQ"));
                for (int i = 0; i < _cfg.CMDS.Programs.Length; i++)
                {
                    AppendProgramCommands(ufc, _cfg.CMDS.Programs[i].Chaff);
                    AppendCommand(ufc.GetCommand("INC"));
                    AppendCommand(Wait());
                }

                // ---- move to flare program 1 and enter flare programs 1-6

                AppendCommand(ufc.GetCommand("SEQ"));
                for (int i = 0; i < _cfg.CMDS.Programs.Length; i++)
                {
                    AppendProgramCommands(ufc, _cfg.CMDS.Programs[i].Flare);
                    AppendCommand(ufc.GetCommand("INC"));
                    AppendCommand(Wait());
                }

                AppendCommand(ufc.GetCommand("RTN"));
            }
        }

        /// <summary>
        /// add commands to update the bq/bi/sq/si fields of the current program. programs and fields are only
        /// updated if they are non-default.
        /// </summary>
        private void AppendProgramCommands(Device ufc, CMDSProgramCore pgm)
        {
            if (!pgm.IsDefault)
            {
                PredAppendDigitsDLZRSWithEnter(ufc, pgm.BQ);
                AppendCommand(ufc.GetCommand("DOWN"));

                PredAppendDigitsDLZRSWithEnter(ufc, pgm.BI);
                AppendCommand(ufc.GetCommand("DOWN"));

                PredAppendDigitsDLZRSWithEnter(ufc, pgm.SQ);
                AppendCommand(ufc.GetCommand("DOWN"));

                PredAppendDigitsDLZRSWithEnter(ufc, pgm.SI);
                AppendCommand(ufc.GetCommand("DOWN"));
            }
        }
    }
}