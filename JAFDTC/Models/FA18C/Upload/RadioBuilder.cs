// ********************************************************************************************************************
//
// RadioBuilder.cs -- fa-18c radio command builder
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
using JAFDTC.Models.FA18C.Radio;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.FA18C.Upload
{
    /// <summary>
    /// command builder for the radio system (com1/com2 uhf/vhf radios) in the hornet. translates radio setup in
    /// FA18CConfiguration into commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class RadioBuilder : FA18CBuilderBase, IBuilder
    {
        private enum ComKnobPresets
        {
            PRESET_G = -3,
            PRESET_M = -2,
            PRESET_C = -1,
            PRESET_S = 0,
            PRESET_1 = 1,
            PRESET_20 = 20
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public RadioBuilder(FA18CConfiguration cfg, FA18CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure radio system (com1/com2 uhf/vhf radios) via the ufc according to the non-default programming
        /// settings (this function is safe to call with a configuration with default settings: defaults are skipped as
        /// necessary).
        /// <summary>
        public override void Build()
        {
            AirframeDevice ufc = _aircraft.GetDevice("UFC");

            if (!_cfg.Radio.IsDefault)
            {
                BuildRadio(ufc, "COM1", _cfg.Radio.Presets[(int)Radios.COMM1], _cfg.Radio.COMM1DefaultTuning);
                BuildRadio(ufc, "COM2", _cfg.Radio.Presets[(int)Radios.COMM2], _cfg.Radio.COMM2DefaultTuning);
            }
        }

        /// <summary>
        /// select preset spinning the COM1/COM2 INC/DEC knob as necessary to go from a current preset to a new
        /// preset. presets are in the range [-3, 20], where 1-20 is presets 1-20, 0 is S, -1 is C, -2 is M, and
        /// -3 is G.
        /// </summary>
        private int SelectPreset(AirframeDevice ufc, string radioCmd, int presetCur, int presetNew)
        {
            // TODO: bounds checking/clipping on preset numbers

            while (presetNew > presetCur)
            {
                presetCur++;
                AddAction(ufc, radioCmd + "ChInc");
            }
            while (presetNew < presetCur)
            {
                presetCur--;
                AddAction(ufc, radioCmd + "ChDec");
            }
            AddWait(WAIT_BASE);
            return presetNew;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildRadio(AirframeDevice ufc, string radioCmd, ObservableCollection<RadioPreset> presets,
                                string initialTuning)
        {
            int presetNum = 0;

            // TODO: verify preset is 1 explictly rather than relying on initial state?

            AddAction(ufc, radioCmd + "ChDec");
            foreach (RadioPresetInfoBase preset in presets)
            {
                presetNum = SelectPreset(ufc, radioCmd, presetNum, preset.Preset);
                AddAction(ufc, radioCmd);
                AddWait(WAIT_LONG);

                AddActions(ufc, ActionsForCleanNum(preset.Frequency), new() { "ENT" });
                AddWait(WAIT_BASE);
            }

            if (string.IsNullOrEmpty(initialTuning))
            {
                SelectPreset(ufc, radioCmd, presetNum, 1);
            }
            else if (int.TryParse(initialTuning, out int presetInit) && (presetInit < 20))
            {
                SelectPreset(ufc, radioCmd, presetNum, presetInit);
            }
            else
            {
                SelectPreset(ufc, radioCmd, presetNum, (int)ComKnobPresets.PRESET_M);
                AddAction(ufc, radioCmd);
                AddWait(WAIT_LONG);
                AddActions(ufc, ActionsForCleanNum(initialTuning), new() { "ENT" });
            }
            AddAction(ufc, radioCmd);
            AddWait(WAIT_BASE);
        }
    }
}
