// ********************************************************************************************************************
//
// F15EEditMFDPage.xaml.cs : ui c# for mudhen mpd/mpcd setup editor page
//
// Copyright(C) 2023-2024 ilominar/raven
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
using JAFDTC.Models.F15E.MPD;
using JAFDTC.UI.App;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace JAFDTC.UI.F15E
{
    /// <summary>
    /// user interface for the page that allows you to edit the mudhen mpd/mpcd system configurations.
    /// </summary>
    public sealed partial class F15EEditMPDPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(MPDSystem.SystemTag, "Displays", "MPD", Glyphs.MPD, typeof(F15EEditMPDPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- private properties

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditMisc property.
        //
        private F15EConfiguration Config { get; set; }

        private MPDSystem EditMPD { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsRebuildingUI { get; set; }

        // ---- private properties, read-only

        private readonly MPDSystem _mpdSysDefault;

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        private readonly Dictionary<string, TextBox> _baseFieldValueMap;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F15EEditMPDPage()
        {
            this.InitializeComponent();

            EditMPD = new MPDSystem();
#if WIP
            EditMPD.ErrorsChanged += BaseField_DataValidationError;
            EditMPD.PropertyChanged += BaseField_PropertyChanged;
#endif

            IsRebuildPending = false;
            IsRebuildingUI = false;

            _mpdSysDefault = MPDSystem.ExplicitDefaults;

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();

#if WIP
            _baseFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["Bingo"] = uiBINGOValueBINGO,
                ["LowAltWarn"] = uiLaltValueWarn,
                ["TACANChannel"] = uiTACANValueChan,
                ["ILSFrequency"] = uiILSValueFreq,
            };
            _defaultBorderBrush = uiBINGOValueBINGO.BorderBrush;
            _defaultBkgndBrush = uiBINGOValueBINGO.Background;

            uiBINGOValueBINGO.PlaceholderText = _miscSysDefault.Bingo;
            uiLaltValueWarn.PlaceholderText = _miscSysDefault.LowAltWarn;
            uiTACANValueChan.PlaceholderText = _miscSysDefault.TACANChannel;
            uiILSValueFreq.PlaceholderText = _miscSysDefault.ILSFrequency;
#endif
            // wait for final setup of the ui until we navigate to the page (at which point we will have a
            // configuration to display).
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshall data from the configuration into our local misc setup.
        /// </summary>
        private void CopyConfigToEdit()
        {
#if WIP
            EditMisc.Bingo = Config.Misc.Bingo;
            EditMisc.ILSFrequency = Config.Misc.ILSFrequency;
            EditMisc.LowAltWarn = Config.Misc.LowAltWarn;
            EditMisc.TACANChannel = Config.Misc.TACANChannel;
            EditMisc.TACANBand = Config.Misc.TACANBand;
            EditMisc.TACANMode = Config.Misc.TACANMode;
#endif
        }

        /// <summary>
        /// marshall data from the local misc setup into the configuration, persisting the configuration if directed.
        /// </summary>
        private void CopyEditToConfig(bool isPersist = false)
        {
            if (!EditMPD.HasErrors)
            {
#if WIP
                Config.Misc.Bingo = EditMisc.Bingo;
                Config.Misc.ILSFrequency = EditMisc.ILSFrequency;
                Config.Misc.LowAltWarn = EditMisc.LowAltWarn;
                Config.Misc.TACANChannel = EditMisc.TACANChannel;
                Config.Misc.TACANBand = EditMisc.TACANBand;
                Config.Misc.TACANMode = EditMisc.TACANMode;
#endif

                if (isPersist)
                {
                    Config.Save(this, MPDSystem.SystemTag);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- page settings -----------------------------------------------------------------------------------------

        /// <summary>
        /// on clicks of the reset all button, reset all settings back to default.
        /// </summary>
        private async void PageBtnReset_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configruation?",
                "Are you sure you want to reset the MPD/MPCD system configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(MPDSystem.SystemTag);
                Config.MPD.Reset();
                Config.Save(this, MPDSystem.SystemTag);
                CopyConfigToEdit();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, MPDSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                CopyEditToConfig(true);
                Config.UnlinkSystem(MPDSystem.SystemTag);
                Config.Save(this);
                CopyConfigToEdit();
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(MPDSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit();
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on configuration saved, rebuild the interface state to align with the latest save (assuming we go here
        /// through a CopyEditToConfig).
        /// </summary>
        private void ConfigurationSavedHandler(object sender, ConfigurationSavedEventArgs args)
        {
#if WIP
            RebuildInterfaceState();
#endif
        }

        /// <summary>
        /// on navigating to this page, set up our internal and ui state based on the configuration we are editing.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;
            Config = (F15EConfiguration)NavArgs.Config;

            Config.ConfigurationSaved += ConfigurationSavedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, MPDSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            CopyConfigToEdit();

#if WIP
            ValidateAllFields(_baseFieldValueMap, EditMisc.GetErrors(null));
            RebuildInterfaceState();
#endif

            base.OnNavigatedTo(args);
        }

        /// <summary>
        /// on navigating from this page, tear down our internal and ui state.
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            Config.ConfigurationSaved -= ConfigurationSavedHandler;

            base.OnNavigatedFrom(args);
        }
    }
}
