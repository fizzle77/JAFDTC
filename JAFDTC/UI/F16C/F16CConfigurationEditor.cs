// ********************************************************************************************************************
//
// F16CConfigurationEditor.cs : supports editors for the f16c configuration
//
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

using JAFDTC.Models;
using JAFDTC.UI.App;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// defines the glyphs to use for each system editor page in the viper configuration.
    /// </summary>
    public class Glyphs
    {
        public const string CMDS = "\xEA18";
        public const string DLNK = "\xE716";
        public const string HARM = "\xE701";
        public const string HTS = "\xF272";
        public const string MFD = "\xE950";
        public const string MISC = "\xE8B7";
        public const string RADIO = "\xE704";
        public const string SMS = "\xEBD2";
        public const string STPT = "\xE707";
        public const string PILOT_DB = "\xE77B";
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
                F16CEditSMSPage.PageInfo,
                F16CEditCMDSPage.PageInfo,
                F16CEditHARMPage.PageInfo,
                F16CEditHTSPage.PageInfo,
                F16CEditDLNKPage.PageInfo,
                F16CEditMiscPage.PageInfo,
                F16CEditSimulatorDTCPageHelper.PageInfo
        };

        private static readonly ObservableCollection<ConfigAuxCommandInfo> _configAuxCmdInfo = new()
        {
            new("PDb", "Edit Pilot Database", Glyphs.PILOT_DB)
        };

        public override ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo() => _configEditorPageInfo;

        public override ObservableCollection<ConfigAuxCommandInfo> ConfigAuxCommandInfo() => _configAuxCmdInfo;

        public F16CConfigurationEditor(IConfiguration config) => (Config) = (config);

        public override bool HandleAuxCommand(ConfigurationPage configPage, ConfigAuxCommandInfo cmd)
        {
            F16CConfigAuxCmdPilotDbase cmdHelper = new((Application.Current as JAFDTC.App)?.Window,
                                                       configPage.Content.XamlRoot);
            cmdHelper.RunPilotDbEditorUI(configPage, cmd);
            return false;
        }
    }
}
