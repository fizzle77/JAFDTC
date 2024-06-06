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

using JAFDTC.Models.A10C.HMCS;
using JAFDTC.Utilities;
using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F16C.SMS
{
    /// <summary>
    /// munition settings accessible through the viper sms system in the jet. this class encodes a super set of all
    /// possible parameters; a given munition will not use (or set) all of the parameters.
    /// </summary>
    public class MunitionSettings : SystemBase
    {
        /// <summary>
        /// munition employment profiles for a2g weapons. a given munition will support a subset of these modes.
        /// by convention, use PROF1 for munitions that do not have explicit profiles (like JDAMs).
        /// </summary>
        public enum Profiles
        {
            Unknown = -1,
            PROF1 = 0,                                          // used for munitions without explicit profiles
            PROF2 = 1,
            PROF3 = 2,
            PROF4 = 3
        }

        /// <summary>
        /// munition employment methods for a2g weapons. a given munition will support a subset of these modes.
        /// </summary>
        public enum EmploymentModes
        {
            Unknown = -1,
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
        /// munition release methods for a2g weapons. a given munition will support a subset of these modes.
        /// </summary>
        public enum ReleaseModes
        {
            Unknown = -1,
            SGL = 0,
            PAIR = 1,
            TRI_SGL = 2,
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
        /// munition fuze methods for a2g weapons. a given munition will support a subset of these modes.
        /// </summary>
        public enum FuzeModes
        {
            Unknown = -1,
            NSTL = 0,
            NOSE = 1,
            TAIL = 2,
            NSTL_HI = 3,
            NOSE_LO = 4,
            TAIL_HI = 5
        }

        /// <summary>
        /// munition auto power modes for a2g weapons. a given munition will support a subset of these modes.
        /// </summary>
        public enum AutoPowerModes
        {
            Unknown = -1,
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

        private string _profile;                                // integer, enum Profiles
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

        private string _rippleDelayMode;                        // string ripple delay value (ms)
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

        private string _armDelay;                               // decimal [0.0, 99.99]
        public string ArmDelay
        {
            get => _armDelay;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsDecimalFieldValid(value, 0.0, 99.99))
                {
                    value = FixupDecimalField(value, "F2");
                    error = null;
                }
                SetProperty(ref _armDelay, value, error);
            }
        }

        private string _armDelayMode;                           // string arm delay value
        public string ArmDelayMode
        {
            get => _armDelayMode;
            set => SetProperty(ref _armDelayMode, value, null);
        }

        private string _armDelay2;                              // decimal [0.00, 99.99]
        public string ArmDelay2
        {
            get => _armDelay2;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsDecimalFieldValid(value, 0.0, 99.99))
                {
                    value = FixupDecimalField(value, "F2");
                    error = null;
                }
                SetProperty(ref _armDelay2, value, error);
            }
        }

        private string _burstAlt;                                // integer cbu87/97 [0, 99999], others [300, 3000]
        public string BurstAlt
        {
            get => _burstAlt;
            set
            {
                bool isWide = ((ID == SMSSystem.Munitions.CBU_87) || (ID == SMSSystem.Munitions.CBU_97));
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if ((isWide && IsIntegerFieldValid(value, 0, 99999)) ||
                    (!isWide && IsIntegerFieldValid(value, 300, 3000)))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _burstAlt, value, error);
            }
        }

        public string Spin { get; set; }                        // string rpm value

        private string _releaseAng;                             // integer gbu24 [-45, 10], others [0, 45]
        public string ReleaseAng
        {
            get => _releaseAng;
            set
            {
                bool isNeg = (ID == SMSSystem.Munitions.GBU_24);
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if ((isNeg && IsIntegerFieldValid(value, -45, 10)) ||
                    (!isNeg && IsIntegerFieldValid(value, 0, 45)))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _releaseAng, value, error);
            }
        }

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

        public string _cueRange;                                // decimal [0.000, 98.999]
        public string CueRange
        {
            get => _cueRange;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsDecimalFieldValid(value, 0.0, 99.99))
                {
                    value = FixupDecimalField(value, "F3");
                    error = null;
                }
                SetProperty(ref _cueRange, value, error);
            }
        }

        public string LADDPR { get; set; }                      // TODO: not supported in ui, 0-99900|25000

        public string LADDToF { get; set; }                     // TODO: not supported in ui, 0.00-99.99|28.0

        public string LADDMRA { get; set; }                     // TODO: not supported in ui, 0-99999|1100

        private string _autoPwrMode;                            // integer, enum AutoPowerModes
        public string AutoPwrMode
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
            => (string.IsNullOrEmpty(EmplMode)) ? EmploymentModes.Unknown : (EmploymentModes)int.Parse(EmplMode);

        [JsonIgnore]
        public ReleaseModes ReleaseModeEnum
            => (string.IsNullOrEmpty(ReleaseMode)) ? ReleaseModes.Unknown : (ReleaseModes)int.Parse(ReleaseMode);

        [JsonIgnore]
        public FuzeModes FuzeEnum
            => (string.IsNullOrEmpty(FuzeMode)) ? FuzeModes.Unknown : (FuzeModes)int.Parse(FuzeMode);

        [JsonIgnore]
        public AutoPowerModes AutoPwrModeEnum
            => (string.IsNullOrEmpty(AutoPwrMode)) ? AutoPowerModes.Unknown : (AutoPowerModes)int.Parse(AutoPwrMode);

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
                                           string.IsNullOrEmpty(ArmDelay) &&
                                           string.IsNullOrEmpty(ArmDelay2) &&
                                           string.IsNullOrEmpty(ArmDelayMode) &&
                                           string.IsNullOrEmpty(BurstAlt) &&
                                           string.IsNullOrEmpty(Spin) &&
                                           string.IsNullOrEmpty(ReleaseAng) &&
                                           string.IsNullOrEmpty(ImpactAng) &&
                                           string.IsNullOrEmpty(ImpactAzi) &&
                                           string.IsNullOrEmpty(ImpactVel) &&
                                           string.IsNullOrEmpty(CueRange) &&
                                           string.IsNullOrEmpty(AutoPwrMode) &&
                                           string.IsNullOrEmpty(AutoPwrSP));

        // ------------------------------------------------------------------------------------------------------------
        //
        // Construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MunitionSettings()
        {
            ID = SMSSystem.Munitions.Unknown;
            Profile = "";
            Reset();
        }

        public MunitionSettings(MunitionSettings other)
        {
            ID = other.ID;
            Profile = new(other.Profile);
            IsProfileSelected = new(other.IsProfileSelected);
            EmplMode = new(other.EmplMode);
            ReleaseMode = new(other.ReleaseMode);
            RipplePulse = new(other.RipplePulse);
            RippleSpacing = new(other.RippleSpacing);
            RippleDelayMode = new(other.RippleDelayMode);
            FuzeMode = new(other.FuzeMode);
            ArmDelay = new(other.ArmDelay);
            ArmDelay2 = new(other.ArmDelay2);
            ArmDelayMode = new(other.ArmDelayMode);
            BurstAlt = new(other.BurstAlt);
            Spin = new(other.Spin);
            ReleaseAng = new(other.ReleaseAng);
            ImpactAng = new(other.ImpactAng);
            ImpactAzi = new(other.ImpactAzi);
            ImpactVel = new(other.ImpactVel);
            CueRange = new(other.CueRange);
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
        /// reset the instance to defaults.
        /// </summary>
        public override void Reset()
        {
            // ID and Profile are immutable once the object is in use, do not reset them here.
            //
            IsProfileSelected = "";
            EmplMode = "";
            ReleaseMode = "";
            RipplePulse = "";
            RippleSpacing = "";
            RippleDelayMode = "";
            FuzeMode = "";
            ArmDelay = "";
            ArmDelay2 = "";
            ArmDelayMode = "";
            BurstAlt = "";
            Spin = "";
            ReleaseAng = "";
            ImpactAng = "";
            ImpactAzi = "";
            ImpactVel = "";
            CueRange = "";
            AutoPwrMode = "";
            AutoPwrSP = "";
        }
    }
}
