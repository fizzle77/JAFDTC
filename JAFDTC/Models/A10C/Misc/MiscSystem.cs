// ********************************************************************************************************************
//
// MiscSystem.cs -- a-10c miscellaneous system
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

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.A10C.Misc
{
    // defines the coordinate system options
    //
    public enum CoordSystems
    {
        LL = 0,
        MGRS = 1
    }

    // defines the flight plan auto options
    //
    public enum FlightPlanManualOptions
    {
        Auto = 0,
        Manual = 1
    }

    // defines the flight plan auto options
    //
    public enum SpeedDisplayOptions
    {
        IAS = 0,
        TAS = 1,
        GS = 2
    }

    // defines the aap steer pt knob options
    //
    public enum AapSteerPtOptions
    {
        FltPlan = 0,
        Mark = 1,
        Mission = 2
    }

    // defines the aap page options
    //
    public enum AapPageOptions
    {
        Other = 0,
        Position = 1,
        Steer = 2,
        Waypt = 3
    }

    // defines the autopilot mode options
    //
    public enum AutopilotModeOptions
    {
        Path = 0,
        AltHdg,
        Alt
    }

    // defines the TACAN mode options
    //
    public enum TACANModeOptions
    {
        Off = 0,
        Rec = 1,
        Tr = 2,
        AaRec = 3,
        AaTr = 4
    }

    // defines the TACAN band options
    //
    public enum TACANBandOptions
    {
        X = 0,
        Y = 1
    }

    // defines the IFF MASTER options
    //
    public enum IFFMasterOptions
    {
        OFF = 0,
        STBY = 1,
        NORM = 2
    }

    /// <summary>
    /// TODO: document
    /// </summary>
    public class MiscSystem : A10CSystemBase
    {
        public const string SystemTag = "JAFDTC:A10C:MISC";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties post change and validation events

        private string _coordSystem;                              // integer [0, 1]
        public string CoordSystem
        {
            get => _coordSystem;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _coordSystem, value, error);
            }
        }

        private string _bullseyeOnHUD;                           // string (boolean)
        public string BullseyeOnHUD
        {
            get => _bullseyeOnHUD;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : "Invalid format";
                SetProperty(ref _bullseyeOnHUD, value, error);
            }
        }

        private string _flightPlan1Manual;                              // integer [0, 1]
        public string FlightPlan1Manual
        {
            get => _flightPlan1Manual;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _flightPlan1Manual, value, error);
            }
        }

        private string _speedDisplay;                              // integer [0, 2]
        public string SpeedDisplay
        {
            get => _speedDisplay;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _speedDisplay, value, error);
            }
        }

        private string _aapSteerPt;                              // integer [0, 2]
        public string AapSteerPt
        {
            get => _aapSteerPt;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _aapSteerPt, value, error);
            }
        }

        private string _aapPage;                              // integer [0, 3]
        public string AapPage
        {
            get => _aapPage;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 3)) ? null : "Invalid format";
                SetProperty(ref _aapPage, value, error);
            }
        }

        private string _autopilotMode;                              // integer [0, 2]
        public string AutopilotMode
        {
            get => _autopilotMode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _autopilotMode, value, error);
            }
        }

        private string _tacanMode;                              // integer [0, 4]
        public string TACANMode
        {
            get => _tacanMode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 4)) ? null : "Invalid format";
                SetProperty(ref _tacanMode, value, error);
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

        private string _tacanChannel;                           // integer [0, 129]
        public string TACANChannel
        {
            get => _tacanChannel;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 129)) ? null : "Invalid format";
                SetProperty(ref _tacanChannel, value, error);
            }
        }

        private string _iffMasterMode;                           // integer [0, 2]
        public string IFFMasterMode
        {
            get => _iffMasterMode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _iffMasterMode, value, error);
            }
        }

        private static readonly Regex _iffMode3CodeRegex = new(@"^[1-7][1-7][1-7][1-7]$");

        private string _iffMode3Code;                           // IFF squawk, [1-7][1-7][1-7][1-7]
        public string IFFMode3Code
        {
            get => _iffMode3Code;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsRegexFieldValid(value, _iffMode3CodeRegex))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _iffMode3Code, value, error);
            }
        }

        private string _iffMode4On;                           // string (boolean)
        public string IFFMode4On
        {
            get => _iffMode4On;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : "Invalid format";
                SetProperty(ref _iffMode4On, value, error);
            }
        }

        private string _aglFloor;                           // integer [0, 5000]
        public string AGLFloor
        {
            get => _aglFloor;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 5000)) ? null : "Invalid format";
                SetProperty(ref _aglFloor, value, error);
            }
        }

        private string _mslFloor;                           // integer [0, 45000]
        public string MSLFloor
        {
            get => _mslFloor;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 45000)) ? null : "Invalid format";
                SetProperty(ref _mslFloor, value, error);
            }
        }

        private string _mslCeiling;                           // integer [0, 45000]
        public string MSLCeiling
        {
            get => _mslCeiling;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 45000)) ? null : "Invalid format";
                SetProperty(ref _mslCeiling, value, error);
            }
        }


        // ---- following properties are synthesized

        // returns a MiscSystem with the fields populated with the actual default values (note that usually the value
        // "" implies default).
        //
        // defaults are as of DCS v2.9.0.47168.
        //
        public readonly static MiscSystem ExplicitDefaults = new()
        {
            CoordSystem = "0",                  // Lat/Long
            BullseyeOnHUD = false.ToString(),
            FlightPlan1Manual = "0",            // Auto
            SpeedDisplay = "0",                 // IAS
            AapSteerPt = "0",                   // Flt Plan
            AapPage = "0",                      // Other
            AutopilotMode = "1",                // Alt/Hdg
            TACANMode = "0",                    // Off
            TACANBand = "0",                    // X
            TACANChannel = "0",
            IFFMasterMode = "0",                // Off
            IFFMode3Code = "0000",
            IFFMode4On = false.ToString(),
            AGLFloor = "500",
            MSLFloor = "0",
            MSLCeiling = "0"
        };

        // returns true if the instance indicates a default setup (all fields are "") or the object is in explicit
        // form, false otherwise.
        //
        [JsonIgnore]
        public override bool IsDefault
        {
            get => IsCoordSystemDefault && IsBullseyeOnHUDDefault && IsFlightPlan1ManualDefault
                && IsSpeedDisplayDefault && IsAapSteerPtDefault && IsAapPageDefault 
                && IsAutopilotModeDefault && IsTACANModeDefault && IsTACANBandDefault && IsTACANChannelDefault
                && IsIFFMasterModeDefault && IsIFFMode3CodeDefault && IsIFFMode4OnDefault
                && IsAGLFloorDefault && IsMSLFloorDefault && IsMSLCeilingDefault;
        }

        [JsonIgnore]
        public bool IsCoordSystemDefault
        {
            get => string.IsNullOrEmpty(CoordSystem) || CoordSystem == ExplicitDefaults.CoordSystem;
        }

        [JsonIgnore]
        public bool IsBullseyeOnHUDDefault
        {
            get => string.IsNullOrEmpty(BullseyeOnHUD) || BullseyeOnHUD == ExplicitDefaults.BullseyeOnHUD;
        }

        [JsonIgnore]
        public bool IsFlightPlan1ManualDefault
        {
            get => string.IsNullOrEmpty(FlightPlan1Manual) || FlightPlan1Manual == ExplicitDefaults.FlightPlan1Manual;
        }

        [JsonIgnore]
        public bool IsSpeedDisplayDefault
        {
            get => string.IsNullOrEmpty(SpeedDisplay) || SpeedDisplay == ExplicitDefaults.SpeedDisplay;
        }

        [JsonIgnore]
        public bool IsAapSteerPtDefault
        {
            get => string.IsNullOrEmpty(AapSteerPt) || AapSteerPt == ExplicitDefaults.AapSteerPt;
        }

        [JsonIgnore]
        public bool IsAapPageDefault
        {
            get => string.IsNullOrEmpty(AapPage) || AapPage == ExplicitDefaults.AapPage;
        }

        [JsonIgnore]
        public bool IsAutopilotModeDefault
        {
            get => string.IsNullOrEmpty(AutopilotMode) || AutopilotMode == ExplicitDefaults.AutopilotMode;
        }

        [JsonIgnore]
        public bool IsTACANModeDefault
        {
            get => string.IsNullOrEmpty(TACANMode) || TACANMode == ExplicitDefaults.TACANMode;
        }

        [JsonIgnore]
        public bool IsTACANBandDefault
        {
            get => string.IsNullOrEmpty(TACANBand) || TACANBand == ExplicitDefaults.TACANBand;
        }

        [JsonIgnore]
        public bool IsTACANChannelDefault
        {
            get => string.IsNullOrEmpty(TACANChannel) || TACANChannel == ExplicitDefaults.TACANChannel;
        }

        [JsonIgnore]
        public bool IsIFFMasterModeDefault
        {
            get => string.IsNullOrEmpty(IFFMasterMode) || IFFMasterMode == ExplicitDefaults.IFFMasterMode;
        }

        [JsonIgnore]
        public bool IsIFFMode3CodeDefault
        {
            get => string.IsNullOrEmpty(IFFMode3Code) || IFFMode3Code == ExplicitDefaults.IFFMode3Code;
        }

        [JsonIgnore]
        public bool IsIFFMode4OnDefault
        {
            get => string.IsNullOrEmpty(IFFMode4On) || IFFMode4On == ExplicitDefaults.IFFMode4On;
        }

        [JsonIgnore]
        public bool IsAGLFloorDefault
        {
            get => string.IsNullOrEmpty(AGLFloor) || AGLFloor == ExplicitDefaults.AGLFloor;
        }

        [JsonIgnore]
        public bool IsMSLFloorDefault
        {
            get => string.IsNullOrEmpty(MSLFloor) || MSLFloor == ExplicitDefaults.MSLFloor;
        }

        [JsonIgnore]
        public bool IsMSLCeilingDefault
        {
            get => string.IsNullOrEmpty(MSLCeiling) || MSLCeiling == ExplicitDefaults.MSLCeiling;
        }

        // ---- following accessors get the current value (default or non-default) for various properties

        [JsonIgnore]
        public CoordSystems CoordSystemValue
        {
            get => (CoordSystems)int.Parse((string.IsNullOrEmpty(CoordSystem)) ? ExplicitDefaults.CoordSystem : CoordSystem);
        }

        [JsonIgnore]
        public bool IsBullseyeOnHUDValue
        {
            get => bool.Parse((string.IsNullOrEmpty(BullseyeOnHUD)) ? ExplicitDefaults.BullseyeOnHUD : BullseyeOnHUD);
        }

        [JsonIgnore]
        public FlightPlanManualOptions FlightPlan1ManualValue
        {
            get => (FlightPlanManualOptions)int.Parse((string.IsNullOrEmpty(FlightPlan1Manual)) ? ExplicitDefaults.FlightPlan1Manual : FlightPlan1Manual);
        }

        [JsonIgnore]
        public SpeedDisplayOptions SpeedDisplayValue
        {
            get => (SpeedDisplayOptions)int.Parse((string.IsNullOrEmpty(SpeedDisplay)) ? ExplicitDefaults.SpeedDisplay : SpeedDisplay);
        }

        [JsonIgnore]
        public AapSteerPtOptions AapSteerPtValue
        {
            get => (AapSteerPtOptions)int.Parse((string.IsNullOrEmpty(AapSteerPt)) ? ExplicitDefaults.AapSteerPt : AapSteerPt);
        }

        [JsonIgnore]
        public AapPageOptions AapPageValue
        {
            get => (AapPageOptions)int.Parse((string.IsNullOrEmpty(AapPage)) ? ExplicitDefaults.AapPage : AapPage);
        }

        [JsonIgnore]
        public AutopilotModeOptions AutopilotModeValue
        {
            get => (AutopilotModeOptions)int.Parse((string.IsNullOrEmpty(AutopilotMode)) ? ExplicitDefaults.AutopilotMode : AutopilotMode);
        }

        [JsonIgnore]
        public TACANModeOptions TACANModeValue
        {
            get => (TACANModeOptions)int.Parse((string.IsNullOrEmpty(TACANMode)) ? ExplicitDefaults.TACANMode : TACANMode);
        }

        [JsonIgnore]
        public TACANBandOptions TACANBandValue
        {
            get => (TACANBandOptions)int.Parse((string.IsNullOrEmpty(TACANBand)) ? ExplicitDefaults.TACANBand : TACANBand);
        }

        [JsonIgnore]
        public int TACANChannelValue
        {
            get => int.Parse((string.IsNullOrEmpty(TACANChannel)) ? ExplicitDefaults.TACANChannel : TACANChannel);
        }

        [JsonIgnore]
        public IFFMasterOptions IFFMasterModeValue
        {
            get => (IFFMasterOptions)int.Parse((string.IsNullOrEmpty(IFFMasterMode)) ? ExplicitDefaults.IFFMasterMode : IFFMasterMode);
        }

        [JsonIgnore]
        public string IFFMode3CodeValue
        {
            get => string.IsNullOrEmpty(IFFMode3Code) ? ExplicitDefaults.IFFMode3Code : IFFMode3Code;
        }

        [JsonIgnore]
        public bool IffMode4OnValue
        {
            get => bool.Parse((string.IsNullOrEmpty(IFFMode4On)) ? ExplicitDefaults.IFFMode4On : IFFMode4On);
        }

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
            CoordSystem = new(other.CoordSystem);
            BullseyeOnHUD = new(other.BullseyeOnHUD);
            FlightPlan1Manual = new(other.FlightPlan1Manual);
            SpeedDisplay = new(other.SpeedDisplay);
            AapSteerPt = new(other.AapSteerPt);
            AapPage = new(other.AapPage);
            AutopilotMode = new(other.AutopilotMode);
            TACANMode = new(other.TACANMode);
            TACANBand = new(other.TACANBand);
            TACANChannel = new(other.TACANChannel);
            IFFMasterMode = new(other.IFFMasterMode);
            IFFMode3Code = new(other.IFFMode3Code);
            IFFMode4On = new(other.IFFMode4On);
            AGLFloor = new(other.AGLFloor);
            MSLFloor = new(other.MSLFloor);
            MSLCeiling = new(other.MSLCeiling);
         }

        public virtual object Clone() => new MiscSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // reset the instance to defaults (by definition, field value of "" implies default).
        //
        public override void Reset()
        {
            CoordSystem = "0";                  // LL
            BullseyeOnHUD = false.ToString();
            FlightPlan1Manual = "0";            // Auto
            SpeedDisplay = "0";                 // IAS
            AapSteerPt = "0";                   // FltPlan
            AapPage = "0";                      // Other
            AutopilotMode = "1";                // Alt/Hdg
            TACANMode = "0";                    // Off
            TACANBand = "0";                    // X
            TACANChannel = "0";
            IFFMasterMode = "0";                // Off
            IFFMode3Code = "";
            IFFMode4On = false.ToString();
            AGLFloor = "500";
            MSLFloor = "0";
            MSLCeiling = "0";
        }
    }
}
