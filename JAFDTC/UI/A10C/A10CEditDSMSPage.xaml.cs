// ********************************************************************************************************************
//
// A10CEditDSMSPage.cs : ui c# for warthog dsms page
//
// Copyright(C) 2024-2025 fizzle
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
using JAFDTC.Models;
using JAFDTC.UI.Base;

namespace JAFDTC.UI.A10C
{
    public interface IA10CDSMSEditorTab
    {
        public void DSMSEditorCopyConfigToEditState();
    }

    /// <summary>
    /// Code-behind class for the A10 DSMS editor.
    /// </summary>
    public sealed partial class A10CEditDSMSPage : SystemEditorPageBase
    {
        internal class DSMSEditorNavArgs
        {
            internal NavigationEventArgs BaseArgs { get; }
            internal ConfigEditorPageNavArgs EditorPageNavArgs { get; }
            internal A10CEditDSMSPage ParentPage { get; }

            internal DSMSEditorNavArgs(NavigationEventArgs args, A10CEditDSMSPage parentPage)
            {
                BaseArgs = args;
                EditorPageNavArgs = (ConfigEditorPageNavArgs)args.Parameter;
                ParentPage = parentPage;
            }
        }

        private const string SYSTEM_NAME = "DSMS";

        protected override SystemBase SystemConfig => ((A10CConfiguration)Config).DSMS;
        protected override string SystemTag => DSMSSystem.SystemTag;
        protected override string SystemName => SYSTEM_NAME;

        private DSMSSystem DSMSConfig => (DSMSSystem)SystemConfig;

        private DSMSEditorNavArgs _dsmsEditorNavArgs;

        public static ConfigEditorPageInfo PageInfo
            => new(DSMSSystem.SystemTag, SYSTEM_NAME, SYSTEM_NAME, Glyphs.DSMS, typeof(A10CEditDSMSPage));

        public A10CEditDSMSPage()
        {
            InitializeComponent();
            InitializeBase(null, null, uiCtlLinkResetBtns);
        }

        // ---- UI helpers -----------------------------------------------------------------------------------------

        protected override void CopyConfigToEditState()
        {
            if (DSMSContentFrame.Content != null)
                ((IA10CDSMSEditorTab)DSMSContentFrame.Content).DSMSEditorCopyConfigToEditState();
            UpdateDefaultStateIndicators();
        }

        private void UpdateDefaultStateIndicators()
        {
            bool munitionsTabIsDefault = DSMSConfig.IsLaserCodeDefault && DSMSConfig.AreAllMunitionSettingsDefault;
            if (munitionsTabIsDefault)
                uiIconMunitionTab.Visibility = Visibility.Collapsed;
            else
                uiIconMunitionTab.Visibility = Visibility.Visible;

            if (DSMSConfig.IsProfileOrderDefault)
                uiIconProfileTab.Visibility = Visibility.Collapsed;
            else
                uiIconProfileTab.Visibility = Visibility.Visible;

            uiCtlLinkResetBtns.SetResetButtonEnabled(!munitionsTabIsDefault || !DSMSConfig.IsProfileOrderDefault);
        }

        private void ConfigurationSavedHandler(object sender, ConfigurationSavedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.SyncSysTag))
                CopyConfigToEditState();
            UpdateDefaultStateIndicators();
        }

        // ---- event handlers -----------------------------------------------------------------------------------------

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (ReferenceEquals(args.SelectedItem, uiMunitionTab))
                DSMSContentFrame.Navigate(typeof(A10CEditDSMSMunitionSettingsPage), _dsmsEditorNavArgs);
            else if (ReferenceEquals(args.SelectedItem, uiProfileTab))
                DSMSContentFrame.Navigate(typeof(A10CEditDSMSProfileOrderPage), _dsmsEditorNavArgs);
            else
                throw new ApplicationException("Unexpected NavigationViewItem type");
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            base.OnNavigatedTo(args);
         
            _dsmsEditorNavArgs = new DSMSEditorNavArgs(args, this);

            Config.ConfigurationSaved += ConfigurationSavedHandler;
            UpdateDefaultStateIndicators();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Config.ConfigurationSaved -= ConfigurationSavedHandler;

            base.OnNavigatedFrom(e);
        }
    }
}
