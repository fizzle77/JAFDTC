// ********************************************************************************************************************
//
// F16CConfiguration.cs -- f-16c airframe configuration
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

using JAFDTC.Models.F16C.CMDS;
using JAFDTC.Models.F16C.DLNK;
using JAFDTC.Models.F16C.HARM;
using JAFDTC.Models.F16C.HTS;
using JAFDTC.Models.F16C.MFD;
using JAFDTC.Models.F16C.Misc;
using JAFDTC.Models.F16C.Radio;
using JAFDTC.Models.F16C.STPT;
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
    /// TODO: document
    /// </summary>
    public class F16CConfiguration : Configuration
    {
        private const string VersionCfgF16C = "F16C-1.0";           // current version

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

        public STPTSystem STPT { get; set; }

        [JsonIgnore]
        public override IUploadAgent UploadAgent => new F16CUploadAgent(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CConfiguration(string uid, string name, Dictionary<string, string> linkedSysMap)
            : base(VersionCfgF16C, AirframeTypes.F16C, uid, name, linkedSysMap)
        {
            CMDS = new CMDSSystem();
            DLNK = new DLNKSystem();
            HARM = new HARMSystem();
            HTS = new HTSSystem();
            MFD = new MFDSystem();
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
            F16CConfiguration clone = new("", Name, linkedSysMap)
            {
                CMDS = (CMDSSystem)CMDS.Clone(),
                DLNK = (DLNKSystem)DLNK.Clone(),
                HARM = (HARMSystem)HARM.Clone(),
                HTS = (HTSSystem)HTS.Clone(),
                MFD = (MFDSystem)MFD.Clone(),
                Misc = (MiscSystem)Misc.Clone(),
                Radio = (RadioSystem)Radio.Clone(),
                STPT = (STPTSystem)STPT.Clone(),
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
                case STPTSystem.SystemTag: STPT = otherViper.STPT.Clone() as STPTSystem; break;
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
            F16CConfigurationEditor editor = new();
            Dictionary<string, string> updatesStrings = editor.BuildUpdatesStrings(this);

            string stpts = "";
            if (!STPT.IsDefault)
            {
                stpts = $" along with { STPT.Count } steerpoint" + ((STPT.Count > 1) ? "s" : "");
            }
            UpdatesInfoText = updatesStrings["UpdatesInfoText"] + stpts;
            UpdatesIcons = updatesStrings["UpdatesIcons"];
            UpdatesIconBadges = updatesStrings["UpdatesIconBadges"];
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
                STPTSystem.SystemTag => JsonSerializer.Serialize(STPT, Configuration.JsonOptions),
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
            STPT  ??= new STPTSystem();

            // TODO: if the version number is older than current, may need to update object

            ConfigurationUpdated();

            Version = VersionCfgF16C;

            Save(this);
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
                    case STPTSystem.SystemTag: STPT = JsonSerializer.Deserialize<STPTSystem>(json); break;
                    case STPTSystem.STPTListTag: STPT.DeserializeNavpoints(json, false); break;
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
                FileManager.Log($"F16CConfigruation:Deserialize exception {ex}");
            }
            return isSuccess;
        }
    }
}
