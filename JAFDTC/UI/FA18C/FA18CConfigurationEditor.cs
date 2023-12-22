// ********************************************************************************************************************
//
// FA18CConfigurationEditor.cs : supports editors for the fa18c configuration
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

using JAFDTC.Models.FA18C;
using JAFDTC.Models.FA18C.CMS;
using JAFDTC.Models.FA18C.Radio;
using JAFDTC.Models.FA18C.WYPT;
using JAFDTC.Models;
using JAFDTC.UI.App;
using System.Collections.ObjectModel;
using System.Diagnostics;
using JAFDTC.UI.F16C;

namespace JAFDTC.UI.FA18C
{
    // defines the glyphs to use for each system editor page in the viper configuration.
    //
    public class Glyphs
    {
        public const string CMS = "\xEA18";
        public const string MISC = "\xE8B7";
        public const string RADIO = "\xE704";
        public const string WYPT = "\xE707";
    }

    /// <summary>
    /// instance of a configuration editor for the fa-18c hornet. this class defines the configuration editor pages
    /// along with abstracting some access to internal system configuration state.
    /// </summary>
    public class FA18CConfigurationEditor : ConfigurationEditor
    {

        private static readonly ObservableCollection<ConfigEditorPageInfo> _configEditorPageInfo = new()
        {
            FA18CEditWaypointListHelper.PageInfo,
            FA18CEditRadioPageHelper.PageInfo,
            FA18CEditCMSPage.PageInfo,
        };

        public FA18CConfigurationEditor() { }

        public override ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo() => _configEditorPageInfo;

        public override ISystem SystemForConfig(IConfiguration config, string tag)
        {
            ISystem system = tag switch
            {
                CMSSystem.SystemTag => ((FA18CConfiguration)config).CMS,
                RadioSystem.SystemTag => ((FA18CConfiguration)config).Radio,
                WYPTSystem.SystemTag => ((FA18CConfiguration) config).WYPT,
                _ => null,
            };
            return system;
        }
    }
}
