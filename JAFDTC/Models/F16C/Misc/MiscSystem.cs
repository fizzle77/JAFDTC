﻿// ********************************************************************************************************************
//
// MiscSystem.cs -- f-16c miscellaneous system
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023-2025 ilominar/raven
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

using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.F16C.Misc
{
    /// <summary>
    /// miscellaneous system, tacan modes.
    /// </summary>
    public enum TACANModes
    {
        REC = 0,                // receive only
        TR = 1,                 // transmit/receive
        AA_TR = 2               // air-to-air transmit/receive
    }

    /// <summary>
    /// miscellaneous system, tacan bands.
    /// </summary>
    public enum TACANBands
    {
        X = 0,
        Y = 1
    }

    /// <summary>
    /// miscellaneous system, declutter levels for the jhmcs.
    /// </summary>
    public enum HMCSDeclutterLevels
    {
        LVL1 = 0,               // least declutter
        LVL2 = 1,
        LVL3 = 2                // most declutter
    }

    // ================================================================================================================

    /// <summary>
    /// ISystem object to persist the miscellaneous system settings for the viper. this includes setup for the
    /// tacan, bingo, alow, bullseye, ils, laser, and jhmcs systems in the jet.
    /// </summary>
    public class MiscSystem : SystemBase
    {
        public const string SystemTag = "JAFDTC:F16C:MISC";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events

        private static readonly Regex _ilsRegex = new(@"^(10[89]\.[13579]{1}[05]{1})|(11[01]\.[13579]{1}[05]{1})$");
        private static readonly Regex _laserRegex = new(@"^[1-2][1-8][1-8][1-8]$");

        // ---- following properties post change and validation events

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

        private string _bullseyeWP;                             // integer [1, 99]
        public string BullseyeWP
        {
            get => _bullseyeWP;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 1, 99))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _bullseyeWP, value, error);
            }
        }

        private string _bullseyeMode;                           // string (boolean)
        public string BullseyeMode
        {
            get => _bullseyeMode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : "Invalid format";
                SetProperty(ref _bullseyeMode, value, error);
            }
        }

        private string _alowCARAALOW;                           // integer [0, 50000]
        public string ALOWCARAALOW
        {
            get => _alowCARAALOW;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 0, 50000))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _alowCARAALOW, value, error);
            }
        }

        private string _alowMSLFloor;                           // integer [0, 80000]
        public string ALOWMSLFloor
        {
            get => _alowMSLFloor;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 0, 80000))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _alowMSLFloor, value, error);
            }
        }

        private string _laserTgpCode;                           // laser code, [1-2][1-8][1-8][1-8]
        public string LaserTGPCode
        {
            get => _laserTgpCode;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsRegexFieldValid(value, _laserRegex))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _laserTgpCode, value, error);
            }
        }

        private string _laserLstCode;                           // laser code, [1-2][1-8][1-8][1-8]
        public string LaserLSTCode
        {
            get => _laserLstCode;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsRegexFieldValid(value, _laserRegex))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _laserLstCode, value, error);
            }
        }

        private string _laserStartTime;                         // integer [0, 176]
        public string LaserStartTime
        {
            get => _laserStartTime;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 0, 176))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _laserStartTime, value, error);
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

        private string _tacanBand;                              // integer [0, 1], TACANBands
        public string TACANBand
        {
            get => _tacanBand;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _tacanBand, value, error);
            }
        }

        private string _tacanMode;                              // integer [0, 2], TACANModes
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

        private string _ilsCourse;                              // integer [0, 359]
        public string ILSCourse
        {
            get => _ilsCourse;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 0, 359))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _ilsCourse, value, error);
            }
        }

        private string _hmcsBlankHUD;                           // string (boolean)
        public string HMCSBlankHUD
        {
            get => _hmcsBlankHUD;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : "Invalid format";
                SetProperty(ref _hmcsBlankHUD, value, error);
            }
        }

        private string _hmcsBlankCockpit;                       // string (boolean)
        public string HMCSBlankCockpit
        {
            get => _hmcsBlankCockpit;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : "Invalid format";
                SetProperty(ref _hmcsBlankCockpit, value, error);
            }
        }

        private string _hmcsDisplayRWR;                         // string (boolean)
        public string HMCSDisplayRWR
        {
            get => _hmcsDisplayRWR;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : "Invalid format";
                SetProperty(ref _hmcsDisplayRWR, value, error);
            }
        }

        private string _hmcsDeclutterLvl;                       // integer [0, 2], HMCSDeclutterLevels
        public string HMCSDeclutterLvl
        {
            get => _hmcsDeclutterLvl;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _hmcsDeclutterLvl, value, error);
            }
        }

        private string _hmcsIntensity;                          // double [0.0, 1.0]
        public string HMCSIntensity
        {
            get => _hmcsIntensity;
            set => SetProperty(ref _hmcsIntensity, value);
        }

        // ---- following properties are synthesized

        // returns a MFDSystem with the fields populated with the actual default values (note that usually the value
        // "" implies default).
        //
        // defaults are as of DCS v2.9.0.47168.
        //
        public readonly static MiscSystem ExplicitDefaults = new()
        {
            Bingo = "2000",
            BullseyeWP = "25",
            BullseyeMode = false.ToString(),
            ALOWCARAALOW = "500",
            ALOWMSLFloor = "5000",
            LaserTGPCode = "0",
            LaserLSTCode = "0",
            LaserStartTime = "8",
            TACANChannel = "1",
            TACANBand = ((int)TACANBands.X).ToString(),
            TACANMode = ((int)TACANModes.REC).ToString(),
            ILSFrequency = "108.10",
            ILSCourse = "0",
            HMCSBlankHUD = true.ToString(),
            HMCSBlankCockpit = true.ToString(),
            HMCSDisplayRWR = true.ToString(),
            HMCSDeclutterLvl = ((int)HMCSDeclutterLevels.LVL1).ToString(),
            HMCSIntensity = "0.0"
        };

        // returns true if the instance indicates a default setup (all fields are "") or the object is in explicit
        // form, false otherwise.
        //
        [JsonIgnore]
        public override bool IsDefault
        {
            get => (IsBINGODefault && IsBULLDefault && IsALOWDefault && IsLaserDefault && IsTACANDefault &&
                    IsILSDefault && IsHMCSDefault);
        }

        [JsonIgnore]
        public bool IsBINGODefault
        {
            get => (string.IsNullOrEmpty(Bingo) || (Bingo == ExplicitDefaults.Bingo));
        }

        [JsonIgnore]
        public bool IsBULLDefault
        {
            get => ((string.IsNullOrEmpty(BullseyeWP) || (BullseyeWP == ExplicitDefaults.BullseyeWP)) &&
                    (string.IsNullOrEmpty(BullseyeMode) || (BullseyeMode == ExplicitDefaults.BullseyeMode)));
        }

        [JsonIgnore]
        public bool IsALOWDefault
        {
            get => ((string.IsNullOrEmpty(ALOWCARAALOW) || (ALOWCARAALOW == ExplicitDefaults.ALOWCARAALOW)) &&
                    (string.IsNullOrEmpty(ALOWMSLFloor)) || (ALOWMSLFloor == ExplicitDefaults.ALOWMSLFloor));
        }

        [JsonIgnore]
        public bool IsLaserDefault
        {
            get => ((string.IsNullOrEmpty(LaserTGPCode) || (LaserTGPCode == ExplicitDefaults.LaserTGPCode)) &&
                    (string.IsNullOrEmpty(LaserLSTCode) || (LaserLSTCode == ExplicitDefaults.LaserLSTCode)) &&
                    (string.IsNullOrEmpty(LaserStartTime) || (LaserStartTime == ExplicitDefaults.LaserStartTime)));
        }

        [JsonIgnore]
        public bool IsTACANDefault
        {
            get => ((string.IsNullOrEmpty(TACANChannel) || (TACANChannel == ExplicitDefaults.TACANChannel)) &&
                    (string.IsNullOrEmpty(TACANBand) || (TACANBand == ExplicitDefaults.TACANBand)) &&
                    (string.IsNullOrEmpty(TACANMode) || (TACANMode == ExplicitDefaults.TACANMode)));
        }

        [JsonIgnore]
        public bool IsILSDefault
        {
            get => ((string.IsNullOrEmpty(ILSFrequency) || (ILSFrequency == ExplicitDefaults.ILSFrequency)) &&
                    (string.IsNullOrEmpty(ILSCourse) || (ILSCourse == ExplicitDefaults.ILSCourse)));
        }

        [JsonIgnore]
        public bool IsHMCSDefault
        {
            get => ((string.IsNullOrEmpty(HMCSBlankHUD) || (HMCSBlankHUD == ExplicitDefaults.HMCSBlankHUD)) &&
                    (string.IsNullOrEmpty(HMCSBlankCockpit) || (HMCSBlankCockpit == ExplicitDefaults.HMCSBlankCockpit)) &&
                    (string.IsNullOrEmpty(HMCSDisplayRWR) || (HMCSDisplayRWR == ExplicitDefaults.HMCSDisplayRWR)) &&
                    (string.IsNullOrEmpty(HMCSDeclutterLvl) || (HMCSDeclutterLvl == ExplicitDefaults.HMCSDeclutterLvl)) &&
                    (string.IsNullOrEmpty(HMCSIntensity) || (HMCSIntensity == ExplicitDefaults.HMCSIntensity)));
        }

        // ---- following accessors get the current value (default or non-default) for various properties

        [JsonIgnore]
        public TACANBands TACANBandValue
        {
            get => (TACANBands)int.Parse((string.IsNullOrEmpty(TACANBand)) ? ExplicitDefaults.TACANBand : TACANBand);
        }

        public TACANModes TACANModeValue
        {
            get => (TACANModes)int.Parse((string.IsNullOrEmpty(TACANMode)) ? ExplicitDefaults.TACANMode : TACANMode);
        }

        [JsonIgnore]
        public bool HMCSBlankHUDValue
        {
            get => bool.Parse((string.IsNullOrEmpty(HMCSBlankHUD)) ? ExplicitDefaults.HMCSBlankHUD : HMCSBlankHUD);
        }

        [JsonIgnore]
        public bool HMCSBlankCockpitValue
        {
            get => bool.Parse((string.IsNullOrEmpty(HMCSBlankCockpit)) ? ExplicitDefaults.HMCSBlankCockpit : HMCSBlankCockpit);
        }

        [JsonIgnore]
        public bool HMCSDisplayRWRValue
        {
            get => bool.Parse((string.IsNullOrEmpty(HMCSDisplayRWR)) ? ExplicitDefaults.HMCSDisplayRWR : HMCSDisplayRWR);
        }

        [JsonIgnore]
        public HMCSDeclutterLevels HMCSDeclutterLvlValue
        {
            get => (HMCSDeclutterLevels)int.Parse((string.IsNullOrEmpty(HMCSDeclutterLvl)) ? ExplicitDefaults.HMCSDeclutterLvl : HMCSDeclutterLvl);
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
            Bingo = new(other.Bingo);
            BullseyeWP = new(other.BullseyeWP);
            BullseyeMode = other.BullseyeMode;
            ALOWCARAALOW = new(other.ALOWCARAALOW);
            ALOWMSLFloor = new(other.ALOWMSLFloor);
            LaserTGPCode = new(other.LaserTGPCode);
            LaserLSTCode = new(other.LaserLSTCode);
            LaserStartTime = new(other.LaserStartTime);
            TACANChannel = new(other.TACANChannel);
            TACANBand = new(other.TACANBand);
            TACANMode = new(other.TACANMode);
            ILSFrequency = new(other.ILSFrequency);
            ILSCourse = new(other.ILSCourse);
            HMCSBlankHUD = new(other.HMCSBlankHUDValue.ToString());
            HMCSBlankCockpit = new(other.HMCSBlankCockpitValue.ToString());
            HMCSDisplayRWR = new(other.HMCSDisplayRWR.ToString());
            HMCSDeclutterLvl = new(other.HMCSDeclutterLvl);
            HMCSIntensity = new(other.HMCSIntensity);
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
            Bingo = "";
            BullseyeWP = "";
            BullseyeMode = "";
            ALOWCARAALOW = "";
            ALOWMSLFloor = "";
            LaserTGPCode = "";
            LaserLSTCode = "";
            LaserStartTime = "";
            TACANChannel = "";
            TACANBand = "";
            TACANMode = "";
            ILSFrequency = "";
            ILSCourse = "";
            HMCSBlankHUD = true.ToString();
            HMCSBlankCockpit = true.ToString();
            HMCSDisplayRWR = true.ToString();
            HMCSDeclutterLvl = "";
            HMCSIntensity = "";
        }
    }
}
