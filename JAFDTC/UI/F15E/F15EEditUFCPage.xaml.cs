// ********************************************************************************************************************
//
// F15EEditUFCPage.xaml.cs : ui c# for mudhen ufc setup editor page
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
using JAFDTC.Models.F15E.UFC;
using JAFDTC.UI.App;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace JAFDTC.UI.F15E
{
    /// <summary>
    /// user interface for the page that allows you to edit the mudhen ufc system configuration.
    /// </summary>
    public sealed partial class F15EEditUFCPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(UFCSystem.SystemTag, "Up-Front Controls", "UFC", Glyphs.UFC, typeof(F15EEditUFCPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- private properties

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditUFC property.
        //
        private F15EConfiguration Config { get; set; }

        private UFCSystem EditUFC { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsRebuildingUI { get; set; }

        // ---- private properties, read-only

        private readonly UFCSystem _ufcSysDefault;

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

        public F15EEditUFCPage()
        {
            InitializeComponent();

            EditUFC = new UFCSystem();
            EditUFC.ErrorsChanged += BaseField_DataValidationError;
            EditUFC.PropertyChanged += BaseField_PropertyChanged;

            IsRebuildPending = false;
            IsRebuildingUI = false;

            _ufcSysDefault = UFCSystem.ExplicitDefaults;

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();

            _baseFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["LowAltWarn"] = uiLaltValueWarn,
                ["TACANChannel"] = uiTACANValueChan,
                ["ILSFrequency"] = uiILSValueFreq,
            };
            _defaultBorderBrush = uiTACANValueChan.BorderBrush;
            _defaultBkgndBrush = uiTACANValueChan.Background;

            uiLaltValueWarn.PlaceholderText = _ufcSysDefault.LowAltWarn;
            uiTACANValueChan.PlaceholderText = _ufcSysDefault.TACANChannel;
            uiILSValueFreq.PlaceholderText = _ufcSysDefault.ILSFrequency;

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
            EditUFC.ILSFrequency = Config.UFC.ILSFrequency;
            EditUFC.LowAltWarn = Config.UFC.LowAltWarn;
            EditUFC.TACANChannel = Config.UFC.TACANChannel;
            EditUFC.TACANBand = Config.UFC.TACANBand;
            EditUFC.TACANMode = Config.UFC.TACANMode;
        }

        /// <summary>
        /// marshall data from the local misc setup into the configuration, persisting the configuration if directed.
        /// </summary>
        private void CopyEditToConfig(bool isPersist = false)
        {
            if (!EditUFC.HasErrors)
            {
                Config.UFC.ILSFrequency = EditUFC.ILSFrequency;
                Config.UFC.LowAltWarn = EditUFC.LowAltWarn;
                Config.UFC.TACANChannel = EditUFC.TACANChannel;
                Config.UFC.TACANBand = EditUFC.TACANBand;
                Config.UFC.TACANMode = EditUFC.TACANMode;

                if (isPersist)
                {
                    Config.Save(this, UFCSystem.SystemTag);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // field validation
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// set the border brush and background for a TextBox based on validity. valid fields use the defaults, invalid
        /// fields use ErrorFieldBorderBrush from the resources.
        /// </summary>
        private void SetFieldValidState(TextBox field, bool isValid)
        {
            field.BorderBrush = (isValid) ? _defaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
            field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
        }

        private void ValidateAllFields(Dictionary<string, TextBox> fields, IEnumerable errors)
        {
            Dictionary<string, bool> map = new();
            foreach (string error in errors)
            {
                map[error] = true;
            }
            foreach (KeyValuePair<string, TextBox> kvp in fields)
            {
                SetFieldValidState(kvp.Value, !map.ContainsKey(kvp.Key));
            }
        }

        /// <summary>
        /// validation error: update ui state for the various components that may have errors.
        /// </summary>
        private void BaseField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            if (args.PropertyName == null)
            {
                ValidateAllFields(_baseFieldValueMap, EditUFC.GetErrors(null));
            }
            else
            {
                // Debug.WriteLine("== DataValErr (b): " + args.PropertyName);
                List<string> errors = (List<string>)EditUFC.GetErrors(args.PropertyName);
                SetFieldValidState(_baseFieldValueMap[args.PropertyName], (errors.Count == 0));
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// property changed: rebuild interface state to account for configuration changes.
        /// </summary>
        private void BaseField_PropertyChanged(object sender, EventArgs args)
        {
            RebuildInterfaceState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// rebuild the setup of the tacan band and mode according to the current settings. 
        /// </summary>
        private void RebuildTACANSetup()
        {
            int band = (string.IsNullOrEmpty(EditUFC.TACANBand)) ? int.Parse(_ufcSysDefault.TACANBand)
                                                                  : int.Parse(EditUFC.TACANBand);
            if (uiTACANComboBand.SelectedIndex != band)
            {
                uiTACANComboBand.SelectedIndex = band;
            }

            int mode = (string.IsNullOrEmpty(EditUFC.TACANMode)) ? int.Parse(_ufcSysDefault.TACANMode)
                                                                  : int.Parse(EditUFC.TACANMode);
            if (uiTACANComboMode.SelectedIndex != mode)
            {
                uiTACANComboMode.SelectedIndex = mode;
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, UFCSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        /// <summary>
        /// update the enable state on the ui elements based on the current settings. link controls must be set up
        /// via RebuildLinkControls() prior to calling this function.
        /// </summary>
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(UFCSystem.SystemTag));
            foreach (KeyValuePair<string, TextBox> kvp in _baseFieldValueMap)
            {
                Utilities.SetEnableState(kvp.Value, isEditable);
            }
            Utilities.SetEnableState(uiTACANComboBand, isEditable);
            Utilities.SetEnableState(uiTACANComboMode, isEditable);

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);

            Utilities.SetEnableState(uiPageBtnReset, !EditUFC.IsDefault);
        }

        /// <summary>
        /// rebuild the state of controls on the page in response to a change in the configuration.
        /// </summary>
        private void RebuildInterfaceState()
        {
            if (!IsRebuildPending)
            {
                IsRebuildPending = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    IsRebuildingUI = true;
                    RebuildTACANSetup();
                    RebuildLinkControls();
                    RebuildEnableState();
                    IsRebuildingUI = false;
                    IsRebuildPending = false;
                });
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
                "Are you sure you want to reset the miscellaneous system configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(UFCSystem.SystemTag);
                Config.UFC.Reset();
                Config.Save(this, UFCSystem.SystemTag);
                CopyConfigToEdit();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, UFCSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                Config.UnlinkSystem(UFCSystem.SystemTag);
                Config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(UFCSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit();
            }
        }

        // ---- tacan setup -------------------------------------------------------------------------------------------

        /// <summary>
        /// selection changed on tacan band: update band state.
        /// </summary>
        private void TACANComboBand_SelectionChanged(object sender, RoutedEventArgs args)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                EditUFC.TACANBand = (string)item.Tag;
                CopyEditToConfig(true);
            }
        }

        /// <summary>
        /// selection changed on tacan mode: update mode state.
        /// </summary>
        private void TACANComboMode_SelectionChanged(object sender, RoutedEventArgs args)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                EditUFC.TACANMode = (string)item.Tag;
                CopyEditToConfig(true);
            }
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        /// <summary>
        /// text box lost focus: copy the local backing values to the configuration (note this is predicated on error
        /// status) and rebuild the interface state.
        ///
        /// NOTE: though the text box has lost focus, the update may not yet have propagated into state. use the
        /// NOTE: dispatch queue to give in-flight state updates time to complete.
        /// </summary>
        private void MiscTextBox_LostFocus(object sender, RoutedEventArgs args)
        {
            // CONSIDER: may be better here to handle this in a property changed handler rather than here?
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CopyEditToConfig(true);
            });
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
            RebuildInterfaceState();
        }

        /// <summary>
        /// on navigating to this page, set up our internal and ui state based on the configuration we are editing.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;
            Config = (F15EConfiguration)NavArgs.Config;

            Config.ConfigurationSaved += ConfigurationSavedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, UFCSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            CopyConfigToEdit();

            ValidateAllFields(_baseFieldValueMap, EditUFC.GetErrors(null));
            RebuildInterfaceState();

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
