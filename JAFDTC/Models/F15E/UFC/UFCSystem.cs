// ********************************************************************************************************************
//
// UFCSystem.cs -- f-15e ufc system
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

namespace JAFDTC.Models.F15E.UFC
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class UFCSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:F15E:UFC";

        /// <summary>
        /// defines the bands for tacan.
        /// </summary>
        public enum TACANBands
        {
            X = 0,
            Y = 1
        }

        /// <summary>
        /// defines the bands for tacan.
        /// </summary>
        public enum TACANModes
        {
            A2A = 0,
            TR = 1,
            REC = 2
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- private properties, static

        private static readonly Regex _ilsRegex = new(@"^(10[89]\.[13579]{1}[05]{1})|(11[01]\.[13579]{1}[05]{1})$");

        // ---- public properties, posts change/validation events

        // TODO: validate valid range
        private string _lowAltWarn;                             // integer [0, 50000]
        public string LowAltWarn
        {
            get => _lowAltWarn;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 0, 50000))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _lowAltWarn, value, error);
            }
        }

        private string _tacanChannel;                           // integer [1, 126]
        public string TACANChannel
        {
            get => _tacanChannel;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 1, 126))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _tacanChannel, value, error);
            }
        }

        private string _tacanBand;                              // integer [0, 1]
        public string TACANBand
        {
            get => _tacanBand;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _tacanBand, value, error);
            }
        }

        private string _tacanMode;                              // integer [0, 2]
        public string TACANMode
        {
            get => _tacanMode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _tacanMode, value, error);
            }
        }

        private string _ilsFrequency;                           // 000.00 decimal [108.10, 111.95] in 0.05 steps
        public string ILSFrequency
        {
            get => _ilsFrequency;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsRegexFieldValid(value, _ilsRegex))
                {
                    // TODO: need to fix value fixup...
                    value = FixupDecimalField(value, "F2");
                    error = null;
                }
                SetProperty(ref _ilsFrequency, value, error);
            }
        }

        // ---- public properties, computed

        /// <summary>
        /// returns a MFDSystem with the fields populated with the actual default values (note that usually the value
        /// "" implies default).
        ///
        /// defaults are as of DCS v2.9.0.47168.
        /// </summary>
        public readonly static UFCSystem ExplicitDefaults = new()
        {
            LowAltWarn = "250",
            TACANChannel = "1",
            TACANBand = ((int)TACANBands.X).ToString(),
            TACANMode = ((int)TACANModes.TR).ToString(),
            ILSFrequency = "108.10",
        };

        /// <summary>
        /// returns true if the instance indicates a default setup (all fields are "") or the object is in explicit
        /// form, false otherwise.
        /// </summary>
        [JsonIgnore]
        public bool IsDefault => (IsLowAltDefault && IsTACANDefault && IsILSDefault);

        // TODO: technically, could be default with non-empty values...
        [JsonIgnore]
        public bool IsLowAltDefault => string.IsNullOrEmpty(LowAltWarn);

        // TODO: technically, could be default with non-empty values...
        [JsonIgnore]
        public bool IsTACANDefault => (string.IsNullOrEmpty(TACANChannel) &&
                                       string.IsNullOrEmpty(TACANBand) &&
                                       string.IsNullOrEmpty(TACANMode));

        // TODO: technically, could be default with non-empty values...
        [JsonIgnore]
        public bool IsILSDefault => string.IsNullOrEmpty(ILSFrequency);

        // ---- following accessors get the current value (default or non-default) for various properties

        [JsonIgnore]
        public TACANBands TACANBandValue
        {
            get => (TACANBands)int.Parse((string.IsNullOrEmpty(TACANBand)) ? ExplicitDefaults.TACANBand : TACANBand);
        }

        [JsonIgnore]
        public TACANModes TACANModeValue
        {
            get => (TACANModes)int.Parse((string.IsNullOrEmpty(TACANMode)) ? ExplicitDefaults.TACANMode : TACANMode);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public UFCSystem()
        {
            Reset();
        }

        public UFCSystem(UFCSystem other)
        {
            LowAltWarn = new(other.LowAltWarn);
            TACANChannel = new(other.TACANChannel);
            TACANBand = new(other.TACANBand);
            TACANMode = new(other.TACANMode);
            ILSFrequency = new(other.ILSFrequency);
        }

        public virtual object Clone() => new UFCSystem(this);

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
            LowAltWarn = "";
            TACANChannel = "";
            TACANBand = "";
            TACANMode = "";
            ILSFrequency = "";
        }
    }
}
