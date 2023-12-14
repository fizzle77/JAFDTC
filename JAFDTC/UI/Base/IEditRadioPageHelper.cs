// ********************************************************************************************************************
//
// IEditRadioPageHelper.cs : interface for EditRadioPage helper classes
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

using JAFDTC.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// interface for the EditRadioPage ui page helper class responsible for specializing the EditRadioPage base
    /// behavior for a specific airframe.
    /// </summary>
    public interface IEditRadioPageHelper
    {
        /// <summary>
        /// returns the system tag for the navpoint system.
        /// </summary>
        public string SystemTag { get; }

        /// <summary>
        /// returns a list of names for each of the radios with presets.
        /// </summary>
        public List<string> RadioNames { get; }

        /// <summary>
        /// update the edit state radio presets for the indicated radio along with miscellaneous setup from the
        /// configuration (index within RadioNames specifies the radio). the update will perform a deep copy of
        /// the data from the configuration.
        /// </summary>
        public void CopyConfigToEdit(int radio, IConfiguration config, ObservableCollection<RadioPresetItem> editPresets,
                                     RadioMiscItem editMisc);

        /// <summary>
        /// update the configuration radio presets for the indicaited radio along with miscellaneous setup from the
        /// edit sate from the configuration (index within RadioNames specifies the radio). the update will perform
        /// a deep copy of the data from the configuration.
        /// </summary>
        public void CopyEditToConfig(int radio, ObservableCollection<RadioPresetItem> editPresets,
                                     RadioMiscItem editMisc, IConfiguration config);

        /// <summary>
        /// reset the radio system configuration.
        /// </summary>
        public void RadioSysReset(IConfiguration config);

        /// <summary>
        /// returns true if the radio system in the configuration is currently in a default state.
        /// </summary>
        public bool RadioSysIsDefault(IConfiguration config);

        /// <summary>
        /// returns true if the radio from the radio system in the configuration is currently in a default state.
        /// </summary>
        public bool RadioModuleIsDefault(IConfiguration config, int radio);

        /// <summary>
        /// retruns the string title to use on the indicated radio (specified by index within RadioNames) can for the
        /// "aux 1" miscellaneous checkbox control, null if no such control is desired.
        /// </summary>
        public string RadioAux1Title(int radio);

        /// <summary>
        /// retruns the string title to use on the indicated radio (specified by index within RadioNames) can for the
        /// "aux 2" miscellaneous checkbox control, null if no such control is desired.
        /// </summary>
        public string RadioAux2Title(int radio);

        /// <summary>
        /// returns the number of presets available to the indicated radio (specified by index within RadioNames).
        /// </summary>
        public int RadioMaxPresets(int radio);

        /// <summary>
        /// returns the default frequency for the indicated radio (specified by index within RadioNames).
        /// </summary>
        public string RadioDefaultFrequency(int radio);

        /// <summary>
        /// returns the number of presets currently defined on the indicated radio (specified by index within
        /// RadioNames) in the configuration.
        /// </summary>
        public int RadioPresetCount(int radio, IConfiguration config);

        /// <summary>
        /// returns true if the preset string is valid for the indicated radio (specified by index within
        /// RadioNames).
        /// </summary>
        public bool ValidatePreset(int radio, string preset, bool isNoEValid = true);

        /// <summary>
        /// returns true if the frequency string is valid for the indicated radio (specified by index within
        /// RadioNames).
        /// </summary>
        public bool ValidateFrequency(int radio, string freq, bool isNoEValid = true);

        /// <summary>
        /// TODO: document
        /// </summary>
        public string FixupPreset(int radio, string preset);

        /// <summary>
        /// TODO: document
        /// </summary>
        public string FixupFrequency(int radio, string freq);
    }
}
