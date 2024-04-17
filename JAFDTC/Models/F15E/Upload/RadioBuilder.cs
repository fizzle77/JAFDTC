// ********************************************************************************************************************
//
// RadioBuilder.cs -- f-15e radio command builder
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
using JAFDTC.Models.F15E.MPD;
using JAFDTC.Models.F15E.Radio;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F15E.Upload
{
    /// <summary>
    /// command builder for the radio system (com1/com2 uhf/vhf radios) in the mudhen. translates radio setup in
    /// F15EConfiguration into commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class RadioBuilder : F15EBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public RadioBuilder(F15EConfiguration cfg, F15EDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

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
            AirframeDevice ufcPilot = _aircraft.GetDevice("UFC_PILOT");
            AirframeDevice ufcWizzo = _aircraft.GetDevice("UFC_WSO");

            if ((_cfg.CrewMember == F15EConfiguration.CrewPositions.PILOT) && !_cfg.Radio.IsDefault)
            {
                AddIfBlock("IsInFrontCockpit", null, delegate ()
                {
                    BuildRadioCore(ufcPilot);
                });
            }
            if ((_cfg.CrewMember == F15EConfiguration.CrewPositions.WSO) && !_cfg.Radio.IsDefault)
            {
                AddIfBlock("IsInRearCockpit", null, delegate ()
                {
                    BuildRadioCore(ufcWizzo);
                });
            }
        }

        /// <summary>
        /// core cockpit independent radio setup.
        /// </summary>
        private void BuildRadioCore(AirframeDevice ufc)
        {
            AddActions(ufc, new() { "CLR", "CLR", "CLR", "CLR", "MENU" });
            BuildRadio(ufc, _cfg.Radio.Presets[(int)Radios.COMM1], "PB5", _cfg.Radio.IsCOMM1MonitorGuard,
                       _cfg.Radio.IsCOMM1PresetMode, _cfg.Radio.COMM1DefaultTuning);

            AddActions(ufc, new() { "CLR", "CLR", "CLR", "CLR", "MENU" });
            BuildRadio(ufc, _cfg.Radio.Presets[(int)Radios.COMM2], "PB6", _cfg.Radio.IsCOMM2MonitorGuard,
                       _cfg.Radio.IsCOMM2PresetMode, _cfg.Radio.COMM2DefaultTuning);

            AddActions(ufc, new() { "CLR", "CLR", "CLR", "CLR", "MENU" });
        }

        private void BuildRadio(AirframeDevice ufc, ObservableCollection<RadioPreset> presets, string pb, bool isMonGuard,
                                bool isPreMode, string dfltTuning)
        {
            bool isRadio1 = (pb == "PB5");

            if (isPreMode)
            {
                AddIfBlock("IsRadioPresetOrFreqSelected", new() { ufc.Name, (isRadio1 ? "1" : "2"), "freq" }, delegate () {
                    AddAction(ufc, isRadio1 ? "GCML" : "GCMR");
                });
            }
            else
            {
                AddIfBlock("IsRadioPresetOrFreqSelected", new() { ufc.Name, (isRadio1 ? "1" : "2"), "preset" }, delegate () {
                    AddAction(ufc, isRadio1 ? "GCML" : "GCMR");
                });
            }

            AddAction(ufc, pb);

            BuildRadioPresets(ufc, presets);

            if (dfltTuning.ToUpper() == "G")
            {
                AddActions(ufc, new() { "1", (isRadio1 ? "PRESL" : "PRESR"), "CLR" });
                AddAction(ufc, isRadio1 ? "PRESLCCW" : "PRESRCCW");
                if (!isRadio1)
                {
                    AddAction(ufc, "PRESRCCW");
                }
            }
            else if (dfltTuning.ToUpper() == "GV")
            {
                AddActions(ufc, new() { "1", "PRESR", "CLR", "PRESRCCW" });
            }
            else if (int.TryParse(dfltTuning, out int dfltPreset) && (dfltPreset >= 1) && (dfltPreset <= 20))
            {
                AddActions(ufc, ActionsForString(dfltTuning), new() { (isRadio1 ? "PRESL" : "PRESR"), "CLR" });
            }
            else if (!string.IsNullOrEmpty(dfltTuning))
            {
                InputFrequency(ufc, dfltTuning);
                AddActions(ufc, new() { pb, "CLR" });
            }

            string state = (isMonGuard) ? "disabled" : "enabled";
            AddIfBlock("IsRadioGuardEnabledDisabled", new() { ufc.Name, (isRadio1 ? "1" : "2"), state }, delegate () {
                AddActions(ufc, new() { "SHF", (isRadio1 ? "GCML" : "GCMR") });
            });

            if ((presets.Count > 0) && (pb == "PB6"))
            {
                AddAction(ufc, "PB9");
            }
        }

        private void BuildRadioPresets(AirframeDevice ufc, ObservableCollection<RadioPreset> presets)
        {
            foreach (RadioPreset preset in presets)
            {
                string freq = preset.Frequency;
                if (!string.IsNullOrEmpty(freq))
                {
                    AddActions(ufc, ActionsForString(preset.Preset.ToString()), new() { "PB1" });
                    InputFrequency(ufc, freq);
                    AddAction(ufc, "PB10");
                }
            }
        }

        /// <summary>
        /// enter a frequency via the ufc.
        /// </summary>
        private void InputFrequency(AirframeDevice ufc, string freq)
        {
            if (freq.Length == 6)
            {
                if (freq.EndsWith("000"))
                {
                    freq = freq.Replace("000", "001");
                }
                var parts = freq.Split('.');
                AddActions(ufc, ActionsForString(parts[0]));
                AddAction(ufc, ".");
                AddActions(ufc, ActionsForString(parts[1]));
            }
            else
            {
                AddActions(ufc, ActionsForString(freq.Replace(".", "")));
            }
        }
    }
}
