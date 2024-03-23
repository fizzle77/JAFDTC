// ********************************************************************************************************************
//
// F16CEditMFDPage.xaml.cs : ui c# for viper misc setup editor page
//
// Copyright(C) 2023 ilominar/raven
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
using JAFDTC.Models.F16C.Misc;
using JAFDTC.UI;
using JAFDTC.UI.App;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class F16CEditMiscPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(MiscSystem.SystemTag, "Miscellaneous", "Miscellaneous", Glyphs.MISC, typeof(F16CEditMiscPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditMisc property.
        //
        private F16CConfiguration Config { get; set; }

        private MiscSystem EditMisc { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsRebuildingUI { get; set; }

        // ---- read-only properties

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

        public F16CEditMiscPage()
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
                ["Bingo"] = uiBINGOValueBINGO,
                ["BullseyeWP"] = uiBULLValueSP,
                ["ALOWCARAALOW"] = uiALOWValueCARAALOW,
                ["ALOWMSLFloor"] = uiALOWValueMSLFLOOR,
                ["LaserTGPCode"] = uiLASRValueTGP,
                ["LaserLSTCode"] = uiLASRValueLST,
                ["LaserStartTime"] = uiLASRValueTime,
                ["TACANChannel"] = uiTACANValueChan,
                ["ILSFrequency"] = uiILSValueFreq,
                ["ILSCourse"] = uiILSValueCourse,
            };
            _defaultBorderBrush = uiBINGOValueBINGO.BorderBrush;
            _defaultBkgndBrush = uiBINGOValueBINGO.Background;

            uiBINGOValueBINGO.PlaceholderText = _miscSysDefault.Bingo;
            uiBULLValueSP.PlaceholderText = _miscSysDefault.BullseyeWP;
            uiALOWValueCARAALOW.PlaceholderText = _miscSysDefault.ALOWCARAALOW;
            uiALOWValueMSLFLOOR.PlaceholderText = _miscSysDefault.ALOWMSLFloor;
            uiLASRValueTGP.PlaceholderText = "1688";
            uiLASRValueLST.PlaceholderText = "1688";
            uiLASRValueTime.PlaceholderText = _miscSysDefault.LaserStartTime;
            uiTACANValueChan.PlaceholderText = _miscSysDefault.TACANChannel;
            uiILSValueFreq.PlaceholderText = _miscSysDefault.ILSFrequency;
            uiILSValueCourse.PlaceholderText = _miscSysDefault.ILSCourse;

            // wait for final setup of the ui until we navigate to the page (at which point we will have a
            // configuration to display).
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
            EditMisc.ALOWCARAALOW = Config.Misc.ALOWCARAALOW;
            EditMisc.ALOWMSLFloor = Config.Misc.ALOWMSLFloor;

            EditMisc.Bingo = Config.Misc.Bingo;

            EditMisc.BullseyeWP = Config.Misc.BullseyeWP;
            EditMisc.BullseyeMode = Config.Misc.BullseyeMode;

            EditMisc.ILSFrequency = Config.Misc.ILSFrequency;
            EditMisc.ILSCourse = Config.Misc.ILSCourse;

            EditMisc.LaserLSTCode = Config.Misc.LaserLSTCode;
            EditMisc.LaserTGPCode = Config.Misc.LaserTGPCode;
            EditMisc.LaserStartTime = Config.Misc.LaserStartTime;

            EditMisc.HMCSBlankCockpit = Config.Misc.HMCSBlankCockpit;
            EditMisc.HMCSBlankHUD = Config.Misc.HMCSBlankHUD;
            EditMisc.HMCSDisplayRWR = Config.Misc.HMCSDisplayRWR;
            EditMisc.HMCSDeclutterLvl = Config.Misc.HMCSDeclutterLvl;
            EditMisc.HMCSIntensity = Config.Misc.HMCSIntensity;

            EditMisc.TACANChannel = Config.Misc.TACANChannel;
            EditMisc.TACANBand = Config.Misc.TACANBand;
            EditMisc.TACANIsYardstick = Config.Misc.TACANIsYardstick;
        }

        private void CopyEditToConfig(bool isPersist = false)
        {
            if (!EditMisc.HasErrors)
            {
                Config.Misc.ALOWCARAALOW = EditMisc.ALOWCARAALOW;
                Config.Misc.ALOWMSLFloor = EditMisc.ALOWMSLFloor;

                Config.Misc.Bingo = EditMisc.Bingo;

                Config.Misc.BullseyeWP = EditMisc.BullseyeWP;
                Config.Misc.BullseyeMode = EditMisc.BullseyeMode;

                Config.Misc.ILSFrequency = EditMisc.ILSFrequency;
                Config.Misc.ILSCourse = EditMisc.ILSCourse;

                Config.Misc.LaserLSTCode = EditMisc.LaserLSTCode;
                Config.Misc.LaserTGPCode = EditMisc.LaserTGPCode;
                Config.Misc.LaserStartTime = EditMisc.LaserStartTime;

                Config.Misc.HMCSBlankCockpit = EditMisc.HMCSBlankCockpit;
                Config.Misc.HMCSBlankHUD = EditMisc.HMCSBlankHUD;
                Config.Misc.HMCSDisplayRWR = EditMisc.HMCSDisplayRWR;
                Config.Misc.HMCSDeclutterLvl = EditMisc.HMCSDeclutterLvl;
                Config.Misc.HMCSIntensity = EditMisc.HMCSIntensity;

                Config.Misc.TACANChannel = EditMisc.TACANChannel;
                Config.Misc.TACANBand = EditMisc.TACANBand;
                Config.Misc.TACANIsYardstick = EditMisc.TACANIsYardstick;

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

        // validation error: update ui state for the various components that may have errors.
        //
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

        // property changed: rebuild interface state to account for configuration changes.
        //
        private void BaseField_PropertyChanged(object sender, EventArgs args)
        {
            RebuildInterfaceState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        // rebuild the setup of the tacan band according to the current settings. 
        //
        private void RebuildTACANSetup()
        {
            int band = (string.IsNullOrEmpty(EditMisc.TACANBand)) ? int.Parse(_miscSysDefault.TACANBand)
                                                                  : int.Parse(EditMisc.TACANBand);
            if (uiTACANComboBand.SelectedIndex != band)
            {
                uiTACANComboBand.SelectedIndex = band;
            }

            bool isEnable = (string.IsNullOrEmpty(EditMisc.TACANIsYardstick)) ? bool.Parse(_miscSysDefault.TACANIsYardstick)
                                                                              : bool.Parse(EditMisc.TACANIsYardstick);
            if (uiTACANCkboxIsYard.IsChecked != isEnable)
            {
                uiTACANCkboxIsYard.IsChecked = isEnable;
            }
        }

        // TODO: document
        private void RebuildBULLSetup()
        {
            bool isEnable = (string.IsNullOrEmpty(EditMisc.BullseyeMode)) ? bool.Parse(_miscSysDefault.BullseyeMode)
                                                                          : bool.Parse(EditMisc.BullseyeMode);
            if (uiBULLCkboxShowRefs.IsChecked != isEnable)
            {
                uiBULLCkboxShowRefs.IsChecked = isEnable;
            }
        }

        // TODO: document
        private void RebuildHMCSSetup()
        {
            int declut = (string.IsNullOrEmpty(EditMisc.HMCSDeclutterLvl)) ? int.Parse(_miscSysDefault.HMCSDeclutterLvl)
                                                                           : int.Parse(EditMisc.HMCSDeclutterLvl);
            if (uiHMCSComboDeclutSelect.SelectedIndex != declut)
            {
                uiHMCSComboDeclutSelect.SelectedIndex = declut;
            }

            bool isEnable;
                
            isEnable = (string.IsNullOrEmpty(EditMisc.HMCSBlankCockpit)) ? bool.Parse(_miscSysDefault.HMCSBlankCockpit)
                                                                         : bool.Parse(EditMisc.HMCSBlankCockpit);
            if (uiHMCSCkboxBlankCock.IsChecked != isEnable)
            {
                uiHMCSCkboxBlankCock.IsChecked = isEnable;
            }
            isEnable = (string.IsNullOrEmpty(EditMisc.HMCSBlankHUD)) ? bool.Parse(_miscSysDefault.HMCSBlankHUD)
                                                                     : bool.Parse(EditMisc.HMCSBlankHUD);
            if (uiHMCSCkboxBlankHUD.IsChecked != isEnable)
            {
                uiHMCSCkboxBlankHUD.IsChecked = isEnable;
            }
            isEnable = (string.IsNullOrEmpty(EditMisc.HMCSDisplayRWR)) ? bool.Parse(_miscSysDefault.HMCSDisplayRWR)
                                                                       : bool.Parse(EditMisc.HMCSDisplayRWR);
            if (uiHMCSCkboxShowRWR.IsChecked != isEnable)
            {
                uiHMCSCkboxShowRWR.IsChecked = isEnable;
            }

            double value = (string.IsNullOrEmpty(EditMisc.HMCSIntensity)) ? 0.0 : double.Parse(EditMisc.HMCSIntensity);
            uiHMCSSliderIntensity.Value = value * 100.0;
        }

        // TODO: document
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, MiscSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        // update the enable state on the ui elements based on the current settings. link controls must be set up
        // vi RebuildLinkControls() prior to calling this function.
        //
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(MiscSystem.SystemTag));
            foreach (KeyValuePair<string,TextBox> kvp in _baseFieldValueMap)
            {
                Utilities.SetEnableState(kvp.Value, isEditable);
            }
            Utilities.SetEnableState(uiTACANComboBand, isEditable);
            Utilities.SetEnableState(uiTACANCkboxIsYard, isEditable);
            Utilities.SetEnableState(uiBULLCkboxShowRefs, isEditable);
            Utilities.SetEnableState(uiHMCSComboDeclutSelect, isEditable);
            Utilities.SetEnableState(uiHMCSCkboxBlankCock, isEditable);
            Utilities.SetEnableState(uiHMCSCkboxBlankHUD, isEditable);
            Utilities.SetEnableState(uiHMCSCkboxShowRWR, isEditable);
            Utilities.SetEnableState(uiHMCSSliderIntensity, isEditable);

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);

            Utilities.SetEnableState(uiPageBtnReset, !EditMisc.IsDefault);
        }

        // rebuild the state of controls on the page in response to a change in the configuration.
        //
        private void RebuildInterfaceState()
        {
            if (!IsRebuildPending)
            {
                IsRebuildPending = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    IsRebuildingUI = true;
                    RebuildTACANSetup();
                    RebuildBULLSetup();
                    RebuildHMCSSetup();
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

        // on clicks of the reset all button, reset all settings back to default.
        //
        private async void PageBtnReset_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configruation?",
                "Are you sure you want to reset the miscellaneous configurations to avionics defaults? This action cannot be undone.",
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

        // TODO: document
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

        // ---- tacan setup -------------------------------------------------------------------------------------------

        // TODO: document
        private void TACANComboBand_SelectionChanged(object sender, RoutedEventArgs args)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                EditMisc.TACANBand = (string)item.Tag;
                CopyEditToConfig(true);
            }
        }

        private void TACANCkboxIsYard_Click(object sender, RoutedEventArgs args)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (!IsRebuildingUI && (checkBox != null))
            {
                EditMisc.TACANIsYardstick = checkBox.IsChecked.ToString();
                CopyEditToConfig(true);
            }
        }

        // ---- bull mode selection -----------------------------------------------------------------------------------

        // TODO: document
        private void BULLCkboxShowRefs_Click(object sender, RoutedEventArgs args)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (!IsRebuildingUI && (checkBox != null))
            {
                EditMisc.BullseyeMode = checkBox.IsChecked.ToString();
                CopyEditToConfig(true);
            }
        }

        // ---- jhmcs setup -------------------------------------------------------------------------------------------

        // TODO: document
        private void HMCSCkboxBlankHUD_Click(object sender, RoutedEventArgs args)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (!IsRebuildingUI && (checkBox != null))
            {
                EditMisc.HMCSBlankHUD = checkBox.IsChecked.ToString();
                CopyEditToConfig(true);
            }
        }

        // TODO: document
        private void HMCSCkboxBlankCock_Click(object sender, RoutedEventArgs args)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (!IsRebuildingUI && (checkBox != null))
            {
                EditMisc.HMCSBlankCockpit = checkBox.IsChecked.ToString();
                CopyEditToConfig(true);
            }
        }

        // TODO: document
        private void HMCSCkboxShowRWR_Click(object sender, RoutedEventArgs args)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (!IsRebuildingUI && (checkBox != null))
            {
                EditMisc.HMCSDisplayRWR = checkBox.IsChecked.ToString();
                CopyEditToConfig(true);
            }
        }

        // TODO: document
        private void HMCSComboDeclutSelect_SelectionChanged(object sender, RoutedEventArgs args)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                EditMisc.HMCSDeclutterLvl = (string)item.Tag;
                CopyEditToConfig(true);
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void HMCSSliderIntensity_ValueChanged(object sender, RoutedEventArgs args)
        {
            Slider slider = (Slider)sender;
            EditMisc.HMCSIntensity = (slider.Value == 0.0) ? "" : $"{slider.Value / 100.0:F2}";
            CopyEditToConfig(true);
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        // text box lost focus: copy the local backing values to the configuration (note this is predicated on error
        // status) and rebuild the interface state.
        //
        // NOTE: though the text box has lost focus, the update may not yet have propagated into state. use the
        // NOTE: dispatch queue to give in-flight state updates time to complete.
        //
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

        // on configuration saved, rebuild the interface state to align with the latest save (assuming we go here
        // through a CopyEditToConfig).
        //
        private void ConfigurationSavedHandler(object sender, ConfigurationSavedEventArgs args)
        {
            RebuildInterfaceState();
        }

        // on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        // we are editing.
        //
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;
            Config = (F16CConfiguration)NavArgs.Config;

            Config.ConfigurationSaved += ConfigurationSavedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, MiscSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            CopyConfigToEdit();

            ValidateAllFields(_baseFieldValueMap, EditMisc.GetErrors(null));
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
