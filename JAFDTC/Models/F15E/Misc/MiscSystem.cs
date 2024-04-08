// ********************************************************************************************************************
//
// MiscSystem.cs -- f-15e miscellaneous system
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

using JAFDTC.Utilities;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.F15E.Misc
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class MiscSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:F15E:MISC";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties, posts change/validation events

        // TODO: validate valid range
        private string _bingo;                                  // integer [0, 99999]
        public string Bingo
        {
            get => _bingo;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 0, 99999))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _bingo, value, error);
            }
        }

        // ---- public properties, computed

        /// <summary>
        /// returns a MFDSystem with the fields populated with the actual default values (note that usually the value
        /// "" implies default).
        ///
        /// defaults are as of DCS v2.9.0.47168.
        /// </summary>
        public readonly static MiscSystem ExplicitDefaults = new()
        {
            Bingo = "4000"
        };

        /// <summary>
        /// returns true if the instance indicates a default setup (all fields are "") or the object is in explicit
        /// form, false otherwise.
        /// </summary>
        [JsonIgnore]
        public bool IsDefault => (IsBINGODefault);

        // TODO: technically, could be default with non-empty values...
        [JsonIgnore]
        public bool IsBINGODefault => string.IsNullOrEmpty(Bingo);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MiscSystem()
        {
            Reset();
        }

        public MiscSystem(MiscSystem other)
        {
            Bingo = new(other.Bingo);
        }

        public virtual object Clone() => new MiscSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the instance to defaults (by definition, field value of "" implies default).
        /// </summary>
        public void Reset()
        {
            Bingo = "";
        }
    }
}
