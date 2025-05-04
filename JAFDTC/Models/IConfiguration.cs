// ********************************************************************************************************************
//
// IConfiguration.cs -- interface for airframe configuration class
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

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JAFDTC.Models
{
    /// <summary>
    /// event arguments for a "configuration saved" event posted when IConfiguration.Save() is called. the InvokedBy
    /// field identifies the object that invoked the save operation on the configuration.
    /// </summary>
    public class ConfigurationSavedEventArgs : EventArgs
    {
        public object InvokedBy { get; }

        public IConfiguration Config { get; }

        public string SyncSysTag { get; }

        public ConfigurationSavedEventArgs(object invokedBy, IConfiguration config, string syncSysTag)
            => (InvokedBy, Config, SyncSysTag) = (invokedBy, config, syncSysTag);
    }

    /// <summary>
    /// an interface to class that represents an avionics configuration for a particular airframe.
    /// </summary>
    public interface IConfiguration
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // events and handlers
        //
        // ------------------------------------------------------------------------------------------------------------

        public event ConfigurationSavedEventHandler ConfigurationSaved;
        public delegate void ConfigurationSavedEventHandler(object sender, ConfigurationSavedEventArgs args);

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// provides the current version of the configuration.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// provides the airframe type that the configuration targets.
        /// </summary>
        public AirframeTypes Airframe { get; }

        /// <summary>
        /// provides the unique identifier for the configuration.
        /// </summary>
        public string UID { get; }

        /// <summary>
        /// provides a file-system friendly name (see FileManager). this will not necessarily match Name and may be
        /// null if the configuration hasn't been persisted.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// provides the name of the configuration. this must be unique within an airframe (name comparisons are
        /// always case-insensitive).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// boolen indicating wheter or not the configuration is marked as a "favorite".
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// provides a map from a system tag (string) to a uid (string) that establishes a link between a system on
        /// this configuration and a different source configuration. linked systems will always track the setup of the
        /// system in the source configuration.
        /// </summary>
        public Dictionary<string, string> LinkedSysMap { get; }

        /// <summary>
        /// index of the system editor page last used. this property is used to persist ui state.
        /// </summary>
        public int LastSystemEdited { get; set; }

        /// <summary>
        /// return a list of system tags for systems that can be merged wth a dcs dtc.
        /// </summary>
        [JsonIgnore]
        public List<string> MergeableSysTagsForDTC { get; }

        /// <summary>
        /// provides an instance of an upload agent object that implements IUploadAgent. this object handles creating
        /// a command set that will set up the jet (according to the configuration) and uploading it to the jet.
        /// </summary>
        [JsonIgnore]
        public IUploadAgent UploadAgent { get; }

        /// <summary>
        /// provides a Segoe Fluent Icon glyph to use to mark the favorite status of the configuration in the ui.
        /// </summary>
        [JsonIgnore]
        public string FavoriteGlyphUI { get; set; }

        /// <summary>
        /// provides a human-readable summary of which of the configuration's systems have been updated from their
        /// default setup for use in the ui.
        /// </summary>
        [JsonIgnore]
        public string UpdatesInfoTextUI { get; set; }

        /// <summary>
        /// provides a string of Segoe Fluent Icon glyphs representing the systems that have been updated from their
        /// default setup for use in the ui.
        /// </summary>
        [JsonIgnore]
        public string UpdatesIconsUI { get; set; }

        /// <summary>
        /// provides a string of Segoe Fluent Icon glyphs representing the badges to apply to the icons from
        /// UpdatesIcons for use in the ui.
        /// </summary>
        [JsonIgnore]
        public string UpdatesIconBadgesUI { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns a deep copy of the configuration. the uid of the clone is updated via ResetUID() to ensure that
        /// the uid of the configuration clone is unique.
        /// </summary>
        public IConfiguration Clone();

        /// <summary>
        /// reset the uid of the configuration.
        /// 
        /// NOTE: use this method with caution. it can break links if not used carefully.
        /// </summary>
        public void ResetUID();

        /// <summary>
        /// returns the system associated with the given tag in the configuration, null if there is no such system.
        /// </summary>
        public ISystem SystemForTag(string tag);

        /// <summary>
        /// returns true if the system with specified tag is default, false otherwise.
        /// </summary>
        public bool IsDefault(string systemTag);

        /// <summary>
        /// returns true if the system with specified tag is merged into dcs dtc configuration, false otherwise.
        /// </summary>
        public bool IsMerged(string systemTag);

        /// <summary>
        /// update a system in this configuration to match the system in a target configuration. this method creates
        /// a deep copy of the system.
        /// </summary>
        public void CloneSystemFrom(string systemTag, IConfiguration other);

        /// <summary>
        /// link the configuration of a system with the specified tag to the system in another configuration.
        /// </summary>
        public void LinkSystemTo(string systemTag, IConfiguration linkedConfig);

        /// <summary>
        /// unlink the configuration of a system with the specified tag.
        /// </summary>
        public void UnlinkSystem(string systemTag);

        /// <summary>
        /// return the uid for the configuration that the system with the specified tag is linked to, null if the
        /// system is not linked.
        /// </summary>
        public string SystemLinkedTo(string systemTag);

        /// <summary>
        /// returns true if the system with specified tag is linked to a different configuration, false otherwise.
        /// </summary>
        public bool IsLinked(string systemTag);

        /// <summary>
        /// clean up the system links in the configuration removing any stale links for the systems.
        /// </summary>
        public void CleanupSystemLinks(List<string> validUIDs);

        /// <summary>
        /// persist the configuration to storage, posting a ConfigurationSaved event with an argument set up
        /// appropriately. the invoked by parameter identifies the object  that invoked the save. the sync system tag
        /// identifies the system (if any) that may need to be synchronized with configurations that link to this
        /// configuration (null indicates no systems should be sync'd).
        /// </summary>
        public void Save(object invokedBy = null, string syncSysTag = null);

        /// <summary>
        /// persist the merged configuration into the dcs dtc file at the output path. the dcs dtc file is built by
        /// merging the configuration of mergable systems into the base template. returns true on success, false on
        /// failure.
        /// </summary>
        public bool SaveMergedSimDTC(string template, string outputPath);

        /// <summary>
        /// TODO: document
        /// </summary>
        public void ConfigurationUpdated();

        /// <summary>
        /// returns a json serialization of the entire configuration or a single system (identified by the system tag)
        /// from the Configuration. a null system tag requests a serialization of the entire configuration.
        /// </summary>
        public string Serialize(string systemTag = null);

        /// <summary>
        /// returns true if able to deserialize the json string into the configuration for a system with the specified
        /// system tag, false on error.
        /// </summary>
        public bool Deserialize(string systemTag, string json);

        /// <summary>
        /// called after deserializing a configuration from json to allow opportunities to update versions, etc.
        /// </summary>
        public void AfterLoadFromJSON();

        /// <summary>
        /// returns true if the given clipboard content can be consumed by the configuration or system (as identified
        /// by a non-null tag), false otherwise. valid clipboard content is text, starting with a system tag on the
        /// first line, followed by json serialized objects.
        /// </summary>
        public bool CanAcceptPasteForSystem(string cboardTag, string systemTag = null);
    }
}
