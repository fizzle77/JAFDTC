using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.HMCS;
using JAFDTC.UI.App;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// Code-behind class for the A10 HMCS editor.
    /// </summary>
    public sealed partial class A10CEditHMCSPage : Page
    {
        private ConfigEditorPageNavArgs _navArgs;
        private A10CConfiguration _config;

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;
        
        public static ConfigEditorPageInfo PageInfo
            => new(HMCSSystem.SystemTag, "HMCS", "HMCS", Glyphs.HMCS, typeof(A10CEditHMCSPage));

        public A10CEditHMCSPage()
        {
            this.InitializeComponent();

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();
        }

        // ---- page settings -----------------------------------------------------------------------------------------

        // on clicks of the reset all button, reset all settings back to default.
        //
        private async void PageBtnReset_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configuration?",
                "Are you sure you want to reset the DSMS configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                _config.UnlinkSystem(HMCSSystem.SystemTag);
                _config.DSMS.Reset();
                _config.Save(this, HMCSSystem.SystemTag);
            }
        }

        // TODO: document
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, _config, HMCSSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                _config.UnlinkSystem(HMCSSystem.SystemTag);
                _config.Save(this);
                //((IA10CDSMSContentFrame)DSMSContentFrame.Content).CopyConfigToEditState();
            }
            else if (selectedItem.Length > 0)
            {
                _config.LinkSystemTo(HMCSSystem.SystemTag, _navArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                _config.Save(this);
                //((IA10CDSMSContentFrame)DSMSContentFrame.Content).CopyConfigToEditState();
            }

            UpdateLinkControls();
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            _navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            _config = (A10CConfiguration)_navArgs.Config;

            Utilities.BuildSystemLinkLists(_navArgs.UIDtoConfigMap, _config.UID, HMCSSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            UpdateLinkControls();

            base.OnNavigatedTo(args);
        }

        private void UpdateLinkControls()
        {
            Utilities.RebuildLinkControls(_config, HMCSSystem.SystemTag, _navArgs.UIDtoConfigMap, uiPageBtnTxtLink, uiPageTxtLink);
        }
    }
}
