﻿// ********************************************************************************************************************
//
// F16CEditRadioPageHelper.cs : viper specialization for EditRadioPage
//
// Copyright(C) 2023-2024 ilominar/raven, JAFDTC contributors
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

using JAFDTC.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.Radio;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// helper class for the generic configuration radio system editor, EditRadioPage. provides support for the uhf
    /// and vhf radios in the viper.
    /// </summary>
    internal class F16CEditRadioPageHelper : RadioPageHelperBase, IEditRadioPageHelper
    {
        public static ConfigEditorPageInfo PageInfo
            => new(RadioSystem.SystemTag, "Radios", "COMM", Glyphs.RADIO, typeof(EditRadioPage), typeof(F16CEditRadioPageHelper));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public string SystemTag => RadioSystem.SystemTag;

        public List<string> RadioNames => new()
        {
            "COM 1 – UHF AN/ARC-164",
            "COM 2 – VHF AN/ARC-222"
        };

        // ------------------------------------------------------------------------------------------------------------
        //
        // IEditRadioPageHelper functions
        //
        // ------------------------------------------------------------------------------------------------------------

        public void CopyConfigToEdit(int radio, IConfiguration config,
                                     ObservableCollection<RadioPresetItem> editPresets, RadioMiscItem editMisc)
        {
            if (radio == (int)Radios.COMM1)
            {
                editMisc.IsAux2Enabled = ((F16CConfiguration)config).Radio.IsCOMM1MonitorGuard;
                editMisc.DefaultTuning = ((F16CConfiguration)config).Radio.COMM1DefaultTuning;
            }
            else if (radio == (int)Radios.COMM2)
            {
                editMisc.DefaultTuning = ((F16CConfiguration)config).Radio.COMM2DefaultTuning;
            }
            editPresets.Clear();
            foreach (RadioPresetInfoBase cfgPreset in ((F16CConfiguration)config).Radio.Presets[radio])
            {
                RadioPresetItem newItem = new(this, 0, radio)
                {
                    Preset = cfgPreset.Preset.ToString(),
                    Frequency = new(cfgPreset.Frequency),
                    Description = new(cfgPreset.Description)
                };
                editPresets.Add(newItem);
            }
        }

        // update the configuration navpoint at the indicaited index from the edit navpoint. the update will perform
        // a deep copy of the navpoint from the configuration.
        //
        public void CopyEditToConfig(int radio, ObservableCollection<RadioPresetItem> editPresets,
                                     RadioMiscItem editMisc, IConfiguration config)
        {
            if (radio == (int)Radios.COMM1)
            {
                ((F16CConfiguration)config).Radio.IsCOMM1MonitorGuard = editMisc.IsAux2Enabled;
                ((F16CConfiguration)config).Radio.COMM1DefaultTuning = editMisc.DefaultTuning;
            }
            else if (radio == (int)Radios.COMM2)
            {
                ((F16CConfiguration)config).Radio.COMM2DefaultTuning = editMisc.DefaultTuning;
            }
            ObservableCollection<RadioPreset> cfgPresetList = ((F16CConfiguration)config).Radio.Presets[radio];
            cfgPresetList.Clear();
            foreach (RadioPresetItem item in editPresets)
            {
                RadioPreset preset = new()
                {
                    Preset = int.Parse(item.Preset),
                    Frequency = new(item.Frequency),
                    Description = new(item.Description),
                    Modulation = ""
                };
                cfgPresetList.Add(preset);
            }
        }

        public void RadioSysReset(IConfiguration config)
            => ((F16CConfiguration)config).Radio.Reset();

        public bool RadioModuleIsDefault(IConfiguration config, int radio)
            => radio switch
            {
                (int)Radios.COMM1 => ((((F16CConfiguration)config).Radio.Presets[(int)Radios.COMM1].Count == 0) &&
                                      !((F16CConfiguration)config).Radio.IsCOMM1MonitorGuard &&
                                      string.IsNullOrEmpty(((F16CConfiguration)config).Radio.COMM1DefaultTuning)),
                (int)Radios.COMM2 => ((((F16CConfiguration)config).Radio.Presets[(int)Radios.COMM2].Count == 0) &&
                                      string.IsNullOrEmpty(((F16CConfiguration)config).Radio.COMM2DefaultTuning)),
                _ => false
            };

        public bool RadioSysIsDefault(IConfiguration config)
            => ((F16CConfiguration)config).Radio.IsDefault;

        public override string RadioAux2Title(int radio)
            => (radio == (int)Radios.COMM1) ? "Monitor Guard" : null;

        public override int RadioMaxPresets(int radio) => 20;

        public string RadioDefaultFrequency(int radio)
            => (radio == (int)Radios.COMM1) ? "225.00" : "108.00";

        public int RadioPresetCount(int radio, IConfiguration config)
            => ((F16CConfiguration)config).Radio.Presets[radio].Count;

        public override bool ValidateFrequency(int radio, string freq, bool isNoEValid = true)
            => radio switch
            {
                // TODO: valid freqs are discrete, not continuous, need to check that as well...
                (int)Radios.COMM1 => BindableObject.IsDecimalFieldValid(freq, 225.0, 399.975, isNoEValid),
                (int)Radios.COMM2 => (BindableObject.IsDecimalFieldValid(freq, 108.0, 115.975, isNoEValid) ||
                                      BindableObject.IsDecimalFieldValid(freq, 116.0, 151.975, isNoEValid) ||
                                      BindableObject.IsDecimalFieldValid(freq, 30.0, 87.975, isNoEValid)),
                _ => false,
            };

        public override string FixupFrequency(int radio, string freq)
            => BindableObject.FixupDecimalField(freq, "F2");
    }
}
