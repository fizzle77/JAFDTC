// ********************************************************************************************************************
//
// LinkResetBtnsControl.xaml : ui c# for common editor page link/reset controls
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

using JAFDTC.Models;
using JAFDTC.UI.Base;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace JAFDTC.UI.Controls
{
    public sealed partial class LinkResetBtnsControl : UserControl
    {
        private SystemEditorPageBase _parentPage;
        private IConfiguration _config;

        private string _systemTag;

        // For the configuration linking UI.
        private readonly Dictionary<string, string> _systemConfigNameToUID = new Dictionary<string, string>();
        private readonly List<string> _systemConfigNameList = new List<string>();
        private string _systemName;
        private Dictionary<string, IConfiguration> _uidToConfigMap;

        // ---- events --------------------------------------------------------------------------------

        /// <summary>
        /// Event that fires after configuration has changed due to linking or reset.
        /// </summary>
        public event Action AfterConfigLinkedOrReset;
        private void OnAfterConfigLinkedOrReset() => AfterConfigLinkedOrReset?.Invoke();

        /// <summary>
        /// Event that fires indicating the contining page should reset the config to default.
        /// </summary>
        public event Action DoReset;
        private void OnDoReset() => DoReset?.Invoke();

        public LinkResetBtnsControl()
        {
            InitializeComponent();
        }

        public void Initialize(string systemName, string systemTag, SystemEditorPageBase parentPage, IConfiguration config)
        {
            _config = config;
            _parentPage = parentPage;
            _systemName = systemName;
            _systemTag = systemTag;
        }

        public void NavigatedTo(Dictionary<string, IConfiguration> uidToConfigMap)
        {
            _uidToConfigMap = uidToConfigMap;
            Utilities.BuildSystemLinkLists(uidToConfigMap, _config.UID, _systemTag, _systemConfigNameList, _systemConfigNameToUID);
            UpdateLinkControls();
        }

        public void SetResetButtonEnabled(bool enabled)
        {
            uiPageBtnReset.IsEnabled = enabled;
        }

        private void UpdateLinkControls()
        {
            Utilities.RebuildLinkControls(_config, _systemTag, _uidToConfigMap, uiPageBtnTxtLink, uiPageTxtLink);
        }

        private async void PageBtnReset_Click(object _, RoutedEventArgs __)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configuration?",
                "Are you sure you want to reset the " + _systemName + " configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                _config.UnlinkSystem(_systemTag);
                UpdateLinkControls();
                OnDoReset();
                _config.Save(_parentPage, _systemTag);
                OnAfterConfigLinkedOrReset();
            }
        }

        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, _config, _systemTag, _systemConfigNameList);
            if (selectedItem == null)
            {
                _config.UnlinkSystem(_systemTag);
                _config.Save(_parentPage);
            }
            else if (selectedItem.Length > 0)
            {
                 _config.LinkSystemTo(_systemTag, _uidToConfigMap[_systemConfigNameToUID[selectedItem]]);
                _config.Save(_parentPage);
            }

            OnAfterConfigLinkedOrReset();
            UpdateLinkControls();
        }
    }
}
