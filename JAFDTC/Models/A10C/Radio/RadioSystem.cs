// ********************************************************************************************************************
//
// RadioSystem.cs -- a-10c radio system
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
using JAFDTC.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.Models.A10C.Radio
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class RadioSystem : RadioSystemBase<RadioPreset>, ISystem
    {
        public const string SystemTag = "JAFDTC:A10C:RADIO";

        // warthog radios. these values index radio-specific arrays in the radio system. 
        //
        public enum Radios
        {
            COMM1 = 0,              // AN/ARC-210
            COMM2 = 1,              // AN/ARC-164
            COMM3 = 2,              // AN/ARC-186
            NUM_RADIOS = 3
        };

        // supported modulation schemes.
        //
        public enum Modulation
        {
            AM = 0,
            FM = 1,
        };

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public bool IsCOMM1StatusOnHUD { get; set; }

        public bool[] IsMonitorGuard { get; set; }

        public bool[] IsPresetMode { get; set; }

        public string[] DefaultSetting { get; set; }

        // ---- synthesized properties

        public override bool IsDefault
        {
            get
            {
                foreach (ObservableCollection<RadioPreset> presets in Presets)
                {
                    if (presets.Count > 0)
                    {
                        return false;
                    }
                }
                for (int i = 0; i < DefaultSetting.Length; i++)
                {
                    if (!string.IsNullOrEmpty(DefaultSetting[i]) || IsMonitorGuard[i] || IsPresetMode[i])
                    {
                        return false;
                    }
                }
                return !IsCOMM1StatusOnHUD;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public RadioSystem()
        {
            Presets = new ObservableCollection<ObservableCollection<RadioPreset>>
            {
                new(),
                new(),
                new()
            };
            IsCOMM1StatusOnHUD = false;
            IsMonitorGuard = new bool[(int)Radios.NUM_RADIOS] { false, false, false };
            IsPresetMode = new bool[(int)Radios.NUM_RADIOS] { false, false, false };
            DefaultSetting = new string[(int)Radios.NUM_RADIOS] { "", "", "" };
        }

        public RadioSystem(RadioSystem other)
        {
            Presets = new ObservableCollection<ObservableCollection<RadioPreset>>();
            foreach (ObservableCollection<RadioPreset> radio in other.Presets)
            {
                ObservableCollection<RadioPreset> newPresets = new();
                foreach (RadioPreset preset in radio)
                {
                    RadioPreset newPreset = new()
                    {
                        Preset = preset.Preset,
                        Frequency = preset.Frequency,
                        Description = preset.Description,
                        Modulation = preset.Modulation,
                    };
                    newPresets.Add(newPreset);
                }
                Presets.Add(newPresets);
            }
            IsCOMM1StatusOnHUD = other.IsCOMM1StatusOnHUD;
            IsMonitorGuard = new bool[(int)Radios.NUM_RADIOS]
            {
                other.IsMonitorGuard[0],
                other.IsMonitorGuard[1],
                other.IsMonitorGuard[2]
            };
            IsPresetMode = new bool[(int)Radios.NUM_RADIOS]
            {
                other.IsPresetMode[0],
                other.IsPresetMode[1],
                other.IsPresetMode[2]
            };
            DefaultSetting = new string[(int)Radios.NUM_RADIOS]
            {
                new(other.DefaultSetting[0]),
                new(other.DefaultSetting[1]),
                new(other.DefaultSetting[2])
            };
        }

        public virtual object Clone() => new RadioSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the instance to defaults by clearing all presets from all radios.
        /// </summary>
        public override void Reset()
        {
            foreach (ObservableCollection<RadioPreset> radio in Presets)
            {
                radio.Clear();
            }
            IsCOMM1StatusOnHUD = false;
            IsMonitorGuard = new bool[(int)Radios.NUM_RADIOS] { false, false, false };
            IsPresetMode = new bool[(int)Radios.NUM_RADIOS] { false, false, false };
            DefaultSetting = new string[(int)Radios.NUM_RADIOS] { "", "", "" };
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // Radio Parameters
        //
        // ------------------------------------------------------------------------------------------------------------

        public static bool IsFreqKHzMultiple(string freq, int khz)
            => (double.TryParse(freq, out double f) && ((f * 1000.0) / khz) - Math.Floor((f * 1000.0) / khz) < 1.0e-6);

        /// <summary>
        /// return true if the frequency is valid for the specified radio, false if not. passing true for isNoEValid
        /// will treat a null or "" frequency as valid.
        /// </summary>
        public static bool IsFreqValidForRadio(Radios radio, string freq, bool isNoEValid = true)
            => radio switch
            {
                Radios.COMM1 => BindableObject.IsDecimalFieldValid(freq, 225.0, 399.975, isNoEValid) ||
                                BindableObject.IsDecimalFieldValid(freq, 108.0, 173.975, isNoEValid) ||
                                BindableObject.IsDecimalFieldValid(freq, 30.0, 87.975, isNoEValid),
                Radios.COMM2 => BindableObject.IsDecimalFieldValid(freq, 225.0, 399.975, isNoEValid) &&
                                IsFreqKHzMultiple(freq, 25),
                Radios.COMM3 => BindableObject.IsDecimalFieldValid(freq, 30.0, 159.975, isNoEValid) &&
                                IsFreqKHzMultiple(freq, 25),
                _ => false
            };

        public static bool IsModulationDefaultForFreq(Radios radio, string freq, string modulation)
        {
            if (string.IsNullOrEmpty(modulation))
            {
                return true;
            }
            else if (radio == Radios.COMM1)
            {
                if ((BindableObject.IsDecimalFieldValid(freq, 30.0, 89.975, false) ||
                     BindableObject.IsDecimalFieldValid(freq, 156.0, 173.975, false) ||
                     BindableObject.IsDecimalFieldValid(freq, 136.0, 155.975, false)) &&
                    (modulation == ((int)Modulation.FM).ToString()))
                {
                    return true;
                }
                else if ((BindableObject.IsDecimalFieldValid(freq, 108.0, 135.975, false) ||
                          BindableObject.IsDecimalFieldValid(freq, 225.0, 399.975, false)) &&
                         (modulation == ((int)Modulation.AM).ToString()))
                {
                    return true;
                }
            }
            else if (((radio == Radios.COMM2) && (modulation == ((int)Modulation.AM).ToString())) ||
                     ((radio == Radios.COMM3) && (modulation == ((int)Modulation.FM).ToString())))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// return a Modulation[] indicating the valid modulations for a frequency on a radio, null on error. note
        /// the return value is only valid if the frequency is valid and non-empty. element [0] is always the default
        /// modulation.
        /// </summary>
        public static Modulation[] DefaultModulationForFreqOnRadio(Radios radio, string freq)
        {
            if (radio == Radios.COMM1)
            {
                if (BindableObject.IsDecimalFieldValid(freq, 30.0, 89.975, false) ||
                    BindableObject.IsDecimalFieldValid(freq, 156.0, 173.975, false))
                {
                    return new Modulation[1] { Modulation.FM };
                }
                else if (BindableObject.IsDecimalFieldValid(freq, 108.0, 135.975, false))
                {
                    return new Modulation[1] { Modulation.AM };
                }
                else if (BindableObject.IsDecimalFieldValid(freq, 136.0, 155.975, false))
                {
                    return new Modulation[2] { Modulation.FM, Modulation.AM };
                }
                else if (BindableObject.IsDecimalFieldValid(freq, 225.0, 399.975, false))
                {
                    return new Modulation[2] { Modulation.AM, Modulation.FM };
                }
            }
            else if (radio == Radios.COMM2)
            {
                return new Modulation[] { Modulation.AM };
            }
            else if (radio == Radios.COMM3)
            {
                return new Modulation[] { Modulation.FM };
            }
            return null;
        }
    }
}
