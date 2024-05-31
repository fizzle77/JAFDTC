// ********************************************************************************************************************
//
// MunitionSettings.cs -- munition settings for f-16c sms system
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

using JAFDTC.Utilities;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.F16C.SMS
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class MunitionSettings : SystemBase
    {
        /// <summary>
        /// TODO: document
        /// </summary>
        public enum EmploymentModes
        {
            UNKNOWN = -1,
            CCIP = 0,
            CCRP = 1,
            DTOS = 2,
            LADD = 3,
            MAN = 4,
            PRE = 5,
            VIS = 6,
            BORE = 7
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public enum ReleaseModes
        {
            UNKNOWN = -1,
            SGL = 0,
            PAIR = 1,
            SGL_TRI = 2,
            TRI_PAIR_F2B = 3,
            TRI_PAIR_L2R = 4,
            MAV_SGL = 5,
            MAV_PAIR = 6,
            GBU24_SGL = 7,
            GBU24_RP1 = 8,
            GBU24_RP2 = 9,
            GBU24_RP3 = 10,
            GBU24_RP4 = 11,
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public enum FuzeModes
        {
            UNKNOWN = -1,
            NSTL = 0,
            NOSE = 1,
            TAIL = 2,
            NSTL_HI = 3,
            NOSE_LO = 4,
            TAIL_HI = 5
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public enum AutoPowerModes
        {
            UNKNOWN = -1,
            OFF = 0,
            NORTH_OF = 1,
            SOUTH_OF = 2,
            EAST_OF = 3,
            WEST_OF = 4
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // Properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events

        public SMSSystem.Munitions ID { get; set; }

        // ---- following properties post change and validation events

        private string _profile;                                // integer
        public string Profile
        {
            get => _profile;
            set => SetProperty(ref _profile, value, null);
        }

        public string _isProfileSelected;                       // bool
        public string IsProfileSelected
        {
            get => _isProfileSelected;
            set => SetProperty(ref _isProfileSelected, value, null);
        }

        private string _emplMode;                               // integer, enum EmploymentModes
        public string EmplMode
        {
            get => _emplMode;
            set => SetProperty(ref _emplMode, value, null);
        }

        public string _releaseMode;                             // integer, enum ReleaseModes
        public string ReleaseMode
        {
            get => _releaseMode;
            set => SetProperty(ref _releaseMode, value, null);
        }

        private string _ripplePulse;                            // integer [0, 99]
        public string RipplePulse
        {
            get => _ripplePulse;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 0, 99))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _ripplePulse, value, error);
            }
        }

        private string _rippleSpacing;                          // integer cbu103/105 [0, 9999], others [10,999]
        public string RippleSpacing
        {
            get => _rippleSpacing;
            set
            {
                bool isWide = ((ID == SMSSystem.Munitions.CBU_103) || (ID == SMSSystem.Munitions.CBU_105));
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if ((isWide && IsIntegerFieldValid(value, 0, 9999)) ||
                    (!isWide && IsIntegerFieldValid(value, 10, 999)))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _rippleSpacing, value, error);
            }
        }

        private string _rippleDelayMode;                        // TODO
        public string RippleDelayMode
        {
            get => _rippleDelayMode;
            set => SetProperty(ref _rippleDelayMode, value, null);
        }

        private string _fuzeMode;                               // integer, enum FuzeModes
        public string FuzeMode
        {
            get => _fuzeMode;
            set => SetProperty(ref _fuzeMode, value, null);
        }

        private string _armDelay;
        public string ArmDelay
        {
            // TODO: validation?
            get => _armDelay;
            set => SetProperty(ref _armDelay, value, null);
        }

        private string _armDelayMode;                               // TODO
        public string ArmDelayMode
        {
            get => _armDelayMode;
            set => SetProperty(ref _armDelayMode, value, null);
        }

        private string _armDelay2;
        public string ArmDelay2
        {
            // TODO: validation?
            get => _armDelay2;
            set => SetProperty(ref _armDelay2, value, null);
        }

        public string BurstAlt { get; set; }

        public string Spin { get; set; }

        public string ReleaseAng { get; set; }

        private string _impactAng;                              // integer [0, 90]
        public string ImpactAng
        {
            get => _impactAng;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 0, 90))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _impactAng, value, error);
            }
        }

        private string _impactAzi;                              // integer [0, 360]
        public string ImpactAzi
        {
            get => _impactAzi;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 0, 360))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _impactAzi, value, error);
            }
        }

        private string _impactVel;                              // integer [0, 9999]
        public string ImpactVel
        {
            get => _impactVel;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 0, 9999))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _impactVel, value, error);
            }
        }

        public string _cueRange;                                // TODO
        public string CueRange
        {
            // TODO: validate
            get => _cueRange;
            set => SetProperty(ref _cueRange, value, null);
        }


        public string LADDPR { get; set; }                      // TODO: not supported in ui, 0-99900|25000

        public string LADDToF { get; set; }                     // TODO: not supported in ui, 0.00-99.99|28.0

        public string LADDMRA { get; set; }                     // TODO: not supported in ui, 0-99999|1100

        private string _autoPwrMode;
        public string AutoPwrMode                               // integer, enum AutoPowerModes
        {
            get => _autoPwrMode;
            set => SetProperty(ref _autoPwrMode, value, null);
        }

        private string _autoPwrSP;                              // integer [1, 699]
        public string AutoPwrSP
        {
            get => _autoPwrSP;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 1, 699))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _autoPwrSP, value, error);
            }
        }

        // ---- following properties are synthesized

        [JsonIgnore]
        public EmploymentModes EmplModeEnum
            => (string.IsNullOrEmpty(EmplMode)) ? EmploymentModes.UNKNOWN : (EmploymentModes)int.Parse(EmplMode);

        [JsonIgnore]
        public ReleaseModes ReleaseModeEnum
            => (string.IsNullOrEmpty(ReleaseMode)) ? ReleaseModes.UNKNOWN : (ReleaseModes)int.Parse(ReleaseMode);

        [JsonIgnore]
        public FuzeModes FuzeEnum
            => (string.IsNullOrEmpty(FuzeMode)) ? FuzeModes.UNKNOWN : (FuzeModes)int.Parse(FuzeMode);

        [JsonIgnore]
        public AutoPowerModes AutoPwrModeEnum
            => (string.IsNullOrEmpty(AutoPwrMode)) ? AutoPowerModes.UNKNOWN : (AutoPowerModes)int.Parse(AutoPwrMode);

        /// <summary>
        /// returns true if the instance indicates a default setup: either Settings is empty or it contains only
        /// default setups.
        /// </summary>
        [JsonIgnore]
        public override bool IsDefault => (string.IsNullOrEmpty(IsProfileSelected) &&
                                           string.IsNullOrEmpty(EmplMode) &&
                                           string.IsNullOrEmpty(ReleaseMode) &&
                                           string.IsNullOrEmpty(RipplePulse) &&
                                           string.IsNullOrEmpty(RippleDelayMode) &&
                                           string.IsNullOrEmpty(FuzeMode) &&
                                           string.IsNullOrEmpty(ImpactAng) &&
                                           string.IsNullOrEmpty(ImpactAzi) &&
                                           string.IsNullOrEmpty(ImpactVel) &&
                                           string.IsNullOrEmpty(AutoPwrMode) &&
                                           string.IsNullOrEmpty(AutoPwrSP));

        // ------------------------------------------------------------------------------------------------------------
        //
        // Construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MunitionSettings()
        {
            Profile = "";
            Reset();
        }

        public MunitionSettings(MunitionSettings other)
        {
            Profile = new(other.Profile);
            IsProfileSelected = new(other.IsProfileSelected);
            EmplMode = new(other.EmplMode);
            ReleaseMode = new(other.ReleaseMode);
            RipplePulse = new(other.RipplePulse);
            RippleSpacing = new(other.RippleSpacing);
            RippleDelayMode = new(other.RippleDelayMode);
            FuzeMode = new(other.FuzeMode);
            ImpactAng = new(other.ImpactAng);
            ImpactAzi = new(other.ImpactAzi);
            ImpactVel = new(other.ImpactVel);
            AutoPwrMode = new(other.AutoPwrMode);
            AutoPwrSP = new(other.AutoPwrSP);
        }

        public virtual object Clone() => new MunitionSettings(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the instance to defaults. Profile is never reset.
        /// </summary>
        public override void Reset()
        {
            IsProfileSelected = "";
            EmplMode = "";
            ReleaseMode = "";
            RipplePulse = "";
            RippleSpacing = "";
            RippleDelayMode = "";
            FuzeMode = "";
            ImpactAng = "";
            ImpactAzi = "";
            ImpactVel = "";
            AutoPwrMode = "";
            AutoPwrSP = "";
        }
    }
}
