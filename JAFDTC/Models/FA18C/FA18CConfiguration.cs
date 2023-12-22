// ********************************************************************************************************************
//
// FA18CConfiguration.cs -- fa-18c airframe configuration
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

using JAFDTC.Models.FA18C.CMS;
using JAFDTC.Models.FA18C.Radio;
using JAFDTC.Models.FA18C.WYPT;
using JAFDTC.UI.FA18C;
using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json;
using System;

namespace JAFDTC.Models.FA18C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class FA18CConfiguration : Configuration
    {
        private const string VersionCfgFA18C = "FA18C-1.0";         // current version

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public CMSSystem CMS { get; set; }

        public RadioSystem Radio { get; set; }

        public WYPTSystem WYPT { get; set; }

        [JsonIgnore]
        public override IUploadAgent UploadAgent => new FA18CUploadAgent(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public FA18CConfiguration(string uid, string name, Dictionary<string, string> linkedSysMap)
            : base(VersionCfgFA18C, AirframeTypes.FA18C, uid, name, linkedSysMap)
        {
            CMS = new CMSSystem();
            Radio = new RadioSystem();
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
            FA18CConfiguration clone = new("", Name, linkedSysMap)
            {
                CMS = (CMSSystem)CMS.Clone(),
                Radio = (RadioSystem)Radio.Clone(),
                WYPT = (WYPTSystem)WYPT.Clone()
            };
            clone.ResetUID();
            clone.ConfigurationUpdated();
            return clone;
        }

        public override void CloneSystemFrom(string systemTag, IConfiguration other)
        {
            FA18CConfiguration otherHornet = other as FA18CConfiguration;
            switch (systemTag)
            {
                case RadioSystem.SystemTag: Radio = otherHornet.Radio.Clone() as RadioSystem; break;
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
            FA18CConfigurationEditor editor = new();
            Dictionary<string, string> updatesStrings = editor.BuildUpdatesStrings(this);

            string stpts = "";
            if (!WYPT.IsDefault)
            {
                stpts = $" along with {WYPT.Count} waypoint" + ((WYPT.Count > 1) ? "s" : "");
            }
            UpdatesInfoText = updatesStrings["UpdatesInfoText"] + stpts;
            UpdatesIcons = updatesStrings["UpdatesIcons"];
            UpdatesIconBadges = updatesStrings["UpdatesIconBadges"];
        }

        public override string Serialize(string systemTag = null)
        {
            return systemTag switch
            {
                null => JsonSerializer.Serialize(this, Configuration.JsonOptions),
                CMSSystem.SystemTag => JsonSerializer.Serialize(CMS, Configuration.JsonOptions),
                RadioSystem.SystemTag => JsonSerializer.Serialize(Radio, Configuration.JsonOptions),
                WYPTSystem.SystemTag => JsonSerializer.Serialize(WYPT, Configuration.JsonOptions),
                _ => null
            };
        }

        public override void AfterLoadFromJSON()
        {
            CMS ??= new CMSSystem();
            Radio ??= new RadioSystem();
            WYPT ??= new WYPTSystem();

            // TODO: if the version number is older than current, may need to update object

            ConfigurationUpdated();

            Version = VersionCfgFA18C;

            Save(this);
        }

        public override bool CanAcceptPasteForSystem(string cboardTag, string systemTag = null)
        {
            return (!string.IsNullOrEmpty(cboardTag) &&
                    (((systemTag != null) && (cboardTag.StartsWith(systemTag))) ||
                     ((systemTag == null) && ((cboardTag == CMSSystem.SystemTag) ||
                                              (cboardTag == RadioSystem.SystemTag) ||
                                              (cboardTag == WYPTSystem.SystemTag)))));
        }

        public override bool Deserialize(string systemTag, string json)
        {
            bool isSuccess = false;
            bool isHandled = true;
            try
            {
                switch (systemTag)
                {
                    case CMSSystem.SystemTag: CMS = JsonSerializer.Deserialize<CMSSystem>(json); break;
                    case RadioSystem.SystemTag: Radio = JsonSerializer.Deserialize<RadioSystem>(json); break;
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
                FileManager.Log($"FA18CConfigruation:Deserialize exception {ex}");
            }
            return isSuccess;
        }
    }
}
