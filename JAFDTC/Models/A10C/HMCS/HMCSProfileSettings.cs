// ********************************************************************************************************************
//
// MunitionSettings.cs -- profile settings for a-10c hmcs system
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

namespace JAFDTC.Models.A10C.HMCS
{
    // Defines the visbility options for each setting in a HMCS profile
    public enum VisibilityOptions
    {
        OFF = 0,
        OCLD = 1, // Occluded
        ON = 2
    }

    // Defines the visbility options for the horizon line setting
    public enum HorizonLineOptions
    {
        OFF = 0,
        NORM = 1,
        GHST = 2
    }

    /// <summary>
    /// Defines the settings that exist under each of the 3 HMCS profiles.
    /// </summary>
    public class HMCSProfileSettings : BindableObject, ISystem
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- properties that post change and validation events

        // The full list of HMCS properties represented here can be found on page 480 of the A-10 Flight Manual.

        private string _crosshair;
        public string Crosshair
        {
            get => _crosshair;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _crosshair, value, error);
            }
        }

        private string _ownSPI;
        public string OwnSPI
        {
            get => _ownSPI;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _ownSPI, value, error);
            }
        }

        private string _spiIndicator;
        public string SPIInIndicator
        {
            get => _spiIndicator;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _spiIndicator, value, error);
            }
        }

        private string _horizonLine;
        public string HorizonLine
        {
            get => _horizonLine;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _horizonLine, value, error);
            }
        }

        private string _hdc;
        public string HDC
        {
            get => _hdc;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _hdc, value, error);
            }
        }

        private string _hookship;
        public string Hookship
        {
            get => _hookship;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _hookship, value, error);
            }
        }

        private string _tgpDiamond;
        public string TGPDiamond
        {
            get => _tgpDiamond;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _tgpDiamond, value, error);
            }
        }

        private string _tgpFOV;
        public string TGPFOV
        {
            get => _tgpFOV;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _tgpFOV, value, error);
            }
        }

        private string _flightMembers;
        public string FlightMembers
        {
            get => _flightMembers;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _flightMembers, value, error);
            }
        }

        private string _flightMembersRange;
        public string FlightMembersRange
        {
            get => _flightMembersRange;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 999)) ? null : "Invalid format";
                SetProperty(ref _flightMembersRange, value, error);
            }
        }

        private string _fmSPI;
        public string FMSPI
        {
            get => _fmSPI;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _fmSPI, value, error);
            }
        }

        private string _fmSPIRange;
        public string FMSPIRange
        {
            get => _fmSPIRange;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 999)) ? null : "Invalid format";
                SetProperty(ref _fmSPIRange, value, error);
            }
        }

        private string _donorAirPPLI;
        public string DonorAirPPLI
        {
            get => _donorAirPPLI;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _donorAirPPLI, value, error);
            }
        }

        private string _donorAirPPLIRange;
        public string DonorAirPPLIRange
        {
            get => _donorAirPPLIRange;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 999)) ? null : "Invalid format";
                SetProperty(ref _donorAirPPLIRange, value, error);
            }
        }

        private string _donorSPI;
        public string DonorSPI
        {
            get => _donorSPI;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _donorSPI, value, error);
            }
        }

        private string _donorSPIRange;
        public string DonorSPIRange
        {
            get => _donorSPIRange;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 999)) ? null : "Invalid format";
                SetProperty(ref _donorSPIRange, value, error);
            }
        }

        // Not in the manual: current SADL tasking from JTAC or flight member
        private string _currentMA;
        public string CurrentMA
        {
            get => _currentMA;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _currentMA, value, error);
            }
        }

        private string _currentMARange;
        public string CurrentMARange
        {
            get => _currentMARange;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 999)) ? null : "Invalid format";
                SetProperty(ref _currentMARange, value, error);
            }
        }

        private string _airEnvir;
        public string AirEnvir
        {
            get => _airEnvir;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _airEnvir, value, error);
            }
        }

        // Skipped because no function in DCS: AIR VMF FRIEND

        private string _airPPLINonDonor;
        public string AirPPLINonDonor
        {
            get => _airPPLINonDonor;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _airPPLINonDonor, value, error);
            }
        }

        private string _airPPLINonDonorRange;
        public string AirPPLINonDonorRange
        {
            get => _airPPLINonDonorRange;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 999)) ? null : "Invalid format";
                SetProperty(ref _airPPLINonDonorRange, value, error);
            }
        }

        // Skipped because no function in DCS: AIR TRK FRIEND
        // Skipped because no function in DCS: AIR NEUTRAL
        // Skipped because no function in DCS: AIR SUSPECT
        // Skipped because no function in DCS: AIR HOSTILE
        // Skipped because no function in DCS: AIR OTHER

        private string _gndEnvir;
        public string GndEnvir
        {
            get => _gndEnvir;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _gndEnvir, value, error);
            }
        }

        private string _gndVMFFriend;
        public string GndVMFFriend
        {
            get => _gndVMFFriend;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _gndVMFFriend, value, error);
            }
        }

        private string _gndVMFFriendRange;
        public string GndVMFFriendRange
        {
            get => _gndVMFFriendRange;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 999)) ? null : "Invalid format";
                SetProperty(ref _gndVMFFriendRange, value, error);
            }
        }

        // Skipped because no function in DCS: GND PPLI
        // Skipped because no function in DCS: GND TRK FRIEND
        // Skipped because no function in DCS: GND NEUTRAL
        // Skipped because no function in DCS: GND SUSPECT
        // Skipped because no function in DCS: GND HOSTILE
        // Skipped because no function in DCS: GND OTHER
        // Skipped because no function in DCS: EMER PONIT

        private string _steerpoint;
        public string Steerpoint
        {
            get => _steerpoint;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _steerpoint, value, error);
            }
        }

        private string _steerpointRange;
        public string SteerpointRange
        {
            get => _steerpointRange;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 999)) ? null : "Invalid format";
                SetProperty(ref _steerpointRange, value, error);
            }
        }

        private string _msnMarkpoints;
        public string MsnMarkpoints
        {
            get => _msnMarkpoints;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _msnMarkpoints, value, error);
            }
        }

        private string _msnMarkpointsRange;
        public string MsnMarkpointsRange
        {
            get => _msnMarkpointsRange;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 999)) ? null : "Invalid format";
                SetProperty(ref _msnMarkpointsRange, value, error);
            }
        }

        private string _msnMarkLabels;
        public string MsnMarkLabels
        {
            get => _msnMarkLabels;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _msnMarkLabels, value, error);
            }
        }

        private string _airspeed;
        public string Airspeed
        {
            get => _airspeed;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _airspeed, value, error);
            }
        }

        private string _radarAltitude;
        public string RadarAltitude
        {
            get => _radarAltitude;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _radarAltitude, value, error);
            }
        }

        private string _baroAltitude;
        public string BaroAltitude
        {
            get => _baroAltitude;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _baroAltitude, value, error);
            }
        }

        private string _acHeading;
        public string ACHeading
        {
            get => _acHeading;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _acHeading, value, error);
            }
        }

        private string _helmetHeading;
        public string HelmetHeading
        {
            get => _helmetHeading;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _helmetHeading, value, error);
            }
        }

        private string _hmdElevLines;
        public string HMDElevLines
        {
            get => _hmdElevLines;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _hmdElevLines, value, error);
            }
        }

        // Mostly here to make JSON de/serialization work.
        // Name must match JsonConstructor constructor parameter (case insensitive).
        public Profiles Profile
        {
            get => _profile;
        }

        // ---- synthesized properties

        [JsonIgnore]
        public bool IsDefault
        {
            get
            {
                return IsCrosshairDefault
                    && IsOwnSPIDefault
                    && IsSPIInIndicatorDefault
                    && IsHorizonLineDefault
                    && IsHDCDefault
                    && IsHookshipDefault
                    && IsTGPDiamondDefault
                    && IsTGPFOVDefault
                    && IsFlightMembersDefault
                    && IsFlightMembersRangeDefault
                    && IsFlightMemberSPIDefault
                    && IsFlightMemberSPIRangeDefault
                    && IsDonorAirPPLIDefault
                    && IsDonorAirPPLIRangeDefault
                    && IsDonorSPIDefault
                    && IsDonorSPIRangeDefault
                    && IsCurrentMADefault
                    && IsCurrentMARangeDefault
                    && IsAirEnvirDefault
                    && IsAirPPLINonDonorDefault
                    && IsAirPPLINonDonorRangeDefault
                    && IsGndEnvirDefault
                    && IsGndVMFFriendDefault
                    && IsGndVMFFriendRangeDefault
                    && IsSteerPointDefault
                    && IsSteerPointRangeDefault
                    && IsMsnMarkpointsDefault
                    && IsMsnMarkpointsRangeDefault
                    && IsMsnMarkLabelsDefault
                    && IsAirspeedDefault
                    && IsRadarAltitudeDefault
                    && IsBaroAltitudeDefault
                    && IsACHeadingDefault
                    && IsHelmetHeadingDefault
                    && IsHMDElevLinesDefault;
            }
        }

        [JsonIgnore]
        public bool IsCrosshairDefault => string.IsNullOrEmpty(Crosshair) || Crosshair == GetExplicitDefaults(_profile).Crosshair;
        [JsonIgnore]
        public int CrosshairValue => string.IsNullOrEmpty(Crosshair) ? int.Parse(GetExplicitDefaults(_profile).HMDElevLines) : int.Parse(Crosshair);

        [JsonIgnore]
        public bool IsOwnSPIDefault => string.IsNullOrEmpty(OwnSPI) || OwnSPI == GetExplicitDefaults(_profile).OwnSPI;

        [JsonIgnore]
        public bool IsSPIInIndicatorDefault => string.IsNullOrEmpty(SPIInIndicator) || SPIInIndicator == GetExplicitDefaults(_profile).SPIInIndicator;
        
        [JsonIgnore]
        public bool IsHorizonLineDefault => string.IsNullOrEmpty(HorizonLine) || HorizonLine == GetExplicitDefaults(_profile).HorizonLine;
        
        [JsonIgnore]
        public bool IsHDCDefault => string.IsNullOrEmpty(HDC) || HDC == GetExplicitDefaults(_profile).HDC;
        
        [JsonIgnore]
        public bool IsHookshipDefault => string.IsNullOrEmpty(Hookship) || Hookship == GetExplicitDefaults(_profile).Hookship;
        
        [JsonIgnore]
        public bool IsTGPDiamondDefault => string.IsNullOrEmpty(TGPDiamond) || TGPDiamond == GetExplicitDefaults(_profile).TGPDiamond;
        
        [JsonIgnore]
        public bool IsTGPFOVDefault => string.IsNullOrEmpty(TGPFOV) || TGPFOV == GetExplicitDefaults(_profile).TGPFOV;
        
        [JsonIgnore]
        public bool IsFlightMembersDefault => string.IsNullOrEmpty(FlightMembers) || FlightMembers == GetExplicitDefaults(_profile).FlightMembers;
        
        [JsonIgnore]
        public bool IsFlightMembersRangeDefault => string.IsNullOrEmpty(FlightMembersRange) || FlightMembersRange == GetExplicitDefaults(_profile).FlightMembersRange;
        
        [JsonIgnore]
        public bool IsFlightMemberSPIDefault => string.IsNullOrEmpty(FMSPI) || FMSPI == GetExplicitDefaults(_profile).FMSPI;
        
        [JsonIgnore]
        public bool IsFlightMemberSPIRangeDefault => string.IsNullOrEmpty(FMSPIRange) || FMSPIRange == GetExplicitDefaults(_profile).FMSPIRange;
        
        [JsonIgnore]
        public bool IsDonorAirPPLIDefault => string.IsNullOrEmpty(DonorAirPPLI) || DonorAirPPLI == GetExplicitDefaults(_profile).DonorAirPPLI;
        
        [JsonIgnore]
        public bool IsDonorAirPPLIRangeDefault => string.IsNullOrEmpty(DonorAirPPLIRange) || DonorAirPPLIRange == GetExplicitDefaults(_profile).DonorAirPPLIRange;
        
        [JsonIgnore]
        public bool IsDonorSPIDefault => string.IsNullOrEmpty(DonorSPI) || DonorSPI == GetExplicitDefaults(_profile).DonorSPI;
        
        [JsonIgnore]
        public bool IsDonorSPIRangeDefault => string.IsNullOrEmpty(DonorSPIRange) || DonorSPIRange == GetExplicitDefaults(_profile).DonorSPIRange;
        
        [JsonIgnore]
        public bool IsCurrentMADefault => string.IsNullOrEmpty(CurrentMA) || CurrentMA == GetExplicitDefaults(_profile).CurrentMA;
        
        [JsonIgnore]
        public bool IsCurrentMARangeDefault => string.IsNullOrEmpty(CurrentMARange) || CurrentMARange == GetExplicitDefaults(_profile).CurrentMARange;
        
        [JsonIgnore]
        public bool IsAirEnvirDefault => string.IsNullOrEmpty(AirEnvir) || AirEnvir == GetExplicitDefaults(_profile).AirEnvir;
        
        [JsonIgnore]
        public bool IsAirPPLINonDonorDefault => string.IsNullOrEmpty(AirPPLINonDonor) || AirPPLINonDonor == GetExplicitDefaults(_profile).AirPPLINonDonor;
        
        [JsonIgnore]
        public bool IsAirPPLINonDonorRangeDefault => string.IsNullOrEmpty(AirPPLINonDonorRange) || AirPPLINonDonorRange == GetExplicitDefaults(_profile).AirPPLINonDonorRange;
        
        [JsonIgnore]
        public bool IsGndEnvirDefault => string.IsNullOrEmpty(GndEnvir) || GndEnvir == GetExplicitDefaults(_profile).GndEnvir;
        
        [JsonIgnore]
        public bool IsGndVMFFriendDefault => string.IsNullOrEmpty(GndVMFFriend) || GndVMFFriend == GetExplicitDefaults(_profile).GndVMFFriend;
        
        [JsonIgnore]
        public bool IsGndVMFFriendRangeDefault => string.IsNullOrEmpty(GndVMFFriendRange) || GndVMFFriendRange == GetExplicitDefaults(_profile).GndVMFFriendRange;
        
        [JsonIgnore]
        public bool IsSteerPointDefault => string.IsNullOrEmpty(Steerpoint) || Steerpoint == GetExplicitDefaults(_profile).Steerpoint;
        
        [JsonIgnore]
        public bool IsSteerPointRangeDefault => string.IsNullOrEmpty(SteerpointRange) || SteerpointRange == GetExplicitDefaults(_profile).SteerpointRange;
        
        [JsonIgnore]
        public bool IsMsnMarkpointsDefault => string.IsNullOrEmpty(MsnMarkpoints) || MsnMarkpoints == GetExplicitDefaults(_profile).MsnMarkpoints;
        
        [JsonIgnore]
        public bool IsMsnMarkpointsRangeDefault => string.IsNullOrEmpty(MsnMarkpointsRange) || MsnMarkpointsRange == GetExplicitDefaults(_profile).MsnMarkpointsRange;
        
        [JsonIgnore]
        public bool IsMsnMarkLabelsDefault => string.IsNullOrEmpty(MsnMarkLabels) || MsnMarkLabels == GetExplicitDefaults(_profile).MsnMarkLabels;
        
        [JsonIgnore]
        public bool IsAirspeedDefault => string.IsNullOrEmpty(Airspeed) || Airspeed == GetExplicitDefaults(_profile).Airspeed;
        
        [JsonIgnore]
        public bool IsRadarAltitudeDefault => string.IsNullOrEmpty(RadarAltitude) || RadarAltitude == GetExplicitDefaults(_profile).RadarAltitude;
        
        [JsonIgnore]
        public bool IsBaroAltitudeDefault => string.IsNullOrEmpty(BaroAltitude) || BaroAltitude == GetExplicitDefaults(_profile).BaroAltitude;
        
        [JsonIgnore]
        public bool IsACHeadingDefault => string.IsNullOrEmpty(ACHeading) || ACHeading == GetExplicitDefaults(_profile).ACHeading;
        
        [JsonIgnore]
        public bool IsHelmetHeadingDefault => string.IsNullOrEmpty(HelmetHeading) || HelmetHeading == GetExplicitDefaults(_profile).HelmetHeading;
        
        [JsonIgnore]
        public bool IsHMDElevLinesDefault => string.IsNullOrEmpty(HMDElevLines) || HMDElevLines == GetExplicitDefaults(_profile).HMDElevLines;
        [JsonIgnore]
        public int HMDElevLinesValue => string.IsNullOrEmpty(HMDElevLines) ? int.Parse(GetExplicitDefaults(_profile).HMDElevLines) : int.Parse(HMDElevLines);

        // ---- non-property members

        private Profiles _profile;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------
        [JsonConstructor]
        public HMCSProfileSettings(Profiles profile)
        {
            _profile = profile;
            Reset();
        }

        public HMCSProfileSettings(HMCSProfileSettings other) : this(other._profile)
        {
            CopySettings(other, this);
        }

        public virtual object Clone() => new HMCSProfileSettings(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // member methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public void Reset()
        {
            HMCSProfileSettings sourceSettings = _profile switch
            {
                Profiles.PRO1 => _pro1Defaults,
                Profiles.PRO2 => _pro2Defaults,
                Profiles.PRO3 => _pro3Defaults,
                _ => throw new System.ApplicationException("Unexpected profile: " + _profile)
            };
            if (sourceSettings != null) // static constructor
                CopySettings(sourceSettings, this);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // static members
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly static HMCSProfileSettings _pro1Defaults;
        private readonly static HMCSProfileSettings _pro2Defaults;
        private readonly static HMCSProfileSettings _pro3Defaults;

        private static HMCSProfileSettings GetExplicitDefaults(Profiles profile)
        {
            return profile switch
            {
                Profiles.PRO1 => _pro1Defaults,
                Profiles.PRO2 => _pro2Defaults,
                Profiles.PRO3 => _pro3Defaults,
                _ => throw new System.ApplicationException("Unexpected profile: " + profile)
            };
        }

        static HMCSProfileSettings()
        {
            // 0 = OFF
            // 1 = OCLD
            // 2 = ON

            _pro1Defaults = new(Profiles.PRO1)
            {
                Crosshair = "1",
                OwnSPI = "1",
                SPIInIndicator = "1",
                HorizonLine = "1",
                HDC = "1",
                Hookship = "1",
                TGPDiamond = "1",
                TGPFOV = "1",
                FlightMembers = "1",
                FlightMembersRange = "50",
                FMSPI = "1",
                FMSPIRange = "50",
                DonorAirPPLI = "1",
                DonorAirPPLIRange = "50",
                DonorSPI = "1",
                DonorSPIRange = "50",
                CurrentMA = "1",
                CurrentMARange = "50",
                AirEnvir = "1",
                AirPPLINonDonor = "1",
                AirPPLINonDonorRange = "50",
                GndEnvir = "1",
                GndVMFFriend = "1",
                GndVMFFriendRange = "50",
                Steerpoint = "1",
                SteerpointRange = "50",
                MsnMarkpoints = "1",
                MsnMarkpointsRange = "50",
                MsnMarkLabels = "1",
                Airspeed = "1",
                RadarAltitude = "1",
                BaroAltitude = "1",
                ACHeading = "1",
                HelmetHeading = "1",
                HMDElevLines = "1"
            };

            _pro2Defaults = (HMCSProfileSettings)_pro1Defaults.Clone();
            _pro2Defaults._profile = Profiles.PRO2;
            _pro2Defaults.DonorSPI = "0";
            _pro2Defaults.HMDElevLines = "0";

            _pro3Defaults = (HMCSProfileSettings)_pro2Defaults.Clone();
            _pro3Defaults._profile = Profiles.PRO3;
            _pro3Defaults.OwnSPI = "0";
            _pro3Defaults.SPIInIndicator = "0";
            _pro3Defaults.Hookship = "0";
            _pro3Defaults.TGPFOV = "0";
            _pro3Defaults.FlightMembers = "0";
            _pro3Defaults.FMSPI = "0";
            _pro3Defaults.DonorAirPPLI = "0";
            _pro3Defaults.DonorSPI = "0";
            _pro3Defaults.CurrentMA = "0";
            _pro3Defaults.AirEnvir = "0";
            _pro3Defaults.AirPPLINonDonor = "0";
            _pro3Defaults.GndEnvir = "0";
            _pro3Defaults.GndVMFFriend = "0";
        }

        public static void CopySettings(HMCSProfileSettings src, HMCSProfileSettings dest)
        {
            dest.Crosshair = src.Crosshair;
            dest.OwnSPI = src.OwnSPI;
            dest.SPIInIndicator = src.SPIInIndicator;
            dest.HorizonLine = src.HorizonLine;
            dest.HDC = src.HDC;
            dest.Hookship = src.Hookship;
            dest.TGPDiamond = src.TGPDiamond;
            dest.TGPFOV = src.TGPFOV;
            dest.FlightMembers = src.FlightMembers;
            dest.FlightMembersRange = src.FlightMembersRange;
            dest.FMSPI = src.FMSPI;
            dest.FMSPIRange = src.FMSPIRange;
            dest.DonorAirPPLI = src.DonorAirPPLI;
            dest.DonorAirPPLIRange = src.DonorAirPPLIRange;
            dest.DonorSPI = src.DonorSPI;
            dest.DonorSPIRange = src.DonorSPIRange;
            dest.CurrentMA = src.CurrentMA;
            dest.CurrentMARange = src.CurrentMARange;
            dest.AirEnvir = src.AirEnvir;
            dest.AirPPLINonDonor = src.AirPPLINonDonor;
            dest.AirPPLINonDonorRange  = src.AirPPLINonDonorRange;
            dest.GndEnvir = src.GndEnvir;
            dest.GndVMFFriend = src.GndVMFFriend;
            dest.GndVMFFriendRange = src.GndVMFFriendRange;
            dest.Steerpoint = src.Steerpoint;
            dest.SteerpointRange = src.SteerpointRange;
            dest.MsnMarkpoints = src.MsnMarkpoints;
            dest.MsnMarkpointsRange  = src.MsnMarkpointsRange;
            dest.MsnMarkLabels = src.MsnMarkLabels;
            dest.Airspeed = src.Airspeed;
            dest.RadarAltitude = src.RadarAltitude;
            dest.BaroAltitude = src.BaroAltitude;
            dest.ACHeading = src.ACHeading;
            dest.HelmetHeading = src.HelmetHeading;
            dest.HMDElevLines = src.HMDElevLines;
        }
    }
}
