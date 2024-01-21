// ********************************************************************************************************************
//
// RadioBuilder.cs -- f-16c radio command builder
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

        public RadioBuilder(F16CConfiguration cfg, F16DeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

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
            AirframeDevice ufc = _aircraft.GetDevice("UFC");

            if (!_cfg.Radio.IsDefault)
            {
                AddActions(ufc, new() { "RTN", "RTN" });
                BuildRadio(ufc, "COM1", _cfg.Radio.Presets[(int)Radios.COMM1], _cfg.Radio.COMM1DefaultTuning,
                           _cfg.Radio.IsCOMM1MonitorGuard);
                BuildRadio(ufc, "COM2", _cfg.Radio.Presets[(int)Radios.COMM2], _cfg.Radio.COMM2DefaultTuning, false);
            }
        }

        /// <summary>
        /// configure presets for a single radio system according to the non-default programming settings.
        /// <summary>
        private void BuildRadio(AirframeDevice ufc, string radioCmd, ObservableCollection<RadioPreset> presets,
                                string initialTuning, bool isGuardMonitor)
        {
            AddAction(ufc, radioCmd);

            if (isGuardMonitor)
            {
                // TODO: this should be conditional on guard not already set (ie MAIN, not BOTH).
                AddAction(ufc, "SEQ");
            }

            AddActions(ufc, new() { "DOWN", "DOWN" });

            foreach (RadioPresetInfoBase preset in presets)
            {
                AddActions(ufc, PredActionsForNumAndEnter(preset.Preset.ToString()), new() { "DOWN" });
                AddActions(ufc, PredActionsForCleanNumAndEnter(preset.Frequency.ToString()), new() { "UP" });
            }

            AddActions(ufc, new() { "1", "ENTR" });

            if (!string.IsNullOrEmpty(initialTuning))
            {
                AddActions(ufc, new() { "DOWN", "DOWN" });
                AddActions(ufc, PredActionsForCleanNumAndEnter(initialTuning));
            }

            AddAction(ufc, "RTN");
        }
    }
}