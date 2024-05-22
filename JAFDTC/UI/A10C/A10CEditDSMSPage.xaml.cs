// ********************************************************************************************************************
//
// A10CEditDSMSPage.cs : ui c# for warthog dsms page
//
// Copyright(C) 2024 fizzle
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

using JAFDTC.UI.App;
using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.DSMS;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using JAFDTC.Models;

namespace JAFDTC.UI.A10C
{
    public interface IA10CDSMSContentFrame
    {
        void CopyConfigToEditState();
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class A10CEditDSMSPage : Page
    {
        internal class DSMSEditorNavArgs
        {
            internal ConfigEditorPageNavArgs NavArgs { get; }
            internal A10CEditDSMSPage ParentPage { get; }

            internal DSMSEditorNavArgs(ConfigEditorPageNavArgs navArgs, A10CEditDSMSPage parentPage)
            {
                NavArgs = navArgs;
                ParentPage = parentPage;
            }
        }

        private DSMSEditorNavArgs _dsmsEditorNavArgs;
        private ConfigEditorPageNavArgs _navArgs;
        private A10CConfiguration _config;

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        public static ConfigEditorPageInfo PageInfo
            => new(DSMSSystem.SystemTag, "DSMS", "DSMS", Glyphs.DSMS, typeof(A10CEditDSMSPage));

        public A10CEditDSMSPage()
        {
            InitializeComponent();

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (ReferenceEquals(args.SelectedItem, uiMunitionTab))
                DSMSContentFrame.Navigate(typeof(A10CEditDSMSMunitionSettingsPage), _dsmsEditorNavArgs);
            else if (ReferenceEquals(args.SelectedItem, uiProfileTab))
                DSMSContentFrame.Navigate(typeof(A10CEditDSMSProfileOrderPage), _dsmsEditorNavArgs);
            else
                throw new ApplicationException("Unexpected NavigationViewItem type");
        }

        private void UpdateNonDefaultIcons() 
        {
            if (_config.DSMS.IsLaserCodeDefault && _config.DSMS.AreAllMunitionSettingsDefault)
                uiIconMunitionTab.Visibility = Visibility.Collapsed;
            else
                uiIconMunitionTab.Visibility = Visibility.Visible;

            if (_config.DSMS.IsProfileOrderDefault)
                uiIconProfileTab.Visibility = Visibility.Collapsed;
            else
                uiIconProfileTab.Visibility = Visibility.Visible;
        }

        // ---- page settings -----------------------------------------------------------------------------------------

        // on clicks of the reset all button, reset all settings back to default.
        //
        private async void PageBtnReset_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configruation?",
                "Are you sure you want to reset the DSMS configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                _config.UnlinkSystem(DSMSSystem.SystemTag);
                _config.DSMS.Reset();
                _config.Save(this, DSMSSystem.SystemTag);

                ((IA10CDSMSContentFrame)DSMSContentFrame.Content).CopyConfigToEditState();
            }
        }

        // TODO: document
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, _config, DSMSSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                _config.UnlinkSystem(DSMSSystem.SystemTag);
                _config.Save(this);
                ((IA10CDSMSContentFrame)DSMSContentFrame.Content).CopyConfigToEditState();
            }
            else if (selectedItem.Length > 0)
            {
                _config.LinkSystemTo(DSMSSystem.SystemTag, _navArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                _config.Save(this);
                ((IA10CDSMSContentFrame)DSMSContentFrame.Content).CopyConfigToEditState();
            }

            UpdateLinkControls();
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            _navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            _dsmsEditorNavArgs = new DSMSEditorNavArgs(_navArgs, this);
            _config = (A10CConfiguration)_navArgs.Config;

            Utilities.BuildSystemLinkLists(_navArgs.UIDtoConfigMap, _config.UID, DSMSSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            _config.ConfigurationSaved += ConfigurationSavedHandler;

            UpdateNonDefaultIcons();
            UpdateLinkControls();

            base.OnNavigatedTo(args);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _config.ConfigurationSaved -= ConfigurationSavedHandler;
            base.OnNavigatedFrom(e);
        }

        private void ConfigurationSavedHandler(object sender, ConfigurationSavedEventArgs args)
        {
            UpdateNonDefaultIcons();
        }

        private void UpdateLinkControls()
        {
            Utilities.RebuildLinkControls(_config, DSMSSystem.SystemTag, _navArgs.UIDtoConfigMap, uiPageBtnTxtLink, uiPageTxtLink);
        }
    }
}
