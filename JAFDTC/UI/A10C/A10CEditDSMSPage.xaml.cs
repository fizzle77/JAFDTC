using JAFDTC.UI.App;
using JAFDTC.Models.A10C.DSMS;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using JAFDTC.Models.A10C;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.Generic;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
                DSMSContentFrame.Navigate(typeof(A10CEditDSMSMunitionSettingsPage), _navArgs);
            else if (ReferenceEquals(args.SelectedItem, uiProfileTab))
                DSMSContentFrame.Navigate(typeof(A10CEditDSMSProfileOrderPage), _navArgs);
            else
                throw new ApplicationException("Unexpected NavigationViewItem type");
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
            _config = (A10CConfiguration)_navArgs.Config;

            Utilities.BuildSystemLinkLists(_navArgs.UIDtoConfigMap, _config.UID, DSMSSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            UpdateLinkControls();

            base.OnNavigatedTo(args);
        }

        private void UpdateLinkControls()
        {
            Utilities.RebuildLinkControls(_config, DSMSSystem.SystemTag, _navArgs.UIDtoConfigMap, uiPageBtnTxtLink, uiPageTxtLink);
        }
    }
}
