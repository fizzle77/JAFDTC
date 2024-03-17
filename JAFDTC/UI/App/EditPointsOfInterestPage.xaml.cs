// ********************************************************************************************************************
//
// EditPointsOfInterestPage.xaml.cs : ui c# point of interest editor
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
using System.Data.SqlTypes;

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

        public string TagsUI => (string.IsNullOrEmpty(PoI.Tags)) ? "—" : PoI.Tags;

        public string LatUI => Coord.RemoveLLDegZeroFill(Coord.ConvertFromLatDD(PoI.Latitude, LLDisplayFmt));

        public string LonUI => Coord.RemoveLLDegZeroFill(Coord.ConvertFromLonDD(PoI.Longitude, LLDisplayFmt));

        public string Glyph => (PoI.Type == PointOfInterestType.USER)
                               ? "\xE718" : ((PoI.Type == PointOfInterestType.CAMPAIGN) ? "\xE7C1" : "");

        public PoIListItem(PointOfInterest poi, LLFormat fmt) => (PoI, LLDisplayFmt) = (poi, fmt);
    }

    // ================================================================================================================

    /// <summary>
    /// point of interest lat/lon helper. this provides for translation between the user-facing presentation of the
    /// lat/lon and the internal decimal degress format.
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

        private string _tags;
        public string Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
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

// TODO: this is broken LL needs to be an all empty check...
        public bool IsEmpty => (string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Tags) && string.IsNullOrEmpty(Alt) &&
                                string.IsNullOrEmpty(LL[0].Lat) && string.IsNullOrEmpty(LL[0].Lon));

        // NOTE: format order of PoILL must be kept in sync with EditPointsOfInterestPage and xaml.
        //
        public PoIDetails()
            => (LL) = (new PoILL[3] { new(LLFormat.DDU), new(LLFormat.DMS), new(LLFormat.DDM_P3ZF) });

        public void Reset()
        {
            Name = "";
            Tags = "";
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

        private string FilterTheater { get; set; }

        private string FilterTags { get; set; }

        private PointOfInterestTypeMask FilterIncludeTypes { get; set; }

        private bool IsFiltered => !(string.IsNullOrEmpty(FilterTheater) &&
                                     string.IsNullOrEmpty(FilterTags) &&
                                     FilterIncludeTypes.HasFlag(PointOfInterestTypeMask.DCS_CORE) &&
                                     FilterIncludeTypes.HasFlag(PointOfInterestTypeMask.USER) &&
                                     FilterIncludeTypes.HasFlag(PointOfInterestTypeMask.CAMPAIGN));

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
                ["Name"] = uiPoIValueName,
                ["Tags"] = uiPoIValueTags
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
        /// returns a list of points of interest from the database that cover the points of interest of a given type
        /// that are currently selected in the point of interest list.
        /// </summary>
        private List<PointOfInterest> GetPoIsOfTypeInSelection(PointOfInterestType type)
        {
            List<PointOfInterest> pois = new();
            foreach (PoIListItem poiItem in uiPoIListView.SelectedItems.Cast<PoIListItem>())
            {
                if (poiItem.PoI.Type == type)
                {
                    pois.Add(CurPoI[CurPoIItems.IndexOf(poiItem)]);
                }
            }
            return pois;
        }

        /// <summary>
        /// returns the current theater assocaited with the coordinates from the lat/lon fields currently in the
        /// point of interest editor.
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
        /// return a list of points of interest matching the current filter configuration with a name that
        /// containst the provided name fragment.
        /// </summary>
        private List<PointOfInterest> GetPoIsMatchingFilter(string name = null)
        {
            PointOfInterestDbQuery query = new(FilterIncludeTypes, FilterTheater, name, FilterTags,
                                               PointOfInterestDbQueryFlags.NAME_PARTIAL_MATCH);
            return PointOfInterestDbase.Instance.Find(query, true);
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
        /// along with the currently selected theater, tags, and included types from the filter specification.
        /// name specifies the partial name to match, null if no match on name.
        /// </summary>
        private void RebuildPoIList(string name = null)
        {
            CurPoI = GetPoIsMatchingFilter(name);
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

            PointOfInterestDbQuery query = new(PointOfInterestTypeMask.USER, theater, uiPoIValueName.Text);
            List<PointOfInterest> pois = PointOfInterestDbase.Instance.Find(query);
            IsEditPoINew = pois.Count == 0;
            uiPoITextBtnAdd.Text = (IsEditPoINew) ? "Add" : "Update";
        }

        /// <summary>
        /// rebuild the enable state of controls on the page.
        /// </summary>
        private void RebuildEnableState()
        {
            bool isUserInSel = false;
            bool isCampaignInSel = false;
            foreach (PoIListItem poi in uiPoIListView.SelectedItems.Cast<PoIListItem>())
            {
                if (poi.PoI.Type == PointOfInterestType.USER)
                {
                    isUserInSel = true;
                    break;
                }
                else if (poi.PoI.Type == PointOfInterestType.CAMPAIGN)
                {
                    isCampaignInSel = true;
                    break;
                }
            }

            PointOfInterestDbQuery query = new(PointOfInterestTypeMask.ANY, null, uiPoIValueName.Text);
            bool isPoIMatching = false;
            foreach (PointOfInterest poi in PointOfInterestDbase.Instance.Find(query))
            {
                if ((poi.Name == EditPoI.Name) &&
                    (poi.Tags == EditPoI.Tags) &&
                    (poi.Elevation == EditPoI.Alt) &&
                    (poi.Latitude == EditPoI.LL[_llFmtToIndexMap[LLDisplayFmt]].Lat) &&
                    (poi.Longitude == EditPoI.LL[_llFmtToIndexMap[LLDisplayFmt]].Lon))
                {
                    isPoIMatching = true;
                    break;
                }
            }

            bool isPoIValid = !string.IsNullOrEmpty(uiPoIValueName.Text) &&
                              !string.IsNullOrEmpty(uiPoIValueAlt.Text) &&
                              !EditPoI.HasErrors;

            Utilities.SetEnableState(uiPoIBtnAdd, !isPoIMatching && isPoIValid);
            Utilities.SetEnableState(uiPoIBtnClear, !EditPoI.IsEmpty);

            if (!isPoIValid)
            {
                uiPoITextBtnAdd.Text = "Add";
            }

            Utilities.SetEnableState(uiBarBtnEdit, uiPoIListView.SelectedItems.Count == 1);

            Utilities.SetEnableState(uiBarBtnDelete, isUserInSel || isCampaignInSel);
            Utilities.SetEnableState(uiBarBtnExport, isUserInSel || isCampaignInSel);
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

        // ---- name search box ---------------------------------------------------------------------------------------

        /// <summary>
        /// filter box text changed: update the items in the search box based on the value in the field.
        /// </summary>
        private void PoINameFilterBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                List<string> suitableItems = new();
                List<PointOfInterest> pois = GetPoIsMatchingFilter(sender.Text);
                if (pois.Count == 0)
                {
                    suitableItems.Add("No Matching Points of Interest Found");
                }
                else
                {
                    foreach (PointOfInterest poi in pois)
                    {
                        suitableItems.Add(poi.Name);
                    }
                }
                sender.ItemsSource = suitableItems;
            }
        }

        /// <summary>
        /// filter box query submitted: apply the query text filter to the pois listed in the poi list.
        /// </summary>
        private void PoINameFilterBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            RebuildPoIList(args.QueryText);
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
                string suffix = (poiListItem.PoI.Type == PointOfInterestType.USER) ? "" : " (User Copy)";
                int index = _llFmtToIndexMap[LLDisplayFmt];
                EditPoI.Name = $"{poiListItem.PoI.Name}{suffix}";
                EditPoI.Tags = PointOfInterest.SanitizedTags(poiListItem.PoI.Tags);
                EditPoI.LL[index].LatUI = Coord.ConvertFromLatDD(poiListItem.PoI.Latitude, LLDisplayFmt);
                EditPoI.LL[index].LonUI = Coord.ConvertFromLonDD(poiListItem.PoI.Longitude, LLDisplayFmt);
                EditPoI.Alt = poiListItem.PoI.Elevation;
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// delete command click: remove the selected points of interest from the points of interest database.
        /// dcs core pois are skipped, user pois are deleted, and all campaign pois in the same campaign are
        /// deleted.
        /// </summary>
        private async void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
            List<PointOfInterest> poisUser = GetPoIsOfTypeInSelection(PointOfInterestType.USER);
            List<PointOfInterest> poisCampaign = GetPoIsOfTypeInSelection(PointOfInterestType.CAMPAIGN);
            if ((poisUser.Count > 0) || (poisCampaign.Count > 0))
            {
                string message = "delete this user point of interest?";
                if (poisUser.Count > 1)
                {
                    message = "delete these user points of interest?";
                }
                else if ((poisUser.Count > 0) && (poisCampaign.Count > 0))
                {
                    message = "delete these user and campaign points of interest? Deleting a point of interest" +
                              " from a particular campaign deletes all points of interest defined for that" +
                              " campaign.";
                }
                else if (poisCampaign.Count > 0)
                {
                    string what = "this campaign point";
                    if (poisCampaign.Count > 1)
                    {
                        what = "these campaign points";
                    }
                    message = $"delete {what} of interest? Deleting a point of interest from a particular campaign" +
                              $" deletes all points of interest defined for that campaign.";
                }
                string dcs = "";
                if (uiPoIListView.SelectedItems.Count > (poisUser.Count + poisCampaign.Count))
                {
                    dcs = "\n\nDCS points of interest will not be deleted.";
                }
                ContentDialogResult result = await Utilities.Message2BDialog(
                    Content.XamlRoot,
                    "Delete Points of Interest?",
                    $"Are you sure you want to {message} This action cannot be undone.{dcs}",
                    "Delete"
                );
                if (result == ContentDialogResult.Primary)
                {
                    foreach (PointOfInterest poi in poisUser)
                    {
                        PointOfInterestDbase.Instance.Remove(poi);
                    }
                    if (poisCampaign.Count > 0)
                    {
                        foreach (PointOfInterest poi in poisCampaign)
                        {
                            FileManager.DeleteUserDatabase(poi.SourceFile);
                        }
                        PointOfInterestDbase.Instance.Reset();
                    }
                    RebuildPoIList();
                    RebuildInterfaceState();
                }
            }
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
                // ---- pick file

                FileOpenPicker picker = new()
                {
                    SuggestedStartLocation = PickerLocationId.Desktop
                };
                picker.FileTypeFilter.Add(".json");
                picker.FileTypeFilter.Add(".tsv");
                picker.FileTypeFilter.Add(".txt");
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as JAFDTC.App)?.Window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                StorageFile file = await picker.PickSingleFileAsync();
                if (file == null)
                {
                    return;                                     // EXIT: cancelled picker...
                }

                // ---- select campaign / user and, if campaign, campaign name

                ContentDialogResult resultWhat = await Utilities.Message3BDialog(
                    Content.XamlRoot,
                    $"What Would You Like to Import?",
                    $"Would you like to import the information in this file as editable general user points of" +
                    $" interest or fixed campaign points of interest?",
                    $"User",
                    $"Campaign",
                    $"Cancel");
                string campaign = null;
                if (resultWhat == ContentDialogResult.Secondary)
                {
                    GetNameDialog nameDialog = new()
                    {
                        XamlRoot = Content.XamlRoot,
                        Title = "Select a Campaign Name"
                    };
                    ContentDialog errDialog = new()
                    {
                        XamlRoot = Content.XamlRoot,
                        Title = "Invalid Name",
                        PrimaryButtonText = "OK",
                    };
                    ContentDialogResult resultName;
                    while (true)
                    {
                        resultName = await nameDialog.ShowAsync();
                        if (resultName == ContentDialogResult.None)
                        {
                            return;                             // EXIT: cancelled campaign name...
                        }
                        else if (!FileManager.IsCampaignDatabase(nameDialog.Value))
                        {
                            campaign = nameDialog.Value.Trim().Replace(';', ':');
                            break;
                        }
                        errDialog.Content = $"The campaign name \"{nameDialog.Value}\" is not unique.";
                        await errDialog.ShowAsync();
                    }
                }
                else
                {
                    return;                                     // EXIT: cancelled import type...
                }

                // ---- read and deserialize/parse file

                string successMsg = "";
                List<PointOfInterest> pois;
                if (file.FileType.ToLower() == ".json")
                {
                    string json = await FileIO.ReadTextAsync(file);
                    pois = JsonSerializer.Deserialize<List<PointOfInterest>>(json);
                    foreach (PointOfInterest poi in pois)
                    {
                        poi.Type = PointOfInterestType.USER;
                        PointOfInterestDbase.Instance.Add(poi, false);
                    }
                    string what = (pois.Count > 1) ? "points" : "point";
                    successMsg = $"Imported {pois.Count} user {what} of interest.";
                }
                else
                {
                    string text = await FileIO.ReadTextAsync(file);
                    pois = PointOfInterestDbase.ParseTSV(text);
                    foreach (PointOfInterest poi in pois)
                    {
                        poi.Type = PointOfInterestType.CAMPAIGN;

                        bool isFound = false;
                        if (!string.IsNullOrEmpty(poi.Tags))
                        {
                            foreach (string tag in poi.Tags.ToLower().Split(';').ToList<string>())
                            {
                                if (tag.Trim() == campaign)
                                {
                                    isFound = true;
                                    break;
                                }
                            }
                        }
                        if (!isFound)
                        {
                            poi.Tags = (string.IsNullOrEmpty(poi.Tags)) ? $"{campaign}" : $"{campaign}; " + poi.Tags;
                        }

                        PointOfInterestDbase.Instance.Add(poi, false);
                    }
                    if (pois.Count > 0)
                    {
                        FileManager.SaveCampaignPointsOfInterest(campaign, pois);
                        string what = (pois.Count > 1) ? "points" : "point";
                        successMsg = $"Imported {pois.Count} {what} of interest into a campaign named \"{campaign}\".";
                    }
                }

                // ---- wrap up

                if (pois.Count > 0)
                {
                    await Utilities.Message1BDialog(Content.XamlRoot, "Success!", successMsg);

                    PointOfInterestDbase.Instance.Save();

                    RebuildPoIList();
                    RebuildInterfaceState();
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
                List<PointOfInterest> pois = GetPoIsOfTypeInSelection(PointOfInterestType.USER);
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
        /// filter command click: setup the filter setup.
        /// </summary>
        private async void CmdFilter_Click(object sender, RoutedEventArgs args)
        {
            AppBarToggleButton button = (AppBarToggleButton)sender;
            if (button.IsChecked != IsFiltered)
            {
                button.IsChecked = IsFiltered;
            }

            GetPoIFilterDialog filterDialog = new(FilterTheater, true, FilterTags, FilterIncludeTypes)
            {
                XamlRoot = Content.XamlRoot,
                Title = $"Set a Filter for Points of Interest",
                PrimaryButtonText = "Set",
                SecondaryButtonText = "Clear Filters",
                CloseButtonText = "Cancel",
            };
            ContentDialogResult result = await filterDialog.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
            {
                FilterTheater = filterDialog.Theater;
                FilterTags = PointOfInterest.SanitizedTags(filterDialog.Tags);
                FilterIncludeTypes = filterDialog.IncludeTypes;
            }
            else if (result == ContentDialogResult.Secondary)
            {
                FilterTheater = "";
                FilterTags = "";
                FilterIncludeTypes = PointOfInterestTypeMask.ANY;
            }
            else
            {
                return;                                         // EXIT: cancelled, no change...
            }

            button.IsChecked = IsFiltered;

            Settings.LastPoIFilterTheater = FilterTheater;
            Settings.LastPoIFilterTags = FilterTags;
            Settings.LastPoIFilterIncludeTypes = FilterIncludeTypes;

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
        /// poi list view double click: edit item
        /// </summary>
        private void PoIListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs args)
        {
            CmdEdit_Click(sender, args);
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
            PointOfInterestDbQuery query = new(PointOfInterestTypeMask.USER, theater, EditPoI.Name);
            List<PointOfInterest> pois = PointOfInterestDbase.Instance.Find(query);
            if (IsEditPoINew || (pois.Count == 0))
            {
                PointOfInterest poi = new()
                {
                    Type = PointOfInterestType.USER,
                    Name = EditPoI.Name,
                    Tags = PointOfInterest.SanitizedTags(EditPoI.Tags),
                    Theater = theater,
                    Latitude = EditPoI.LL[index].Lat,
                    Longitude = EditPoI.LL[index].Lon,
                    Elevation = EditPoI.Alt,
                };
                PointOfInterestDbase.Instance.Add(poi);
                EditPoI.Tags = poi.Tags;
            }
            else
            {
                pois[0].Tags = PointOfInterest.SanitizedTags(EditPoI.Tags);
                pois[0].Elevation = EditPoI.Alt;
                pois[0].Latitude = EditPoI.LL[index].Lat;
                pois[0].Longitude = EditPoI.LL[index].Lon;
                EditPoI.Tags = pois[0].Tags;
            }

            RebuildPoIList();
            RebuildInterfaceState();
        }

        /// <summary>
        /// poi clear button click: clear the poi editor.
        /// </summary>
        private void PoIBtnClear_Click(object sender, RoutedEventArgs args)
        {
            EditPoI.Reset();
            RebuildInterfaceState();
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        /// <summary>
        /// poi editor value changed: update the interface state to reflect changes in the text value.
        /// </summary>
        private void PoITextBox_TextChanged(object sender, TextChangedEventArgs args)
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
            FilterTheater = Settings.LastPoIFilterTheater;
            FilterTags = Settings.LastPoIFilterTags;
            FilterIncludeTypes = Settings.LastPoIFilterIncludeTypes;

            uiBarBtnFilter.IsChecked = IsFiltered;

            ChangeCoordFormat(Settings.LastPoICoordFmtSelection);
            EditPoI.ClearErrors();
            EditPoI.Reset();

            RebuildPoIList();
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }
    }
}
