// ********************************************************************************************************************
//
// RadioSystem.cs -- f-16c radio system
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023-2025 ilominar/raven
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
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace JAFDTC.Models.F16C.Radio
{
    public enum Radios
    {
        COMM1 = 0,
        COMM2 = 1,
    };

    /// <summary>
    /// TODO: document
    /// </summary>
    public class RadioSystem : RadioSystemBase<RadioPreset>, ISystem
    {
        public const string SystemTag = "JAFDTC:F16C:RADIO";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public bool IsCOMM1MonitorGuard { get; set; }

        public string COMM1DefaultTuning { get; set; }

        public string COMM2DefaultTuning { get; set; }

        // ---- synthesized properties

        public override bool IsDefault
        {
            get
            {
                foreach (ObservableCollection<RadioPreset> presets in Presets)
                    if (presets.Count > 0)
                        return false;
                return !IsCOMM1MonitorGuard &&
                       string.IsNullOrEmpty(COMM1DefaultTuning) && string.IsNullOrEmpty(COMM2DefaultTuning);
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
                new()
            };
            IsCOMM1MonitorGuard = false;
            COMM1DefaultTuning = "";
            COMM2DefaultTuning = "";
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
                    };
                    newPresets.Add(newPreset);
                }
                Presets.Add(newPresets);
            }
            IsCOMM1MonitorGuard = other.IsCOMM1MonitorGuard;
            COMM1DefaultTuning = new(other.COMM1DefaultTuning);
            COMM2DefaultTuning = new(other.COMM2DefaultTuning);
        }

        public virtual object Clone() => new RadioSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// merge the presets for the given radio in the dcs dtc dom. we will only change the frequence in the dcs dtc
        /// as the modulation for the viper is fixed and can't change from what is already in the dtc.
        /// </summary>
        private static void MergeRadioIntoSimDTC(JsonNode radioRoot, ObservableCollection<RadioPreset> presets)
        {
            foreach (RadioPreset preset in presets)
            {
                JsonNode dtcPreset = radioRoot[$"Channel_{preset.Preset}"];
                if (double.TryParse(preset.Frequency, out double freq))
                    dtcPreset["freq"] = Math.Truncate(freq * 1000.0) / 1000.0;
            }
        }

        /// <summary>
        /// merge radio settings into dcs dtc configuration.
        /// </summary>
        public override void MergeIntoSimDTC(JsonNode dataRoot)
        {
            JsonNode commRoot = dataRoot["COMM"];
            MergeRadioIntoSimDTC(commRoot["COMM1"], Presets[(int)Radios.COMM1]);
            MergeRadioIntoSimDTC(commRoot["COMM2"], Presets[(int)Radios.COMM2]);
        }

        /// <summary>
        /// reset the instance to defaults by clearing all presets from all radios.
        /// </summary>
        public override void Reset()
        {
            foreach (ObservableCollection<RadioPreset> radio in Presets)
                radio.Clear();
            IsCOMM1MonitorGuard = false;
            COMM1DefaultTuning = "";
            COMM2DefaultTuning = "";
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public bool ImportFromCSV(Radios radio, string filename)
        {
            // TODO: implement radio import
            return true;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public bool ExportToCSV(Radios radio, string filename)
        {
            // TODO: implement radio export
            return true;
        }
    }
}
