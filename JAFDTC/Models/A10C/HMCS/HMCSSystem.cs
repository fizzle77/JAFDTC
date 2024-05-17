// ********************************************************************************************************************
//
// HMCSSystem.cs -- a-10c hmcs profile system
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

namespace JAFDTC.Models.A10C.HMCS
{
    // Defines the available HMCS profiles
    public enum Profiles
    {
        PRO1 = 1,
        PRO2 = 2,
        PRO3 = 3
    }

    // Defines the TGP track to be set from the HMCS
    public enum TGPTrackOptions
    {
        INR = 0,
        AREA = 1,
        POINT = 2
    }

    // Defines the day/night brightness options
    public enum BrightnessSettingOptions
    {
        DEFAULT = 0, // Pre-set to day or night base on mission start time.
        DAY = 1,
        NIGHT = 2
    }

    public class HMCSSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:A10C:HMCS";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- properties that post change and validation events

        // Which of the 3 HMCS profiles is set active
        private string _activeProfile;
        public string ActiveProfile
        {
            get => _activeProfile;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 3)) ? null : "Invalid format";
                SetProperty(ref _activeProfile, value, error);
            }
        }

        // Which TGP track mode should be used when
        // set from HMCS and DMS-right-long.
        private string _tgpTrack;
        public string TGPTrack
        {
            get => _tgpTrack;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _tgpTrack, value, error);
            }
        }

        // Which day/night brightness mode should be used.
        private string _brightnessSetting;
        public string BrightnessSetting
        {
            get => _brightnessSetting;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _brightnessSetting, value, error);
            }
        }

        [JsonPropertyName("ProfileSettings")]
        public Dictionary<Profiles, HMCSProfileSettings> ProfileSettingMap
        {
            get => _profileSettingMap;
            set => _profileSettingMap = value;
        }
        private Dictionary<Profiles, HMCSProfileSettings> _profileSettingMap;

        // ---- synthesized properties

        [JsonIgnore]
        public bool IsDefault
        {
            get
            {
                if (!(IsActiveProfileDefault && IsTGPTrackDefault && IsBrightnessSettingDefault))
                    return false;

                foreach (var profile in ProfileSettingMap.Values)
                {
                    if (!profile.IsDefault)
                        return false;
                }
                return true;   
            }
        }

        [JsonIgnore]
        public bool IsActiveProfileDefault => string.IsNullOrEmpty(ActiveProfile) || ActiveProfile == _explicitDefaults.ActiveProfile;

        [JsonIgnore]
        public bool IsTGPTrackDefault => string.IsNullOrEmpty(TGPTrack) || TGPTrack == _explicitDefaults.TGPTrack;

        [JsonIgnore]
        public bool IsBrightnessSettingDefault => string.IsNullOrEmpty(BrightnessSetting) || BrightnessSetting == _explicitDefaults.BrightnessSetting;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public HMCSSystem()
        {
            Reset();
        }

        public HMCSSystem(HMCSSystem other)
        {
            ActiveProfile = other.ActiveProfile;
            TGPTrack = other.TGPTrack;
            BrightnessSetting = other.BrightnessSetting;
            foreach (var kv in ProfileSettingMap)
                other._profileSettingMap.Add(kv.Key, (HMCSProfileSettings)kv.Value.Clone());
        }

        public virtual object Clone() => new HMCSSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // member methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public void Reset()
        {
            ActiveProfile = _explicitDefaults.ActiveProfile;
            TGPTrack = _explicitDefaults.TGPTrack;
            BrightnessSetting = _explicitDefaults.BrightnessSetting;
        }

        public HMCSProfileSettings GetProfileSettings(Profiles profile)
        {
            if (!_profileSettingMap.TryGetValue(profile, out HMCSProfileSettings profileSettings))
            {
                profileSettings = new HMCSProfileSettings(profile);
                _profileSettingMap.Add(profile, profileSettings);
            }
            return profileSettings;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // static members
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly static HMCSSystem _explicitDefaults = new()
        {
            ActiveProfile = "1",
            TGPTrack = "0",
            BrightnessSetting = "0"
        };

    }
}
