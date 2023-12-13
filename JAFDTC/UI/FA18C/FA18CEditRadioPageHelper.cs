// ********************************************************************************************************************
//
// FA18CEditRadioPageHelper.xaml.cs : hornet specialization for EditRadioPage
//
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
using JAFDTC.Models.FA18C;
using JAFDTC.Models.FA18C.Radio;
using JAFDTC.Models;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.UI.FA18C
{
    /// <summary>
    /// helper class for the generic configuration radio system editor, EditRadioPage. provides support for the
    /// uhf/vhf radios in the hornet.
    /// </summary>
    internal class FA18CEditRadioPageHelper : IEditRadioPageHelper
    {
        public static ConfigEditorPageInfo PageInfo
            => new(RadioSystem.SystemTag, "Radios", "COMM", Glyphs.RADIO, typeof(EditRadioPage), typeof(FA18CEditRadioPageHelper));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public string SystemTag => RadioSystem.SystemTag;

        public List<string> RadioNames => new()
        {
            "COM 1 – UHF/VHF AN/ARC-210",
            "COM 2 – UHF/VHF AN/ARC-210"
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
                editMisc.DefaultTuning = ((FA18CConfiguration)config).Radio.COMM1DefaultTuning;
            }
            else if (radio == (int)Radios.COMM2)
            {
                editMisc.DefaultTuning = ((FA18CConfiguration)config).Radio.COMM2DefaultTuning;
            }
            editPresets.Clear();
            foreach (RadioPresetInfoBase cfgPreset in ((FA18CConfiguration)config).Radio.Presets[radio])
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
                ((FA18CConfiguration)config).Radio.COMM1DefaultTuning = editMisc.DefaultTuning;
            }
            else if (radio == (int)Radios.COMM2)
            {
                ((FA18CConfiguration)config).Radio.COMM2DefaultTuning = editMisc.DefaultTuning;
            }
            ObservableCollection<RadioPreset> cfgPresetList = ((FA18CConfiguration)config).Radio.Presets[radio];
            cfgPresetList.Clear();
            foreach (RadioPresetItem item in editPresets)
            {
                RadioPreset preset = new()
                {
                    Preset = int.Parse(item.Preset),
                    Frequency = new(item.Frequency),
                    Description = new(item.Description),
                };
                cfgPresetList.Add(preset);
            }
        }

        public void RadioSysReset(IConfiguration config)
        {
            ((FA18CConfiguration)config).Radio.Reset();
        }

        public bool RadioSysIsDefault(IConfiguration config)
        {
            return ((FA18CConfiguration)config).Radio.IsDefault;
        }

        public bool RadioCanMonitorGuard(int radio)
        {
            return false;
        }

        public int RadioMaxPresets(int radio)
        {
            return 20;
        }

        public string RadioDefaultFrequency(int radio)
        {
            return "225.00";
        }

        public int RadioPresetCount(int radio, IConfiguration config)
        {
            return ((FA18CConfiguration)config).Radio.Presets[radio].Count;
        }

        public bool ValidatePreset(int radio, string preset, bool isNoEValid = true)
        {
            return BindableObject.IsIntegerFieldValid(preset, 1, 20, isNoEValid);
        }

        public bool ValidateFrequency(int radio, string freq, bool isNoEValid = true)
        {
            // TODO: legal freqencies are more complex than >30, <400. fix this at some point...
            return radio switch
            {
                (int)Radios.COMM1 => BindableObject.IsDecimalFieldValid(freq, 30.0, 400.0, isNoEValid),
                (int)Radios.COMM2 => BindableObject.IsDecimalFieldValid(freq, 30.0, 400.0, isNoEValid),
                _ => false,
            };
        }

        public string FixupPreset(int radio, string preset)
        {
            return BindableObject.FixupIntegerField(preset);
        }

        public string FixupFrequency(int radio, string freq)
        {
            return BindableObject.FixupDecimalField(freq, "F3");
        }
    }
}
