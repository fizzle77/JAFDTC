// ********************************************************************************************************************
//
// RadioBuilder.cs -- f-16c radio command builder
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

using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.Models.F16C.Radio;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// command builder for the radio system (com1/com2 uhf/vhf radios) in the viper. translates radio setup in
    /// F16CConfiguration into commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class RadioBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public RadioBuilder(F16CConfiguration cfg, F16CCommands dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

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
            Device ufc = _aircraft.GetDevice("UFC");

            if (!_cfg.Radio.IsDefault)
            {
                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("RTN"));

                BuildRadio(ufc, "COM1", _cfg.Radio.Presets[(int)Radios.COMM1], _cfg.Radio.COMM1DefaultTuning,
                           _cfg.Radio.IsCOMM1MonitorGuard);

                BuildRadio(ufc, "COM2", _cfg.Radio.Presets[(int)Radios.COMM2], _cfg.Radio.COMM2DefaultTuning, false);
            }
        }

        /// <summary>
        /// configure presets for a single radio system according to the non-default programming settings.
        /// <summary>
        private void BuildRadio(Device ufc, string radioCmd, ObservableCollection<RadioPreset> presets,
                                string initialTuning, bool isGuardMonitor)
        {
            AppendCommand(ufc.GetCommand(radioCmd));

            if (isGuardMonitor)
            {
                // TODO: this should be conditional on guard not already set (ie MAIN, not BOTH).
                AppendCommand(ufc.GetCommand("SEQ"));
            }

            AppendCommand(ufc.GetCommand("DOWN"));
            AppendCommand(ufc.GetCommand("DOWN"));

            foreach (RadioPresetInfoBase preset in presets)
            {
                PredAppendDigitsWithEnter(ufc, preset.Preset.ToString());
                AppendCommand(ufc.GetCommand("DOWN"));

                PredAppendDigitsNoSepWithEnter(ufc, preset.Frequency.ToString());
                AppendCommand(ufc.GetCommand("UP"));
            }

            AppendCommand(ufc.GetCommand("1"));
            AppendCommand(ufc.GetCommand("ENTR"));

            if (!string.IsNullOrEmpty(initialTuning))
            {
                AppendCommand(ufc.GetCommand("DOWN"));
                AppendCommand(ufc.GetCommand("DOWN"));
                PredAppendDigitsNoSepWithEnter(ufc, initialTuning);
            }

            AppendCommand(ufc.GetCommand("RTN"));
        }
    }
}