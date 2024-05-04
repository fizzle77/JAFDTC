// ********************************************************************************************************************
//
// DSMSSystem.cs -- a-10c dsms system
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
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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
        CLB = 1,
        TRN = 2,
        TLT = 3
    }

    // defines the possible release mode settings
    // must match combobox order in ui
    public enum ReleaseModes
    {
        SGL = 0,
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
        HOF_1800 = 6,
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
        RPM_1500 = 3,
        RPM_2000 = 4,
        RPM_2500 = 5
    }

    // defines the Fuze settings
    // must match combobox order in ui
    public enum FuzeOptions
    {
        NoseTail = 0,
        Nose = 1,
        Tail = 2
    }

    public class DSMSSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:A10C:DSMS";


        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private static readonly Regex _laserCodeRegex = new(@"^1[1-7][1-8][1-8]$");

        // ---- properties that post change and validation events

        private string _laserCode;                           // laser code, [1][1-7][1-8][1-8]
        public string LaserCode
        {
            get => _laserCode;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsRegexFieldValid(value, _laserCodeRegex))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _laserCode, value, error);
            }
        }

        [JsonPropertyName("MunitionSettings")]
        public Dictionary<string, MunitionSettings> MunitionSettingMap
        {
            get => _munitionSettingMap;
            // I would prefer to not have a public setter but this makes JSON deserialization work.
            set => _munitionSettingMap = value;
        }
        private Dictionary<string, MunitionSettings> _munitionSettingMap;

        // ---- synthesized properties

        public readonly static DSMSSystem ExplicitDefaults = new()
        {
            LaserCode = "1688"
        };

        [JsonIgnore]
        public bool IsDefault
        {
            get
            {
                if (LaserCode != ExplicitDefaults.LaserCode)
                    return false;
                foreach (MunitionSettings setting in _munitionSettingMap.Values)
                {
                    if (!setting.IsDefault)
                        return false;
                }
                return true;
            }
        }


        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public DSMSSystem()
        {
            Reset();
        }

        public DSMSSystem(DSMSSystem other)
        {
            LaserCode = other.LaserCode;
            _munitionSettingMap = other._munitionSettingMap;
        }

        public virtual object Clone() => new DSMSSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public MunitionSettings GetMunitionSettings(string key)
        {
            MunitionSettings settings;
            if (!_munitionSettingMap.TryGetValue(key, out settings))
            {
                settings = new MunitionSettings();
                _munitionSettingMap.Add(key, settings);
            }
            return settings;
        }

        // reset the instance to defaults
        public void Reset()
        {
            LaserCode = "";
            _munitionSettingMap = new Dictionary<string, MunitionSettings>();
        }

        //
        // MunitionSettings accessors
        //
        public void SetAutoLase(string key, string value) => GetMunitionSettings(key).AutoLase = value;
        public string GetAutoLase(string key) => GetMunitionSettings(key).AutoLase;
        public bool GetAutoLaseValue(string key)
        {
            string s = GetAutoLase(key);
            return bool.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.AutoLase : s);
        }

        public void SetLaseSeconds(string key, string value) => GetMunitionSettings(key).LaseSeconds = value;
        public string GetLaseSeconds(string key) => GetMunitionSettings(key).LaseSeconds;

        public void SetDeliveryMode(string key, string value) => GetMunitionSettings(key).DeliveryMode = value;
        public string GetDeliveryMode(string key) => GetMunitionSettings(key).DeliveryMode;
        public DeliveryModes GetDeliveryModeValue(string key)
        {
            string s = GetDeliveryMode(key);
            return (DeliveryModes)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.DeliveryMode : s);
        }

        public void SetEscapeManeuver(string key, string value) => GetMunitionSettings(key).EscapeManeuver = value;
        public string GetEscapeManeuver(string key) => GetMunitionSettings(key).EscapeManeuver;
        public EscapeManeuvers GetEscapeManeuverValue(string key)
        {
            string s = GetEscapeManeuver(key);
            return (EscapeManeuvers)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.EscapeManeuver : s);
        }

        public void SetReleaseMode(string key, string value) => GetMunitionSettings(key).ReleaseMode = value;
        public string GetReleaseMode(string key) => GetMunitionSettings(key).ReleaseMode;
        public ReleaseModes GetReleaseModeValue(string key)
        {
            string s = GetReleaseMode(key);
            return (ReleaseModes)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.ReleaseMode : s);
        }

        public void SetRippleQty(string key, string value) => GetMunitionSettings(key).RippleQty = value;
        public string GetRippleQty(string key) => GetMunitionSettings(key).RippleQty;

        public void SetRippleFt(string key, string value) => GetMunitionSettings(key).RippleFt = value;
        public string GetRippleFt(string key) => GetMunitionSettings(key).RippleFt;

        public void SetHOFOption(string key, string value) => GetMunitionSettings(key).HOFOption = value;
        public string GetHOFOption(string key) => GetMunitionSettings(key).HOFOption;
        public HOFOptions GetHOFOptionValue(string key)
        {
            string s = GetHOFOption(key);
            return (HOFOptions)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.HOFOption : s);
        }

        public void SetRPMOption(string key, string value) => GetMunitionSettings(key).RPMOption = value;
        public string GetRPMOption(string key) => GetMunitionSettings(key).RPMOption;
        public RPMOptions GetRPMOptionValue(string key)
        {
            string s = GetRPMOption(key);
            return (RPMOptions)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.RPMOption : s);
        }

        public void SetFuzeOption(string key, string value) => GetMunitionSettings(key).FuzeOption = value;
        public string GetFuzeOption(string key) => GetMunitionSettings(key).FuzeOption;
        public FuzeOptions GetFuzeOptionValue(string key)
        {
            string s = GetFuzeOption(key);
            return (FuzeOptions)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.FuzeOption : s);
        }
    }
}
