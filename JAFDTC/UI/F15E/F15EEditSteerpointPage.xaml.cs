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

        private bool IsRebuildPending { get; set; }

        private List<string> CurPoITheaters { get; set; }

        // ---- read-only properties

        private readonly Dictionary<string, TextBox> _curStptFieldValueMap;
        private readonly Dictionary<string, TextBox> _curRfptFieldValueMap;
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
            EditRfpt.ErrorsChanged += EditRfpt_DataValidationError;
            EditRfpt.PropertyChanged += EditField_PropertyChanged;

            CurPoITheaters = PointOfInterestDbase.KnownTheaters();

            IsRebuildPending = false;

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
            _refptSelMenuIcon = new()
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
                    EditRfpt.LatUI = new(info.LatUI);
                    EditRfpt.LonUI = new(info.LonUI);
                    break;
                }
            }
        }

        /// <summary>
        /// marshall data from our local editable steerpoint copy to the appropriate steerpoint in the configuration.
        /// optionally persisting the configuration.
        /// </summary>
        private void CopyEditToConfig(int index, bool isPersist = false)
        {
            if (!EditStpt.HasErrors)
            {
                SteerpointInfo stptDst = Config.STPT.Points[index];
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
                        info.LatUI = EditRfpt.LatUI;
                        info.LonUI = EditRfpt.LonUI;
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
        /// TODO: document
        /// </summary>
        private void EditRfpt_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            CoreDataValidationError(EditRfpt, args.PropertyName, _curRfptFieldValueMap);
        }

        /// <summary>
        /// property changed: rebuild interface state to account for property changes.
        /// </summary>
        private void EditField_PropertyChanged(object sender, EventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// returns true if the current state has errors, false otherwise.
        /// </summary>
        private bool CurStateHasErrors()
        {
            return EditStpt.HasErrors | EditRfpt.HasErrors;
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
            string theater = PointOfInterestDbase.TheaterForCoords(lat, lon);
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
        /// TODO: document
        /// </summary>
        private void SelectMatchingPoI()
        {
            uiPoIComboSelect.SelectedItem = NavpointUIHelper.FindMatchingPoI((string)uiPoIComboTheater.SelectedItem,
                                                                             EditStpt, LLFormat.DDM_P3ZF);
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
                    EditRfpt.LatUI = info.LatUI;
                    EditRfpt.LonUI = info.LonUI;
                    return;
                }
            }

            EditStpt.RefPoints.Add(new RefPointInfo(number));
            EditRfpt.Number = number;
            EditRfpt.LatUI = "";
            EditRfpt.LonUI = "";
        }

        /// <summary>
        /// reset the edit copy of the current reference point along with the selected reference point in response to
        /// a change to the steerpoint.
        /// </summary>
        private void ResetRefPointForSteerpointChange()
        {
            EditRfpt.Number = 1;
            EditRfpt.LatUI = "";
            EditRfpt.LonUI = "";
            uiRfptComboSelect.SelectedIndex = 0;
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

        /// <summary>
        /// rebuild the title and "edited" badge visibility for the menu in the reference point selection combo box.
        /// </summary>
        private void RebuildRefPointSelectMenu()
        {
            string route = "A";
            string zero = (EditStpt.IsTarget) ? "0" : "";
            for (int i = 0; i < _refptSelMenuText.Count; i++)
            {
                _refptSelMenuText[i].Text = $"{EditStpt.Number}.{zero}{i + 1}{route}";
                _refptSelMenuIcon[i].Visibility = Visibility.Collapsed;
            }
            foreach (RefPointInfo info in EditStpt.RefPoints)
            {
                if (info.IsValid)
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

            Utilities.SetEnableState(uiPoIComboTheater, isEditable);
            Utilities.SetEnableState(uiPoIComboSelect, isEditable && (uiPoIComboSelect.Items.Count > 0));
            Utilities.SetEnableState(uiPoIBtnApply, isEditable && (uiPoIComboSelect.SelectedIndex > 0));
            Utilities.SetEnableState(uiPoIBtnCapture, isEditable && isDCSListening);

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
            Utilities.SetEnableState(uiStptBtnPrev, !CurStateHasErrors() && (EditStptIndex > 0));
            Utilities.SetEnableState(uiStptBtnAdd, isEditable && !CurStateHasErrors());
            Utilities.SetEnableState(uiStptBtnNext, !CurStateHasErrors() && (EditStptIndex < (Config.STPT.Points.Count - 1)));

            Utilities.SetEnableState(uiRfptBtnClear, isEditable && !EditRfpt.IsEmpty);
            // TODO: implement capture for refpt
            Utilities.SetEnableState(uiRfptBtnCapture, isEditable && isDCSListening && false);

            // TODO: ok button should also enable if you have lat/lon/alt specified, if vrp/vip both points needed
            Utilities.SetEnableState(uiAcceptBtnOK, isEditable && !CurStateHasErrors());
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
            if (CurStateHasErrors())
            {
                RebuildEnableState();
            }
            else
            {
                CopyEditToConfig(EditStptIndex, true);
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
            PointOfInterest poi = (PointOfInterest)uiPoIComboSelect.SelectedItem;
            EditStpt.Name = poi.Name;
            EditStpt.LatUI = Coord.ConvertFromLatDD(poi.Latitude, LLFormat.DDM_P3ZF);
            EditStpt.LonUI = Coord.ConvertFromLonDD(poi.Longitude, LLFormat.DDM_P3ZF);
            EditStpt.Alt = poi.Elevation;
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
        /// TODO: document
        /// </summary>
        private async void StptBtnAddPoI_Click(object sender, RoutedEventArgs args)
        {
            if (!string.IsNullOrEmpty(EditStpt.Name) && EditStpt.IsValid &&
                double.TryParse(EditStpt.Lat, out double lat) && double.TryParse(EditStpt.Lon, out double lon))
            {
                string theater = PointOfInterestDbase.TheaterForCoords(lat, lon);
                PointOfInterestDbQuery query = new(PointOfInterestTypeMask.USER, theater, EditStpt.Name);
                List<PointOfInterest> pois = PointOfInterestDbase.Instance.Find(query);
                if (pois.Count > 0)
                {
                    ContentDialogResult result = await Utilities.Message2BDialog(
                        Content.XamlRoot,
                        "Point of Interest Already Defined",
                        $"The database already contains a point of interest for “{EditStpt.Name}”. Would you like to replace it?",
                        "Replace");
                    if (result == ContentDialogResult.Primary)
                    {
                        pois[0].Name = EditStpt.Name;
                        pois[0].Latitude = EditStpt.Lat;
                        pois[0].Longitude = EditStpt.Lon;
                        pois[0].Elevation = EditStpt.Alt;
                        RebuildPointsOfInterest();
                        RebuildInterfaceState();
                    }
                }
                else
                {
                    PointOfInterest poi = new(PointOfInterestType.USER,
                                              theater, EditStpt.Name, "", EditStpt.Lat, EditStpt.Lon, EditStpt.Alt);
                    PointOfInterestDbase.Instance.Add(poi);
                    RebuildPointsOfInterest();
                    RebuildInterfaceState();
                }
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
            SelectMatchingPoI();
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
            SelectMatchingPoI();
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
        /// TODO: document
        /// </summary>
        private void RfptBtnClear_Click(object obj, RoutedEventArgs args)
        {
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

        /// <summary>
        /// TODO: document
        ///
        /// NOTE: though the text box has lost focus, the update may not yet have propagated into state. use the
        /// NOTE: dispatch queue to give in-flight state updates time to complete.
        /// </summary>
        private void RfptTextBox_LostFocus(object obj, RoutedEventArgs args)
        {
            // CONSIDER: may be better here to handle this in a property changed handler rather than here?
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CopyEditToConfig(EditStptIndex, true);
            });
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void StptTextBox_LostFocus(object sender, RoutedEventArgs args)
        {
            TextBox textBox = (TextBox)sender;
            // TODO: check TOT format in mudhen
            if ((textBox == uiStptValueTOT) && (textBox.Text == "––:––:––"))
            {
                // TOT field uses text mask and can come back as "--:--:--" when empty. this is really "" and, since
                // that value is OK, remove the error. note that as we just lost focus, the bound property in
                // EditStpt.TOT may not yet be set up.
                //
                EditStpt.ClearErrors("TOT");
                SetFieldValidState(uiStptValueTOT, true);
            }
            RebuildEnableState();
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
            CopyConfigToEdit(EditStptIndex);

            string theater = null;
            if ((Config.STPT.Points.Count > 0) && Config.STPT.Points[0].IsValid)
            {
                theater = PointOfInterestDbase.TheaterForCoords(Config.STPT.Points[0].Lat, Config.STPT.Points[0].Lon);
            }
            theater = (string.IsNullOrEmpty(theater)) ? CurPoITheaters[0] : theater;
            uiPoIComboTheater.SelectedItem = theater;

            EditRfptNum = 1;
            uiRfptComboSelect.SelectedIndex = EditRfptNum - 1;
            LoadEditRfptFromPointNumber(EditRfptNum);

            ValidateAllFields(_curStptFieldValueMap, EditStpt.GetErrors(null));
            ValidateAllFields(_curRfptFieldValueMap, EditRfpt.GetErrors(null));
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }
    }
}
