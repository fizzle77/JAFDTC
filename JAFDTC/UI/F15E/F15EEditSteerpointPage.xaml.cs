// ********************************************************************************************************************
//
// F15EEditSteerpointPage.xaml.cs : ui c# for mudhen steerpoint editor page
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

using JAFDTC.Models.DCS;
using JAFDTC.Models.F15E;
using JAFDTC.Models.F15E.STPT;
using JAFDTC.Utilities.Networking;
using JAFDTC.Utilities;
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
using JAFDTC.UI.Base;
using Microsoft.UI.Xaml.Controls.Primitives;
using CommunityToolkit.WinUI.UI;

namespace JAFDTC.UI.F15E
{
    /// <summary>
    /// navigation argument to pass from pages that navigate to the steerpoint editor (F15EEditSteerpointPage). this
    /// provides the configuration being edited along with the specific steerpoint within the configuration that
    /// should be edited.
    /// </summary>
    public sealed class F15EEditStptPageNavArgs
    {
        public F15EConfiguration Config { get; set; }

        public F15EEditSteerpointListPage ParentEditor { get; set; }

        public int IndexStpt { get; set; }

        public bool IsUnlinked { get; set; }

        public F15EEditStptPageNavArgs(F15EEditSteerpointListPage parentEditor, F15EConfiguration config, int indexStpt,
                                       bool isUnlinked)
            => (ParentEditor, Config, IndexStpt, IsUnlinked) = (parentEditor, config, indexStpt, isUnlinked);
    }

    // ================================================================================================================

    /// <summary>
    /// user interface for the page that allows you to edit a steerpoint from the mudhen steerpoint system 
    /// configuration.
    /// </summary>
    public sealed partial class F15EEditSteerpointPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public F15EEditStptPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. edits by the ui are usually
        // NOTE: directed at the EditStpt/EditStptIndex and EditRfpt/EditRfptNum properties (exceptions occur when the
        // NOTE: edit requires changes to other steerpoints).
        //
        private F15EConfiguration Config { get; set; }

        private SteerpointInfo EditStpt { get; set; }

        private RefPointInfo EditRfpt { get; set; }

        private int EditStptIndex { get; set; }

        private int EditRfptNum { get; set; }

        private bool IsCancelInFlight { get; set; }

        private bool IsRebuildPending { get; set; }

        private PointOfInterest CurSelectedPoI { get; set; }

        private PoIFilterSpec FilterSpec { get; set; }


        // ---- read-only properties

        private readonly Dictionary<string, TextBox> _curStptFieldValueMap;
        private readonly Dictionary<string, TextBox> _curRfptFieldValueMap;
        private readonly Dictionary<string, TextBox> _curStptTxtBoxExtMap;
        private readonly Dictionary<string, TextBox> _curRfptTxtBoxExtMap;
        private readonly List<FontIcon> _refptSelMenuIcon;
        private readonly List<TextBlock> _refptSelMenuText;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F15EEditSteerpointPage()
        {
            InitializeComponent();

            EditStpt = new()
            {
                RefPoints = new()
            };
            EditStpt.ErrorsChanged += EditStpt_DataValidationError;
            EditStpt.PropertyChanged += EditField_PropertyChanged;

            EditRfpt = new();
            EditRfpt.PropertyChanged += EditField_PropertyChanged;

            IsRebuildPending = false;

            CurSelectedPoI = null;

            _curStptFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["Lat"] = uiStptValueLat,
                ["Lon"] = uiStptValueLon,
                ["Alt"] = uiStptValueAlt,
                ["TOT"] = uiStptValueTOT
            };
            _curRfptFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["Lat"] = uiRfptValueLat,
                ["Lon"] = uiRfptValueLon,
                ["Alt"] = uiRfptValueAlt
            };
            _curStptTxtBoxExtMap = new Dictionary<string, TextBox>()
            {
                ["LatUI"] = uiStptValueLat,
                ["LonUI"] = uiStptValueLon,
                ["TOT"] = uiStptValueTOT
            };
            _curRfptTxtBoxExtMap = new Dictionary<string, TextBox>()
            {
                ["LatUI"] = uiRfptValueLat,
                ["LonUI"] = uiRfptValueLon
            };
            _refptSelMenuIcon = new ()
            {
                uiRfptSelectItem1Icon, uiRfptSelectItem2Icon, uiRfptSelectItem3Icon, uiRfptSelectItem4Icon,
                uiRfptSelectItem5Icon, uiRfptSelectItem6Icon, uiRfptSelectItem7Icon
            };
            _refptSelMenuText = new()
            {
                uiRfptSelectItem1Text, uiRfptSelectItem2Text, uiRfptSelectItem3Text, uiRfptSelectItem4Text,
                uiRfptSelectItem5Text, uiRfptSelectItem6Text, uiRfptSelectItem7Text
            };
            _defaultBorderBrush = uiStptValueLat.BorderBrush;
            _defaultBkgndBrush = uiStptValueLat.Background;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshall data from the appropriate steerpoint in the stpt configuration to our local editable copy. as we
        /// edit outside the config, we will make a deep copy. we cannot Clone() here as the UI is tied to the specific
        /// EditStpt instance we set up at load.
        /// </summary>
        private void CopyConfigToEdit(int index)
        {
            SteerpointInfo stptSrc = Config.STPT.Points[index];
            EditStpt.Route = stptSrc.Route;
            EditStpt.Number = stptSrc.Number;
            EditStpt.Name = new(stptSrc.Name);
            EditStpt.LatUI = Coord.ConvertFromLatDD(stptSrc.Lat, LLFormat.DDM_P3ZF);
            EditStpt.LonUI = Coord.ConvertFromLonDD(stptSrc.Lon, LLFormat.DDM_P3ZF);
            EditStpt.Alt = new(stptSrc.Alt);
            EditStpt.TOT = new(stptSrc.TOT);
            EditStpt.IsTarget = stptSrc.IsTarget;

            EditStpt.RefPoints.Clear();
            foreach (RefPointInfo info in stptSrc.RefPoints)
            {
                EditStpt.RefPoints.Add((RefPointInfo)info.Clone());
                if (info.Number == EditRfptNum)
                {
                    EditRfpt.Number = info.Number;
                    EditRfpt.Name = new(info.Name);
                    EditRfpt.LatUI = new(info.LatUI);
                    EditRfpt.LonUI = new(info.LonUI);
                    EditRfpt.Alt = new(info.Alt);
                }
            }
        }

        /// <summary>
        /// marshall data from our local editable steerpoint copy to the appropriate steerpoint in the configuration.
        /// optionally persisting the configuration.
        /// </summary>
        private void CopyEditToConfig(int index, bool isPersist = false)
        {
            if (!CurStateHasErrors())
            {
                SteerpointInfo stptDst = Config.STPT.Points[index];
                stptDst.Route = EditStpt.Route;
                stptDst.Number = EditStpt.Number;
                stptDst.Name = EditStpt.Name;
                stptDst.Lat = EditStpt.Lat;
                stptDst.Lon = EditStpt.Lon;
                stptDst.Alt = EditStpt.Alt;
                stptDst.IsTarget = EditStpt.IsTarget;
                //
                // TOT field uses text mask and can come back as "--:--:--" when empty. this is really "" and, since
                // that value is OK, remove the error.
                //
                // TODO: check TOT format in mudhen
                //
                stptDst.TOT = (EditStpt.TOT == "––:––:––") ? "" : EditStpt.TOT;

                List<RefPointInfo> emptyRfpts = new();
                bool isNew = true;
                foreach (RefPointInfo info in EditStpt.RefPoints)
                {
                    if (info.Number == EditRfptNum)
                    {
                        info.Name = EditRfpt.Name;
                        info.LatUI = EditRfpt.LatUI;
                        info.LonUI = EditRfpt.LonUI;
                        info.Alt = EditRfpt.Alt;
                        isNew = false;
                    }
                    if (info.IsEmpty)
                    {
                        emptyRfpts.Add(info);
                    }
                }
                if (isNew)
                {
                    EditStpt.RefPoints.Add((RefPointInfo)EditRfpt.Clone());
                }
                foreach (RefPointInfo info in emptyRfpts)
                {
                    EditStpt.RefPoints.Remove(info);
                }
                stptDst.RefPoints = new(EditStpt.RefPoints);

                if (isPersist)
                {
                    Config.Save(NavArgs.ParentEditor, STPTSystem.SystemTag);
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
            if (!IsCancelInFlight)
            {
                field.BorderBrush = (isValid) ? _defaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
                field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBackgroundBrush"];
            }
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

        private void CoreDataValidationError(INotifyDataErrorInfo obj, string propertyName, Dictionary<string, TextBox> fields)
        {
            if (propertyName == null)
            {
                ValidateAllFields(fields, obj.GetErrors(null));
            }
            else
            {
                List<string> errors = (List<string>)obj.GetErrors(propertyName);
                if (fields.ContainsKey(propertyName))
                {
                    SetFieldValidState(fields[propertyName], (errors.Count == 0));
                }
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void EditStpt_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            // TODO: check TOT format in mudhen
            if ((args.PropertyName == "TOT") && (EditStpt.TOT == "––:––:––"))
            {
                // TOT field uses text mask and can come back as "--:--:--" when empty. this is really "" and, since
                // that value is OK, remove the error.
                //
                EditStpt.ClearErrors("TOT");
                TextBox field = (TextBox)_curStptFieldValueMap[args.PropertyName];
                SetFieldValidState(field, true);
            }
            else
            {
                CoreDataValidationError(EditStpt, args.PropertyName, _curStptFieldValueMap);
            }
        }

        /// <summary>
        /// steerpoint or reference point property changed: rebuild interface state to account for property changes.
        /// </summary>
        private void EditField_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// returns true if the current steerpoint state has errors, false otherwise.
        /// </summary>
        private bool CurStptStateHasErrors()
        {
            foreach (KeyValuePair<string, TextBox> kvp in _curStptTxtBoxExtMap)
            {
                TextBox tbox = kvp.Value;
                if (!TextBoxExtensions.GetIsValid(tbox) && (((string)tbox.Tag != "TOT") || (tbox.Text != "––:––:––")))
                {
                    return true;
                }
            }
            return EditStpt.HasErrors;
        }

        /// <summary>
        /// returns true if the current reference point state has errors, false otherwise.
        /// </summary>
        private bool CurRefptStateHasErrors()
        {
            foreach (KeyValuePair<string, TextBox> kvp in _curRfptTxtBoxExtMap)
            {
                TextBox tbox = kvp.Value;
                if (!TextBoxExtensions.GetIsValid(tbox) &&
                    ((((string)tbox.Tag == "LatUI") && (tbox.Text != "– ––° ––.–––’")) ||
                     (((string)tbox.Tag == "LonUI") && (tbox.Text != "– –––° ––.–––’"))))
                {
                    return true;
                }
            }
            return !EditRfpt.IsEmpty && (string.IsNullOrEmpty(EditRfpt.Lat) ||
                                         string.IsNullOrEmpty(EditRfpt.Lon) ||
                                         string.IsNullOrEmpty(EditRfpt.Alt));
        }

        /// <summary>
        /// returns true if the current state has errors, false otherwise.
        /// </summary>
        private bool CurStateHasErrors()
        {
            return CurStptStateHasErrors() || CurRefptStateHasErrors();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return true if the current edit steerpoint is a valid poi, false otherwise. a valid poi has a name,
        /// latitude, and longitude. the name should be unique within the poi database.
        /// </summary>
        private bool IsEditCoordValidPoI()
        {
            if (string.IsNullOrEmpty(EditStpt.Name) || string.IsNullOrEmpty(EditStpt.Alt) ||
                !double.TryParse(EditStpt.Lat, out double lat) || !double.TryParse(EditStpt.Lon, out double lon))
            {
                return false;
            }
            string theater = PointOfInterest.TheaterForCoords(lat, lon);
            PointOfInterestDbQuery query = new(PointOfInterestTypeMask.ANY, theater, EditStpt.Name);
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
        /// load the internal edit copy of the currently selected reference point from the edit copy of the current
        /// steerpoint, creating a new empty reference point if it does not yet exist.
        /// </summary>
        private void LoadEditRfptFromPointNumber(int number)
        {
            foreach (RefPointInfo info in EditStpt.RefPoints)
            {
                if (info.Number == number)
                {
                    EditRfpt.Number = info.Number;
                    EditRfpt.Name = info.Name;
                    EditRfpt.LatUI = info.LatUI;
                    EditRfpt.LonUI = info.LonUI;
                    EditRfpt.Alt = info.Alt;
                    return;
                }
            }

            EditStpt.RefPoints.Add(new RefPointInfo(number));
            EditRfpt.Number = number;
            EditRfpt.Name = "";
            EditRfpt.LatUI = "";
            EditRfpt.LonUI = "";
            EditRfpt.Alt = "";
        }

        /// <summary>
        /// reset the edit copy of the current reference point along with the selected reference point in response to
        /// a change to the steerpoint.
        /// </summary>
        private void ResetRefPointForSteerpointChange()
        {
            EditRfpt.Number = 1;
            EditRfpt.Name = "";
            EditRfpt.LatUI = "";
            EditRfpt.LonUI = "";
            EditRfpt.Alt = "";
            uiRfptComboSelect.SelectedIndex = 0;
        }

        /// <summary>
        /// rebuild the point of interest select combo box. this only needs to be called when the theater changes or
        /// when a poi is added to the current theater.
        /// </summary>
        private void RebuildPointsOfInterest()
        {
            uiPoINameFilterBox.ItemsSource = NavpointUIHelper.RebuildPointsOfInterest(FilterSpec, uiPoINameFilterBox.Text);
        }

        /// <summary>
        /// rebuild the state of the reference point editor in response to a change in the configuration. even though
        /// an empty refpoint is flagged as invalid, we treat it as valid here an indicating the refpoint is not in use.
        /// </summary>
        private void RebuildRefptErrorIndicators()
        {
            SetFieldValidState(uiRfptValueLat, EditRfpt.IsEmpty || !string.IsNullOrEmpty(EditRfpt.LatUI));
            SetFieldValidState(uiRfptValueLon, EditRfpt.IsEmpty || !string.IsNullOrEmpty(EditRfpt.LonUI));
            SetFieldValidState(uiRfptValueAlt, EditRfpt.IsEmpty || !string.IsNullOrEmpty(EditRfpt.Alt));
        }

        /// <summary>
        /// rebuild the title and "edited" badge visibility for the menu in the reference point selection combo box.
        /// </summary>
        private void RebuildRefPointSelectMenu()
        {
            string route = EditStpt.Route;
            string zero = (EditStpt.IsTarget) ? "0" : "";
            for (int i = 0; i < _refptSelMenuText.Count; i++)
            {
                _refptSelMenuText[i].Text = $"{EditStpt.Number}.{zero}{i + 1}{route}";
                _refptSelMenuIcon[i].Visibility = Visibility.Collapsed;
            }
            foreach (RefPointInfo info in EditStpt.RefPoints)
            {
                if (((info.Number == EditRfpt.Number) && EditRfpt.IsValid && !CurRefptStateHasErrors()) ||
                    ((info.Number != EditRfpt.Number) && info.IsValid))
                {
                    _refptSelMenuIcon[info.Number - 1].Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// rebuild the enable state of the buttons in the ui based on current configuration setup.
        /// </summary>
        private void RebuildEnableState()
        {
            JAFDTC.App curApp = Application.Current as JAFDTC.App;

            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(STPTSystem.SystemTag));
            bool isDCSListening = curApp.IsDCSAvailable && (curApp.DCSActiveAirframe == Config.Airframe);
            bool isErrorsInUI = CurStateHasErrors();

            Utilities.SetEnableState(uiPoINameFilterBox, isEditable);
            Utilities.SetEnableState(uiPoIBtnFilter, isEditable);
            Utilities.SetEnableState(uiPoIBtnApply, isEditable && (CurSelectedPoI != null));
            Utilities.SetEnableState(uiPoIBtnCapture, isEditable && isDCSListening);

            uiPoIBtnFilter.IsChecked = (FilterSpec.IsFiltered && isEditable);

            Utilities.SetEnableState(uiStptValueName, isEditable);
            foreach (KeyValuePair<string, TextBox> kvp in _curStptFieldValueMap)
            {
                Utilities.SetEnableState(kvp.Value, isEditable);
            }
            foreach (KeyValuePair<string, TextBox> kvp in _curRfptFieldValueMap)
            {
                Utilities.SetEnableState(kvp.Value, isEditable);
            }

            Utilities.SetEnableState(uiStptBtnAddPoI, isEditable && IsEditCoordValidPoI());
            Utilities.SetEnableState(uiStptBtnPrev, !isErrorsInUI && (EditStptIndex > 0));
            Utilities.SetEnableState(uiStptBtnAdd, isEditable && !isErrorsInUI);
            Utilities.SetEnableState(uiStptBtnNext, !isErrorsInUI && (EditStptIndex < (Config.STPT.Points.Count - 1)));

            Utilities.SetEnableState(uiRfptComboSelect, !isEditable || !CurRefptStateHasErrors());
            Utilities.SetEnableState(uiRfptBtnApply, isEditable && (CurSelectedPoI != null));
            // TODO: allow enable when refpt capture is implemented
            Utilities.SetEnableState(uiRfptBtnCapture, isEditable && isDCSListening && false);
            Utilities.SetEnableState(uiRfptBtnClear, isEditable && !EditRfpt.IsEmpty);

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
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    uiStptTextNum.Text = (EditStpt.IsTarget) ? $"Target Point {EditStpt.Number}{EditStpt.Route} Information"
                                                             : $"Steerpoint {EditStpt.Number}{EditStpt.Route} Information";
                    uiRfptTextTitle.Text = (EditStpt.IsTarget) ? $"Aim Points for {EditStpt.Number}{EditStpt.Route}"
                                                               : $"Offset Points for {EditStpt.Number}{EditStpt.Route}";
                    RebuildRefptErrorIndicators();
                    RebuildRefPointSelectMenu();
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

        // ---- page buttons ------------------------------------------------------------------------------------------

        /// <summary>
        /// accept click: save configuration and navigate back to previous page in nav stack.
        /// </summary>
        private void AcceptBtnOk_Click(object sender, RoutedEventArgs args)
        {
            if (!CurStateHasErrors() && string.IsNullOrEmpty(Config.SystemLinkedTo(STPTSystem.SystemTag)))
            {
                CopyEditToConfig(EditStptIndex, true);
            }
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
                CurSelectedPoI = (args.ChosenSuggestion as Base.PoIListItem).PoI;
            }
            else
            {
                CurSelectedPoI = null;
                foreach (Base.PoIListItem poi in uiPoINameFilterBox.ItemsSource as IEnumerable<Base.PoIListItem>)
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
        /// apply poi click: copy poi information into current steerpoint.
        /// </summary>
        private void PoIBtnApply_Click(object sender, RoutedEventArgs args)
        {
            EditStpt.Name = CurSelectedPoI.Name;
            EditStpt.LatUI = Coord.ConvertFromLatDD(CurSelectedPoI.Latitude, LLFormat.DDM_P3ZF);
            EditStpt.LonUI = Coord.ConvertFromLonDD(CurSelectedPoI.Longitude, LLFormat.DDM_P3ZF);
            EditStpt.Alt = CurSelectedPoI.Elevation;
            EditStpt.TOT = "";
            EditStpt.ClearErrors();

            RebuildInterfaceState();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void PoIBtnCapture_Click(object sender, RoutedEventArgs args)
        {
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived += PoIBtnCapture_WyptCaptureDataReceived;
            await Utilities.CaptureSingleDialog(Content.XamlRoot, "Steerpoint");
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived -= PoIBtnCapture_WyptCaptureDataReceived;

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
                    EditStpt.Name = "DCS Capture";
                    EditStpt.LatUI = Coord.ConvertFromLatDD(wypts[0].Latitude, LLFormat.DDM_P3ZF);
                    EditStpt.LonUI = Coord.ConvertFromLonDD(wypts[0].Longitude, LLFormat.DDM_P3ZF);
                    EditStpt.Alt = wypts[0].Elevation.ToString();
                    EditStpt.TOT = "";
                    EditStpt.ClearErrors();
                });
            }
        }

        // ---- steerpoint management ---------------------------------------------------------------------------------

        /// <summary>
        /// steerpoint add poi click: add current steerpoint to the poi database or update a matching editable poi.
        /// </summary>
        private async void StptBtnAddPoI_Click(object sender, RoutedEventArgs args)
        {
            if (EditStpt.IsValid && await NavpointUIHelper.CreatePoIAt(Content.XamlRoot, EditStpt.Name, EditStpt.Lat,
                                                                       EditStpt.Lon, EditStpt.Alt))
            {
                RebuildPointsOfInterest();
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// steerpoint previous click: save the current steerpoint and move to the previous steerpoint.
        /// </summary>
        private void StptBtnPrev_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(EditStptIndex, true);
            ResetRefPointForSteerpointChange();
            EditStptIndex -= 1;
            CopyConfigToEdit(EditStptIndex);
            RebuildInterfaceState();
        }

        /// <summary>
        /// steerpoint previous click: save the current steerpoint and move to the next steerpoint.
        /// </summary>
        private void StptBtnNext_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(EditStptIndex, true);
            ResetRefPointForSteerpointChange();
            EditStptIndex += 1;
            CopyConfigToEdit(EditStptIndex);
            RebuildInterfaceState();
        }

        /// <summary>
        /// steerpoint add click: save the current steerpoint and add a new steerpoint to the end of the list.
        /// </summary>
        private void StptBtnAdd_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(EditStptIndex, true);
            ResetRefPointForSteerpointChange();
            SteerpointInfo stpt = Config.STPT.Add();
            EditStptIndex = Config.STPT.Points.IndexOf(stpt);
            CopyConfigToEdit(EditStptIndex);
            RebuildInterfaceState();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void StptCkbxTarget_Click(object sender, RoutedEventArgs args)
        {
            // HACK: x:Bind doesn't work with bools? seems that way? this is a hack.
            //
            CheckBox cbox = (CheckBox)sender;
            EditStpt.IsTarget = (bool)cbox.IsChecked;
            CopyEditToConfig(EditStptIndex, true);
            RebuildInterfaceState();
        }

        // ---- reference point management ----------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RfptComboSelect_SelectionChanged(object sender, RoutedEventArgs args)
        {
            int rfptNum = uiRfptComboSelect.SelectedIndex + 1;
            if ((EditRfpt.Number != rfptNum) && !EditRfpt.IsEmpty)
            {
                CopyEditToConfig(EditStptIndex, true);
            }
            if (EditRfpt.Number != rfptNum)
            {
                LoadEditRfptFromPointNumber(rfptNum);
                EditRfptNum = rfptNum;
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// apply refpt click: copy poi information into current reference point.
        /// </summary>
        private void RfptBtnApply_Click(object sender, RoutedEventArgs args)
        {
            EditRfpt.Name = CurSelectedPoI.Name;
            EditRfpt.LatUI = Coord.ConvertFromLatDD(CurSelectedPoI.Latitude, LLFormat.DDM_P3ZF);
            EditRfpt.LonUI = Coord.ConvertFromLonDD(CurSelectedPoI.Longitude, LLFormat.DDM_P3ZF);
            EditRfpt.Alt = CurSelectedPoI.Elevation;
            EditRfpt.ClearErrors();

            RebuildInterfaceState();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RfptBtnClear_Click(object obj, RoutedEventArgs args)
        {
            EditRfpt.Name = "";
            EditRfpt.LatUI = "";
            EditRfpt.LonUI = "";
            EditRfpt.Alt = "";
            CopyEditToConfig(EditStptIndex, true);
            RebuildInterfaceState();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RfptBtnCapture_Click(object obj, RoutedEventArgs args)
        {
            // TODO: implement
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        /// <summary>
        /// steerpoint text box text changed: rebuild interface state to align with text field value.
        /// </summary>
        private void StptTextBoxExt_TextChanged(object sender, TextChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// reference point text box text changed: rebuild interface state to align with text field value.
        /// </summary>
        private void RfptTextBox_TextChanged(object sender, TextChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on navigating to this page, set up our internal and ui state based on the configuration we are editing.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (F15EEditStptPageNavArgs)args.Parameter;

            Config = NavArgs.Config;

            EditStptIndex = NavArgs.IndexStpt;
            EditRfptNum = 1;

            IsCancelInFlight = false;

            CopyConfigToEdit(EditStptIndex);

            uiRfptComboSelect.SelectedIndex = EditRfptNum - 1;
            LoadEditRfptFromPointNumber(EditRfptNum);

            FilterSpec = new(Settings.LastStptFilterTheater, Settings.LastStptFilterCampaign,
                             Settings.LastStptFilterTags, Settings.LastStptFilterIncludeTypes);

            ValidateAllFields(_curStptFieldValueMap, EditStpt.GetErrors(null));
            RebuildPointsOfInterest();
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }
    }
}
