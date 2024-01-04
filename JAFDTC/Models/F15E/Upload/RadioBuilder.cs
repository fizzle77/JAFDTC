// ********************************************************************************************************************
//
// RadioBuilder.cs -- f-15e radio command builder
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
using JAFDTC.Models.F15E.Radio;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F15E.Upload
{
    /// <summary>
    /// command stream builder for the mudhen radio system that covers COM1/COM2 presets and other settings.
    /// </summary>
    internal class RadioBuilder : F15EBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public RadioBuilder(F15EConfiguration cfg, F15ECommands dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

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
            Device ufc = _aircraft.GetDevice("UFC_PILOT");
    
            if (!_cfg.Radio.IsDefault)
            {
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("MENU"));

                BuildRadio(ufc, _cfg.Radio.Presets[(int)Radios.COMM1], "PB5", _cfg.Radio.IsCOMM1MonitorGuard,
                           _cfg.Radio.IsCOMM1PresetMode, _cfg.Radio.COMM1DefaultTuning);

                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("MENU"));

                BuildRadio(ufc, _cfg.Radio.Presets[(int)Radios.COMM2], "PB6", _cfg.Radio.IsCOMM2MonitorGuard,
                           _cfg.Radio.IsCOMM2PresetMode, _cfg.Radio.COMM2DefaultTuning);

                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("MENU"));
            }
        }

        private void BuildRadio(Device ufc, ObservableCollection<RadioPreset> presets, string pb, bool isMonGuard,
                                bool isPreMode, string dfltTuning)
        {
            var isRadio1 = (pb == "PB5");

            if (isPreMode)
            {
                AppendCommand(StartCondition("IsRadioPresetOrFreqSelected", isRadio1 ? "1" : "2", "freq"));
                AppendCommand(ufc.GetCommand(isRadio1 ? "GCML" : "GCMR"));
                AppendCommand(EndCondition("IsRadioPresetOrFreqSelected"));
            }
            else
            {
                AppendCommand(StartCondition("IsRadioPresetOrFreqSelected", isRadio1 ? "1" : "2", "preset"));
                AppendCommand(ufc.GetCommand(isRadio1 ? "GCML" : "GCMR"));
                AppendCommand(EndCondition("IsRadioPresetOrFreqSelected"));
            }

            AppendCommand(ufc.GetCommand(pb));

            BuildRadioPresets(ufc, presets);

            if (dfltTuning.ToUpper() == "G")
            {
                AppendCommand(BuildDigits(ufc, "1"));
                AppendCommand(ufc.GetCommand(isRadio1 ? "PRESL" : "PRESR"));
                AppendCommand(ufc.GetCommand("CLR"));

                AppendCommand(ufc.GetCommand(isRadio1 ? "PRESLCCW" : "PRESRCCW"));
                if (!isRadio1)
                {
                    AppendCommand(ufc.GetCommand("PRESRCCW"));
                }
            }
            else if (dfltTuning.ToUpper() == "GV")
            {
                AppendCommand(BuildDigits(ufc, "1"));
                AppendCommand(ufc.GetCommand("PRESR"));
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("PRESRCCW"));
            }
            else if (int.TryParse(dfltTuning, out int dfltPreset) && (dfltPreset >= 1) && (dfltPreset <= 20))
            {
                AppendCommand(BuildDigits(ufc, dfltTuning));
                AppendCommand(ufc.GetCommand(isRadio1 ? "PRESL" : "PRESR"));
                AppendCommand(ufc.GetCommand("CLR"));
            }
            else if (!string.IsNullOrEmpty(dfltTuning))
            {
                InputFrequency(ufc, dfltTuning);
                AppendCommand(ufc.GetCommand(pb));
                AppendCommand(ufc.GetCommand("CLR"));
            }

            if (isMonGuard)
            {
                AppendCommand(StartCondition("IsRadioGuardEnabledDisabled", isRadio1 ? "1" : "2", "disabled"));
                AppendCommand(ufc.GetCommand("SHF"));
                AppendCommand(ufc.GetCommand(isRadio1 ? "GCML" : "GCMR"));
                AppendCommand(EndCondition("IsRadioGuardEnabledDisabled"));
            }
            else
            {
                AppendCommand(StartCondition("IsRadioGuardEnabledDisabled", isRadio1 ? "1" : "2", "enabled"));
                AppendCommand(ufc.GetCommand("SHF"));
                AppendCommand(ufc.GetCommand(isRadio1 ? "GCML" : "GCMR"));
                AppendCommand(EndCondition("IsRadioGuardEnabledDisabled"));
            }
        }

        private void BuildRadioPresets(Device ufc, ObservableCollection<RadioPreset> presets)
        {
            foreach (RadioPreset preset in presets)
            {
                string freq = preset.Frequency;
                if (!string.IsNullOrEmpty(freq))
                {
                    string presetNum = preset.Preset.ToString();
                    AppendCommand(BuildDigits(ufc, presetNum));
                    AppendCommand(ufc.GetCommand("PB1"));

                    InputFrequency(ufc, freq);
                    AppendCommand(ufc.GetCommand("PB10"));
                }
            }
        }

        private void InputFrequency(Device ufc, string freq)
        {
            if (freq.Length == 6)
            {
                if (freq.EndsWith("000"))
                {
                    freq = freq.Replace("000", "001");
                }
                var parts = freq.Split('.');
                AppendCommand(BuildDigits(ufc, parts[0]));
                AppendCommand(ufc.GetCommand("."));
                AppendCommand(BuildDigits(ufc, parts[1]));
            }
            else
            {
                freq = freq.Replace(".", "");
                AppendCommand(BuildDigits(ufc, freq));
            }
        }
    }
}
