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

using JAFDTC.Models.A10C.HMCS;
using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F16C.SMS
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class SMSSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:F16C:SMS";

        /// <summary>
        /// munition identifiers for f-16c munitions.
        /// </summary>
        public enum Munitions
        {
            CBU_87 = 1,
            CBU_97 = 2,
            CBU_103 = 3,
            CBU_105 = 4,
            GBU_10 = 5,
            GBU_12 = 6,
            GBU_24 = 7,
            GBU_31 = 8,
            GBU_31P = 9,
            GBU_38 = 10,
            MK_82 = 11,
            MK_82SE = 12,
            MK_82AB = 13,
            MK_84 = 14,
            MK_84a = 15,
            MK_84b = 16,
            AGM_65D = 17,
            AGM_65G = 18,
            AGM_65H = 19,
            AGM_65K = 20
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
        public bool IsDefault
        {
            get
            {
                foreach (Dictionary<string, MunitionSettings> profiles in Settings.Values)
                {
                    foreach (MunitionSettings munition in profiles.Values)
                    {
                        if (!munition.IsDefault)
                        {
                            return false;
                        }
                    }         
                }
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
                {
                    profiles.Add(kvpProf.Key, (MunitionSettings)kvpProf.Value.Clone());
                }
            }
        }

        public virtual object Clone() => new SMSSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the instance to defaults.
        /// </summary>
        public void Reset()
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
                        {
                            newSettings.Add(kvpProfiles.Key, new());
                        }
                        Dictionary<string, MunitionSettings> profiles = newSettings[kvpProfiles.Key];
                        profiles.Add(kvpProfile.Key, new(kvpProfile.Value));
                    }
                }
            }
            Settings = newSettings;
        }

        /// <summary>
        /// return the munition settings for the specified profile of the specified munition. a new default
        /// MunitionSettings will be added to the settings (and returned) if the munition and/or profile are not
        /// present.
        /// </summary>
        public MunitionSettings GetSettingsForMunitionProfile(Munitions muni, string profile)
        {
            if (!Settings.ContainsKey(muni))
            {
                Settings.Add(muni, new());
            }
            Dictionary<string, MunitionSettings> profiles = Settings[muni];
            if (!profiles.ContainsKey(profile))
            {
                profiles.Add(profile, new());
            }
            return profiles[profile];
        }
    }
}
