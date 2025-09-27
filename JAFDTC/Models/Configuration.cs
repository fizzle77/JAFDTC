// ********************************************************************************************************************
//
// Configuration.cs -- abstract base class for airframe configuration
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

using JAFDTC.Models.A10C;
using JAFDTC.Models.AV8B;
using JAFDTC.Models.F14AB;
using JAFDTC.Models.F15E;
using JAFDTC.Models.F16C;
using JAFDTC.Models.FA18C;
using JAFDTC.Models.M2000C;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
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

        // ---- public properties

        public string Version { get; set; }

        public AirframeTypes Airframe { get; private set; }

        public string UID { get; protected set; }

        public string Filename { get; set; }

        public Dictionary<string, string> LinkedSysMap { get; private set; }

        public int LastSystemEdited { get; set; }

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    FavoriteGlyphUI = (_isFavorite) ? "\xE735" : "";
                }
            }
        }

        // ---- public properties, posts change/validation events

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
        private string _favoriteGlyphUI;
        [JsonIgnore]
        public string FavoriteGlyphUI
        {
            get => _favoriteGlyphUI;
            set
            {
                if (_favoriteGlyphUI != value)
                {
                    _favoriteGlyphUI = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        private string _updatesInfoTextUI;
        [JsonIgnore]
        public string UpdatesInfoTextUI
        {
            get => _updatesInfoTextUI;
            set
            {
                if (_updatesInfoTextUI != value)
                {
                    _updatesInfoTextUI = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        private string _updatesIconsUI;
        [JsonIgnore]
        public string UpdatesIconsUI
        {
            get => _updatesIconsUI;
            set
            {
                if (_updatesIconsUI != value)
                {
                    _updatesIconsUI = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        private string _updatesIconBadgesUI;
        [JsonIgnore]
        public string UpdatesIconBadgesUI
        {
            get => _updatesIconBadgesUI;
            set
            {
                if (_updatesIconBadgesUI != value)
                {
                    _updatesIconBadgesUI = value;
                    OnPropertyChanged();
                }
            }
        }

        // ---- properties, computed

        [JsonIgnore]
        public virtual List<string> MergeableSysTagsForDTC => [ ];

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

        public virtual ISystem SystemForTag(string tag) => null;

        public bool IsDefault(string systemTag)
        {
            ISystem system = SystemForTag(systemTag);
            return system == null || system.IsDefault;
        }

        public virtual bool IsMerged(string systemTag) => false;

        public void LinkSystemTo(string systemTag, IConfiguration linkedConfig)
        {
            LinkedSysMap ??= [ ];
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

        public bool SaveMergedSimDTC(string template, string outputPath)
        {
            string name = Path.GetFileNameWithoutExtension(outputPath);
            try
            {
                string json = FileManager.LoadDTCTemplate(Airframe, template)
                    ?? throw new Exception("TODO: FAILED TO LOAD");
                JsonNode dom = JsonNode.Parse(json)
                    ?? throw new Exception("TODO: FAILED TO PARSE");

                dom["name"] = name;
                dom["data"]["name"] = name;
                foreach (string tag in MergeableSysTagsForDTC)
                {
                    ISystem system = SystemForTag(tag);
                    if (!system.IsDefault && IsMerged(tag))
                        system.MergeIntoSimDTC(dom["data"]);
                }

                json = dom.ToJsonString(Globals.JSONOptions)
                    ?? throw new Exception("TODO: FAILED TO SERIALIZE");
                FileManager.WriteFile(outputPath, json);
            }
            catch (Exception ex)
            {
                FileManager.Log($"Configuration:SaveMergedSimDTC exception {ex}");
                return false;
            }
            return true;
        }

        public string SystemLinkedTo(string systemTag)
        {
            return ((LinkedSysMap != null) && LinkedSysMap.TryGetValue(systemTag, out string value)) ? value : null;
        }

        public bool IsLinked(string systemTag) => !string.IsNullOrEmpty(SystemLinkedTo(systemTag));

        public void CleanupSystemLinks(List<string> validUIDs)
        {
            List<string> invalidSystems = [ ];
            foreach (KeyValuePair<string, string> kvp in LinkedSysMap)
                if (!validUIDs.Contains(kvp.Value))
                    invalidSystems.Add(kvp.Key);
            foreach (string system in invalidSystems)
                LinkedSysMap.Remove(system);
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
                AirframeTypes.A10C  => new A10CConfiguration(Guid.NewGuid().ToString(), name, [ ]),
                AirframeTypes.AH64D => null,
                AirframeTypes.AV8B  => new AV8BConfiguration(Guid.NewGuid().ToString(), name, [ ]),
                AirframeTypes.F14AB => new F14ABConfiguration(Guid.NewGuid().ToString(), name, [ ]),
                AirframeTypes.F15E  => new F15EConfiguration(Guid.NewGuid().ToString(), name, [ ]),
                AirframeTypes.F16C  => new F16CConfiguration(Guid.NewGuid().ToString(), name, [ ]),
                AirframeTypes.FA18C => new FA18CConfiguration(Guid.NewGuid().ToString(), name, [ ]),
                AirframeTypes.M2000C => new M2000CConfiguration(Guid.NewGuid().ToString(), name, [ ]),
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
                    AirframeTypes.F14AB => JsonSerializer.Deserialize<F14ABConfiguration>(json),
                    AirframeTypes.F15E  => JsonSerializer.Deserialize<F15EConfiguration>(json),
                    AirframeTypes.F16C  => JsonSerializer.Deserialize<F16CConfiguration>(json),
                    AirframeTypes.FA18C => JsonSerializer.Deserialize<FA18CConfiguration>(json),
                    AirframeTypes.M2000C => JsonSerializer.Deserialize<M2000CConfiguration>(json),
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
