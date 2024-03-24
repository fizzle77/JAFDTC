// ********************************************************************************************************************
//
// ConfigurationEditor.cs : abstract base class for a configuration editor
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
using JAFDTC.UI.A10C;
using JAFDTC.UI.AV8B;
using JAFDTC.UI.F14AB;
using JAFDTC.UI.F16C;
using JAFDTC.UI.F15E;
using JAFDTC.UI.FA18C;
using JAFDTC.UI.M2000C;
using JAFDTC.UI.App;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using JAFDTC.Utilities;
using Windows.Devices.Radios;
using Windows.ApplicationModel.Contacts;

namespace JAFDTC.UI
{
    // defines the glyphs common to the ui for configuration editors.
    //
    public class Glyphs
    {
        public const string Badge = "\xF0B6";
    }

    /// <summary>
    /// abstract base class for a configuration editor that implements IConfigurationEditor. the abstract base
    /// class provides a factory method to build concrete instances based on airframe.
    /// </summary>
    public abstract class ConfigurationEditor : IConfigurationEditor
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // IConfigurationEditor
        //
        // ------------------------------------------------------------------------------------------------------------

        public virtual ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo() => new();

        public virtual ObservableCollection<ConfigAuxCommandInfo> ConfigAuxCommandInfo() => new();

        public virtual ISystem SystemForConfig(IConfiguration config, string tag) => null;

        public virtual bool IsSystemDefault(IConfiguration config, string tag)
        {
            ISystem system = SystemForConfig(config, tag);
            return system == null || system.IsDefault;
        }

        public virtual bool IsSystemLinked(IConfiguration config, string tag)
        {
            return !string.IsNullOrEmpty(config.SystemLinkedTo(tag));
        }

        public virtual Dictionary<string, string> BuildUpdatesStrings(IConfiguration config)
        {
            List<string> sysList = new();
            string icons = "";
            string iconBadges = "";
            foreach (ConfigEditorPageInfo info in ConfigEditorPageInfo())
            {
                if (!IsSystemDefault(config, info.Tag))
                {
                    sysList.Add(info.ShortName);
                    icons += $" {info.Glyph}";
                    if (config.SystemLinkedTo(info.Tag) != null)
                    {
                        iconBadges += $" {Glyphs.Badge}";
                    }
                    else
                    {
                        iconBadges += $" {info.Glyph}";
                    }
                }
            }

            string infoText = "Default setup, no changes to avionics";
            if (sysList.Count > 0)
            {
                infoText = $"Includes changes to {General.JoinList(sysList)} system" + ((sysList.Count > 1) ? "s" : "");
            }

            return new Dictionary<string, string>()
            {
                ["UpdatesInfoTextUI"] = infoText,
                ["UpdatesIconsUI"] = icons,
                ["UpdatesIconBadgesUI"] = iconBadges,
            };
        }

        public virtual void HandleAuxCommand(ConfigurationPage configPage, ConfigAuxCommandInfo cmd) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // factories
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns an instance of the configuration editor to use for a particular airframe. null if the airframe is
        /// invalid or not supported.
        /// </summary>
        public static IConfigurationEditor Factory(AirframeTypes airframe)
        {
            return airframe switch
            {
                AirframeTypes.None => null,
                AirframeTypes.A10C => new A10CConfigurationEditor(),
                AirframeTypes.AH64D => null,
                AirframeTypes.AV8B => new AV8BConfigurationEditor(),
                AirframeTypes.F14AB => new F14ABConfigurationEditor(),
                AirframeTypes.F16C => new F16CConfigurationEditor(),
                AirframeTypes.F15E => new F15EConfigurationEditor(),
                AirframeTypes.FA18C => new FA18CConfigurationEditor(),
                AirframeTypes.M2000C => new M2000CConfigurationEditor(),
                _ => null,
            };
        }
    }
}
