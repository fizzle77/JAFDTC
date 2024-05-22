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
    public class MunitionSettings : BindableObject, ISystem
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
        public bool IsDefault =>
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
                if (string.IsNullOrEmpty(DeliveryMode) || DeliveryMode == "-1")
                    return true;
                if (_munition == null)
                    return true;
                if (_munition.CCIP ^ _munition.CCRP)
                {
                    // For the weapons that only allow one of CCIP or CCRP, ensure the one they allow is treated as default.
                    if (_munition.CCIP && DeliveryMode == ((int)DeliveryModes.CCIP).ToString())
                        return true;
                    if (_munition.CCRP && DeliveryMode == ((int)DeliveryModes.CCRP).ToString())
                        return true;
                }
                else
                {
                    // For weapons that support both, CCIP is the default, specified in ExplicitDefaults.
                    return DeliveryMode == ExplicitDefaults.DeliveryMode;
                }
                return false;
            }
        }
        [JsonIgnore]
        public bool IsEscapeManeuverDefault => string.IsNullOrEmpty(EscapeManeuver) || EscapeManeuver == ExplicitDefaults.EscapeManeuver || EscapeManeuver == "-1";
        [JsonIgnore]
        public bool IsReleaseModeDefault => string.IsNullOrEmpty(ReleaseMode) || ReleaseMode == ExplicitDefaults.ReleaseMode || ReleaseMode == "-1";
        [JsonIgnore]
        public bool IsRippleQtyDefault => string.IsNullOrEmpty(RippleQty) || RippleQty == ExplicitDefaults.RippleQty;
        [JsonIgnore]
        public bool IsRippleFtDefault => string.IsNullOrEmpty(RippleFt) || RippleFt == ExplicitDefaults.RippleFt;
        [JsonIgnore]
        public bool IsHOFOptionDefault => string.IsNullOrEmpty(HOFOption) || HOFOption == ExplicitDefaults.HOFOption || HOFOption == "-1";
        [JsonIgnore]
        public bool IsRPMOptionDefault => string.IsNullOrEmpty(RPMOption) || RPMOption == ExplicitDefaults.RPMOption || RPMOption == "-1";
        [JsonIgnore]
        public bool IsFuzeOptionDefault => string.IsNullOrEmpty(FuzeOption) || FuzeOption == ExplicitDefaults.FuzeOption || FuzeOption == "-1";

        public readonly static MunitionSettings ExplicitDefaults = new()
        {
            AutoLase = "False",
            LaseSeconds = "0",
            DeliveryMode = "0", // CCIP
            EscapeManeuver = "1", // CLB
            ReleaseMode = "0", // SGL
            RippleQty = "1",
            RippleFt = "75",
            HOFOption = "6", // 1800
            RPMOption = "3", // 1500
            FuzeOption = "0" // N/T
        };

        private MunitionSettings()
        {
            Reset();
        }

        public MunitionSettings(A10CMunition munition) : this()
        {
            _munition = munition;
        }

        public void Reset()
        {
            AutoLase = "";
            LaseSeconds = "";
            DeliveryMode = "";
            EscapeManeuver = "";
            ReleaseMode = "";
            RippleQty = "";
            RippleFt = "";
            HOFOption = "";
            RPMOption = "";
            FuzeOption = "";
        }
    }
}
