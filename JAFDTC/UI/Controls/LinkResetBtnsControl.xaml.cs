using CommunityToolkit.WinUI.UI;
using JAFDTC.Models;
using JAFDTC.Models.A10C;
using JAFDTC.UI.A10C;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace JAFDTC.UI.Controls
{
    public sealed partial class LinkResetBtnsControl : UserControl
    {
        private A10CPageBase _parentPage;
        private A10CConfiguration _config;

        private string _systemTag;

        // For the configuration linking UI.
        private readonly Dictionary<string, string> _systemConfigNameToUID = new Dictionary<string, string>();
        private readonly List<string> _systemConfigNameList = new List<string>();
        private string _systemName;
        private Dictionary<string, IConfiguration> _uidToConfigMap;

        public event Action ConfigChanged;
        private void OnConfigChanged()
        {
            ConfigChanged?.Invoke();
        }

        public LinkResetBtnsControl()
        {
            InitializeComponent();
        }

        public void Initialize(string systemName, string systemTag, A10CPageBase parentPage, A10CConfiguration config)
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

        private async void PageBtnReset_Click(object sender, RoutedEventArgs e)
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
                _parentPage.SystemConfig.Reset();
                _config.Save(_parentPage, _systemTag);
                OnConfigChanged();
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

            OnConfigChanged();
            UpdateLinkControls();
        }
    }
}
