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
                if (!IsLaserCodeDefault)
                    return false;
                foreach (MunitionSettings setting in _munitionSettingMap.Values)
                {
                    if (!setting.IsDefault)
                        return false;
                }
                return true;
            }
        }

        [JsonIgnore]
        public bool IsLaserCodeDefault => LaserCode == ExplicitDefaults.LaserCode;

        // HACK TODO?
        [JsonIgnore]
        public Dictionary<int, string> Loadout { set; get; }

        // Lazy-load munitions DB
        private static List<A10CMunition> _munitions;
        private static List<A10CMunition> GetMunitions()
        {
            if (_munitions == null)
                _munitions = FileManager.LoadA10Munitions();
            return _munitions;
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

        public MunitionSettings GetMunitionSettings(A10CMunition munition)
        {
            MunitionSettings settings;
            if (!_munitionSettingMap.TryGetValue(munition.Key, out settings))
            {
                settings = new MunitionSettings(munition);
                _munitionSettingMap.Add(munition.Key, settings);
            }
            return settings;
        }

        // reset the instance to defaults
        public void Reset()
        {
            LaserCode = "";
            _munitionSettingMap = new Dictionary<string, MunitionSettings>();
        }

        internal void FixupMunitionReferences()
        {
            List<A10CMunition> munitions = GetMunitions();
            foreach (var munition in munitions)
            {
                if (_munitionSettingMap.TryGetValue(munition.Key, out MunitionSettings setting))
                    setting.Munition = munition;
            }
        }

        //
        // MunitionSettings accessors
        //

        // Get munition settings that have non-default settings on the jet's INV page.
        public Dictionary<string, MunitionSettings> GetNonDefaultInvSettings()
        {
            Dictionary<string, MunitionSettings> settings = new Dictionary<string, MunitionSettings>();
            foreach (KeyValuePair<string, MunitionSettings> kv in _munitionSettingMap)
            {
                if (!kv.Value.IsInvDefault)
                    settings.Add(kv.Key, kv.Value);
                else if (kv.Value.Munition.Laser && !IsLaserCodeDefault)
                    settings.Add(kv.Key, kv.Value);
            }

            // If laser code is non-default, ensure all laser weapons are added to list
            // even if they have no other non-default settings.
            List<A10CMunition> munitions = GetMunitions();
            if (!IsLaserCodeDefault)
            {
                foreach (A10CMunition munition in munitions)
                    if (munition.Laser && !settings.ContainsKey(munition.Key))
                        settings.Add(munition.Key, GetMunitionSettings(munition));
            }
            return settings;
        }

        public void SetAutoLase(A10CMunition munition, string value) => GetMunitionSettings(munition).AutoLase = value;
        public string GetAutoLase(A10CMunition munition) => GetMunitionSettings(munition).AutoLase;
        public bool GetAutoLaseValue(A10CMunition munition)
        {
            string s = GetAutoLase(munition);
            return bool.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.AutoLase : s);
        }

        public void SetLaseSeconds(A10CMunition munition, string value) => GetMunitionSettings(munition).LaseSeconds = value;
        public string GetLaseSeconds(A10CMunition munition) => GetMunitionSettings(munition).LaseSeconds;

        public void SetDeliveryMode(A10CMunition munition, string value) => GetMunitionSettings(munition).DeliveryMode = value;
        public string GetDeliveryMode(A10CMunition munition) => GetMunitionSettings(munition).DeliveryMode;
        public DeliveryModes GetDeliveryModeValue(A10CMunition munition)
        {
            string s = GetDeliveryMode(munition);
            return (DeliveryModes)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.DeliveryMode : s);
        }

        public void SetEscapeManeuver(A10CMunition munition, string value) => GetMunitionSettings(munition).EscapeManeuver = value;
        public string GetEscapeManeuver(A10CMunition munition) => GetMunitionSettings(munition).EscapeManeuver;
        public EscapeManeuvers GetEscapeManeuverValue(A10CMunition munition)
        {
            string s = GetEscapeManeuver(munition);
            return (EscapeManeuvers)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.EscapeManeuver : s);
        }

        public void SetReleaseMode(A10CMunition munition, string value) => GetMunitionSettings(munition).ReleaseMode = value;
        public string GetReleaseMode(A10CMunition munition) => GetMunitionSettings(munition).ReleaseMode;
        public ReleaseModes GetReleaseModeValue(A10CMunition munition)
        {
            string s = GetReleaseMode(munition);
            return (ReleaseModes)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.ReleaseMode : s);
        }

        public void SetRippleQty(A10CMunition munition, string value) => GetMunitionSettings(munition).RippleQty = value;
        public string GetRippleQty(A10CMunition munition) => GetMunitionSettings(munition).RippleQty;

        public void SetRippleFt(A10CMunition munition, string value) => GetMunitionSettings(munition).RippleFt = value;
        public string GetRippleFt(A10CMunition munition) => GetMunitionSettings(munition).RippleFt;

        public void SetHOFOption(A10CMunition munition, string value) => GetMunitionSettings(munition).HOFOption = value;
        public string GetHOFOption(A10CMunition munition) => GetMunitionSettings(munition).HOFOption;
        public HOFOptions GetHOFOptionValue(A10CMunition munition)
        {
            string s = GetHOFOption(munition);
            return (HOFOptions)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.HOFOption : s);
        }

        public void SetRPMOption(A10CMunition munition, string value) => GetMunitionSettings(munition).RPMOption = value;
        public string GetRPMOption(A10CMunition munition) => GetMunitionSettings(munition).RPMOption;
        public RPMOptions GetRPMOptionValue(A10CMunition munition)
        {
            string s = GetRPMOption(munition);
            return (RPMOptions)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.RPMOption : s);
        }

        public void SetFuzeOption(A10CMunition munition, string value) => GetMunitionSettings(munition).FuzeOption = value;
        public string GetFuzeOption(A10CMunition munition) => GetMunitionSettings(munition).FuzeOption;
        public FuzeOptions GetFuzeOptionValue(A10CMunition munition)
        {
            string s = GetFuzeOption(munition);
            return (FuzeOptions)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.FuzeOption : s);
        }
    }
}
