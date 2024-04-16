// ********************************************************************************************************************
//
// M2000CConfiguration.cs -- m-2000c airframe configuration
//
// Copyright(C) 2023-2024 ilominar/raven
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

using JAFDTC.Models.M2000C.WYPT;
using JAFDTC.UI.M2000C;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace JAFDTC.Models.M2000C
{
    /// <summary>
    /// configuration object for the mirage that encapsulates the configurations of each system that jafdtc can set
    /// up. this object is serialized to/from json when persisting configurations. configuration supports navigation
    /// system.
    /// </summary>
    class M2000CConfiguration : Configuration
    {
        private const string _versionCfg = "M2000C-1.0";        // current version

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public WYPTSystem WYPT { get; set; }

        [JsonIgnore]
        public override IUploadAgent UploadAgent => new M2000CUploadAgent(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public M2000CConfiguration(string uid, string name, Dictionary<string, string> linkedSysMap)
            : base(_versionCfg, AirframeTypes.M2000C, uid, name, linkedSysMap)
        {
            WYPT = new WYPTSystem();
            ConfigurationUpdated();
        }

        public override IConfiguration Clone()
        {
            Dictionary<string, string> linkedSysMap = new();
            foreach (KeyValuePair<string, string> kvp in LinkedSysMap)
            {
                linkedSysMap[new(kvp.Key)] = new(kvp.Value);
            }
            M2000CConfiguration clone = new(UID, Name, linkedSysMap)
            {
                WYPT = (WYPTSystem)WYPT.Clone(),
            };
            clone.ConfigurationUpdated();
            return clone;
        }

        public override void CloneSystemFrom(string systemTag, IConfiguration other)
        {
            M2000CConfiguration otherHawg = (M2000CConfiguration)other;
            switch (systemTag)
            {
                case WYPTSystem.SystemTag: WYPT = (WYPTSystem)otherHawg.WYPT.Clone(); break;
                default: break;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void ConfigurationUpdated()
        {
            M2000CConfigurationEditor editor = new(this);
            Dictionary<string, string> updatesStrings = editor.BuildUpdatesStrings(this);

            string stpts = "";
            if (!WYPT.IsDefault)
            {
                stpts = $" along with {WYPT.Count} waypoint" + ((WYPT.Count > 1) ? "s" : "");
            }
            UpdatesInfoTextUI = updatesStrings["UpdatesInfoTextUI"] + stpts;
            UpdatesIconsUI = updatesStrings["UpdatesIconsUI"];
            UpdatesIconBadgesUI = updatesStrings["UpdatesIconBadgesUI"];
        }

        public override string Serialize(string systemTag = null)
        {
            return systemTag switch
            {
                null => JsonSerializer.Serialize(this, Configuration.JsonOptions),
                WYPTSystem.SystemTag => JsonSerializer.Serialize(WYPT, Configuration.JsonOptions),
                _ => null
            };
        }

        public override void AfterLoadFromJSON()
        {
            WYPT ??= new WYPTSystem();

            // TODO: if the version number is older than current, may need to update object
            Version = _versionCfg;

            Save(this);
            ConfigurationUpdated();
        }

        public override bool CanAcceptPasteForSystem(string cboardTag, string systemTag = null)
        {
            return (!string.IsNullOrEmpty(cboardTag) &&
                    (((systemTag != null) && (cboardTag.StartsWith(systemTag))) ||
                     ((systemTag == null) && ((cboardTag == WYPTSystem.SystemTag)))));
        }

        public override bool Deserialize(string systemTag, string json)
        {
            bool isSuccess = false;
            bool isHandled = true;
            try
            {
                switch (systemTag)
                {
                    case WYPTSystem.SystemTag: WYPT = JsonSerializer.Deserialize<WYPTSystem>(json); break;
                    default: isHandled = false; break;
                }
                if (isHandled)
                {
                    ConfigurationUpdated();
                    Save(this);
                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"M2000CConfigruation:Deserialize exception {ex}");
            }
            return isSuccess;
        }
    }
}
