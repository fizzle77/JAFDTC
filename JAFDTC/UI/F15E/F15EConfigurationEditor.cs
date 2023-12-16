// ********************************************************************************************************************
//
// F15EConfigurationEditor.cs : supports editors for the f16c configuration
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
using JAFDTC.Models.F15E;
using JAFDTC.Models.F15E.Radio;
using JAFDTC.UI.App;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JAFDTC.UI.F15E
{
    // defines the glyphs to use for each system editor page in the viper configuration.
    //
    public class Glyphs
    {
        public const string MISC = "\xE8B7";
        public const string RADIO = "\xE704";
        public const string STPT = "\xE707";
    }

    /// <summary>
    /// instance of a configuration editor for the f-16c viper. this class defines the configuration editor pages
    /// along with abstracting some access to internal system configuration state.
    /// </summary>
    public class F15EConfigurationEditor : ConfigurationEditor
    {
        private static readonly ObservableCollection<ConfigEditorPageInfo> _configEditorPageInfo = new()
        {
                F15EEditRadioPageHelper.PageInfo,
        };

        public F15EConfigurationEditor() { }

        public override ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo() => _configEditorPageInfo;

        public override ISystem SystemForConfig(IConfiguration config, string tag)
        {
            ISystem system = tag switch
            {
                RadioSystem.SystemTag => ((F15EConfiguration)config).Radio,
                _ => null,
            };
            return system;
        }
    }
}
