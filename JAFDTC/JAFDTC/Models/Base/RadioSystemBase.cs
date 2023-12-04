// ********************************************************************************************************************
//
// RadioSystemBase.cs : radio system abstract base class
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// abstract base class for a radio system that consists of a set of N radios each of which have M presets (where
    /// M can vary from radio to radio) of type T (where T should be conform to IRadioPresetInfo and is typically
    /// derived from the RadioPresetInfoBase abstract base class).
    /// </summary>
    public abstract class RadioSystemBase<T> : BindableObject, ISystem
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- INotifyPropertyChanged properties

        public ObservableCollection<ObservableCollection<T>> Presets { get; set; }

        // ---- synthesized properties

        [JsonIgnore]
        public virtual bool IsDefault
        {
            get
            {
                foreach (ObservableCollection<T> collection in Presets)
                {
                    if (collection.Count > 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the instance to defaults by removing all presets.
        /// </summary>
        public virtual void Reset()
        {
            foreach (ObservableCollection<T> list in Presets)
            {
                list.Clear();
            }
        }
    }
}
