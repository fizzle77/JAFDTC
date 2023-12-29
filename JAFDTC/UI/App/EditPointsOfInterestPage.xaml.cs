// ********************************************************************************************************************
//
// EditPointsOfInterestPage.xaml.cs : ui c# point of interest editor
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
using JAFDTC.Models.DCS;
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// item class for an item in the point of interest list view. this provides ui-friendly views of properties
    /// suitable for display in the ui.
    /// </summary>
    internal class PoIListItem
    {
        public PointOfInterest PoI { get; set; }

        public LLFormat LLDisplayFmt { get; set; }

        public string LatUI => Coord.RemoveLLDegZeroFill(Coord.ConvertFromLatDD(PoI.Latitude, LLDisplayFmt));

        public string LonUI => Coord.RemoveLLDegZeroFill(Coord.ConvertFromLonDD(PoI.Longitude, LLDisplayFmt));

        public string Glyph => (PoI.Type == PointOfInterestType.USER) ? "\xE718" : "";

        public PoIListItem(PointOfInterest poi, LLFormat fmt) => (PoI, LLDisplayFmt) = (poi, fmt);
    }

    // ================================================================================================================

    /// <summary>
    /// TODO: document
    /// </summary>
    internal class PoILL : BindableObject
    {
        public LLFormat Format { get; set; }

        public string Lat { get; set; }             // string, dd format

        public string Lon { get; set; }             // string, dd format

        private string _latUI;                      // string, format per property
        public string LatUI
        {
            get => Coord.ConvertFromLatDD(Lat, Format);
            set
            {
                string error = "Invalid latitude format";
                if (IsRegexFieldValid(value, Coord.LatRegexFor(Format), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lat = Coord.ConvertToLatDD(value, Format);
                SetProperty(ref _latUI, value, error);
            }
        }

        private string _lonUI;                      // string, format per property
        public string LonUI
        {
            get => Coord.ConvertFromLonDD(Lon, Format);
            set
            {
                string error = "Invalid longitude format";
                if (IsRegexFieldValid(value, Coord.LonRegexFor(Format), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lon = Coord.ConvertToLonDD(value, Format);
                SetProperty(ref _lonUI, value, error);
            }
        }

        public PoILL(LLFormat format) => (Format) = (format);

        public void Reset()
        {
            LatUI = "";
            LonUI = "";
        }
    }

    // ================================================================================================================

    /// <summary>
    /// TODO: document
    /// </summary>
    internal class PoIDetails : BindableObject
    {
        // HACK: we will use per-format PoILL instances to avoid binding multiple controls to the same property (which
        // HACK: doesn't seem to work well). should be a way to dynamically bind/unbind properties that we could use
        // HACK: to avoid this, but...
        //
        public PoILL[] LL { get; set; }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _alt;
        public string Alt
        {
            get => _alt;
            set
            {
                string error = "Invalid altitude format";
                if (IsIntegerFieldValid(value, -1500, 80000, false))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _alt, value, error);
            }
        }

        // NOTE: format order of PoILL must be kept in sync with EditPointsOfInterestPage and xaml.
        //
        public PoIDetails()
            => (LL) = (new PoILL[3] { new(LLFormat.DDU), new(LLFormat.DMS), new(LLFormat.DDM_P3ZF) });

        public void Reset()
        {
            Name = "";
            Alt = "";
            for (int i = 0; i < LL.Length; i++)
            {
                LL[i].Reset();
            }
        }
    }

    // ================================================================================================================

    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class EditPointsOfInterestPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ObservableCollection<PoIListItem> CurPoIItems { get; set; }

        private List<PointOfInterest> CurPoI { get; set; }

        private PoIDetails EditPoI { get; set; }

        private bool IsEditPoINew { get; set; }

        private bool IsRebuildPending { get; set; }

        private LLFormat LLDisplayFmt { get; set; }

        // read-only properties

        private readonly Dictionary<LLFormat, int> _llFmtToIndexMap;
        private readonly Dictionary<string, LLFormat> _llFmtTextToFmtMap;

        private readonly Dictionary<string, TextBox> _curPoIFieldValueMap;
        private readonly List<TextBox> _poiFieldValues;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public EditPointsOfInterestPage()
        {
            InitializeComponent();

            CurPoIItems = new ObservableCollection<PoIListItem>();

            EditPoI = new PoIDetails();
            EditPoI.ErrorsChanged += PoIField_DataValidationError;
            EditPoI.PropertyChanged += PoIField_PropertyChanged;
            for (int i = 0; i < EditPoI.LL.Length; i++)
            {
                EditPoI.LL[i].ErrorsChanged += PoIField_DataValidationError;
                EditPoI.LL[i].PropertyChanged += PoIField_PropertyChanged;
            }

            IsEditPoINew = true;

            LLDisplayFmt = LLFormat.DDM_P3ZF;

            // NOTE: these need to be kept in sync with PoIDetails and the xaml.
            //
            _llFmtToIndexMap = new Dictionary<LLFormat, int>()
            {
                [LLFormat.DDU] = 0,
                [LLFormat.DMS] = 1,
                [LLFormat.DDM_P3ZF] = 2
            };
            _llFmtTextToFmtMap = new Dictionary<string, LLFormat>()
            {
                ["Decimal Degrees"] = LLFormat.DDU,
                ["Degrees, Minutes, Seconds"] = LLFormat.DMS,
                ["Degrees, Decimal Minutes"] = LLFormat.DDM_P3ZF,
            };

            _curPoIFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["LatUI"] = uiPoIValueLatDDM,
                ["LonUI"] = uiPoIValueLonDDM,
                ["Alt"] = uiPoIValueAlt,
                ["Name"] = uiPoIValueName
            };
            _poiFieldValues = new List<TextBox>()
            {
                uiPoIValueLatDD, uiPoIValueLatDDM, uiPoIValueLatDMS, uiPoIValueLonDD, uiPoIValueLonDDM, uiPoIValueLonDMS
            };
            _defaultBorderBrush = uiPoIValueLatDDM.BorderBrush;
            _defaultBkgndBrush = uiPoIValueLatDDM.Background;
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
        /// TODO: document
        /// </summary>
        private void PoIField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            if (args.PropertyName == null)
            {
                // TODO: this is not right for LatUI, LonUI
                ValidateAllFields(_curPoIFieldValueMap, EditPoI.GetErrors(null));
            }
            else
            {
                bool isValid = ((List<string>)EditPoI.GetErrors(args.PropertyName)).Count == 0;
                if ((args.PropertyName == "LatUI") || (args.PropertyName == "LonUI"))
                {
                    int index = _llFmtToIndexMap[LLDisplayFmt];
                    isValid = ((List<string>)EditPoI.LL[index].GetErrors(args.PropertyName)).Count == 0;
                }
                if (_curPoIFieldValueMap.ContainsKey(args.PropertyName))
                {
                    SetFieldValidState(_curPoIFieldValueMap[args.PropertyName], isValid);
                }
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// property changed: rebuild interface state to account for configuration changes.
        /// </summary>
        private void PoIField_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui utility
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns a list of points of interest from the database that cover the user points of interest currently
        /// selected in the point of interest list.
        /// </summary>
        private List<PointOfInterest> GetUserPoIsInSelection()
        {
            List<PointOfInterest> pois = new();
            foreach (PoIListItem poiItem in uiPoIListView.SelectedItems.Cast<PoIListItem>())
            {
                if (poiItem.PoI.Type == PointOfInterestType.USER)
                {
                    pois.Add(CurPoI[CurPoIItems.IndexOf(poiItem)]);
                }
            }
            return pois;
        }

        /// <summary>
        /// returns the current theater assocaited with the coordinates from the point of interest editor.
        /// </summary>
        private string GetTheaterFromEditor()
        {
            string theater = "";
            int index = _llFmtToIndexMap[LLDisplayFmt];
            if (double.TryParse(EditPoI.LL[index].Lat, out double lat) &&
                double.TryParse(EditPoI.LL[index].Lon, out double lon))
            {
                theater = PointOfInterestDbase.TheaterForCoords(lat, lon);
            }
            return theater;
        }

        /// <summary>
        /// update the coordinate format used in the poi lat/lon specification. there are per-format fields to
        /// deal with the apparent inability to change masks dynamically. show the field for the current format,
        /// hide all others. updates LLDisplayFmt.
        /// </summary>
        private void ChangeCoordFormat(LLFormat fmt)
        {
            switch (fmt)
            {
                case LLFormat.DDM_P3ZF:
                    _curPoIFieldValueMap["LatUI"] = uiPoIValueLatDDM;
                    _curPoIFieldValueMap["LonUI"] = uiPoIValueLonDDM;
                    break;
                case LLFormat.DMS:
                    _curPoIFieldValueMap["LatUI"] = uiPoIValueLatDMS;
                    _curPoIFieldValueMap["LonUI"] = uiPoIValueLonDMS;
                    break;
                default:
                    _curPoIFieldValueMap["LatUI"] = uiPoIValueLatDD;
                    _curPoIFieldValueMap["LonUI"] = uiPoIValueLonDD;
                    break;
            }

            foreach (TextBox tbox in _poiFieldValues)
            {
                tbox.Visibility = Visibility.Collapsed;
            }
            _curPoIFieldValueMap["LatUI"].Visibility = Visibility.Visible;
            _curPoIFieldValueMap["LonUI"].Visibility = Visibility.Visible;

            LLDisplayFmt = fmt;
        }

        /// <summary>
        /// rebuild the content of the point of interest list based on the current contents of the poi database
        /// along with the currently selected theater and user/sys mode from the ui.
        /// </summary>
        private void RebuildPoIList()
        {
            PointOfInterestMask mask = (uiBarBtnUser.IsChecked == true) ? PointOfInterestMask.USER : PointOfInterestMask.ANY;
            string theater = (uiBarComboTheater.SelectedIndex != 0) ? (string)uiBarComboTheater.SelectedItem : null;

            CurPoI = PointOfInterestDbase.Instance.Find(theater, mask);
            CurPoI.Sort((a, b) =>
            {
                int theaterCmp = a.Theater.CompareTo(b.Theater);
                if ((theaterCmp == 0) && (a.Type == b.Type))
                {
                    return a.Name.CompareTo(b.Name);
                }
                else if (theaterCmp == 0)
                {
                    return (a.Type == PointOfInterestType.USER) ? -1 : 1;
                }
                return theaterCmp;
            });
            CurPoIItems.Clear();
            foreach (PointOfInterest poi in CurPoI)
            {
                CurPoIItems.Add(new PoIListItem(poi, LLDisplayFmt));
            }
        }

        /// <summary>
        /// rebuild the title of the action button based on the current values in the point of interest editor and
        /// the theater text. if name and theater match an existing user poi, we are updating; otherwise we are adding.
        /// </summary>
        private void RebuildActionButtonTitle()
        {
            string theater = GetTheaterFromEditor();
            uiPoITextTheater.Text = theater;

            List<PointOfInterest> pois = PointOfInterestDbase.Instance.Find(theater, PointOfInterestMask.USER,
                                                                            uiPoIValueName.Text);
            IsEditPoINew = pois.Count <= 0;
            uiPoITextBtnAdd.Text = (IsEditPoINew) ? "Add" : "Update";
        }

        /// <summary>
        /// rebuild the enable state of controls on the page.
        /// </summary>
        private void RebuildEnableState()
        {
            bool isPoIValid = !string.IsNullOrEmpty(uiPoIValueName.Text) &&
                  !string.IsNullOrEmpty(EditPoI.Alt) &&
                  !EditPoI.HasErrors;
            Utilities.SetEnableState(uiPoIBtnAdd, isPoIValid);
            if (!isPoIValid)
            {
                uiPoITextBtnAdd.Text = "Add";
            }

            Utilities.SetEnableState(uiBarBtnEdit, uiPoIListView.SelectedItems.Count == 1);

            bool isUserInSel = false;
            foreach (PoIListItem poi in uiPoIListView.SelectedItems.Cast<PoIListItem>())
            {
                if (poi.PoI.Type == PointOfInterestType.USER)
                {
                    isUserInSel = true;
                    break;
                }
            }
            Utilities.SetEnableState(uiBarBtnDelete, isUserInSel);
            Utilities.SetEnableState(uiBarBtnExport, isUserInSel);
        }

        /// <summary>
        /// rebuild user interface state such as the enable state of the command bars.
        /// </summary>
        private void RebuildInterfaceState()
        {
            if (!IsRebuildPending)
            {
                IsRebuildPending = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    RebuildActionButtonTitle();
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
        /// back button click: navigate back to the configuration list.
        /// </summary>
        private void HdrBtnBack_Click(object sender, RoutedEventArgs args)
        {
            Frame.GoBack();
        }

        // ---- theater selection -------------------------------------------------------------------------------------

        /// <summary>
        /// theater combo box selection changed: rebuild the poi list for the newly-selected theater.
        /// </summary>
        private void BarComboTheater_SelectionChanged(object sender, RoutedEventArgs args)
        {
            Settings.LastPoITheaterSelection = (string)uiBarComboTheater.SelectedItem;
            RebuildPoIList();
            RebuildInterfaceState();
        }

        // ---- command bar / commands --------------------------------------------------------------------------------

        /// <summary>
        /// edit command click: copy the name, latitude, longitude, and elevation of the currnetly selected point of
        /// interest into the poi editor fields and rebuild the interface state to reflect the change.
        /// </summary>
        private void CmdEdit_Click(object sender, RoutedEventArgs args)
        {
            if (uiPoIListView.SelectedItems.Count > 0)
            {
                PoIListItem poiListItem = (PoIListItem)(uiPoIListView.SelectedItems[0]);
                int index = _llFmtToIndexMap[LLDisplayFmt];
                EditPoI.Name = poiListItem.PoI.Name;
                EditPoI.LL[index].LatUI = Coord.ConvertFromLatDD(poiListItem.PoI.Latitude, LLDisplayFmt);
                EditPoI.LL[index].LonUI = Coord.ConvertFromLonDD(poiListItem.PoI.Longitude, LLDisplayFmt);
                EditPoI.Alt = poiListItem.PoI.Elevation;
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// delete command click: remove the selected user points of interest from the points of interest database.
        /// non-user pois are skipped.
        /// </summary>
        private void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
            List<PointOfInterest> pois = GetUserPoIsInSelection();
            foreach (PointOfInterest poi in pois)
            {
                PointOfInterestDbase.Instance.Remove(poi);
            }
            RebuildPoIList();
            RebuildInterfaceState();
        }

        /// <summary>
        /// import command click: prompt the user for a file to import points of interest from and deserialize
        /// the contents of the file into the points of interest database. the database is saved following the
        /// import.
        /// </summary>
        private async void CmdImport_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                FileOpenPicker picker = new()
                {
                    SuggestedStartLocation = PickerLocationId.Desktop
                };
                picker.FileTypeFilter.Add(".json");
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as JAFDTC.App)?.Window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    string json = await FileIO.ReadTextAsync(file);
                    List<PointOfInterest> pois = JsonSerializer.Deserialize<List<PointOfInterest>>(json);
                    int count = 0;
                    foreach (PointOfInterest poi in pois)
                    {
                        if (poi.Type == PointOfInterestType.USER)
                        {
                            PointOfInterestDbase.Instance.Add(poi);
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        string what = (pois.Count > 1) ? "points" : "point";
                        await Utilities.Message1BDialog(Content.XamlRoot,
                                                        "Success!", $"Imported {count} {what} of interest.");
                        RebuildPoIList();
                        RebuildInterfaceState();
                    }
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"EditPointsOfInterestPage:CmdImport_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed", "Unable to import points of interest.");
            }
        }

        /// <summary>
        /// export command click: prompt the user for a file to export the selected points of interest to and
        /// serialized the selected pois to the file.
        /// </summary>
        private async void CmdExport_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                List<PointOfInterest> pois = GetUserPoIsInSelection();
                if (pois.Count > 0)
                {
                    FileSavePicker picker = new()
                    {
                        SuggestedStartLocation = PickerLocationId.Desktop,
                        SuggestedFileName = "pois"
                    };
                    picker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });
                    var hwnd = WindowNative.GetWindowHandle((Application.Current as JAFDTC.App)?.Window);
                    InitializeWithWindow.Initialize(picker, hwnd);

                    StorageFile file = await picker.PickSaveFileAsync();
                    if (file != null)
                    {
                        await FileIO.WriteTextAsync(file, JsonSerializer.Serialize(pois, Configuration.JsonOptions));
                        string what = (pois.Count > 1) ? "points" : "point";
                        await Utilities.Message1BDialog(Content.XamlRoot,
                                                        "Success!",  $"Exported {pois.Count} {what} of interest.");
                    }
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"EditPointsOfInterestPage:CmdExport_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Export Failed", "Unable to export the points of interest.");
            }
        }

        /// <summary>
        /// user/all mode command click: toggle between the user-pois-only and all-pois mode by rebuilding the poi
        /// list and updating interface state to match.
        /// </summary>
        private void CmdUser_Click(object sender, RoutedEventArgs args)
        {
            Settings.LastPoIUserModeSelection = (bool)((AppBarToggleButton)sender).IsChecked;
            uiPoIListView.SelectedItems.Clear();
            RebuildPoIList();
            RebuildInterfaceState();
        }

        /// <summary>
        /// select coordinate format click: present the user with a list dialog to select the display format for poi
        /// coordinates and update the ui to reflect the new choice.
        /// </summary>
        private async void CmdCoords_Click(object sender, RoutedEventArgs args)
        {
            List<string> items = new(_llFmtTextToFmtMap.Keys);
            GetListDialog coordList = new(items, null, 0, _llFmtToIndexMap[LLDisplayFmt])
            {
                XamlRoot = Content.XamlRoot,
                Title = $"Select a Coordinate Format",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };
            ContentDialogResult result = await coordList.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
            {
                Settings.LastPoICoordFmtSelection = _llFmtTextToFmtMap[coordList.SelectedItem];
                ChangeCoordFormat(_llFmtTextToFmtMap[coordList.SelectedItem]);
                RebuildPoIList();
                RebuildInterfaceState();
            }
        }

        // ---- poi list ----------------------------------------------------------------------------------------------

        /// <summary>
        /// poi list view right click: show context menu
        /// </summary>
        private void PoIListView_RightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            uiPoiListCtxMenuFlyout.Items[0].IsEnabled = (uiPoIListView.SelectedItems.Count > 0);    // edit
            uiPoiListCtxMenuFlyout.Items[1].IsEnabled = (uiPoIListView.SelectedItems.Count > 0);    // export
            uiPoiListCtxMenuFlyout.Items[3].IsEnabled = (uiPoIListView.SelectedItems.Count > 0);    // delete
            uiPoiListCtxMenuFlyout.ShowAt((ListView)sender, args.GetPosition((ListView)sender));
        }

        /// <summary>
        /// poi list view selection changed: rebuild the interface state to reflect the newly selected poi(s).
        /// </summary>
        private void PoIListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// poi add/update button click: add a new poi or update an existing poi with the values from the poi editor.
        /// </summary>
        private void PoIBtnAdd_Click(object sender, RoutedEventArgs args)
        {
            string theater = GetTheaterFromEditor();
            int index = _llFmtToIndexMap[LLDisplayFmt];
            List<PointOfInterest> pois = PointOfInterestDbase.Instance.Find(theater,
                                                                            PointOfInterestMask.USER,
                                                                            EditPoI.Name);
            if (IsEditPoINew || (pois.Count == 0))
            {
                PointOfInterest poi = new()
                {
                    Type = PointOfInterestType.USER,
                    Name = EditPoI.Name,
                    Theater = theater,
                    Latitude = EditPoI.LL[index].Lat,
                    Longitude = EditPoI.LL[index].Lon,
                    Elevation = EditPoI.Alt,
                };
                PointOfInterestDbase.Instance.Add(poi);
            }
            else
            {
                pois[0].Elevation = EditPoI.Alt;
                pois[0].Latitude = EditPoI.LL[index].Lat;
                pois[0].Longitude = EditPoI.LL[index].Lon;
            }

            RebuildPoIList();
            RebuildInterfaceState();
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        /// <summary>
        /// poi editor value changed: update the interface state to reflect changes in the text value.
        /// </summary>
        private void PoIValueName_TextChanged(object sender, TextChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// poi editor value field lost focus: update the interface state to reflect changes in the text value.
        /// </summary>
        private void PoITextBox_LostFocus(object sender, RoutedEventArgs args)
        {
            RebuildInterfaceState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        /// we are editing.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            uiBarComboTheater.Items.Add("All Theaters");
            foreach (string theater in PointOfInterestDbase.KnownTheaters())
            {
                uiBarComboTheater.Items.Add(theater);
            }
            if (!uiBarComboTheater.Items.Contains(Settings.LastPoITheaterSelection))
            {
                Settings.LastPoITheaterSelection = (string)uiBarComboTheater.Items[0];
            }
            uiBarComboTheater.SelectedItem = Settings.LastPoITheaterSelection;

            uiBarBtnUser.IsChecked = Settings.LastPoIUserModeSelection;

            ChangeCoordFormat(Settings.LastPoICoordFmtSelection);
            EditPoI.ClearErrors();
            EditPoI.Reset();

            RebuildPoIList();
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }
    }
}
