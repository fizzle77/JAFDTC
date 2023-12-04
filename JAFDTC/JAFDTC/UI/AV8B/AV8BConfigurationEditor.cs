// ********************************************************************************************************************
//
// AV8BConfigurationEditor.cs : supports editors for the av8b configuration
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

using JAFDTC.Models.AV8B;
using JAFDTC.Models.AV8B.WYPT;
using JAFDTC.Models;
using JAFDTC.UI.App;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.UI.AV8B
{
    // defines the glyphs to use for each system editor page in the viper configuration.
    //
    public class Glyphs
    {
        public const string MISC = "\xE8B7";
        public const string RADIO = "\xE704";
        public const string WYPT = "\xE707";
    }

    /// <summary>
    /// TODO: document
    /// </summary>
    public class AV8BConfigurationEditor : ConfigurationEditor
    {
        public AV8BConfigurationEditor() { }

        public override ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo()
        {
            return new ObservableCollection<ConfigEditorPageInfo>()
            {
                AV8BEditWaypointListHelper.PageInfo,
            };
        }

        public override ISystem SystemForConfig(IConfiguration config, string tag)
        {
            ISystem system = tag switch
            {
                WYPTSystem.SystemTag => ((AV8BConfiguration)config).WYPT,
                _ => null,
            };
            return system;
        }
    }
}
