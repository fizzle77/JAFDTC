// ********************************************************************************************************************
//
// A10CEditRadioPageHelper.cs : viper specialization for EditRadioPage
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
using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.Radio;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using static JAFDTC.Models.A10C.Radio.RadioSystem;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// helper class for the generic configuration radio system editor, EditRadioPage. provides support for the uhf
    /// and vhf radios in the viper.
    /// </summary>
    internal class A10CEditRadioPageHelper : RadioPageHelperBase, IEditRadioPageHelper
    {
        public static ConfigEditorPageInfo PageInfo
            => new(RadioSystem.SystemTag, "Radios", "COMM", Glyphs.RADIO, typeof(EditRadioPage), typeof(A10CEditRadioPageHelper));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public string SystemTag => RadioSystem.SystemTag;

        public List<string> RadioNames => new()
        {
            "COM 1 – UHF/VHF AN/ARC-210",
            "COM 2 – UHF AN/ARC-164",
            //
            // TODO: enable com 3 once we have support for pushing settings to jet
            //
            // "COM 3 – UHF AN/ARC-186",
        };

        // ------------------------------------------------------------------------------------------------------------
        //
        // IEditRadioPageHelper functions
        //
        // ------------------------------------------------------------------------------------------------------------

        public void CopyConfigToEdit(int radio, IConfiguration config,
                                     ObservableCollection<RadioPresetItem> editPresets, RadioMiscItem editMisc)
        {
            editMisc.DefaultTuning = ((A10CConfiguration)config).Radio.DefaultSetting[radio];
            editMisc.IsAux1Enabled = ((A10CConfiguration)config).Radio.IsPresetMode[radio];
            if (radio == (int)RadioSystem.Radios.COMM1)
            {
                editMisc.IsAux2Enabled = ((A10CConfiguration)config).Radio.IsMonitorGuard[radio];
                editMisc.IsAux3Enabled = ((A10CConfiguration)config).Radio.IsCOMM1StatusOnHUD;
            }
            else if (radio == (int)RadioSystem.Radios.COMM2)
            {
                editMisc.IsAux2Enabled = ((A10CConfiguration)config).Radio.IsMonitorGuard[radio];
            }

            editPresets.Clear();
            foreach (RadioPresetInfoBase cfgPreset in ((A10CConfiguration)config).Radio.Presets[radio])
            {
                RadioPresetItem newItem = new(this, 0, radio)
                {
                    Preset = cfgPreset.Preset.ToString(),
                    Frequency = new(cfgPreset.Frequency),
                    Description = new(cfgPreset.Description),
                    Modulation = cfgPreset.Modulation
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
            ((A10CConfiguration)config).Radio.DefaultSetting[radio] = editMisc.DefaultTuning;
            ((A10CConfiguration)config).Radio.IsPresetMode[radio] = editMisc.IsAux1Enabled;
            if (radio == (int)RadioSystem.Radios.COMM1)
            {
                ((A10CConfiguration)config).Radio.IsMonitorGuard[radio] = editMisc.IsAux2Enabled;
                ((A10CConfiguration)config).Radio.IsCOMM1StatusOnHUD = editMisc.IsAux3Enabled;
            }
            else if (radio == (int)RadioSystem.Radios.COMM2)
            {
                ((A10CConfiguration)config).Radio.IsMonitorGuard[radio] = editMisc.IsAux2Enabled;
            }

            ObservableCollection<RadioPreset> cfgPresetList = ((A10CConfiguration)config).Radio.Presets[radio];
            cfgPresetList.Clear();
            foreach (RadioPresetItem item in editPresets)
            {
                RadioPreset preset = new()
                {
                    Preset = int.Parse(item.Preset),
                    Frequency = new(item.Frequency),
                    Description = new(item.Description),
                    Modulation = item.Modulation
                };
                cfgPresetList.Add(preset);
            }
        }

        public void RadioSysReset(IConfiguration config)
            => ((A10CConfiguration)config).Radio.Reset();

        public bool RadioModuleIsDefault(IConfiguration config, int radio)
            => radio switch
            {
                (int)Radios.COMM1 => ((((A10CConfiguration)config).Radio.Presets[radio].Count == 0) &&
                                      !((A10CConfiguration)config).Radio.IsPresetMode[radio] &&
                                      !((A10CConfiguration)config).Radio.IsMonitorGuard[radio] &&
                                      ((A10CConfiguration)config).Radio.IsCOMM1StatusOnHUD &&
                                      string.IsNullOrEmpty(((A10CConfiguration)config).Radio.DefaultSetting[radio])),
                (int)Radios.COMM2 => ((((A10CConfiguration)config).Radio.Presets[radio].Count == 0) &&
                                      !((A10CConfiguration)config).Radio.IsPresetMode[radio] &&
                                      !((A10CConfiguration)config).Radio.IsMonitorGuard[radio] &&
                                      string.IsNullOrEmpty(((A10CConfiguration)config).Radio.DefaultSetting[radio])),
                (int)Radios.COMM3 => ((((A10CConfiguration)config).Radio.Presets[(int)Radios.COMM3].Count == 0) &&
                                      !((A10CConfiguration)config).Radio.IsPresetMode[radio] &&
                                      string.IsNullOrEmpty(((A10CConfiguration)config).Radio.DefaultSetting[radio])),
                _ => false
            };

        public bool RadioSysIsDefault(IConfiguration config)
            => ((A10CConfiguration)config).Radio.IsDefault;

        public override string RadioAux1Title(int radio)
            => radio switch
            {
                (int)RadioSystem.Radios.COMM1 => "Preset Mode",
                (int)RadioSystem.Radios.COMM2 => "Preset Mode",
                (int)RadioSystem.Radios.COMM3 => "Preset Mode",
                _ => null
            };

        public override string RadioAux2Title(int radio)
            => radio switch
            {
                (int)RadioSystem.Radios.COMM1 => "Monitor Guard",
                (int)RadioSystem.Radios.COMM2 => "Monitor Guard",
                _ => null
            };

        public override string RadioAux3Title(int radio)
            => radio switch
            {
                (int)RadioSystem.Radios.COMM1 => "COM1 on HUD",
                _ => null
            };

        public override bool RadioCanProgramModulation(int radio)
            => (radio == (int)RadioSystem.Radios.COMM1);

        public override List<TextBlock> RadioModulationItems(int radio, string freq)
        {
            if (radio == (int)RadioSystem.Radios.COMM1)
            {
                Modulation[] mods = DefaultModulationForFreqOnRadio((RadioSystem.Radios)radio, freq);
                if (mods != null)
                {
                    List<TextBlock> result = new List<TextBlock>();
                    for (int i = 0; i < mods.Length; i++)
                    {
                        result.Add(new TextBlock() { Text = mods[i].ToString(), Tag = ((int)mods[i]).ToString() });
                    }
                    return result;
                }
            }
            return null;
        }

        public override int RadioMaxPresets(int radio)
            => (radio == (int)RadioSystem.Radios.COMM1) ? 25 : 20;

        public string RadioDefaultFrequency(int radio)
            => radio switch
            {
                (int)RadioSystem.Radios.COMM1 => "251.000",
                (int)RadioSystem.Radios.COMM2 => "305.000",
                (int)RadioSystem.Radios.COMM3 => "30.000",
                _ => "100.000"
            };

        public int RadioPresetCount(int radio, IConfiguration config)
            => ((A10CConfiguration)config).Radio.Presets[radio].Count;
    }
}
