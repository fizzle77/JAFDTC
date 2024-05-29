// ********************************************************************************************************************
//
// F16CEditSMSPage.xaml.cs : ui c# for viper sms editor page
//
// Copyright(C) 2024 ilominar/raven
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
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.SMS;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class F16CEditSMSPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(SMSSystem.SystemTag, "Munitions", "SMS", Glyphs.SMS, typeof(F16CEditSMSPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditSettings property.
        //
        private F16CConfiguration Config { get; set; }

        private MunitionSettings EditSettings { get; set; }

        private SMSSystem.Munitions EditMuni { get; set; }

        private string EditProfile { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsRebuildingUI { get; set; }

        // ---- read-only properties

        private readonly List<F16CMunition> _munitions;

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

        public F16CEditSMSPage()
        {
            InitializeComponent();

            EditSettings = new MunitionSettings();
            EditMuni = SMSSystem.Munitions.CBU_87;
            EditProfile = "1";

            IsRebuildPending = false;
            IsRebuildingUI = false;

            _munitions = FileManager.LoadF16CMunitions();

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        // marshall data between our local misc setup and the configuration.
        //
        private void CopyConfigToEdit()
        {
            MunitionSettings profile = Config.SMS.GetSettingsForMunitionProfile(EditMuni, EditProfile);
            EditSettings.EmplMode = profile.EmplMode;
        }

        private void CopyEditToConfig(bool isPersist = false)
        {
            if (!EditSettings.HasErrors)
            {
                MunitionSettings profile = Config.SMS.GetSettingsForMunitionProfile(EditMuni, EditProfile);
                profile.EmplMode = new(EditSettings.EmplMode);
                if (isPersist)
                {
                    Config.SMS.CleanUp();
                    Config.Save(this, SMSSystem.SystemTag);
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

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// rebuild the link controls on the page based on where the configuration is linked to.
        /// </summary>
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, SMSSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        /// <summary>
        /// update the enable state on the ui elements based on the current settings. link controls must be set up
        /// via RebuildLinkControls() prior to calling this function.
        /// </summary>
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(SMSSystem.SystemTag));

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);

#if NOPE
            Utilities.SetEnableState(uiPageBtnReset, !EditMisc.IsDefault);
#endif
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
                    F16CMunition munition = (F16CMunition)uiListMunition.SelectedItem;
                    uiTextMuniDesc.Text = (munition != null) ? munition.DescrUI : "No Munition Selected";
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

        // ---- common editor controls --------------------------------------------------------------------------------

        /// <summary>
        /// reset all button click: reset all dlnk settings back to their defaults if the user consents.
        /// </summary>
        private async void PageBtnReset_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configuration?",
                "Are you sure you want to reset the Stores Management System configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(SMSSystem.SystemTag);
                Config.Misc.Reset();
                Config.Save(this, SMSSystem.SystemTag);
                CopyConfigToEdit();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, SMSSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                Config.UnlinkSystem(SMSSystem.SystemTag);
                Config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(SMSSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit();
            }
        }

        // ---- munition list -----------------------------------------------------------------------------------------

        // It's important that these TextBoxes use the LosingFocus focus event, not LostFocus. LosingFocus
        // fires synchronoulsy before uiComboMunition_SelectionChanged, ensuring an altered text value is
        // correctly saved before switching munitions.

        /// <summary>
        /// TODO: document
        /// </summary>
        private void ListMunition_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (args.RemovedItems.Count > 0)
            {
                F16CMunition oldSelectedMunition = (F16CMunition)args.RemovedItems[0];
                if (oldSelectedMunition != null)
                {
#if NOPE
                    MunitionSettings oldSettings = _editState.GetMunitionSettings(oldSelectedMunition);
                    oldSettings.ErrorsChanged -= BaseField_DataValidationError;
                    oldSettings.PropertyChanged -= BaseField_PropertyChanged;
#endif
                }
            }

            if (args.AddedItems.Count > 0)
            {
                F16CMunition newSelectedMunition = (F16CMunition)args.AddedItems[0];
                if (newSelectedMunition != null)
                {
                    EditMuni = (SMSSystem.Munitions)newSelectedMunition.ID;
                    EditProfile = "1";
#if NOPE
                    CopyConfigToEditState(newSelectedMunition);
                    UpdateUIFromEditState();
#endif
                }
            }

            RebuildInterfaceState();
        }

        // ---- munition parameters -----------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void ComboProfile_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void ComboEmploy_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void TextRippleQty_LosingFocus(object sender, LosingFocusEventArgs args)
        {
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void TextRippleFt_LosingFocus(object sender, LosingFocusEventArgs args)
        {
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
        /// on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        /// we are editing.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;
            Config = (F16CConfiguration)NavArgs.Config;

            Config.SMS.CleanUp();

            Config.ConfigurationSaved += ConfigurationSavedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, SMSSystem.SystemTag,
                                           _configNameList, _configNameToUID);
            CopyConfigToEdit();

            uiListMunition.SelectedIndex = 0;

#if NOPE
            ValidateAllFields(_baseFieldValueMap, EditMisc.GetErrors(null));
#endif
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            Config.ConfigurationSaved -= ConfigurationSavedHandler;

            base.OnNavigatedFrom(args);
        }
    }
}
