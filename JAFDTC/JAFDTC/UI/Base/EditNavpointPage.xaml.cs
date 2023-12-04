// ********************************************************************************************************************
//
// EditNavpointListPage.cs : ui c# for general navigation point editor page
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

using CommunityToolkit.WinUI.UI;
using JAFDTC.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
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

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// navigation argument for pages that push to the EditNavpointPage navpoint editor. this identifies the specific
    /// navpoint being edited. this class enables targeting the general EditNavpointPage to a specific airframe.
    /// </summary>
    public sealed class EditNavptPageNavArgs
    {
        public Page ParentEditor { get; }                       // parent editor (typically a navpoint list)

        public IConfiguration Config { get; }                   // configuration

        public int IndexNavpt { get; }                          // index of navpoint being edited

        public bool IsUnlinked { get; }                         // true => navpoints not linked to other configuration

        public string NavptName { get; }                        // "name" of a navpoint ("Waypoint", "Steerpoint", etc.)

        public Type EditorHelperType { get; }                   // helper class for EditNavpointPage

        public EditNavptPageNavArgs(Page parent, IConfiguration config, int index, bool isUnlinked, Type helper)
            => (ParentEditor, Config, IndexNavpt, IsUnlinked, EditorHelperType) = (parent, config, index, isUnlinked, helper);
    }

    /// <summary>
    /// page to edit navigation point fields. this is a general-purpose class that is instatiated in combination with
    /// a IEditNavpointHelper class to provide airframe-specific specialization.
    /// 
    /// using IEditNavpointHelper, this class can support other navigation point systems that go beyond the basic
    /// functionality in NavpointInfoBase and NavpointSystemBase.
    /// </summary>
    public sealed partial class EditNavpointPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public EditNavptPageNavArgs NavArgs { get; set; }

        private IEditNavpointPageHelper NavHelper { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. edits by the ui are usually
        // NOTE: directed at the EditNavpt/EditNavptIndex properties (exceptions occur when the edit requires changes
        // NOTE: to other steerpoints, such as add or replace vip/vrp).
        //
        private IConfiguration Config { get; set; }

        private NavpointInfoBase EditNavpt { get; set; }

        private int EditNavptIndex { get; set; }

        private bool IsRebuildPending { get; set; }

        private List<PointOfInterest> CurPoIs { get; set; }

        // read-only properties

        private readonly Dictionary<string, TextBox> _curNavptFieldValueMap;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public EditNavpointPage()
        {
            InitializeComponent();

            EditNavpt = null;

            CurPoIs = PointOfInterestDbase.Instance.Find();
            CurPoIs.Insert(0, new(PointOfInterestType.UNKNOWN, null, null, null, null, 0));

            IsRebuildPending = false;

            _curNavptFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["Lat"] = uiNavptValueLat,
                ["Lon"] = uiNavptValueLon,
                ["Alt"] = uiNavptValueAlt,
            };
            _defaultBorderBrush = uiNavptValueLat.BorderBrush;
            _defaultBkgndBrush = uiNavptValueLat.Background;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        // marshall data between our local navpoint setting and the appropriate navpoint in the navigation system
        // configuration.
        //
        private void CopyConfigToEdit(int index)
        {
            NavHelper.CopyConfigToEdit(index, Config, EditNavpt);
        }

        private void CopyEditToConfig(int index, bool isPersist = false)
        {
            if (NavHelper.CopyEditToConfig(index, EditNavpt, Config) && isPersist)
            {
                Config.Save(this, NavHelper.SystemTag);
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

        // TODO: document
        private void EditNavpt_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            if (args.PropertyName == null)
            {
                ValidateAllFields(_curNavptFieldValueMap, NavHelper.GetErrors(EditNavpt, null));
            }
            else
            {
                List<string> errors = NavHelper.GetErrors(EditNavpt, args.PropertyName);
                if (_curNavptFieldValueMap.ContainsKey(args.PropertyName))
                {
                    SetFieldValidState(_curNavptFieldValueMap[args.PropertyName], (errors.Count == 0));
                }
            }
            RebuildInterfaceState();
        }

        // property changed: rebuild interface state to account for configuration changes.
        //
        private void EditField_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        // returns true if the current state has errors, false otherwise.
        //
        private bool CurStateHasErrors()
        {
            return NavHelper.HasErrors(EditNavpt);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        // rebuild the enable state of the buttons in the ui based on current configuration setup.
        //
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(NavHelper.SystemTag));
            Utilities.SetEnableState(uiPoIComboSelect, isEditable);
            Utilities.SetEnableState(uiPoIBtnApply, isEditable);
            Utilities.SetEnableState(uiNavptValueName, isEditable);
            foreach (KeyValuePair<string, TextBox> kvp in _curNavptFieldValueMap)
            {
                Utilities.SetEnableState(kvp.Value, isEditable);
            }

            Utilities.SetEnableState(uiPoIBtnApply, isEditable && (uiPoIComboSelect.SelectedIndex > 0));
            // TODO: capture is always disabled for now
            Utilities.SetEnableState(uiPoIBtnCapture, false);

            Utilities.SetEnableState(uiNavptBtnPrev, !CurStateHasErrors() && (EditNavptIndex > 0));
            Utilities.SetEnableState(uiNavptBtnAdd, isEditable && !CurStateHasErrors());
            Utilities.SetEnableState(uiNavptBtnNext, !CurStateHasErrors() &&
                                                     (EditNavptIndex < (NavHelper.NavpointCount(Config) - 1)));

            Utilities.SetEnableState(uiAcceptBtnOK, isEditable && !CurStateHasErrors());
        }

        // rebuild the state of controls on the page in response to a change in the configuration. the configuration
        // is saved if requested.
        //
        private void RebuildInterfaceState()
        {
            if (!IsRebuildPending)
            {
                IsRebuildPending = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    uiNavptTextNum.Text = $"{NavHelper.NavptName} {EditNavpt.Number} Information";
                    RebuildEnableState();
                    IsRebuildPending = false;
                });
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- buttons -----------------------------------------------------------------------------------------------

        // cancel click: unwind navigation without saving any changes to the configuration.
        //
        private void AcceptBtnCancel_Click(object sender, RoutedEventArgs args)
        {
            Frame.GoBack();
        }

        // ok click: save configuration and navigate back to previous page in nav stack.
        //
        private void AcceptBtnOK_Click(object sender, RoutedEventArgs args)
        {
            if (NavHelper.HasErrors(EditNavpt))
            {
                RebuildEnableState();
            }
            else
            {
                CopyEditToConfig(EditNavptIndex, true);
                Frame.GoBack();
            }
        }

        // ---- poi management ----------------------------------------------------------------------------------------

        // poi combo selection changed: update enable state in the ui.
        //
        private void PoIComboSelect_SelectionChanged(object sender, RoutedEventArgs args)
        {
            RebuildEnableState();
        }

        // apply poi click: copy poi information into current steerpoint and reset poi selection to "none".
        //
        private void PoIBtnApply_Click(object sender, RoutedEventArgs args)
        {
            NavHelper.ApplyPoI(EditNavpt, (PointOfInterest)uiPoIComboSelect.SelectedItem);
            uiPoIComboSelect.SelectedIndex = 0;
            CopyEditToConfig(EditNavptIndex, true);
        }

        // TODO: implement
        private void PoIBtnCapture_Click(object sender, RoutedEventArgs args)
        {
        }

        // ---- steerpoint management ---------------------------------------------------------------------------------

        // steerpoint previous click: save the current steerpoint and move to the previous steerpoint.
        //
        private void NavptBtnPrev_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(EditNavptIndex, true);
            EditNavptIndex -= 1;
            CopyConfigToEdit(EditNavptIndex);
            RebuildInterfaceState();
        }

        // steerpoint previous click: save the current steerpoint and move to the next steerpoint.
        //
        private void NavptBtnNext_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(EditNavptIndex, true);
            EditNavptIndex += 1;
            CopyConfigToEdit(EditNavptIndex);
            RebuildInterfaceState();
        }

        // steerpoint add click: save the current steerpoint and add a new steerpoint to the end of the list.
        //
        private void NavptBtnAdd_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(EditNavptIndex, true);
            EditNavptIndex = NavHelper.AddNavpoint(Config);
            CopyConfigToEdit(EditNavptIndex);
            RebuildInterfaceState();
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        // TODO: document
        private void NavptTextBox_LostFocus(object sender, RoutedEventArgs args)
        {
            RebuildEnableState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        private void UpdateLatLonTextBoxFormat(TextBox textBox, Dictionary<string, string> format)
        {
            if (format != null)
            {
                foreach (KeyValuePair<string, string> kvp in format)
                {
                    switch (kvp.Key)
                    {
                        case "MaskPlaceholder": TextBoxExtensions.SetMaskPlaceholder(textBox, kvp.Value); break;
                        case "Regex": TextBoxExtensions.SetRegex(textBox, kvp.Value); break;
                        case "CustomMask": TextBoxExtensions.SetCustomMask(textBox, kvp.Value); break;
                        case "Mask": TextBoxExtensions.SetMask(textBox, kvp.Value); break;
                    }
                }
            }
        }
        // on navigating to this page, set up and tear down our internal and ui state based on the configuration we
        // are editing.
        //
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (EditNavptPageNavArgs)args.Parameter;

            NavHelper = (IEditNavpointPageHelper)Activator.CreateInstance(NavArgs.EditorHelperType);
            UpdateLatLonTextBoxFormat(uiNavptValueLat, NavHelper.LatExtProperties);
            UpdateLatLonTextBoxFormat(uiNavptValueLon, NavHelper.LonExtProperties);

            EditNavpt ??= NavHelper.CreateEditNavpt(EditField_PropertyChanged, EditNavpt_DataValidationError);

            Config = NavArgs.Config;

            EditNavptIndex = NavArgs.IndexNavpt;
            CopyConfigToEdit(EditNavptIndex);

            ValidateAllFields(_curNavptFieldValueMap, NavHelper.GetErrors(EditNavpt, null));
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }

    }
}
