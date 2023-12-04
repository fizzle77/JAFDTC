// ********************************************************************************************************************
//
// RadioPresetBase.cs : radio preset abstract base class
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

using JAFDTC.Utilities;

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// abstract base class for a radio preset with basic preset number, frequency, and description fields.
    /// </summary>
    public abstract class RadioPresetInfoBase : BindableObject, IRadioPresetInfo
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- INotifyPropertyChanged, INotifyDataErrorInfo properties

        private int _preset;
        public int Preset
        {
            get => _preset;
            set => SetProperty(ref _preset, value, null);
        }

        private string _frequency;
        public string Frequency
        {
            get => _frequency;
            set => SetProperty(ref _frequency, value, null);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value, null);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // consturction
        //
        // ------------------------------------------------------------------------------------------------------------

        public RadioPresetInfoBase() => (Preset, Frequency, Description) = (1, "", "");

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the preset to default values. the preset number field is set to 1.
        /// </summary>
        public virtual void Reset() => (Preset, Frequency, Description) = (1, "", "");

        /// <summary>
        /// cleanup the preset.
        /// </summary>
        public virtual void CleanUp() { }
    }
}
