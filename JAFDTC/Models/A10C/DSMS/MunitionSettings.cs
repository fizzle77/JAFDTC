// ********************************************************************************************************************
//
// MunitionSettings.cs -- munition settings for a-10c dsms system
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023 ilominar/raven
// Copyright(C) 2024 fizzle
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
using System.Text.Json.Serialization;

namespace JAFDTC.Models.A10C.DSMS
{
    // defines the possible delivery mode settings
    // must match combobox order in ui
    public enum DeliveryModes
    {
        CCIP = 0,
        CCRP = 1
    }

    // defines the possible escape maneuver settings
    // must match combobox order in ui
    public enum EscapeManeuvers
    {
        NONE = 0,
        CLB = 1, // jet default
        TRN = 2,
        TLT = 3
    }

    // defines the possible release mode settings
    // must match combobox order in ui
    public enum ReleaseModes
    {
        SGL = 0, // jet default
        PRS = 1,
        RIP_SGL = 2,
        RIP_PRS = 3
    }

    // defines the possible height of function (HOF) settings
    // must match combobox order in ui
    public enum HOFOptions
    {
        HOF_300 = 0,
        HOF_500 = 1,
        HOF_700 = 2,
        HOF_900 = 3,
        HOF_1200 = 4,
        HOF_1500 = 5,
        HOF_1800 = 6, // jet default
        HOF_2200 = 7,
        HOF_2600 = 8,
        HOF_3000 = 9
    }

    // defines the RPM settings
    // must match combobox order in ui
    public enum RPMOptions
    {
        RPM_0 = 0,
        RPM_500 = 1,
        RPM_1000 = 2,
        RPM_1500 = 3, // jet default
        RPM_2000 = 4,
        RPM_2500 = 5
    }

    // defines the Fuze settings
    // must match combobox order in ui
    public enum FuzeOptions
    {
        NoseTail = 0, // jet default
        Nose = 1,
        Tail = 2
    }

    public class MunitionSettings : A10CSystemBase
    {
        private string _autoLase;                              // string (boolean)
        public string AutoLase
        {
            get => _autoLase;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : "Invalid format";
                SetProperty(ref _autoLase, value, error);
            }
        }

        private string _laseSeconds;                          // integer [0..99]
        public string LaseSeconds
        {
            get => _laseSeconds;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 99)) ? null : "Invalid format";
                SetProperty(ref _laseSeconds, value, error);
            }
        }

        private string _deliveryMode;                          // integer [0, 1]
        public string DeliveryMode
        {
            get => _deliveryMode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _deliveryMode, value, error);
            }
        }

        private string _escapeManeuver;                        // integer [0..3]
        public string EscapeManeuver
        {
            get => _escapeManeuver;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, -1, 3)) ? null : "Invalid format";
                SetProperty(ref _escapeManeuver, value, error);
            }
        }

        private string _releaseMode;                          // integer [0..3]
        public string ReleaseMode
        {
            get => _releaseMode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 3)) ? null : "Invalid format";
                SetProperty(ref _releaseMode, value, error);
            }
        }

        private string _rippleQty;                          // integer [1..99]
        public string RippleQty
        {
            get => _rippleQty;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 99)) ? null : "Invalid format";
                SetProperty(ref _rippleQty, value, error);
            }
        }

        private string _rippleFt;                          // integer [10..990]
        public string RippleFt
        {
            get => _rippleFt;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 10, 990)) ? null : "Invalid format";
                SetProperty(ref _rippleFt, value, error);
            }
        }

        private string _HOFOption;                          // integer [-1..9] -1 indicates no selection
        public string HOFOption
        {
            get => _HOFOption;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, -1, 9)) ? null : "Invalid format";
                SetProperty(ref _HOFOption, value, error);
            }
        }

        private string _RPMOption;                          // integer [-1..5] -1 indicates no selection
        public string RPMOption
        {
            get => _RPMOption;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, -1, 5)) ? null : "Invalid format";
                SetProperty(ref _RPMOption, value, error);
            }
        }

        private string _FuzeOption;                          // integer [-1..2] -1 indicates no selection
        public string FuzeOption
        {
            get => _FuzeOption;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, -1, 2)) ? null : "Invalid format";
                SetProperty(ref _FuzeOption, value, error);
            }
        }

        [JsonIgnore]
        public override bool IsDefault =>
            IsAutoLaseDefault && IsLaseSecondsDefault && IsDeliveryModeDefault && IsEscapeManeuverDefault &&
            IsReleaseModeDefault && IsHOFOptionDefault && IsRPMOptionDefault && IsFuzeOptionDefault;

        /// <summary>
        /// Does this configuration require changes on the DSMS INV page?
        /// </summary>
        [JsonIgnore]
        public bool IsInvDefault =>
            IsHOFOptionDefault && IsRPMOptionDefault;

        /// <summary>
        /// Does this configuration require changes to the DSMS Profile?
        /// </summary>
        [JsonIgnore]
        public bool IsProfileDefault =>
            IsAutoLaseDefault && IsLaseSecondsDefault && IsDeliveryModeDefault && IsEscapeManeuverDefault &&
            IsReleaseModeDefault && IsFuzeOptionDefault;

        /// <summary>
        /// Does this configuration require changes to the "change settings" page of the DSMS Profile?
        /// </summary>
        [JsonIgnore]
        public bool IsProfileCHGSETDefault =>
            IsAutoLaseDefault && IsLaseSecondsDefault && IsEscapeManeuverDefault;

        [JsonIgnore]
        public A10CMunition Munition
        {
            get => _munition;
            set => _munition = value;
        }
        private A10CMunition _munition;

        [JsonIgnore]
        public bool IsAutoLaseDefault => string.IsNullOrEmpty(AutoLase) || AutoLase == ExplicitDefaults.AutoLase;
        [JsonIgnore]
        public bool IsLaseSecondsDefault => string.IsNullOrEmpty(LaseSeconds) || LaseSeconds == ExplicitDefaults.LaseSeconds;
        [JsonIgnore]
        public bool IsDeliveryModeDefault 
        {
            get
            {
                if (string.IsNullOrEmpty(DeliveryMode))
                    return true;
                if (_munition == null)
                    return true;
                return DeliveryMode == DefaultDeliveryMode;
            }
        }
        [JsonIgnore]
        public bool IsEscapeManeuverDefault => string.IsNullOrEmpty(EscapeManeuver) || EscapeManeuver == ExplicitDefaults.EscapeManeuver;
        [JsonIgnore]
        public bool IsReleaseModeDefault => string.IsNullOrEmpty(ReleaseMode) || ReleaseMode == ExplicitDefaults.ReleaseMode;
        [JsonIgnore]
        public bool IsRippleQtyDefault => string.IsNullOrEmpty(RippleQty) || RippleQty == ExplicitDefaults.RippleQty;
        [JsonIgnore]
        public bool IsRippleFtDefault => string.IsNullOrEmpty(RippleFt) || RippleFt == ExplicitDefaults.RippleFt;
        [JsonIgnore]
        public bool IsHOFOptionDefault => string.IsNullOrEmpty(HOFOption) || HOFOption == ExplicitDefaults.HOFOption;
        [JsonIgnore]
        public bool IsRPMOptionDefault => string.IsNullOrEmpty(RPMOption) || RPMOption == ExplicitDefaults.RPMOption;
        [JsonIgnore]
        public bool IsFuzeOptionDefault => string.IsNullOrEmpty(FuzeOption) || FuzeOption == ExplicitDefaults.FuzeOption;

        [JsonIgnore]
        public string DefaultDeliveryMode
        {
            get
            {
                if (_munition == null || _munition.CCIP)
                    return "0";
                return "1";
            }
        }

        public readonly static MunitionSettings ExplicitDefaults = new()
        {
            AutoLase = "False",
            LaseSeconds = "0",
            DeliveryMode = "",
            EscapeManeuver = "1",   // CLB
            ReleaseMode = "0",      // SGL
            RippleQty = "1",
            RippleFt = "75",
            HOFOption = "6",        // 1800
            RPMOption = "3",        // 1500
            FuzeOption = "0"        // N/T
        };

        private MunitionSettings()
        {
            Reset();
        }

        public MunitionSettings(A10CMunition munition) : this()
        {
            _munition = munition;
        }

        public override void Reset()
        {
            AutoLase = "False";
            LaseSeconds = "0";
            DeliveryMode = "";
            EscapeManeuver = "1";   // CLB
            ReleaseMode = "0";      // SGL
            RippleQty = "1";
            RippleFt = "75";
            HOFOption = "6";        // 1800
            RPMOption = "3";        // 1500
            FuzeOption = "0";       // N/T
        }
    }
}
