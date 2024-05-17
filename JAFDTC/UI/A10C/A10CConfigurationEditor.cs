﻿// ********************************************************************************************************************
//
// A10CConfigurationEditor.cs : supports editors for the a-10c configuration
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
using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.Misc;
using JAFDTC.Models.A10C.Radio;
using JAFDTC.Models.A10C.WYPT;
using JAFDTC.UI.App;
using System.Collections.ObjectModel;
using JAFDTC.Models.A10C.DSMS;
using JAFDTC.Models.A10C.HMCS;

namespace JAFDTC.UI.A10C
{
    // defines the glyphs to use for each system editor page in the hawg configuration.
    //
    public class Glyphs
    {
        public const string WYPT = "\xE707";
        public const string DSMS = "\xEBD2";
        public const string RADIO = "\xE704";
        public const string HMCS = "\xEA4A";
        public const string MISC = "\xE8B7";
    }

    /// <summary>
    /// TODO: document
    /// </summary>
    public class A10CConfigurationEditor : ConfigurationEditor
    {
        public A10CConfigurationEditor(IConfiguration config) => (Config) = (config);

        public override ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo()
            => new()
            {
                // This is the order they appear in the UI. Resist the temptation to alphabetize.
                A10CEditWaypointListHelper.PageInfo,
                A10CEditDSMSPage.PageInfo,
                A10CEditRadioPageHelper.PageInfo,
                A10CEditHMCSPage.PageInfo,
                A10CEditMiscPage.PageInfo
            };

        public override ISystem SystemForConfig(IConfiguration config, string tag)
        {
            ISystem system = tag switch
            {
                DSMSSystem.SystemTag => ((A10CConfiguration)config).DSMS,
                HMCSSystem.SystemTag => ((A10CConfiguration)config).HMCS,
                MiscSystem.SystemTag => ((A10CConfiguration)config).Misc,
                RadioSystem.SystemTag => ((A10CConfiguration)config).Radio,
                WYPTSystem.SystemTag => ((A10CConfiguration)config).WYPT,
                _ => null,
            };
            return system;
        }
    }
}
