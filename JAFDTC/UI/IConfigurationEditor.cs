// ********************************************************************************************************************
//
// IConfigurationEditor.cs : general interface to a configuration editor
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
using JAFDTC.UI.App;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JAFDTC.UI
{
    /// <summary>
    /// interface to a configuration editor instance that provides information on the systems the configuration
    /// provides and allows airframe-independent access to the systems for common operations.
    /// </summary>
    public interface IConfigurationEditor
    {
        /// <summary>
        /// returns a collection of ConfigEditorPageInfo with information on the editor pages for the airframe
        /// configuration.
        /// </summary>
        public ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo();

        /// <summary>
        /// returns a collection of ConfigAuxCommandInfo with information on the aux commands for the airframe
        /// configuration.
        /// </summary>
        public ObservableCollection<ConfigAuxCommandInfo> ConfigAuxCommandInfo();

        /// <summary>
        /// returns the system associated with the given tag in the given configuration. 
        /// </summary>
        public ISystem SystemForConfig(IConfiguration config, string tag);

        /// <summary>
        /// returns true if the system with the given tag is in a default state in the given configuration,
        /// false otherwise.
        /// </summary>
        public bool IsSystemDefault(IConfiguration config, string tag);

        /// <summary>
        /// returns true if the system with the given tag is linked in the given configuration, false otherwise.
        /// </summary>
        public bool IsSystemLinked(IConfiguration config, string tag);

        /// <summary>
        /// returns the string with a human-readable description of what avionics systems the configuration changes.
        /// </summary>
        public Dictionary<string, string> BuildUpdatesStrings(IConfiguration config);

        /// <summary>
        /// TODO: document
        /// </summary>
        public void HandleAuxCommand(ConfigurationPage configPage, ConfigAuxCommandInfo cmd);
    }
}
