// ********************************************************************************************************************
//
// MiscBuilder.cs -- f-16c miscellaneous command builder
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
using JAFDTC.Models.F16C.Misc;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// command builder for the miscellaneous setups in the viper (TACAN/ILS, ALOW, BINDO, LASR, BULLS, and HMCS).
    /// translates cmds setup in F16CConfiguration into commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class MiscBuilder : F16CBuilderBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MiscBuilder(F16CConfiguration cfg, F16CCommands dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure misc systems (TACAN/ILS, ALOW, BINDO, LASR, BULLS, and HMCS) via the icp/ded according to the
        /// non-default programming settings (this function is safe to call with a configuration with default settings:
        /// defaults are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            Device ufc = _aircraft.GetDevice("UFC");
            Device ehsi = _aircraft.GetDevice("EHSI");
            Device hmcsInt = _aircraft.GetDevice("HMCS_INT");

            if (!_cfg.Misc.IsDefault)
            {
                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("RTN"));

                BuildTILS(ufc, ehsi);
                BuildALOW(ufc);
                BuildBingo(ufc);
                BuildLaserSettings(ufc);
                BuildBullseye(ufc);
                BuildHMCS(ufc, hmcsInt);

                AppendCommand(ufc.GetCommand("RTN"));
            }
        }

        /// <summary>
        /// configure icp t-ils (1) programming includes tacan (and ehsi mode) and ils via the icp/ded according to the
        /// non-default programming settings (this function is safe to call with a configuration with default settings:
        /// defaults are skipped as necessary).
        /// <summary>
        private void BuildTILS(Device ufc, Device ehsi)
        {
            AppendCommand(ufc.GetCommand("1"));

            // TODO: do a better job detecting defaults here to avoid just moving around ded.

            // ---- tacan mode

            if (_cfg.Misc.TACANIsYardstickValue)
            {
                // TODO: ideally, want a start condition here on current mode, assume default rec here
                AppendCommand(ufc.GetCommand("SEQ"));
                AppendCommand(ufc.GetCommand("SEQ"));
            }

            // ---- tacan channel

            PredAppendDigitsWithEnter(ufc, _cfg.Misc.TACANChannel);

            // ---- tacan channel

            if (_cfg.Misc.TACANBandValue == TACANBands.X)
            {
                AppendCommand(StartCondition("TACANBandY"));
                AppendCommand(ufc.GetCommand("0"));
                AppendCommand(ufc.GetCommand("ENTR"));
                AppendCommand(EndCondition("TACANBandY"));
            }
            else
            {
                AppendCommand(StartCondition("TACANBandX"));
                AppendCommand(ufc.GetCommand("0"));
                AppendCommand(ufc.GetCommand("ENTR"));
                AppendCommand(EndCondition("TACANBandX"));
            }

            // ---- ehsi mode

            // TODO: ideally, want a start condition here on current ehsi mode, assume default nav here
            AppendCommand(ehsi.GetCommand("MODE"));
            AppendCommand(ehsi.GetCommand("MODE"));

            // ---- ils

            AppendCommand(ufc.GetCommand("DOWN"));
            AppendCommand(ufc.GetCommand("DOWN"));

            if (!PredAppendDigitsNoSepWithEnter(ufc, _cfg.Misc.ILSFrequency))
            {
                AppendCommand(ufc.GetCommand("DOWN"));
            }
            PredAppendDigitsWithEnter(ufc, _cfg.Misc.ILSCourse);

            AppendCommand(ufc.GetCommand("RTN"));
        }

        /// <summary>
        /// configure icp alow (icp 2) programming via the icp/ded according to the non-default programming settings
        /// (this function is safe to call with a configuration with default settings: defaults are skipped as
        /// necessary).
        /// <summary>
        private void BuildALOW(Device ufc)
        {
            if (!_cfg.Misc.IsALOWDefault)
            {
                AppendCommand(ufc.GetCommand("2"));

                PredAppendDigitsWithEnter(ufc, _cfg.Misc.ALOWCARAALOW);
                AppendCommand(ufc.GetCommand("DOWN"));

                PredAppendDigitsWithEnter(ufc, _cfg.Misc.ALOWMSLFloor);
                AppendCommand(ufc.GetCommand("DOWN"));

                AppendCommand(ufc.GetCommand("RTN"));
            }
        }

        /// <summary>
        /// configure ded bngo (list 2) programming via the icp/ded according to the non-default programming settings
        /// (this function is safe to call with a configuration with default settings: defaults are skipped as
        /// necessary).
        /// <summary>
        private void BuildBingo(Device ufc)
        {
            if (!_cfg.Misc.IsBINGODefault)
            {
                AppendCommand(ufc.GetCommand("LIST"));
                AppendCommand(ufc.GetCommand("2"));

                PredAppendDigitsWithEnter(ufc, _cfg.Misc.Bingo);

                AppendCommand(ufc.GetCommand("RTN"));
            }
        }

        /// <summary>
        /// configure ded lasr (list misc, 5) programming via the icp/ded according to the non-default programming
        /// settings (this function is safe to call with a configuration with default settings: defaults are skipped
        /// as necessary).
        /// <summary>
        private void BuildLaserSettings(Device ufc)
        {
            if (!_cfg.Misc.IsLaserDefault)
            {
                AppendCommand(ufc.GetCommand("LIST"));
                AppendCommand(ufc.GetCommand("0"));
                AppendCommand(ufc.GetCommand("5"));

                // ---- tgp designator laser code

                PredAppendDigitsWithEnter(ufc, _cfg.Misc.LaserTGPCode);
                AppendCommand(ufc.GetCommand("DOWN"));

                // ---- tgp lst laser code

                PredAppendDigitsWithEnter(ufc, _cfg.Misc.LaserLSTCode);
                AppendCommand(ufc.GetCommand("DOWN"));

                // ---- tgp laser start time

                PredAppendDigitsWithEnter(ufc, _cfg.Misc.LaserStartTime);

                AppendCommand(ufc.GetCommand("RTN"));
            }
        }

        /// <summary>
        /// configure ded bull (list misc, 8) via the icp/ded according to the non-default programming settings (this
        /// function is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        private void BuildBullseye(Device ufc)
        {
            if (!_cfg.Misc.IsBULLDefault)
            {
                AppendCommand(ufc.GetCommand("LIST"));
                AppendCommand(ufc.GetCommand("0"));
                AppendCommand(ufc.GetCommand("8"));

                AppendCommand(Wait());

                AppendCommand(StartCondition("BullseyeNotSelected"));
                AppendCommand(ufc.GetCommand("0"));
                AppendCommand(EndCondition("BullseyeNotSelected"));

                AppendCommand(ufc.GetCommand("DOWN"));

                PredAppendDigitsDLZRSWithEnter(ufc, _cfg.Misc.BullseyeWP);
                AppendCommand(ufc.GetCommand("DOWN"));

                AppendCommand(ufc.GetCommand("RTN"));
            }
        }

        /// <summary>
        /// configure jhms (list misc, rcl) programming via the icp/ded according to the non-default programming
        /// settings (this function is safe to call with a configuration with default settings: defaults are skipped
        /// as necessary).
        /// <summary>
        private void BuildHMCS(Device ufc, Device hmcsInt)
        {
            if (!_cfg.Misc.IsHMCSDefault)
            {
                AppendCommand(ufc.GetCommand("LIST"));
                AppendCommand(ufc.GetCommand("0"));
                AppendCommand(ufc.GetCommand("RCL"));

                // TODO: check current state, assume enabled by default for now
                if (!_cfg.Misc.HMCSBlankHUDValue)
                {
                    AppendCommand(ufc.GetCommand("0"));
                }
                else
                {
                    AppendCommand(ufc.GetCommand("DOWN"));
                }
                AppendCommand(Wait());

                // TODO: check current state, assume enabled by default for now
                if (!_cfg.Misc.HMCSBlankCockpitValue)
                {
                    AppendCommand(ufc.GetCommand("0"));
                }
                else
                {
                    AppendCommand(ufc.GetCommand("DOWN"));
                }
                AppendCommand(Wait());

                // TODO: check current state, assume lvl1 by default for now
                if (_cfg.Misc.HMCSDeclutterLvlValue != HMCSDeclutterLevels.LVL1)
                {
                    AppendCommand(ufc.GetCommand("1"));
                }
                if (_cfg.Misc.HMCSDeclutterLvlValue == HMCSDeclutterLevels.LVL3)
                {
                    AppendCommand(ufc.GetCommand("1"));
                }
                AppendCommand(Wait());
                AppendCommand(ufc.GetCommand("DOWN"));

                // TODO: check current state, assume enabled by default for now
                if (!_cfg.Misc.HMCSDisplayRWRValue)
                {
                    AppendCommand(ufc.GetCommand("0"));
                }
                AppendCommand(Wait());

                if (!string.IsNullOrEmpty(_cfg.Misc.HMCSIntensity))
                {
                    AppendCommand(hmcsInt.GetCommand("INT", true, double.Parse(_cfg.Misc.HMCSIntensity)));
                }

                AppendCommand(ufc.GetCommand("RTN"));
            }
        }
    }
}