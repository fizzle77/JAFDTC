// ********************************************************************************************************************
//
// RadioBuilder.cs -- a-10c radio command builder
//
// Copyright(C) 2024 ilominar/raven
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
using JAFDTC.Models.A10C.Radio;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.A10C.Upload
{
    /// <summary>
    /// command builder for the radio system in the warthog. translates cmds setup in A10CConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class RadioBuilder : A10CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public RadioBuilder(A10CConfiguration cfg, A10CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure radio system via the cdu according to the non-default programming settings (this function is
        /// safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// </summary>
        public override void Build()
        {
            AirframeDevice cdu = _aircraft.GetDevice("CDU");
            AirframeDevice rmfd = _aircraft.GetDevice("RMFD");
            AirframeDevice ufc = _aircraft.GetDevice("UFC");
            AirframeDevice arc210 = _aircraft.GetDevice("UHF_ARC210");
            AirframeDevice arc164 = _aircraft.GetDevice("UHF_ARC164");
            AirframeDevice arc186 = _aircraft.GetDevice("VHF_ARC186");

            if (!_cfg.Radio.IsDefault)
            {
                BuildARC210(cdu, ufc, rmfd, arc210, _cfg.Radio);
                BuildARC164(arc164, _cfg.Radio);
                BuildARC186(arc186, _cfg.Radio);
            }
        }

        /// <summary>
        /// configure the primary ARC-210 UHF/VHF radio according to the non-default programming settings (this
        /// function is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// this includes presets, default freq/preset, preset mode, guard monitor, and HUD status display.
        /// </summary>
        private void BuildARC210(AirframeDevice cdu, AirframeDevice ufc, AirframeDevice rmfd, AirframeDevice arc210,
                                 RadioSystem radios)
        {
            if (radios.Presets[(int)RadioSystem.Radios.COMM1].Count > 0)
            {
                int maxPreset = RadioMaxPreset(radios.Presets[(int)RadioSystem.Radios.COMM1]);
                AddActions(rmfd, new() { "RMFD_12_LONG", "RMFD_06", "RMFD_12", "RMFD_12", "RMFD_19" });
                for (int i = 1; (i <= 18) && (i <= maxPreset); i++)
                {
                    RadioPreset preset = RadioHasPreset(i, radios.Presets[(int)RadioSystem.Radios.COMM1]);
                    if (preset != null)
                    {
                        BuildARC210Preset(cdu, rmfd, preset);
                    }
                    AddAction(rmfd, "RMFD_19");
                }
                AddAction(rmfd, "RMFD_02");
                for (int i = 19; (i <= 25) && (i <= maxPreset); i++)
                {
                    RadioPreset preset = RadioHasPreset(i, radios.Presets[(int)RadioSystem.Radios.COMM1]);
                    if (preset != null)
                    {
                        BuildARC210Preset(cdu, rmfd, preset);
                    }
                    AddAction(rmfd, "RMFD_19");
                }
                AddAction(rmfd, "RMFD_01");
            }

            string defSetting = radios.DefaultSetting[(int)RadioSystem.Radios.COMM1];
            if (!string.IsNullOrEmpty(radios.DefaultSetting[(int)RadioSystem.Radios.COMM1]))
            {
                if (int.TryParse(defSetting, out _))
                {
                    AddActions(ufc, ActionsForString(defSetting), new() { "UFC_COM1" });
                }
                else if (double.TryParse(defSetting, out double val))
                {
                    int val100MHz = (int)(val / 100.0);
                    val -= val100MHz * 100.0;
                    int val010MHz = (int)(val / 10.0);
                    val -= val010MHz * 10.0;
                    int val001MHz = (int)(val);
                    val = Math.Round((val - val001MHz) * 1000.0, 0);
                    int val100KHz = (int)(val / 100.0);
                    val -= val100KHz * 100.0;
                    int val025KHz = (int)(val / 25.0) % 4;
                    AddDynamicAction(arc210, "ARC210_100MHZ_SEL", (double)val100MHz * 0.1, (double)val100MHz * 0.1);
                    AddDynamicAction(arc210, "ARC210_10MHZ_SEL", (double)val010MHz * 0.1, (double)val010MHz * 0.1);
                    AddDynamicAction(arc210, "ARC210_1MHZ_SEL", (double)val001MHz * 0.1, (double)val001MHz * 0.1);
                    AddDynamicAction(arc210, "ARC210_100KHZ_SEL", (double)val100KHz * 0.1, (double)val100KHz * 0.1);
                    AddDynamicAction(arc210, "ARC210_25KHZ_SEL", (double)val025KHz * 0.1, (double)val025KHz * 0.1);
                }
            }

            AddAction(arc210, (radios.IsMonitorGuard[(int)RadioSystem.Radios.COMM1]) ? "ARC210_MASTER_TR_G"
                                                                                     : "ARC210_MASTER_TR");
            AddAction(arc210, (radios.IsPresetMode[(int)RadioSystem.Radios.COMM1]) ? "ARC210_SEC_SW_PRST"
                                                                                   : "ARC210_SEC_SW_MAN");
            if (!radios.IsCOMM1StatusOnHUD)
            {
                AddAction(ufc, "UFC_COM1_LONG");
            }
            // Always hide COM2: it's unimplemented and just HUD clutter.
            AddAction(ufc, "UFC_COM2_LONG");
        }

        /// <summary>
        /// configure the primary ARC-164 UHF radio according to the non-default programming settings (this
        /// function is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// this includes presets, default freq/preset, preset mode, guard monitor, and HUD status display.
        /// </summary>
        private void BuildARC164(AirframeDevice arc164, RadioSystem radios)
        {
            if (radios.Presets[(int)RadioSystem.Radios.COMM2].Count > 0)
            {
                AddActions(arc164, new() { "UHF_COVER_OPEN", "UHF_MODE_PRESET" });
                foreach (RadioPreset preset in radios.Presets[(int)RadioSystem.Radios.COMM2])
                {
                    double presetValue = (double)(preset.Preset - 1) * 0.05;
                    AddDynamicAction(arc164, "UHF_PRESET_SEL", presetValue, presetValue);
                    AddWait(WAIT_BASE);
                    BuildARC164ManualFrequency(arc164, preset.Frequency);
                    AddWait(WAIT_BASE);
                    AddAction(arc164, "UHF_LOAD");
                    AddWait(WAIT_BASE);
                }
                AddAction(arc164, "UHF_COVER_CLOSED");
            }

            string defSetting = radios.DefaultSetting[(int)RadioSystem.Radios.COMM2];
            if (!string.IsNullOrEmpty(radios.DefaultSetting[(int)RadioSystem.Radios.COMM2]))
            {
                if (int.TryParse(defSetting, out int defPreset))
                {
                    AddAction(arc164, "UHF_MODE_PRESET");
                    double presetValue = (double)(defPreset - 1) * 0.05;
                    AddDynamicAction(arc164, "UHF_PRESET_SEL", presetValue, presetValue);
                }
                else
                {
                    BuildARC164ManualFrequency(arc164, defSetting);
                }
            }

            AddAction(arc164, (radios.IsMonitorGuard[(int)RadioSystem.Radios.COMM2]) ? "UHF_FUNCTION_BOTH"
                                                                                     : "UHF_FUNCTION_MAIN");
            AddAction(arc164, (radios.IsPresetMode[(int)RadioSystem.Radios.COMM2]) ? "UHF_MODE_PRESET"
                                                                                   : "UHF_MODE_MNL");
        }

        /// <summary>
        /// configure the primary ARC-186 VHF radio according to the non-default programming settings (this
        /// function is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// this includes presets, default freq/preset, and preset mode.
        /// </summary>
        private void BuildARC186(AirframeDevice arc186, RadioSystem radios)
        {
            if (radios.Presets[(int)RadioSystem.Radios.COMM3].Count > 0)
            {
                // TODO: implement, will need to read radio settings on lua side with while or custom function
                // TODO: that turns wheels enough...
            }
        }

        /// <summary>
        /// returns the preset with the specified number from the list of presets; null if no such preset exists.
        /// </summary>
        private static RadioPreset RadioHasPreset(int presetNum, ObservableCollection<RadioPreset> presets)
        {
            foreach (RadioPreset preset in presets)
            {
                if (preset.Preset == presetNum)
                {
                    return preset;
                }
            }
            return null;
        }

        /// <summary>
        /// returns the maximum preset defined, 0 if no presets are defined.
        /// </summary>
        private static int RadioMaxPreset(ObservableCollection<RadioPreset> presets)
        {
            int max = 0;
            foreach (RadioPreset preset in presets)
            {
                max = Math.Max(preset.Preset, max);
            }
            return max;
        }

        /// <summary>
        /// build the commands to set up the arc-210 preset including the description, frequency, and modulation.
        /// </summary>
        private void BuildARC210Preset(AirframeDevice cdu, AirframeDevice rmfd, RadioPreset preset)
        {
            string descr = preset.Description.ToUpper();
            if (string.IsNullOrEmpty(descr))
            {
                descr = $"SP{preset.Preset}";
            }
            AddActions(cdu, new() { "CLR", "CLR" }, ActionsForString(AdjustOnlyAlphaNum(descr)));
            AddAction(rmfd, "RMFD_16");

            AddActions(cdu, new() { "CLR", "CLR" }, ActionsForString(AdjustNoSeparators(preset.Frequency)));
            AddAction(rmfd, "RMFD_17");

            if (!RadioSystem.IsModulationDefaultForFreq(RadioSystem.Radios.COMM1, preset.Frequency, preset.Modulation))
            {
                AddAction(rmfd, "RMFD_05");
            }
        }

        /// <summary>
        /// build the commands to set up the arc-164 frequency manually.
        /// </summary>
        private void BuildARC164ManualFrequency(AirframeDevice arc164, string freq)
        {
            if (double.TryParse(freq, out double val))
            {
                int val100MHz = (int)(val / 100.0);
                val -= val100MHz * 100.0;
                int val010MHz = (int)(val / 10.0);
                val -= val010MHz * 10.0;
                int val001MHz = (int)(val);
                val = Math.Round((val - val001MHz) * 1000.0, 0);
                int val100KHz = (int)(val / 100.0);
                val -= val100KHz * 100.0;
                int val025KHz = (int)(val / 25.0) % 4;
                AddDynamicAction(arc164, "UHF_100MHZ_SEL", (double)(val100MHz - 2) * 0.1, (double)(val100MHz - 2) * 0.1);
                AddDynamicAction(arc164, "UHF_10MHZ_SEL", (double)val010MHz * 0.1, (double)val010MHz * 0.1);
                AddDynamicAction(arc164, "UHF_1MHZ_SEL", (double)val001MHz * 0.1, (double)val001MHz * 0.1);
                AddDynamicAction(arc164, "UHF_POINT1MHZ_SEL", (double)val100KHz * 0.1, (double)val100KHz * 0.1);
                AddDynamicAction(arc164, "UHF_POINT025_SEL", (double)val025KHz * 0.1, (double)val025KHz * 0.1);
            }
        }
    }
}
