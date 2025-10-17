// ********************************************************************************************************************
//
// EditNavpointListPage.cs : ui c# for general navigation point editor page
//
// Copyright(C) 2023-2025 ilominar/raven
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

using CommunityToolkit.WinUI;
using JAFDTC.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.UI.App;
using JAFDTC.UI.Controls.Map;
using JAFDTC.Utilities;
using JAFDTC.Utilities.Networking;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.System;

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

        public IMapControlVerbMirror VerbMirror { get; set; }   // map window (may be null)

        public IConfiguration Config { get; }                   // configuration

        public int IndexNavpt { get; }                          // index of navpoint being edited

        public bool IsUnlinked { get; }                         // true => navpoints not linked to other configuration

        public string NavptName { get; }                        // "name" of a navpoint ("Waypoint", "Steerpoint", etc.)

        public Type EditorHelperType { get; }                   // helper class for EditNavpointPage

        public EditNavptPageNavArgs(Page parent, IMapControlVerbMirror mirror, IConfiguration config, int index,
                                    bool isUnlinked, Type helper)
            => (ParentEditor, VerbMirror, Config,
                IndexNavpt, IsUnlinked, EditorHelperType) = (parent, mirror, config, index, isUnlinked, helper);
    }

    // ================================================================================================================

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

        private NavpointInfoBase ValidateNavpt { get; set; }

        private int EditNavptIndex { get; set; }

        private bool IsCancelInFlight { get; set; }

        private bool IsRebuildPending { get; set; }

        private PointOfInterest CurSelectedPoI { get; set; }

        private PoIFilterSpec FilterSpec { get; set; }

        // read-only properties

        private readonly Dictionary<string, TextBox> _curNavptFieldValueMap;
        private readonly Dictionary<string, TextBox> _curNavptTxtBoxExtMap;
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
            CurSelectedPoI = null;

            IsRebuildPending = false;

            _curNavptFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["Lat"] = uiNavptValueLat,
                ["Lon"] = uiNavptValueLon,
                ["Alt"] = uiNavptValueAlt
            };
            _curNavptTxtBoxExtMap = new Dictionary<string, TextBox>()
            {
                ["LatUI"] = uiNavptValueLat,
                ["LonUI"] = uiNavptValueLon
            };
            _defaultBorderBrush = uiNavptValueLat.BorderBrush;
            _defaultBkgndBrush = uiNavptValueLat.Background;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshall data between our local navpoint setting and the appropriate navpoint in the navigation system
        /// configuration using PageHelper.CopyConfigToEdit.
        /// </summary>
        private void CopyConfigToEdit(int index)
        {
            PageHelper.CopyConfigToEdit(index, Config, EditNavpt);
        }

        /// <summary>
        /// marshall data between our local navpoint setting and the appropriate navpoint in the navigation system
        /// configuration using PageHelper.CopyEditToConfig and save if persistance is required.
        /// </summary>
        private void CopyEditToConfig(int index, bool isPersist = false)
        {
            if (PageHelper.CopyEditToConfig(index, EditNavpt, Config) && isPersist)
                Config.Save(this, PageHelper.SystemTag);
        }

        /// <summary>
        /// change the editor to edit the navpoint at the given index.
        /// </summary>
        public void ChangeToEditNavpointAtIndex(int index)
        {
            if (!CurStateHasErrors())
            {
                CurSelectedPoI = null;
                uiPoINameFilterBox.Text = null;
                CopyEditToConfig(EditNavptIndex, true);
                EditNavptIndex = index;
                CopyConfigToEdit(EditNavptIndex);
                RebuildInterfaceState();
                uiNavptValueName.Focus(FocusState.Programmatic);

                MapMarkerInfo info = new(MapMarkerInfo.MarkerType.NAVPT, EditNavpointListPage.ROUTE_NAME,
                                         EditNavptIndex + 1);
                NavArgs.VerbMirror?.MirrorVerbMarkerSelected(NavArgs.ParentEditor as IMapControlVerbHandler, info);
            }
        }

        /// <summary>
        /// copy config to edit if editing the navpoint at a given index (as this navpoint is being deleted).
        /// </summary>
        public void CopyConfigToEditIfEditingNavpointAtIndex(int index)
        {
            if (index == EditNavptIndex)
            {
                CopyConfigToEdit(EditNavptIndex);
                EditNavpt.LatUI = EditNavpt.LatUI;              // clears transient false positive errors
                EditNavpt.LonUI = EditNavpt.LonUI;
            }
        }

        /// <summary>
        /// cancel the current editor if editing the navpoint at a given index (as this navpoint is being deleted).
        /// </summary>
        public void CancelIfEditingNavpointAtIndex(int index)
        {
            if (index == EditNavptIndex)
                Frame.GoBack();
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
            if (!IsCancelInFlight)
            {
                field.BorderBrush = (isValid) ? _defaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
                field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBackgroundBrush"];
            }
        }

        private void ValidateAllFields(Dictionary<string, TextBox> fields, IEnumerable errors)
        {
            Dictionary<string, bool> map = [ ];
            foreach (string error in errors)
                map[error] = true;
            foreach (KeyValuePair<string, TextBox> kvp in fields)
                SetFieldValidState(kvp.Value, !map.ContainsKey(kvp.Key));
        }

        /// <summary>
        /// If the PageHelper specifies a non-zero maximum name length,
        /// indicate when it is exceeded with the warning style.
        /// </summary>
        private void ValidateNavptNameLength()
        {
            if (PageHelper.MaxNameLength > 0 && uiNavptValueName.Text.Length > PageHelper.MaxNameLength)
                uiNavptValueName.Style = (Style)Application.Current.Resources["WarningTextBoxStyle"];
            else
                uiNavptValueName.Style = (Style)Application.Current.Resources["EditorParamEditTextBoxStyle"];
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void EditNavpt_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            if (args.PropertyName == null)
            {
                ValidateAllFields(_curNavptFieldValueMap, PageHelper.GetErrors(EditNavpt, null));
            }
            else
            {
                List<string> errors = PageHelper.GetErrors(EditNavpt, args.PropertyName);
                if (_curNavptFieldValueMap.TryGetValue(args.PropertyName, out TextBox value))
                    SetFieldValidState(value, (errors.Count == 0));
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// property changed: rebuild interface state to account for configuration changes.
        /// </summary>
        private void EditField_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// returns true if the current state has errors, false otherwise.
        /// </summary>
        private bool CurStateHasErrors()
        {
            foreach (KeyValuePair<string, TextBox> kvp in _curNavptTxtBoxExtMap)
                if (!TextBoxExtensions.GetIsValid(kvp.Value))
                    return true;
            return PageHelper.HasErrors(EditNavpt);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// rebuild the point of interest list in the filter box.
        /// </summary>
        private void RebuildPointsOfInterest()
        {
            uiPoINameFilterBox.ItemsSource = NavpointUIHelper.RebuildPointsOfInterest(FilterSpec, uiPoINameFilterBox.Text);
        }

        /// <summary>
        /// rebuild the enable state of the buttons in the ui based on current configuration setup.
        /// </summary>
        private void RebuildEnableState()
        {
            JAFDTC.App curApp = Application.Current as JAFDTC.App;

            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(PageHelper.SystemTag));
            bool isDCSListening = curApp.IsDCSAvailable && (curApp.DCSActiveAirframe == Config.Airframe);
            bool isErrorsInUI = CurStateHasErrors();

            Utilities.SetEnableState(uiPoINameFilterBox, isEditable);
            Utilities.SetEnableState(uiPoIBtnFilter, isEditable);
            Utilities.SetEnableState(uiPoIBtnApply, isEditable && (CurSelectedPoI != null));
            Utilities.SetEnableState(uiPoIBtnCapture, isEditable && isDCSListening);

            uiPoIBtnFilter.IsChecked = (FilterSpec.IsFiltered && isEditable);

            Utilities.SetEnableState(uiNavptValueName, isEditable);

            foreach (KeyValuePair<string, TextBox> kvp in _curNavptFieldValueMap)
                Utilities.SetEnableState(kvp.Value, isEditable);

            Utilities.SetEnableState(uiNavptBtnPrev, !isErrorsInUI && (EditNavptIndex > 0));
            Utilities.SetEnableState(uiNavptBtnAdd, isEditable && !isErrorsInUI);
            Utilities.SetEnableState(uiNavptBtnNext, !isErrorsInUI &&
                                                     (EditNavptIndex < (PageHelper.NavpointCount(Config) - 1)));

            uiAcceptBtnCancel.Visibility = (isEditable) ? Visibility.Visible : Visibility.Collapsed;
            Utilities.SetEnableState(uiAcceptBtnOK, !isEditable || !isErrorsInUI);
        }

        /// <summary>
        /// rebuild the state of controls on the page in response to a change in the configuration. the configuration
        /// is saved if requested.
        /// </summary>
        private void RebuildInterfaceState()
        {
            if (!IsRebuildPending)
            {
                IsRebuildPending = true;
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
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
        /// accept click: if page has errors and we aren't linked, update the configuration state before returning to
        /// the navpoint list.
        /// </summary>
        private void AcceptBtnOk_Click(object sender, RoutedEventArgs args)
        {
            if (!CurStateHasErrors() && string.IsNullOrEmpty(Config.SystemLinkedTo(PageHelper.SystemTag)))
                CopyEditToConfig(EditNavptIndex, true);
            Frame.GoBack();
        }

        /// <summary>
        /// accept cancel click: return to the navpoint list without making any changes to the navpoint.
        /// </summary>
        private void AcceptBtnCancel_Click(object sender, RoutedEventArgs args)
        {
            IsCancelInFlight = false;
            Frame.GoBack();
        }

        /// <summary>
        /// accept cancel gettting focus: cancel button is getting focus, note cancel is in flight so we can avoid
        /// some ui visual artifacts if we're cancelling with an error on the page.
        /// </summary>
        private void AcceptBtnCancel_GettingFocus(object sender, RoutedEventArgs args)
        {
            IsCancelInFlight = true;
        }

        // ---- poi management ----------------------------------------------------------------------------------------

        /// <summary>
        /// filter box focused: show the suggestion list when the control gains focus.
        /// </summary>
        private void PoINameFilterBox_GotFocus(object sender, RoutedEventArgs args)
        {
            AutoSuggestBox box = (AutoSuggestBox)sender;
            box.IsSuggestionListOpen = true;
        }

        /// <summary>
        /// filter box text changed: update the items in the search box based on the value in the field.
        /// </summary>
        private void PoINameFilterBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                CurSelectedPoI = null;
                RebuildPointsOfInterest();
                RebuildEnableState();
            }
        }

        /// <summary>
        /// filter box query submitted: apply the query text filter to the pois listed in the poi list.
        /// </summary>
        private void PoINameFilterBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                CurSelectedPoI = (args.ChosenSuggestion as PoIListItem).PoI;
            }
            else
            {
                CurSelectedPoI = null;
                foreach (PoIListItem poi in uiPoINameFilterBox.ItemsSource as IEnumerable<PoIListItem>)
                {
                    if (poi.Name == args.QueryText)
                    {
                        CurSelectedPoI = poi.PoI;
                        break;
                    }
                }
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// filter button click: setup the filter setup.
        /// </summary>
        private async void PoIBtnFilter_Click(object sender, RoutedEventArgs args)
        {
            ToggleButton button = (ToggleButton)sender;
            PoIFilterSpec spec = await NavpointUIHelper.FilterSpecDialog(Content.XamlRoot, FilterSpec, button);
            if (spec != null)
            {
                FilterSpec = spec;
                button.IsChecked = FilterSpec.IsFiltered;

                Settings.LastStptFilterTheater = FilterSpec.Theater;
                Settings.LastStptFilterTags = FilterSpec.Tags;
                Settings.LastStptFilterIncludeTypes = FilterSpec.IncludeTypes;

                RebuildPointsOfInterest();
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// apply poi click: copy poi information into current steerpoint and reset poi selection to "none".
        /// </summary>
        private void PoIBtnApply_Click(object sender, RoutedEventArgs args)
        {
            PageHelper.ApplyPoI(EditNavpt, CurSelectedPoI);
            CopyEditToConfig(EditNavptIndex, true);

            uiPoINameFilterBox.Text = null;
            CurSelectedPoI = null;
            RebuildPointsOfInterest();
            RebuildInterfaceState();
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
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    PageHelper.ApplyCapture(EditNavpt, wypts[0]);
                });
            }
        }

        // ---- steerpoint management ---------------------------------------------------------------------------------

        /// <summary>
        /// steerpoint previous click: save the current steerpoint and move to the previous steerpoint.
        /// </summary>
        private void NavptBtnPrev_Click(object sender, RoutedEventArgs args)
        {
            ChangeToEditNavpointAtIndex(EditNavptIndex - 1);
        }

        /// <summary>
        /// steerpoint previous click: save the current steerpoint and move to the next steerpoint.
        /// </summary>
        private void NavptBtnNext_Click(object sender, RoutedEventArgs args)
        {
            ChangeToEditNavpointAtIndex(EditNavptIndex + 1);
        }

        /// <summary>
        /// steerpoint add click: save the current steerpoint and add a new steerpoint to the end of the list.
        /// </summary>
        private async void NavptBtnAdd_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(EditNavptIndex, true);
            CopyConfigToEdit(PageHelper.NavpointCount(Config) - 1);

            Tuple<string, string> ll = await NavpointUIHelper.ProposeNewNavptLatLon(Content.XamlRoot, [ EditNavpt ]);
            if (ll != null)
            {
                EditNavptIndex = PageHelper.AddNavpoint(Config);
                EditNavpt.Reset();
                CopyConfigToEdit(EditNavptIndex);
                EditNavpt.Lat = ll.Item1;
                EditNavpt.Lon = ll.Item2;
                EditNavpt.LatUI = EditNavpt.LatUI;              // regenerate ui flavors to clear errors...
                EditNavpt.LonUI = EditNavpt.LonUI;
                CopyEditToConfig(EditNavptIndex, true);
                RebuildInterfaceState();
                uiNavptValueName.Focus(FocusState.Programmatic);

                MapMarkerInfo info = new(MapMarkerInfo.MarkerType.NAVPT, EditNavpointListPage.ROUTE_NAME,
                                         EditNavptIndex + 1, ll.Item1, ll.Item2);
                NavArgs.VerbMirror?.MirrorVerbMarkerAdded(NavArgs.ParentEditor as IMapControlVerbHandler, info);
                NavArgs.VerbMirror?.MirrorVerbMarkerSelected(NavArgs.ParentEditor as IMapControlVerbHandler, info);
            }
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        /// <summary>
        /// navpoint text box text changed: rebuild the interface state to update based on current valid/invalid state.
        /// </summary>
        private void NavptTextBoxExt_TextChanged(object sender, TextChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// navpoint text box text lost focus: pass along lat/lon updates after there's been a jiffy to let the
        /// changes propagate to the edit state.
        /// </summary>
        private void NavptTextBoxExt_LostFocus(object sender, RoutedEventArgs args)
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                MapMarkerInfo info = new(MapMarkerInfo.MarkerType.NAVPT, EditNavpointListPage.ROUTE_NAME,
                                         EditNavptIndex + 1, EditNavpt.Lat, EditNavpt.Lon);
                NavArgs.VerbMirror?.MirrorVerbMarkerMoved(NavArgs.ParentEditor as IMapControlVerbHandler, info);
            });
        }

        private void NavptValueName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateNavptNameLength();
        }

        private void NavptValueName_GotFocus(object sender, RoutedEventArgs e)
        {
            uiNavptValueName.SelectAll();
        }

        /// <summary>
        /// Handle keyboard shortcuts for navigating to next/prev navpoint when a textbox is focused.
        /// </summary>
        private void TextBox_PreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                Utilities.GetModifierKeyStates(out bool isShiftDown, out bool isCtrlDown);
                if (isCtrlDown)
                {
                    // Explicit property set is necessary because the binding update
                    // is on lost focus, which doesn't occur here.

                    // TODO: this is broken on TextBoxExtensions text boxes (like the lat/lon fields). for now,
                    // TODO: only do support the hotkeys on the name field.

                    // TODO: this code likely needs to use reflection once more than name is supported (see the
                    // TODO: viper code)...

                    EditNavpt.Name = uiNavptValueName.Text;

                    if (!isShiftDown && uiNavptBtnNext.IsEnabled)
                    {
                        NavptBtnNext_Click(sender, e);
                        uiNavptValueName.SelectAll();
                        e.Handled = true;
                    }
                    if (isShiftDown && uiNavptBtnPrev.IsEnabled)
                    {
                        NavptBtnPrev_Click(sender, e);
                        uiNavptValueName.SelectAll();
                        e.Handled = true;
                    }
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// update the text box format fields (mask placeholder, regex, custom mask, and mask) for lat/lon based on
        /// the specific airframe format the page helper provides.
        /// </summary>
        private static void UpdateLatLonTextBoxFormat(TextBox textBox, Dictionary<string, string> format)
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
            ValidateNavpt ??= PageHelper.CreateEditNavpt(null, null);

            Config = NavArgs.Config;

            IsCancelInFlight = false;

            EditNavptIndex = NavArgs.IndexNavpt;
            CopyConfigToEdit(EditNavptIndex);

            FilterSpec = new(Settings.LastStptFilterTheater, Settings.LastStptFilterCampaign,
                             Settings.LastStptFilterTags, Settings.LastStptFilterIncludeTypes);

            ValidateAllFields(_curNavptFieldValueMap, PageHelper.GetErrors(EditNavpt, null));
            RebuildPointsOfInterest();
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // We do this here (and not in OnNavigatedTo) for two reasons:
            // 1. The visual tree is done loading here.
            // 2. We want this to happen every time you click a WP from the list.
            uiNavptValueName.Focus(FocusState.Programmatic);
        }
    }
}
