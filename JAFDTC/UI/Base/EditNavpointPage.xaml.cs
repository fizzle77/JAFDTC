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
using JAFDTC.Utilities;
using JAFDTC.Utilities.Networking;
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

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

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

        private IEditNavpointPageHelper PageHelper { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. edits by the ui are usually
        // NOTE: directed at the EditNavpt/EditNavptIndex properties (exceptions occur when the edit requires changes
        // NOTE: to other steerpoints, such as add or replace vip/vrp).
        //
        private IConfiguration Config { get; set; }

        private NavpointInfoBase EditNavpt { get; set; }

        private int EditNavptIndex { get; set; }

        private bool IsRebuildPending { get; set; }

        private List<string> CurPoITheaters { get; set; }

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

            CurPoITheaters = PointOfInterestDbase.KnownTheaters();

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
            PageHelper.CopyConfigToEdit(index, Config, EditNavpt);
        }

        private void CopyEditToConfig(int index, bool isPersist = false)
        {
            if (PageHelper.CopyEditToConfig(index, EditNavpt, Config) && isPersist)
            {
                Config.Save(this, PageHelper.SystemTag);
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
                ValidateAllFields(_curNavptFieldValueMap, PageHelper.GetErrors(EditNavpt, null));
            }
            else
            {
                List<string> errors = PageHelper.GetErrors(EditNavpt, args.PropertyName);
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
            return PageHelper.HasErrors(EditNavpt);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return true if the current edit navpoint is a valid poi, false otherwise. a valid poi has a name,
        /// latitude, and longitude. the name should be unique within the user part of the poi database.
        /// </summary>
        private bool IsEditCoordValidPoI()
        {
            if (string.IsNullOrEmpty(EditNavpt.Name) || string.IsNullOrEmpty(EditNavpt.Alt) ||
                !double.TryParse(EditNavpt.Lat, out double lat) || !double.TryParse(EditNavpt.Lon, out double lon))
            {
                return false;
            }
            string theater = PointOfInterestDbase.TheaterForCoords(lat, lon);
            PointOfInterestDbQuery query = new(PointOfInterestTypeMask.ANY, theater, EditNavpt.Name);
            List<PointOfInterest> pois = PointOfInterestDbase.Instance.Find(query);
            foreach (PointOfInterest poi in pois)
            {
                if (poi.Type != PointOfInterestType.USER)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void SelectMatchingPoI()
        {
            uiPoIComboSelect.SelectedItem = NavpointUIHelper.FindMatchingPoI((string)uiPoIComboTheater.SelectedItem,
                                                                             EditNavpt, PageHelper.NavptCoordFmt);
        }

        /// <summary>
        /// rebuild the point of interest select combo box. this only needs to be called when the theater changes or
        /// when a poi is added to the current theater.
        /// </summary>
        private void RebuildPointsOfInterest()
        {
            NavpointUIHelper.RebuildPoICombo((string)uiPoIComboTheater.SelectedItem, uiPoIComboSelect);
            SelectMatchingPoI();
        }

        // rebuild the enable state of the buttons in the ui based on current configuration setup.
        //
        private void RebuildEnableState()
        {
            JAFDTC.App curApp = Application.Current as JAFDTC.App;

            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(PageHelper.SystemTag));
            bool isDCSListening = curApp.IsDCSAvailable && (curApp.DCSActiveAirframe == Config.Airframe);

            Utilities.SetEnableState(uiPoIComboTheater, isEditable);
            Utilities.SetEnableState(uiPoIComboSelect, isEditable && (uiPoIComboSelect.Items.Count > 0));
            Utilities.SetEnableState(uiPoIBtnApply, isEditable && (uiPoIComboSelect.SelectedIndex > 0));
            Utilities.SetEnableState(uiPoIBtnCapture, isEditable && isDCSListening);

            Utilities.SetEnableState(uiNavptValueName, isEditable);

            foreach (KeyValuePair<string, TextBox> kvp in _curNavptFieldValueMap)
            {
                Utilities.SetEnableState(kvp.Value, isEditable);
            }

            Utilities.SetEnableState(uiNavptBtnAddPoI, isEditable && IsEditCoordValidPoI());
            Utilities.SetEnableState(uiNavptBtnPrev, !CurStateHasErrors() && (EditNavptIndex > 0));
            Utilities.SetEnableState(uiNavptBtnAdd, isEditable && !CurStateHasErrors());
            Utilities.SetEnableState(uiNavptBtnNext, !CurStateHasErrors() &&
                                                     (EditNavptIndex < (PageHelper.NavpointCount(Config) - 1)));

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
                    uiPoITextTitle.Text = $"{PageHelper.NavptName} Initial Setup";
                    uiNavptTextNum.Text = $"{PageHelper.NavptName} {EditNavpt.Number} Information";
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

        /// <summary>
        /// cancel click: unwind navigation without saving any changes to the configuration.
        /// </summary>
        private void AcceptBtnCancel_Click(object sender, RoutedEventArgs args)
        {
            Frame.GoBack();
        }

        /// <summary>
        /// ok click: save configuration and navigate back to previous page in nav stack.
        /// </summary>
        private void AcceptBtnOK_Click(object sender, RoutedEventArgs args)
        {
            if (PageHelper.HasErrors(EditNavpt))
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

        /// <summary>
        /// TODO: document
        /// </summary>
        private void PoIComboTheater_SelectionChanged(object sender, RoutedEventArgs args)
        {
            RebuildPointsOfInterest();
            RebuildEnableState();
        }

        /// <summary>
        /// poi combo selection changed: update enable state in the ui.
        /// </summary>
        private void PoIComboSelect_SelectionChanged(object sender, RoutedEventArgs args)
        {
            RebuildEnableState();
        }

        /// <summary>
        /// apply poi click: copy poi information into current steerpoint and reset poi selection to "none".
        /// </summary>
        private void PoIBtnApply_Click(object sender, RoutedEventArgs args)
        {
            PageHelper.ApplyPoI(EditNavpt, (PointOfInterest)uiPoIComboSelect.SelectedItem);
            uiPoIComboSelect.SelectedIndex = 0;
            CopyEditToConfig(EditNavptIndex, true);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void PoIBtnCapture_Click(object sender, RoutedEventArgs args)
        {
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived += PoIBtnCapture_WyptCaptureDataReceived;
            await Utilities.CaptureSingleDialog(Content.XamlRoot, PageHelper.NavptName);
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived -= PoIBtnCapture_WyptCaptureDataReceived;

            CopyEditToConfig(EditNavptIndex, true);
            RebuildInterfaceState();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void PoIBtnCapture_WyptCaptureDataReceived(WyptCaptureData[] wypts)
        {
            if ((wypts.Length > 0) && !wypts[0].IsTarget)
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    PageHelper.ApplyCapture(EditNavpt, wypts[0]);
                });
            }
        }

        // ---- steerpoint management ---------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void StptBtnAddPoI_Click(object sender, RoutedEventArgs args)
        {
            if (!string.IsNullOrEmpty(EditNavpt.Name) && EditNavpt.IsValid &&
                double.TryParse(EditNavpt.Lat, out double lat) && double.TryParse(EditNavpt.Lon, out double lon))
            {
                string theater = PointOfInterestDbase.TheaterForCoords(lat, lon);
                PointOfInterestDbQuery query = new(PointOfInterestTypeMask.USER, theater, EditNavpt.Name);
                List<PointOfInterest> pois = PointOfInterestDbase.Instance.Find(query);
                if (pois.Count > 0)
                {
                    ContentDialogResult result = await Utilities.Message2BDialog(
                        Content.XamlRoot,
                        "Point of Interest Already Defined",
                        $"The database already contains a point of interest for “{EditNavpt.Name}”. Would you like to replace it?",
                        "Replace");
                    if (result == ContentDialogResult.Primary)
                    {
                        pois[0].Name = EditNavpt.Name;
                        pois[0].Latitude = EditNavpt.Lat;
                        pois[0].Longitude = EditNavpt.Lon;
                        pois[0].Elevation = EditNavpt.Alt;
                        RebuildPointsOfInterest();
                        RebuildInterfaceState();
                    }
                }
                else
                {
                    PointOfInterest poi = new(PointOfInterestType.USER,
                                              theater, EditNavpt.Name, "", EditNavpt.Lat, EditNavpt.Lon, EditNavpt.Alt);
                    PointOfInterestDbase.Instance.Add(poi);
                    RebuildPointsOfInterest();
                    RebuildInterfaceState();
                }
            }
        }

        /// <summary>
        /// steerpoint previous click: save the current steerpoint and move to the previous steerpoint.
        /// </summary>
        private void NavptBtnPrev_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(EditNavptIndex, true);
            EditNavptIndex -= 1;
            CopyConfigToEdit(EditNavptIndex);
            SelectMatchingPoI();
            RebuildInterfaceState();
        }

        /// <summary>
        /// steerpoint previous click: save the current steerpoint and move to the next steerpoint.
        /// </summary>
        private void NavptBtnNext_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(EditNavptIndex, true);
            EditNavptIndex += 1;
            CopyConfigToEdit(EditNavptIndex);
            SelectMatchingPoI();
            RebuildInterfaceState();
        }

        /// <summary>
        /// steerpoint add click: save the current steerpoint and add a new steerpoint to the end of the list.
        /// </summary>
        private void NavptBtnAdd_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(EditNavptIndex, true);
            EditNavptIndex = PageHelper.AddNavpoint(Config);
            CopyConfigToEdit(EditNavptIndex);
            RebuildInterfaceState();
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void NavptTextBox_LostFocus(object sender, RoutedEventArgs args)
        {
            RebuildEnableState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
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

        /// <summary>
        /// on navigating to this page, set up and tear down our internal and ui state based on the configuration we
        /// are editing.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (EditNavptPageNavArgs)args.Parameter;

            PageHelper = (IEditNavpointPageHelper)Activator.CreateInstance(NavArgs.EditorHelperType);
            UpdateLatLonTextBoxFormat(uiNavptValueLat, PageHelper.LatExtProperties);
            UpdateLatLonTextBoxFormat(uiNavptValueLon, PageHelper.LonExtProperties);

            EditNavpt ??= PageHelper.CreateEditNavpt(EditField_PropertyChanged, EditNavpt_DataValidationError);

            Config = NavArgs.Config;

            EditNavptIndex = NavArgs.IndexNavpt;
            CopyConfigToEdit(EditNavptIndex);

            string theater = null;
            if ((PageHelper.NavpointCount(Config) > 0) && EditNavpt.IsValid)
            {
                theater = PointOfInterestDbase.TheaterForCoords(EditNavpt.Lat, EditNavpt.Lon);
            }
            theater = (string.IsNullOrEmpty(theater)) ? CurPoITheaters[0] : theater;
            uiPoIComboTheater.SelectedItem = theater;

            ValidateAllFields(_curNavptFieldValueMap, PageHelper.GetErrors(EditNavpt, null));
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }
    }
}
