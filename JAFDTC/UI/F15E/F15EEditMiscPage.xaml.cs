// ********************************************************************************************************************
//
// F15EEditMFDPage.xaml.cs : ui c# for mudhen misc setup editor page
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
using JAFDTC.Models.F15E.Misc;
using JAFDTC.Models.F15E.MPD;
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
    /// user interface for the page that allows you to edit the mudhen miscellaneous system configuration.
    /// </summary>
    public sealed partial class F15EEditMiscPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(MiscSystem.SystemTag, "Miscellaneous", "Miscellaneous", Glyphs.MISC, typeof(F15EEditMiscPage));

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

        private MiscSystem EditMisc { get; set; }

        private F15EConfiguration.CrewPositions EditCrewMember { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsRebuildingUI { get; set; }

        // ---- private properties, read-only

        private readonly MiscSystem _miscSysDefault;

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

        public F15EEditMiscPage()
        {
            InitializeComponent();

            EditMisc = new MiscSystem();
            EditMisc.ErrorsChanged += BaseField_DataValidationError;
            EditMisc.PropertyChanged += BaseField_PropertyChanged;

            IsRebuildPending = false;
            IsRebuildingUI = false;

            _miscSysDefault = MiscSystem.ExplicitDefaults;

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();

            _baseFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["Bingo"] = uiBINGOValueBINGO
            };
            _defaultBorderBrush = uiBINGOValueBINGO.BorderBrush;
            _defaultBkgndBrush = uiBINGOValueBINGO.Background;

            uiBINGOValueBINGO.PlaceholderText = _miscSysDefault.Bingo;

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
            EditCrewMember = Config.CrewMember;
            EditMisc.Bingo = Config.Misc.Bingo;
        }

        /// <summary>
        /// marshall data from the local misc setup into the configuration, persisting the configuration if directed.
        /// </summary>
        private void CopyEditToConfig(bool isPersist = false)
        {
            if (!EditMisc.HasErrors)
            {
                Config.Misc.Bingo = EditMisc.Bingo;

                if (isPersist)
                {
                    Config.Save(this, MiscSystem.SystemTag);
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
                ValidateAllFields(_baseFieldValueMap, EditMisc.GetErrors(null));
            }
            else
            {
                // Debug.WriteLine("== DataValErr (b): " + args.PropertyName);
                List<string> errors = (List<string>)EditMisc.GetErrors(args.PropertyName);
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
        /// change the selected crew member and update various ui and model state.
        /// </summary>
        private void SelectCrewMember(F15EConfiguration.CrewPositions member)
        {
            if (member == (int)F15EConfiguration.CrewPositions.PILOT)
            {
                uiGridPilotRow.Visibility = Visibility.Visible;
                uiGridWizzoRow.Visibility = Visibility.Collapsed;
            }
            else
            {
                uiGridPilotRow.Visibility = Visibility.Collapsed;
                uiGridWizzoRow.Visibility = Visibility.Visible;
            }
            EditCrewMember = member;
            CopyEditToConfig(true);
            RebuildInterfaceState();
        }

        /// <summary>
        /// rebuild the link controls on the page based on where the configuration is linked to.
        /// </summary>
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, MiscSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        /// <summary>
        /// update the enable state on the ui elements based on the current settings. link controls must be set up
        /// via RebuildLinkControls() prior to calling this function.
        /// </summary>
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(MiscSystem.SystemTag));
            foreach (KeyValuePair<string, TextBox> kvp in _baseFieldValueMap)
            {
                Utilities.SetEnableState(kvp.Value, isEditable);
            }

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);

            Utilities.SetEnableState(uiPageBtnReset, !EditMisc.IsDefault);
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
                Config.UnlinkSystem(MiscSystem.SystemTag);
                Config.Misc.Reset();
                Config.Save(this, MiscSystem.SystemTag);
                CopyConfigToEdit();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, MiscSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                Config.UnlinkSystem(MiscSystem.SystemTag);
                Config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(MiscSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit();
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
        /// on aux command invoked, update the state of the editor based on the command.
        /// </summary>
        private void AuxCommandInvokedHandler(object sender, ConfigAuxCommandInfo args)
        {
            Config.Save(this, MPDSystem.SystemTag);
            SelectCrewMember(Config.CrewMember);
        }

        /// <summary>
        /// on navigating to this page, set up our internal and ui state based on the configuration we are editing.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;
            Config = (F15EConfiguration)NavArgs.Config;

            NavArgs.ConfigPage.AuxCommandInvoked += AuxCommandInvokedHandler;
            Config.ConfigurationSaved += ConfigurationSavedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, MiscSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            CopyConfigToEdit();

            SelectCrewMember(EditCrewMember);

            ValidateAllFields(_baseFieldValueMap, EditMisc.GetErrors(null));
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }

        /// <summary>
        /// on navigating from this page, tear down our internal and ui state.
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            NavArgs.ConfigPage.AuxCommandInvoked -= AuxCommandInvokedHandler;
            Config.ConfigurationSaved -= ConfigurationSavedHandler;

            base.OnNavigatedFrom(args);
        }
    }
}
