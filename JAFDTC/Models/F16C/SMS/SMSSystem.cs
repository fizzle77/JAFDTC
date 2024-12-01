// ********************************************************************************************************************
//
// SMSSystem.cs -- f-16c sms system
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

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F16C.SMS
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class SMSSystem : SystemBase
    {
        public const string SystemTag = "JAFDTC:F16C:SMS";

        /// <summary>
        /// munition identifiers for known f-16c munitions the system can configure.
        /// </summary>
        public enum Munitions
        {
            Unknown = -1,
            CBU_87 = 0,
            CBU_97 = 1,
            CBU_103 = 2,
            CBU_105 = 3,
            GBU_10 = 4,
            GBU_12 = 5,
            GBU_24 = 6,
            GBU_31 = 7,
            GBU_31P = 8,
            GBU_38 = 9,
            MK_82_LD = 10,
            MK_82_HDSE = 11,
            MK_82_HDAB = 12,
            MK_84_LD = 13,
            MK_84_HD = 14,
            AGM_65D = 15,
            AGM_65G = 16,
            AGM_65H = 17,
            AGM_65K = 18
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events

        public Dictionary<Munitions, Dictionary<string, MunitionSettings>> Settings { get; set; }

        // ---- following properties are synthesized

        /// <summary>
        /// returns true if the instance indicates a default setup: either Settings is empty or it contains only
        /// default setups.
        /// </summary>
        [JsonIgnore]
        public override bool IsDefault
        {
            get
            {
                foreach (Dictionary<string, MunitionSettings> profiles in Settings.Values)
                    foreach (MunitionSettings munition in profiles.Values)
                        if (!munition.IsDefault)
                            return false;
                return true;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public SMSSystem()
        {
            Reset();
        }

        public SMSSystem(SMSSystem other)
        {
            Reset();
            foreach (KeyValuePair<Munitions, Dictionary<string, MunitionSettings>> kvpMuni in other.Settings)
            {
                Dictionary<string, MunitionSettings> profiles = new();
                Settings.Add(kvpMuni.Key, profiles);
                foreach (KeyValuePair<string, MunitionSettings> kvpProf in kvpMuni.Value)
                    profiles.Add(kvpProf.Key, (MunitionSettings)kvpProf.Value.Clone());
            }
        }

        public virtual object Clone() => new SMSSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // version update
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// update the sms system for the transition from v1.0 to v1.1. this involves changing the way ripple pulses
        /// are encoded in the munition settings for a gbu-24: in v1.1 they are encoded in RipplePulse rather than as
        /// different release modes.
        /// </summary>
        public void UpdateFrom10to11()
        {
            if (Settings.ContainsKey(Munitions.GBU_24))
            {
                foreach (KeyValuePair<string, MunitionSettings> kvpProfile in Settings[Munitions.GBU_24])
                {
                    MunitionSettings settings = kvpProfile.Value;
                    if (int.TryParse(settings.ReleaseMode, out int releaseMode))
                    {
                        settings.RipplePulse = (MunitionSettings.ReleaseModes)releaseMode switch
                        {
                            MunitionSettings.ReleaseModes.DEPRECATE_v11_GBU24_RP1 => "1",
                            MunitionSettings.ReleaseModes.DEPRECATE_v11_GBU24_RP2 => "2",
                            MunitionSettings.ReleaseModes.DEPRECATE_v11_GBU24_RP3 => "3",
                            MunitionSettings.ReleaseModes.DEPRECATE_v11_GBU24_RP4 => "4",
                            _ => ""
                        };
                    }
                    settings.ReleaseMode = "";
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the instance to defaults.
        /// </summary>
        public override void Reset()
        {
            Settings = new();
        }

        /// <summary>
        /// clean up the settings by walking through all profiles for all munitions and removing any default
        /// profiles from the settings.
        /// </summary>
        public void CleanUp()
        {
            Dictionary<Munitions, Dictionary<string, MunitionSettings>> newSettings = new();
            foreach (KeyValuePair<Munitions, Dictionary<string, MunitionSettings>> kvpProfiles in Settings)
            {
                foreach (KeyValuePair<string, MunitionSettings> kvpProfile in kvpProfiles.Value)
                {
                    if (!kvpProfile.Value.IsDefault)
                    {
                        if (!newSettings.ContainsKey(kvpProfiles.Key))
                            newSettings.Add(kvpProfiles.Key, new());
                        Dictionary<string, MunitionSettings> profiles = newSettings[kvpProfiles.Key];
                        profiles.Add(kvpProfile.Key, new(kvpProfile.Value));
                    }
                }
            }
            Settings = newSettings;
        }

        /// <summary>
        /// return the profiles defined for the specified munition in a dictionary keyed by the profile index.
        /// the dictionary is empty if there are no non-default profiles defined for the munition.
        /// </summary>
        public Dictionary<string, MunitionSettings> GetProfilesForMunition(Munitions muni)
        {
            return (Settings.ContainsKey(muni)) ? Settings[muni] : new();
        }

        /// <summary>
        /// return the munition settings for the specified profile of the specified munition. if isCreate is true,
        /// a new default MunitionSettings will be added to the settings (and returned) if the munition and/or profile
        /// are not present. otherwise, the method returns null if the settings are not defined.
        /// </summary>
        public MunitionSettings GetSettingsForMunitionProfile(Munitions muni, string profile, bool isCreate = true)
        {
            if (isCreate)
            {
                if (!Settings.ContainsKey(muni))
                    Settings.Add(muni, new());
                if (!Settings[muni].ContainsKey(profile))
                    Settings[muni].Add(profile, new MunitionSettings() { ID = muni, Profile = profile });
            }
            else if (!Settings.ContainsKey(muni) || !Settings[muni].ContainsKey(profile))
                return null;

            return Settings[muni][profile];
        }
    }
}
