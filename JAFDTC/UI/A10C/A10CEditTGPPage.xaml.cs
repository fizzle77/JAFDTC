using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.TGP;
using JAFDTC.UI.App;
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using JAFDTC.Utilities;
using System.Reflection;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// Code-behind class for the A10 TGP editor.
    /// </summary>
    public sealed partial class A10CEditTGPPage : Page
    {
        private ConfigEditorPageNavArgs _navArgs;
        private A10CConfiguration _config;

        // For configuration linking UI.
        private readonly Dictionary<string, string> _configNameToUID = new Dictionary<string, string>();
        private readonly List<string> _configNameList = new List<string>();

        private bool _isUIUpdatePending = false;
        private TGPSystem _editState;

        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        public static ConfigEditorPageInfo PageInfo
            => new(TGPSystem.SystemTag, "TGP", "TGP", Glyphs.TGP, typeof(A10CEditTGPPage));

        public A10CEditTGPPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Change handler for all of the ComboBoxes that manage a setting.
        /// Sets the corresponding edit state property value and updates the underlying config.
        /// </summary>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            //GetControlEditStateProperty(comboBox, out PropertyInfo property, out BindableObject editState);

            //if (property != null && editState != null)
            //    property.SetValue(editState, comboBox.SelectedIndex.ToString());

            //SaveEditStateToConfig();
        }

        /// <summary>
        /// Change handler for all of the TextBoxes that manage a setting.
        /// Sets the corresponding edit state property value and updates the underlying config.
        /// </summary>
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            //GetControlEditStateProperty(textBox, out PropertyInfo property, out BindableObject editState);

            //if (property != null && editState != null)
            //    property.SetValue(editState, textBox.Text);

            //SaveEditStateToConfig();
        }

        /// <summary>
        /// A UX nicety: highlight the whole value in a TextBox when it
        /// gets focus such that the new value can immediately be entered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            textBox.SelectAll();
        }

        private void uiCheckLatch_Clicked(object sender, RoutedEventArgs args)
        {
            CheckBox checkBox = (CheckBox)sender;
            //if (!IsRebuildingUI && (checkBox != null))
            //{
            //    EditMisc.BullseyeOnHUD = checkBox.IsChecked.ToString();
            //    CopyEditToConfig(true);
            //}
        }


        // ---- page settings -----------------------------------------------------------------------------------------

        // on clicks of the reset all button, reset all settings back to default.
        //
        private async void PageBtnReset_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configuration?",
                "Are you sure you want to reset the TGP configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                _config.UnlinkSystem(TGPSystem.SystemTag);
                _config.HMCS.Reset();
                _config.Save(this, TGPSystem.SystemTag);
                //CopyConfigToEditState();
            }
        }

        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, _config, TGPSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                _config.UnlinkSystem(TGPSystem.SystemTag);
                _config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                _config.LinkSystemTo(TGPSystem.SystemTag, _navArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                _config.Save(this);
            }

            //CopyConfigToEditState();
            UpdateLinkControls();
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            _navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            _config = (A10CConfiguration)_navArgs.Config;

            Utilities.BuildSystemLinkLists(_navArgs.UIDtoConfigMap, _config.UID, TGPSystem.SystemTag, _configNameList, _configNameToUID);
            UpdateLinkControls();

            base.OnNavigatedTo(args);
        }

        private void UpdateLinkControls()
        {
            Utilities.RebuildLinkControls(_config, TGPSystem.SystemTag, _navArgs.UIDtoConfigMap, uiPageBtnTxtLink, uiPageTxtLink);
        }
    }
}
