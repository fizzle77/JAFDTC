// ********************************************************************************************************************
//
// F16CConfiguration.cs -- f-16c airframe configuration
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

using JAFDTC.Models.Base;
using JAFDTC.Models.F16C.CMDS;
using JAFDTC.Models.F16C.DLNK;
using JAFDTC.Models.F16C.HARM;
using JAFDTC.Models.F16C.HTS;
using JAFDTC.Models.F16C.MFD;
using JAFDTC.Models.F16C.Misc;
using JAFDTC.Models.F16C.Radio;
using JAFDTC.Models.F16C.SMS;
using JAFDTC.Models.F16C.STPT;
using JAFDTC.UI.App;
using JAFDTC.UI.F16C;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F16C
{
    /// <summary>
    /// configuration object for the viper that encapsulates the configurations of each system that jafdtc can set
    /// up. this object is serialized to/from json when persisting configurations. configuration supports navigation,
    /// countermeasure, datalink, harm, hts, mfd, radio, and miscellaneous systems.
    /// </summary>
    public class F16CConfiguration : Configuration
    {
        private const string _versionCfg = "F16C-1.1";          // current version

        // v1.0 --> v1.1:
        // - interpretation of ReleaseMode, RipplePulse fields in the sms system changed.
        //
        private const string _versionCfg_10 = "F16C-1.0";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public CMDSSystem CMDS { get; set; }

        public DLNKSystem DLNK { get; set; }

        public HARMSystem HARM { get; set; }

        public HTSSystem HTS { get; set; }

        public MFDSystem MFD { get; set; }

        public MiscSystem Misc { get; set; }

        public RadioSystem Radio { get; set; }

        public SMSSystem SMS { get; set; }

        public STPTSystem STPT { get; set; }

        public SimDTCSystem DTE { get; set; }

        [JsonIgnore]
        public override List<string> MergeableSysTagsForDTC => new()
        {
            RadioSystem.SystemTag,
            CMDSSystem.SystemTag
        };

        [JsonIgnore]
        public override IUploadAgent UploadAgent => new F16CUploadAgent(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CConfiguration(string uid, string name, Dictionary<string, string> linkedSysMap)
            : base(_versionCfg, AirframeTypes.F16C, uid, name, linkedSysMap)
        {
            CMDS = new CMDSSystem();
            DLNK = new DLNKSystem();
            HARM = new HARMSystem();
            HTS = new HTSSystem();
            MFD = new MFDSystem();
            Misc = new MiscSystem();
            Radio = new RadioSystem();
            SMS = new SMSSystem();
            STPT = new STPTSystem();
            DTE = new SimDTCSystem();
            ConfigurationUpdated();
        }

        public override IConfiguration Clone()
        {
            Dictionary<string, string> linkedSysMap = new();
            foreach (KeyValuePair<string, string> kvp in LinkedSysMap)
                linkedSysMap[new(kvp.Key)] = new(kvp.Value);
            F16CConfiguration clone = new("", Name, linkedSysMap)
            {
                CMDS = (CMDSSystem)CMDS.Clone(),
                DLNK = (DLNKSystem)DLNK.Clone(),
                HARM = (HARMSystem)HARM.Clone(),
                HTS = (HTSSystem)HTS.Clone(),
                MFD = (MFDSystem)MFD.Clone(),
                Misc = (MiscSystem)Misc.Clone(),
                Radio = (RadioSystem)Radio.Clone(),
                SMS = (SMSSystem)SMS.Clone(),
                STPT = (STPTSystem)STPT.Clone(),
                DTE = (SimDTCSystem)DTE.Clone(),
            };
            clone.ResetUID();
            clone.ConfigurationUpdated();
            return clone;
        }

        public override void CloneSystemFrom(string systemTag, IConfiguration other)
        {
            F16CConfiguration otherViper = other as F16CConfiguration;
            switch (systemTag)
            {
                case CMDSSystem.SystemTag: CMDS = otherViper.CMDS.Clone() as CMDSSystem; break;
                case DLNKSystem.SystemTag: DLNK = otherViper.DLNK.Clone() as DLNKSystem; break;
                case HARMSystem.SystemTag: HARM = otherViper.HARM.Clone() as HARMSystem; break;
                case HTSSystem.SystemTag: HTS = otherViper.HTS.Clone() as HTSSystem; break;
                case MFDSystem.SystemTag: MFD = otherViper.MFD.Clone() as MFDSystem; break;
                case MiscSystem.SystemTag: Misc = otherViper.Misc.Clone() as MiscSystem; break;
                case RadioSystem.SystemTag: Radio = otherViper.Radio.Clone() as RadioSystem; break;
                case SMSSystem.SystemTag: SMS = otherViper.SMS.Clone() as SMSSystem; break;
                case STPTSystem.SystemTag: STPT = otherViper.STPT.Clone() as STPTSystem; break;
                case SimDTCSystem.SystemTag: DTE = otherViper.DTE.Clone() as SimDTCSystem; break;
                default: break;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // overriden class methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override ISystem SystemForTag(string tag)
        {
            return tag switch
            {
                CMDSSystem.SystemTag => CMDS,
                DLNKSystem.SystemTag => DLNK,
                HARMSystem.SystemTag => HARM,
                HTSSystem.SystemTag => HTS,
                MFDSystem.SystemTag => MFD,
                MiscSystem.SystemTag => Misc,
                RadioSystem.SystemTag => Radio,
                SMSSystem.SystemTag => SMS,
                STPTSystem.SystemTag => STPT,
                SimDTCSystem.SystemTag => DTE,
                _ => null,
            };
        }

        public override bool IsMerged(string systemTag) => DTE.MergedSystemTags.Contains(systemTag);

        public override void ConfigurationUpdated()
        {
            F16CConfigurationEditor editor = new(this);
            Dictionary<string, string> updatesStrings = editor.BuildUpdatesStrings(this);

            string stpts = "";
            if (!STPT.IsDefault)
                stpts = $" along with { STPT.Count } steerpoint" + ((STPT.Count > 1) ? "s" : "");
            UpdatesInfoTextUI = updatesStrings["UpdatesInfoTextUI"] + stpts;
            UpdatesIconsUI = updatesStrings["UpdatesIconsUI"];
            UpdatesIconBadgesUI = updatesStrings["UpdatesIconBadgesUI"];
        }

        public override string Serialize(string systemTag = null)
        {
            return systemTag switch
            {
                null                 => JsonSerializer.Serialize(this, Configuration.JsonOptions),
                CMDSSystem.SystemTag => JsonSerializer.Serialize(CMDS, Configuration.JsonOptions),
                DLNKSystem.SystemTag => JsonSerializer.Serialize(DLNK, Configuration.JsonOptions),
                HARMSystem.SystemTag => JsonSerializer.Serialize(HARM, Configuration.JsonOptions),
                HTSSystem.SystemTag  => JsonSerializer.Serialize(HTS, Configuration.JsonOptions),
                MFDSystem.SystemTag  => JsonSerializer.Serialize(MFD, Configuration.JsonOptions),
                MiscSystem.SystemTag => JsonSerializer.Serialize(Misc, Configuration.JsonOptions),
                RadioSystem.SystemTag => JsonSerializer.Serialize(Radio, Configuration.JsonOptions),
                SMSSystem.SystemTag => JsonSerializer.Serialize(SMS, Configuration.JsonOptions),
                STPTSystem.SystemTag => JsonSerializer.Serialize(STPT, Configuration.JsonOptions),
                SimDTCSystem.SystemTag => JsonSerializer.Serialize(DTE, Configuration.JsonOptions),
                _                    => null
            };
        }

        public override void AfterLoadFromJSON()
        {
            CMDS  ??= new CMDSSystem();
            DLNK  ??= new DLNKSystem();
            HARM  ??= new HARMSystem();
            HTS   ??= new HTSSystem();
            MFD   ??= new MFDSystem();
            Misc  ??= new MiscSystem();
            Radio ??= new RadioSystem();
            SMS   ??= new SMSSystem();
            STPT  ??= new STPTSystem();
            DTE   ??= new SimDTCSystem();

            // TODO: should parse out version number from version string and compare that as an integer
            // TODO: to allow for "update if version older than x".
            if (Version == _versionCfg_10)
                SMS.UpdateFrom10to11();
            Version = _versionCfg;

            Save(this);
            ConfigurationUpdated();
        }

        public override bool CanAcceptPasteForSystem(string cboardTag, string systemTag = null)
        {
            return (!string.IsNullOrEmpty(cboardTag) &&
                    (((systemTag != null) && (cboardTag.StartsWith(systemTag))) ||
                     ((systemTag == null) && ((cboardTag == CMDSSystem.SystemTag) ||
                                              (cboardTag == DLNKSystem.SystemTag) ||
                                              (cboardTag == HARMSystem.SystemTag) ||
                                              (cboardTag == HTSSystem.SystemTag) ||
                                              (cboardTag == MFDSystem.SystemTag) ||
                                              (cboardTag == MiscSystem.SystemTag) ||
                                              (cboardTag == RadioSystem.SystemTag) ||
                                              (cboardTag == SMSSystem.SystemTag) ||
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
                    case CMDSSystem.SystemTag: CMDS = JsonSerializer.Deserialize<CMDSSystem>(json); break;
                    case DLNKSystem.SystemTag: DLNK = JsonSerializer.Deserialize<DLNKSystem>(json); break;
                    case HARMSystem.SystemTag: HARM = JsonSerializer.Deserialize<HARMSystem>(json); break;
                    case HTSSystem.SystemTag: HTS = JsonSerializer.Deserialize<HTSSystem>(json); break;
                    case MFDSystem.SystemTag: MFD = JsonSerializer.Deserialize<MFDSystem>(json); break;
                    case MiscSystem.SystemTag: Misc = JsonSerializer.Deserialize<MiscSystem>(json); break;
                    case RadioSystem.SystemTag: Radio = JsonSerializer.Deserialize<RadioSystem>(json); break;
                    case SMSSystem.SystemTag: SMS = JsonSerializer.Deserialize<SMSSystem>(json); break;
                    case STPTSystem.SystemTag: STPT = JsonSerializer.Deserialize<STPTSystem>(json); break;
                    case STPTSystem.STPTListTag: STPT.ImportSerializedNavpoints(json, false); break;
                    case SimDTCSystem.SystemTag: DTE = JsonSerializer.Deserialize<SimDTCSystem>(json); break;
                    default: isHandled = false;  break;
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
                FileManager.Log($"F16CConfiguration:Deserialize exception {ex}");
            }
            return isSuccess;
        }
    }
}
