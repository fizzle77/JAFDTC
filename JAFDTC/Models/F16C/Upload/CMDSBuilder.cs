// ********************************************************************************************************************
//
// CMDSBuilder.cs -- f-16c cmds command builder
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
using JAFDTC.Models.F16C.CMDS;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// command builder for the cmds system in the viper. translates cmds setup in F16CConfiguration into commands
    /// that drive the dcs clickable cockpit.
    /// </summary>
    internal class CMDSBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public CMDSBuilder(F16CConfiguration cfg, F16DeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

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
            AirframeDevice ufc = _aircraft.GetDevice("UFC");

            if (!_cfg.CMDS.IsDefault)
            {
                AddActions(ufc, new() { "RTN", "RTN", "LIST", "7" }, null, WAIT_SHORT);

                // ---- chaff, flare bingo

                if (!string.IsNullOrEmpty(_cfg.CMDS.BingoChaff) || !string.IsNullOrEmpty(_cfg.CMDS.BingoFlare))
                {
                    AddActions(ufc, PredActionsForNumAndEnter(_cfg.CMDS.BingoChaff), new() { "DOWN" }, WAIT_BASE);
                    AddActions(ufc, PredActionsForNumAndEnter(_cfg.CMDS.BingoFlare), new() { "UP" }, WAIT_BASE);
                }

                // ---- move to chaff program 1 and enter chaff programs 1-6

                AddAction(ufc, "SEQ", WAIT_BASE);

                for (int i = 0; i < _cfg.CMDS.Programs.Length; i++)
                {
                    BuildProgramCommands(ufc, _cfg.CMDS.Programs[i].Chaff);
                }

                // ---- move to flare program 1 and enter flare programs 1-6

                AddAction(ufc, "SEQ", WAIT_BASE);
                for (int i = 0; i < _cfg.CMDS.Programs.Length; i++)
                {
                    BuildProgramCommands(ufc, _cfg.CMDS.Programs[i].Flare);
                }

                AddAction(ufc, "RTN");
            }
        }

        /// <summary>
        /// add commands to update the bq/bi/sq/si fields of the current program. programs and fields are only
        /// updated if they are non-default. advances to the next program after completion.
        /// </summary>
        private void BuildProgramCommands(AirframeDevice ufc, CMDSProgramCore pgm)
        {
            if (!pgm.IsDefault)
            {
                AddActions(ufc, PredActionsForCleanNumAndEnter(pgm.BQ), new() { "DOWN" }, WAIT_SHORT);
                AddActions(ufc, PredActionsForCleanNumAndEnter(pgm.BI), new() { "DOWN" }, WAIT_SHORT);
                AddActions(ufc, PredActionsForCleanNumAndEnter(pgm.SQ), new() { "DOWN" }, WAIT_SHORT);
                AddActions(ufc, PredActionsForCleanNumAndEnter(pgm.SI), new() { "DOWN" }, WAIT_SHORT);
            }
            AddAction(ufc, "INC", WAIT_BASE);
        }
    }
}