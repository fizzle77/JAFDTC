// ********************************************************************************************************************
//
// F16CConfigurationEditor.cs : supports editors for the f16c configuration
//
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

using JAFDTC.Models;
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.CMDS;
using JAFDTC.Models.F16C.DLNK;
using JAFDTC.Models.F16C.HARM;
using JAFDTC.Models.F16C.HTS;
using JAFDTC.Models.F16C.MFD;
using JAFDTC.Models.F16C.Misc;
using JAFDTC.Models.F16C.Radio;
using JAFDTC.Models.F16C.STPT;
using JAFDTC.UI.App;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JAFDTC.UI.F16C
{
    // defines the glyphs to use for each system editor page in the viper configuration.
    //
    public class Glyphs
    {
        public const string CMDS = "\xEA18";
        public const string DLNK = "\xE716";
        public const string HARM = "\xE701";
        public const string HTS = "\xF272";
        public const string MFD = "\xE950";
        public const string MISC = "\xE8B7";
        public const string RADIO = "\xE704";
        public const string STPT = "\xE707";
    }

    /// <summary>
    /// instance of a configuration editor for the f-16c viper. this class defines the configuration editor pages
    /// along with abstracting some access to internal system configuration state.
    /// </summary>
    public class F16CConfigurationEditor : ConfigurationEditor
    {
        private static readonly ObservableCollection<ConfigEditorPageInfo> _configEditorPageInfo = new()
        {
                F16CEditSteerpointListPage.PageInfo,
                F16CEditMFDPage.PageInfo,
                F16CEditRadioPageHelper.PageInfo,
                F16CEditCMDSPage.PageInfo,
                F16CEditHARMPage.PageInfo,
                F16CEditHTSPage.PageInfo,
                F16CEditDLNKPage.PageInfo,
                F16CEditMiscPage.PageInfo
        };

        public F16CConfigurationEditor() { }

        public override ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo()
            => _configEditorPageInfo;

        public override ISystem SystemForConfig(IConfiguration config, string tag)
        {
            ISystem system = tag switch
            {
                CMDSSystem.SystemTag => ((F16CConfiguration)config).CMDS,
                DLNKSystem.SystemTag => ((F16CConfiguration)config).DLNK,
                HARMSystem.SystemTag => ((F16CConfiguration)config).HARM,
                HTSSystem.SystemTag => ((F16CConfiguration)config).HTS,
                MFDSystem.SystemTag => ((F16CConfiguration)config).MFD,
                MiscSystem.SystemTag => ((F16CConfiguration)config).Misc,
                RadioSystem.SystemTag => ((F16CConfiguration)config).Radio,
                STPTSystem.SystemTag => ((F16CConfiguration)config).STPT,
                _ => null,
            };
            return system;
        }
    }
}
