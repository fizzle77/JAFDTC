﻿// ********************************************************************************************************************
//
// F15EConfiguration.cs -- f-15e airframe configuration
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

using JAFDTC.Models.F15E.Misc;
using JAFDTC.Models.F15E.Radio;
using JAFDTC.Models.F15E.STPT;
using JAFDTC.UI.F15E;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F15E
{
    /// <summary>
    /// top-level mudhen configuration that includes per-system configurations for the managed systems.
    /// </summary>
    public class F15EConfiguration : Configuration
    {
        private const string VersionCfgF15E = "F15E-1.0";           // current configuration version

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public MiscSystem Misc { get; set; }

        public RadioSystem Radio { get; set; }

        public STPTSystem STPT { get; set; }

        [JsonIgnore]
        public override IUploadAgent UploadAgent => new F15EUploadAgent(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F15EConfiguration(string uid, string name, Dictionary<string, string> linkedSysMap)
            : base(VersionCfgF15E, AirframeTypes.F15E, uid, name, linkedSysMap)
        {
            Misc = new MiscSystem();
            Radio = new RadioSystem();
            STPT = new STPTSystem();
            ConfigurationUpdated();
        }

        public override IConfiguration Clone()
        {
            Dictionary<string, string> linkedSysMap = new();
            foreach (KeyValuePair<string, string> kvp in LinkedSysMap)
            {
                linkedSysMap[new(kvp.Key)] = new(kvp.Value);
            }
            F15EConfiguration clone = new("", Name, linkedSysMap)
            {
                Misc = (MiscSystem)Misc.Clone(),
                Radio = (RadioSystem)Radio.Clone(),
                STPT = (STPTSystem)STPT.Clone()
            };
            clone.ResetUID();
            clone.ConfigurationUpdated();
            return clone;
        }

        public override void CloneSystemFrom(string systemTag, IConfiguration other)
        {
            F15EConfiguration otherMudhen = other as F15EConfiguration;
            switch (systemTag)
            {
                case MiscSystem.SystemTag: Misc = otherMudhen.Misc.Clone() as MiscSystem; break;
                case RadioSystem.SystemTag: Radio = otherMudhen.Radio.Clone() as RadioSystem; break;
                case STPTSystem.SystemTag: STPT = otherMudhen.STPT.Clone() as STPTSystem; break;
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
            F15EConfigurationEditor editor = new();
            Dictionary<string, string> updatesStrings = editor.BuildUpdatesStrings(this);

            string stpts = "";
            if (!STPT.IsDefault)
            {
                stpts = $" along with {STPT.Count} steerpoint" + ((STPT.Count > 1) ? "s" : "");
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
                MiscSystem.SystemTag => JsonSerializer.Serialize(Misc, Configuration.JsonOptions),
                RadioSystem.SystemTag => JsonSerializer.Serialize(Radio, Configuration.JsonOptions),
                STPTSystem.SystemTag => JsonSerializer.Serialize(STPT, Configuration.JsonOptions),
                _ => null
            };
        }

        public override void AfterLoadFromJSON()
        {
            Misc ??= new MiscSystem();
            Radio ??= new RadioSystem();
            STPT ??= new STPTSystem();

            // TODO: if the version number is older than current, may need to update object

            ConfigurationUpdated();

            Version = VersionCfgF15E;

            Save(this);
        }

        public override bool CanAcceptPasteForSystem(string cboardTag, string systemTag = null)
        {
            return (!string.IsNullOrEmpty(cboardTag) &&
                    (((systemTag != null) && (cboardTag.StartsWith(systemTag))) ||
                     ((systemTag == null) && ((cboardTag == MiscSystem.SystemTag) ||
                                              (cboardTag == RadioSystem.SystemTag) ||
                                              (cboardTag == STPTSystem.SystemTag) ||
                                              (cboardTag == STPTSystem.STPTListTag)))));
        }

        public override bool Deserialize(string systemTag, string json)
        {
            bool isSuccess = false;
            bool isHandled = true;
            try
            {
                switch (systemTag)
                {
                    case MiscSystem.SystemTag: Misc = JsonSerializer.Deserialize<MiscSystem>(json); break;
                    case RadioSystem.SystemTag: Radio = JsonSerializer.Deserialize<RadioSystem>(json); break;
                    case STPTSystem.SystemTag: STPT = JsonSerializer.Deserialize<STPTSystem>(json); break;
                    case STPTSystem.STPTListTag: STPT.ImportSerializedNavpoints(json, false); break;
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
                FileManager.Log($"F15EConfiguration:Deserialize exception {ex}");
            }
            return isSuccess;
        }
    }
}
