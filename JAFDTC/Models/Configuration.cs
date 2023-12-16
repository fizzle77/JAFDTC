// ********************************************************************************************************************
//
// Configuration.cs -- abstract base class for airframe configuration
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023 ilominar/raven
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

using JAFDTC.Models.A10C;
using JAFDTC.Models.AV8B;
using JAFDTC.Models.F15E;
using JAFDTC.Models.F16C;
using JAFDTC.Models.FA18C;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using static JAFDTC.Models.IConfiguration;

namespace JAFDTC.Models
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public abstract class Configuration : IConfiguration, INotifyPropertyChanged
    {
        public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change or validation events.

        public string Version { get; set; }

        public AirframeTypes Airframe { get; private set; }

        public string UID { get; protected set; }

        public string Filename { get; set; }

        public Dictionary<string, string> LinkedSysMap { get; private set; }

        public int LastSystemEdited { get; set; }

        // ---- following properties post change or validation events.

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        private string _updatesInfoText;
        [JsonIgnore]
        public string UpdatesInfoText
        {
            get => _updatesInfoText;
            set
            {
                if (_updatesInfoText != value)
                {
                    _updatesInfoText = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        private string _updatesIcons;
        [JsonIgnore]
        public string UpdatesIcons
        {
            get => _updatesIcons;
            set
            {
                if (_updatesIcons != value)
                {
                    _updatesIcons = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        private string _updatesIconBadges;
        [JsonIgnore]
        public string UpdatesIconBadges
        {
            get => _updatesIconBadges;
            set
            {
                if (_updatesIconBadges != value)
                {
                    _updatesIconBadges = value;
                    OnPropertyChanged();
                }
            }
        }

        // ---- following properties are synthesized.

        [JsonIgnore]
        public virtual IUploadAgent UploadAgent { get; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        [JsonConstructor]
        public Configuration(string version, AirframeTypes airframe, string uid, string name,
                             Dictionary<string, string> linkedSysMap)
            => (Version, Airframe, UID, Name, LinkedSysMap) = (version, airframe, uid, name, linkedSysMap);

        // NOTE: when cloning, derived classes should call ResetUID() on the clone prior to returning it.
        //
        public abstract IConfiguration Clone();

        public abstract void CloneSystemFrom(string systemTag, IConfiguration other);

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event ConfigurationSavedEventHandler ConfigurationSaved;
        protected virtual void OnConfigurationSaved(object invokedBy = null, string systemTagHint = null)
        {
            ConfigurationSaved?.Invoke(this, new ConfigurationSavedEventArgs(invokedBy, this, systemTagHint));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IConfiguration methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public void ResetUID()
        {
            UID = Guid.NewGuid().ToString();
        }

        public void LinkSystemTo(string systemTag, IConfiguration linkedConfig)
        {
            LinkedSysMap ??= new Dictionary<string, string>();
            LinkedSysMap[systemTag] = linkedConfig.UID;
            CloneSystemFrom(systemTag, linkedConfig);
            ConfigurationUpdated();
        }

        public void UnlinkSystem(string systemTag)
        {
            if ((LinkedSysMap != null) && (LinkedSysMap.ContainsKey(systemTag)))
            {
                LinkedSysMap.Remove(systemTag);
                ConfigurationUpdated();
            }
        }

        public void Save(object invokedBy = null, string syncSysTag = null)
        {
            FileManager.SaveConfigurationFile(this);
            ConfigurationUpdated();
            OnConfigurationSaved(invokedBy, syncSysTag);
        }

        public string SystemLinkedTo(string systemTag)
        {
            return ((LinkedSysMap != null) && LinkedSysMap.ContainsKey(systemTag)) ? LinkedSysMap[systemTag] : null;
        }

        public void CleanupSystemLinks(List<string> validUIDs)
        {
            List<string> invalidUIDs = new List<string>();
            foreach (KeyValuePair<string, string> kvp in LinkedSysMap)
            {
                if (!validUIDs.Contains(kvp.Key))
                {
                    invalidUIDs.Add(kvp.Key);
                }
            }
            foreach (string uid in invalidUIDs)
            {
                LinkedSysMap.Remove(uid);
            }
        }

        public abstract void ConfigurationUpdated();

        public abstract string Serialize(string systemTag = null);

        public abstract bool Deserialize(string systemTag, string json);

        public abstract void AfterLoadFromJSON();

        public abstract bool CanAcceptPasteForSystem(string cboardTag, string systemTag = null);

        // ------------------------------------------------------------------------------------------------------------
        //
        // factories
        //
        // ------------------------------------------------------------------------------------------------------------

        // factory to create a new configuration instance of the proper type bassed on airframe. the configuration is
        // set up with avionics defaults.
        //
        static public IConfiguration Factory(AirframeTypes airframe, string name)
        {
            return airframe switch
            {
                AirframeTypes.A10C  => new A10CConfiguration(Guid.NewGuid().ToString(), name, new Dictionary<string, string>()),
                AirframeTypes.AH64D => null,
                AirframeTypes.AV8B  => new AV8BConfiguration(Guid.NewGuid().ToString(), name, new Dictionary<string, string>()),
                AirframeTypes.F15E  => new F15EConfiguration(Guid.NewGuid().ToString(), name, new Dictionary<string, string>()),
                AirframeTypes.F16C  => new F16CConfiguration(Guid.NewGuid().ToString(), name, new Dictionary<string, string>()),
                AirframeTypes.FA18C => new FA18CConfiguration(Guid.NewGuid().ToString(), name, new Dictionary<string, string>()),
                AirframeTypes.M2000C => null,
                AirframeTypes.F14AB => null,
                AirframeTypes.None  => null,
                _                   => null,
            };
        }

        // factory to create a new configuration instance of the proper type bassed on airframe. the configuration is
        // set up from json representing a serialized configuration instance. the name can be replaced with the given
        // name parameter. returns null on error.
        //
        static public IConfiguration FactoryJSON(AirframeTypes airframe, string json, string name = null)
        {
            IConfiguration config;
            try
            {
                config = airframe switch
                {
                    AirframeTypes.A10C  => JsonSerializer.Deserialize<A10CConfiguration>(json),
                    AirframeTypes.AH64D => null,
                    AirframeTypes.AV8B  => JsonSerializer.Deserialize<AV8BConfiguration>(json),
                    AirframeTypes.F15E  => JsonSerializer.Deserialize<F15EConfiguration>(json),
                    AirframeTypes.F16C  => JsonSerializer.Deserialize<F16CConfiguration>(json),
                    AirframeTypes.FA18C => JsonSerializer.Deserialize<FA18CConfiguration>(json),
                    AirframeTypes.M2000C => null,
                    AirframeTypes.F14AB => null,
                    AirframeTypes.None  => null,
                    _                   => null,
                };
            }
            catch (Exception ex)
            {
                FileManager.Log($"Configuration:FactoryJSON exception {ex}");
                config = null;
            }
            if ((config != null) && !string.IsNullOrEmpty(name))
            {
                // if we are changing the name, null-out the filename to make sure the new configuration gets its own
                // unique filename once persisted and doesn't over-write an existing file we may have cloned from.
                //
                config.Name = name;
                config.Filename = null;
            }
            config?.AfterLoadFromJSON();
            return config;
        }
    }
}
