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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// builder to generate the command stream to configure the radios through the ded/ufc (com1/2 uhf/vhf) according
    /// to an F16CConfiguration. the stream returns the ded to its default page. the builder does not require any
    /// state to function.
    /// </summary>
    internal class RadioBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public RadioBuilder(F16CConfiguration cfg, F16CDeviceManager dm, StringBuilder sb) : base(cfg, dm, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure radio system (com1/com2 uhf/vhf radios) via the ded/ufc according to the non-default programming
        /// settings (this function is safe to call with a configuration with default settings: defaults are skipped as
        /// necessary).
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.Radio.IsDefault)
                return;

            AirframeDevice ufc = _aircraft.GetDevice("UFC");

            BuildRadio(ufc, "COM1", _cfg.Radio.Presets[(int)Radios.COMM1], _cfg.Radio.COMM1DefaultTuning,
                       _cfg.Radio.IsCOMM1MonitorGuard);
            BuildRadio(ufc, "COM2", _cfg.Radio.Presets[(int)Radios.COMM2], _cfg.Radio.COMM2DefaultTuning, false);
        }

        /// <summary>
        /// configure presets for a single radio system according to the non-default programming settings.
        /// <summary>
        private void BuildRadio(AirframeDevice ufc, string radioCmd, ObservableCollection<RadioPreset> presets,
                                string initialTuning, bool isGuardMonitor)
        {
            AddAction(ufc, radioCmd);

            // TODO: this should be conditional on guard not already set (ie MAIN, not BOTH).
            if (isGuardMonitor)
                AddAction(ufc, "SEQ");

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

            SelectDEDPageDefault(ufc);
        }
    }
}