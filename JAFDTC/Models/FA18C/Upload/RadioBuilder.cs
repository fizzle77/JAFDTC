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
        // TODO: implement non-numeric presets
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
                BuildRadio(ufc, 1, _cfg.Radio.Presets[(int)Radios.COMM1], _cfg.Radio.COMM1DefaultTuning);
                BuildRadio(ufc, 2, _cfg.Radio.Presets[(int)Radios.COMM2], _cfg.Radio.COMM2DefaultTuning);
                AddAction(ufc, "COM", WAIT_LONG);
            }
        }

        /// <summary>
        /// select preset spinning the COM1/COM2 INC/DEC knob as necessary to go from a current preset to a new
        /// preset. presets are in the range [-3, 20], where 1-20 is presets 1-20, 0 is S, -1 is C, -2 is M, and
        /// -3 is G.
        /// </summary>
        private void SelectPreset(AirframeDevice ufc, int radioNum, string preset)
        {
            AddWhileBlock("IsNotRadioOnChannel", new() { $"{radioNum}", preset }, delegate ()
            {
                AddAction(ufc, $"COM{radioNum}ChInc", WAIT_SHORT);
            });
        }

        /// <summary>
        /// build commands to configure the radio presets and initial tuning.
        /// </summary>
        private void BuildRadio(AirframeDevice ufc, int radioNum, ObservableCollection<RadioPreset> presets,
                                string initialTuning)
        {
            foreach (RadioPresetInfoBase preset in presets)
            {
                SelectPreset(ufc, radioNum, preset.Preset.ToString());
                AddAction(ufc, $"COM{radioNum}", WAIT_LONG);
                AddActions(ufc, ActionsForCleanNum(preset.Frequency), new() { "ENT" }, WAIT_BASE);
            }

            if (int.TryParse(initialTuning, out int presetInit) && (presetInit < 20))
            {
                SelectPreset(ufc, radioNum, presetInit.ToString());
            }
            else if (float.TryParse(initialTuning, out float _)) 
            {
                SelectPreset(ufc, radioNum, "M");
                AddAction(ufc, $"COM{radioNum}", WAIT_LONG);
                AddAction(ufc, "CLR", WAIT_BASE);
                AddActions(ufc, ActionsForCleanNum(initialTuning), new() { "ENT" }, WAIT_BASE);
            }
        }
    }
}
