// ********************************************************************************************************************
//
// IFFCCSystem.cs -- a-10c iffcc system
//
// Copyright(C) 2024 fizzle, JAFDTC contributors
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

using System;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.A10C.IFFCC
{
    public enum CCIPConsentOptions
    {
        OFF = 0,
        THREE_NINE,
        FIVE_MIL
    }

    public enum AirspeedOptions
    {
        IAS = 0,
        MACH_IAS,
        GS,
        TRUE
    }

    public class IFFCCSystem : SystemBase
    {
        public const string SystemTag = "JAFDTC:A10C:IFFCC";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- properties that post change and validation events

        private string _ccipConsent;
        public string CCIPConsent
        {
            get => _ccipConsent;
            set => ValidateAndSetIntProp(value, 0, 2, ref _ccipConsent);
        }

        private string _isA10Enabled;
        public string IsA10Enabled
        {
            get => _isA10Enabled;
            set => ValidateAndSetBoolProp(value, ref _isA10Enabled);
        }

        private string _isF15Enabled;
        public string IsF15Enabled
        {
            get => _isF15Enabled;
            set => ValidateAndSetBoolProp(value, ref _isF15Enabled);
        }

        private string _isF16Enabled;
        public string IsF16Enabled
        {
            get => _isF16Enabled;
            set => ValidateAndSetBoolProp(value, ref _isF16Enabled);
        }

        private string _isF18Enabled;
        public string IsF18Enabled
        {
            get => _isF18Enabled;
            set => ValidateAndSetBoolProp(value, ref _isF18Enabled);
        }

        private string _isMig29Enabled;
        public string IsMig29Enabled
        {
            get => _isMig29Enabled;
            set => ValidateAndSetBoolProp(value, ref _isMig29Enabled);
        }

        private string _isSu27Enabled;
        public string IsSu27Enabled
        {
            get => _isSu27Enabled;
            set => ValidateAndSetBoolProp(value, ref _isSu27Enabled);
        }

        private string _isSu25Enabled;
        public string IsSu25Enabled
        {
            get => _isSu25Enabled;
            set => ValidateAndSetBoolProp(value, ref _isSu25Enabled);
        }

        private string _isAH64Enabled;
        public string IsAH64Enabled
        {
            get => _isAH64Enabled;
            set => ValidateAndSetBoolProp(value, ref _isAH64Enabled);
        }

        private string _isUH60Enabled;
        public string IsUH60Enabled
        {
            get => _isUH60Enabled;
            set => ValidateAndSetBoolProp(value, ref _isUH60Enabled);
        }

        private string _isMi8Enabled;
        public string IsMi8Enabled
        {
            get => _isMi8Enabled;
            set => ValidateAndSetBoolProp(value, ref _isMi8Enabled);
        }

        private string _fxdWingspan;
        public string FxdWingspan
        {
            get => _fxdWingspan;
            set => ValidateAndSetIntProp(value, 0, 99, ref _fxdWingspan);
        }

        private string _fxdLength;
        public string FxdLength
        {
            get => _fxdLength;
            set => ValidateAndSetIntProp(value, 10, 200, ref _fxdLength);
        }

        private string _fxdTgtSpeed;
        public string FxdTgtSpeed
        {
            get => _fxdTgtSpeed;
            set => ValidateAndSetIntProp(value, 50, 500, ref _fxdTgtSpeed);
        }

        private string _rtyWingspan;
        public string RtyWingspan
        {
            get => _rtyWingspan;
            set => ValidateAndSetIntProp(value, 0, 99, ref _rtyWingspan);
        }

        private string _rtyLength;
        public string RtyLength
        {
            get => _rtyLength;
            set => ValidateAndSetIntProp(value, 10, 200, ref _rtyLength);
        }

        private string _rtyTgtSpeed;
        public string RtyTgtSpeed
        {
            get => _rtyTgtSpeed;
            set => ValidateAndSetIntProp(value, 50, 500, ref _rtyTgtSpeed);
        }

        private string _autoDataDisplay;
        public string AutoDataDisplay
        {
            get => _autoDataDisplay;
            set => ValidateAndSetBoolProp(value, ref _autoDataDisplay);
        }

        // Pretty sure this is a funny translation failure. This should almost certainly be
        // "occlude" not "occult" but it appears this way everywhere in both the manual and
        // in-pit. Leaving it this way here, but I can't stand to leave "occult" in the UI.
        private string _ccipGunCrossOccult;
        public string CCIPGunCrossOccult
        {
            get => _ccipGunCrossOccult;
            set => ValidateAndSetBoolProp(value, ref _ccipGunCrossOccult);
        }

        private string _tapes;
        public string Tapes
        {
            get => _tapes;
            set => ValidateAndSetBoolProp(value, ref _tapes);
        }

        private string _metric;
        public string Metric
        {
            get => _metric;
            set => ValidateAndSetBoolProp(value, ref _metric);
        }

        private string _rdrAltTape;
        public string RdrAltTape
        {
            get => _rdrAltTape;
            set => ValidateAndSetBoolProp(value, ref _rdrAltTape);
        }

        private string _airspeed;
        public string Airspeed
        {
            get => _airspeed;
            set => ValidateAndSetIntProp(value, 0, 3, ref _airspeed);
        }

        private string _vertVel;
        public string VertVel
        {
            get => _vertVel;
            set => ValidateAndSetBoolProp(value, ref _vertVel);
        }

        // ---- synthesized properties

        [JsonIgnore]
        public override bool IsDefault => IsCCIPConsentDefault &&
                                          IsAASDefault &&
                                          IsDisplayModesDefault;

        [JsonIgnore]
        public bool IsAASDefault => IsA10EnabledDefault &&
                                    IsF15EnabledDefault &&
                                    IsF16EnabledDefault &&
                                    IsF18EnabledDefault &&
                                    IsMig29EnabledDefault &&
                                    IsSu27EnabledDefault &&
                                    IsSu25EnabledDefault &&
                                    IsAH64EnabledDefault &&
                                    IsUH60EnabledDefault &&
                                    IsMi8EnabledDefault &&
                                    IsFxdWingspanDefault &&
                                    IsFxdLengthDefault &&
                                    IsFxdTgtSpeedDefault &&
                                    IsRtyWingspanDefault &&
                                    IsRtyLengthDefault &&
                                    IsRtyTgtSpeedDefault;

        [JsonIgnore]
        public bool IsDisplayModesDefault => IsAutoDataDisplayDefault &&
                                             IsCCIPGunCrossOccultDefault &&
                                             IsTapesDefault &&
                                             IsMetricDefault &&
                                             IsRdrAltTapeDefault &&
                                             IsAirspeedDefault &&
                                             IsVertVelDefault;

        [JsonIgnore]
        public bool IsCCIPConsentDefault => string.IsNullOrEmpty(CCIPConsent) || CCIPConsent == ExplicitDefaults.CCIPConsent;
        [JsonIgnore]
        public int CCIPConsentValue => string.IsNullOrEmpty(CCIPConsent) ? int.Parse(ExplicitDefaults.CCIPConsent) : int.Parse(CCIPConsent);

        [JsonIgnore]
        public bool IsA10EnabledDefault => string.IsNullOrEmpty(IsA10Enabled) || IsA10Enabled == ExplicitDefaults.IsA10Enabled;
        [JsonIgnore]
        public bool IsA10EnabledValue => string.IsNullOrEmpty(IsA10Enabled) ? bool.Parse(ExplicitDefaults.IsA10Enabled) : bool.Parse(IsA10Enabled);

        [JsonIgnore]
        public bool IsF15EnabledDefault => string.IsNullOrEmpty(IsF15Enabled) || IsF15Enabled == ExplicitDefaults.IsF15Enabled;
        [JsonIgnore]
        public bool IsF15EnabledValue => string.IsNullOrEmpty(IsF15Enabled) ? bool.Parse(ExplicitDefaults.IsF15Enabled) : bool.Parse(IsF15Enabled);

        [JsonIgnore]
        public bool IsF16EnabledDefault => string.IsNullOrEmpty(IsF16Enabled) || IsF16Enabled == ExplicitDefaults.IsF16Enabled;
        [JsonIgnore]
        public bool IsF16EnabledValue => string.IsNullOrEmpty(IsF16Enabled) ? bool.Parse(ExplicitDefaults.IsF16Enabled) : bool.Parse(IsF16Enabled);

        [JsonIgnore]
        public bool IsF18EnabledDefault => string.IsNullOrEmpty(IsF18Enabled) || IsF18Enabled == ExplicitDefaults.IsF18Enabled;
        [JsonIgnore]
        public bool IsF18EnabledValue => string.IsNullOrEmpty(IsF18Enabled) ? bool.Parse(ExplicitDefaults.IsF18Enabled) : bool.Parse(IsF18Enabled);

        [JsonIgnore]
        public bool IsMig29EnabledDefault => string.IsNullOrEmpty(IsMig29Enabled) || IsMig29Enabled == ExplicitDefaults.IsMig29Enabled;
        [JsonIgnore]
        public bool IsMig29EnabledValue => string.IsNullOrEmpty(IsMig29Enabled) ? bool.Parse(ExplicitDefaults.IsMig29Enabled) : bool.Parse(IsMig29Enabled);

        [JsonIgnore]
        public bool IsSu27EnabledDefault => string.IsNullOrEmpty(IsSu27Enabled) || IsSu27Enabled == ExplicitDefaults.IsSu27Enabled;
        [JsonIgnore]
        public bool IsSu27EnabledValue => string.IsNullOrEmpty(IsSu27Enabled) ? bool.Parse(ExplicitDefaults.IsSu27Enabled) : bool.Parse(IsSu27Enabled);

        [JsonIgnore]
        public bool IsSu25EnabledDefault => string.IsNullOrEmpty(IsSu25Enabled) || IsSu25Enabled == ExplicitDefaults.IsSu25Enabled;
        [JsonIgnore]
        public bool IsSu25EnabledValue => string.IsNullOrEmpty(IsSu25Enabled) ? bool.Parse(ExplicitDefaults.IsSu25Enabled) : bool.Parse(IsSu25Enabled);

        [JsonIgnore]
        public bool IsAH64EnabledDefault => string.IsNullOrEmpty(IsAH64Enabled) || IsAH64Enabled == ExplicitDefaults.IsAH64Enabled;
        [JsonIgnore]
        public bool IsAH64EnabledValue => string.IsNullOrEmpty(IsAH64Enabled) ? bool.Parse(ExplicitDefaults.IsAH64Enabled) : bool.Parse(IsAH64Enabled);

        [JsonIgnore]
        public bool IsUH60EnabledDefault => string.IsNullOrEmpty(IsUH60Enabled) || IsUH60Enabled == ExplicitDefaults.IsUH60Enabled;
        [JsonIgnore]
        public bool IsUH60EnabledValue => string.IsNullOrEmpty(IsUH60Enabled) ? bool.Parse(ExplicitDefaults.IsUH60Enabled) : bool.Parse(IsUH60Enabled);

        [JsonIgnore]
        public bool IsMi8EnabledDefault => string.IsNullOrEmpty(IsMi8Enabled) || IsMi8Enabled == ExplicitDefaults.IsMi8Enabled;
        [JsonIgnore]
        public bool IsMi8EnabledValue => string.IsNullOrEmpty(IsMi8Enabled) ? bool.Parse(ExplicitDefaults.IsMi8Enabled) : bool.Parse(IsMi8Enabled);

        [JsonIgnore]
        public bool IsFxdWingspanDefault => string.IsNullOrEmpty(FxdWingspan) || FxdWingspan == ExplicitDefaults.FxdWingspan;
        [JsonIgnore]
        public int FxdWingspanValue => string.IsNullOrEmpty(FxdWingspan) ? int.Parse(ExplicitDefaults.FxdWingspan) : int.Parse(FxdWingspan);

        [JsonIgnore]
        public bool IsFxdLengthDefault => string.IsNullOrEmpty(FxdLength) || FxdLength == ExplicitDefaults.FxdLength;
        [JsonIgnore]
        public int FxdLengthValue => string.IsNullOrEmpty(FxdLength) ? int.Parse(ExplicitDefaults.FxdLength) : int.Parse(FxdLength);

        [JsonIgnore]
        public bool IsFxdTgtSpeedDefault => string.IsNullOrEmpty(FxdTgtSpeed) || FxdTgtSpeed == ExplicitDefaults.FxdTgtSpeed;
        [JsonIgnore]
        public int FxdTgtSpeedValue => string.IsNullOrEmpty(FxdTgtSpeed) ? int.Parse(ExplicitDefaults.FxdTgtSpeed) : int.Parse(FxdTgtSpeed);

        [JsonIgnore]
        public bool IsRtyWingspanDefault => string.IsNullOrEmpty(RtyWingspan) || RtyWingspan == ExplicitDefaults.RtyWingspan;
        [JsonIgnore]
        public int RtyWingspanValue => string.IsNullOrEmpty(RtyWingspan) ? int.Parse(ExplicitDefaults.RtyWingspan) : int.Parse(RtyWingspan);

        [JsonIgnore]
        public bool IsRtyLengthDefault => string.IsNullOrEmpty(RtyLength) || RtyLength == ExplicitDefaults.RtyLength;
        [JsonIgnore]
        public int RtyLengthValue => string.IsNullOrEmpty(RtyLength) ? int.Parse(ExplicitDefaults.RtyLength) : int.Parse(RtyLength);

        [JsonIgnore]
        public bool IsRtyTgtSpeedDefault => string.IsNullOrEmpty(RtyTgtSpeed) || RtyTgtSpeed == ExplicitDefaults.RtyTgtSpeed;
        [JsonIgnore]
        public int RtyTgtSpeedValue => string.IsNullOrEmpty(RtyTgtSpeed) ? int.Parse(ExplicitDefaults.RtyTgtSpeed) : int.Parse(RtyTgtSpeed);

        [JsonIgnore]
        public bool IsAutoDataDisplayDefault => string.IsNullOrEmpty(AutoDataDisplay) || AutoDataDisplay == ExplicitDefaults.AutoDataDisplay;
        [JsonIgnore]
        public bool AutoDataDisplayValue => string.IsNullOrEmpty(AutoDataDisplay) ? bool.Parse(ExplicitDefaults.AutoDataDisplay) : bool.Parse(AutoDataDisplay);

        [JsonIgnore]
        public bool IsCCIPGunCrossOccultDefault => string.IsNullOrEmpty(CCIPGunCrossOccult) || CCIPGunCrossOccult == ExplicitDefaults.CCIPGunCrossOccult;
        [JsonIgnore]
        public bool CCIPGunCrossOccultValue => string.IsNullOrEmpty(CCIPGunCrossOccult) ? bool.Parse(ExplicitDefaults.CCIPGunCrossOccult) : bool.Parse(CCIPGunCrossOccult);

        [JsonIgnore]
        public bool IsTapesDefault => string.IsNullOrEmpty(Tapes) || Tapes == ExplicitDefaults.Tapes;
        [JsonIgnore]
        public bool TapesValue => string.IsNullOrEmpty(Tapes) ? bool.Parse(ExplicitDefaults.Tapes) : bool.Parse(Tapes);

        [JsonIgnore]
        public bool IsMetricDefault => string.IsNullOrEmpty(Metric) || Metric == ExplicitDefaults.Metric;
        [JsonIgnore]
        public bool MetricValue => string.IsNullOrEmpty(Metric) ? bool.Parse(ExplicitDefaults.Metric) : bool.Parse(Metric);

        [JsonIgnore]
        public bool IsRdrAltTapeDefault => string.IsNullOrEmpty(RdrAltTape) || RdrAltTape == ExplicitDefaults.RdrAltTape;
        [JsonIgnore]
        public bool RdrAltTapeValue => string.IsNullOrEmpty(RdrAltTape) ? bool.Parse(ExplicitDefaults.RdrAltTape) : bool.Parse(RdrAltTape);

        [JsonIgnore]
        public bool IsAirspeedDefault => string.IsNullOrEmpty(Airspeed) || Airspeed == ExplicitDefaults.Airspeed;
        [JsonIgnore]
        public int AirspeedValue => string.IsNullOrEmpty(Airspeed) ? int.Parse(ExplicitDefaults.Airspeed) : int.Parse(Airspeed);

        [JsonIgnore]
        public bool IsVertVelDefault => string.IsNullOrEmpty(VertVel) || VertVel == ExplicitDefaults.VertVel;
        [JsonIgnore]
        public int VertVelValue => string.IsNullOrEmpty(VertVel) ? int.Parse(ExplicitDefaults.VertVel) : int.Parse(VertVel);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------
        public IFFCCSystem() 
        {
            Reset();
        }

        public IFFCCSystem(IFFCCSystem other)
        {
            CCIPConsent = other.CCIPConsent;

            IsA10Enabled = other.IsA10Enabled;
            IsF15Enabled = other.IsF15Enabled;
            IsF16Enabled = other.IsF16Enabled;
            IsF18Enabled = other.IsF18Enabled;
            IsMig29Enabled = other.IsMig29Enabled;
            IsSu27Enabled = other.IsSu27Enabled;
            IsSu25Enabled = other.IsSu25Enabled;
            IsAH64Enabled = other.IsAH64Enabled;
            IsUH60Enabled = other.IsUH60Enabled;
            IsMi8Enabled = other.IsMi8Enabled;

            FxdWingspan = other.FxdWingspan;
            FxdLength = other.FxdLength;
            FxdTgtSpeed = other.FxdTgtSpeed;

            RtyWingspan = other.RtyWingspan;
            RtyLength = other.RtyLength;
            RtyTgtSpeed = other.RtyTgtSpeed;

            AutoDataDisplay = other.AutoDataDisplay;
            CCIPGunCrossOccult = other.CCIPGunCrossOccult;
            Tapes = other.Tapes;
            Metric = other.Metric;
            RdrAltTape = other.RdrAltTape;
            Airspeed = other.Airspeed;
            VertVel = other.VertVel;
        }

        public virtual object Clone() => new IFFCCSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // member methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void Reset()
        {
            Reset(this);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // static members
        //
        // ------------------------------------------------------------------------------------------------------------

        private static IFFCCSystem _default;
        public static IFFCCSystem ExplicitDefaults
        {
            get
            {
                if (_default == null)
                {
                    _default = new IFFCCSystem();
                    Reset(_default);
                }
                return _default;
            }
        }

        private static void Reset(IFFCCSystem iffcc)
        {
            iffcc.CCIPConsent = "0";

            iffcc.IsA10Enabled = true.ToString();
            iffcc.IsF15Enabled = true.ToString();
            iffcc.IsF16Enabled = false.ToString();
            iffcc.IsF18Enabled = false.ToString();
            iffcc.IsMig29Enabled = false.ToString();
            iffcc.IsSu27Enabled = false.ToString();
            iffcc.IsSu25Enabled = true.ToString();
            iffcc.IsAH64Enabled = true.ToString();
            iffcc.IsUH60Enabled = false.ToString();
            iffcc.IsMi8Enabled = false.ToString();

            iffcc.FxdWingspan = "0";
            iffcc.FxdLength = "10";
            iffcc.FxdTgtSpeed = "50";

            iffcc.RtyWingspan = "0";
            iffcc.RtyLength = "10";
            iffcc.RtyTgtSpeed = "50";

            iffcc.AutoDataDisplay = false.ToString();
            iffcc.CCIPGunCrossOccult = true.ToString();
            iffcc.Tapes = false.ToString();
            iffcc.Metric = false.ToString();
            iffcc.RdrAltTape = false.ToString();
            iffcc.Airspeed = "0";
            iffcc.VertVel = false.ToString();
        }
    }
}
