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
using JAFDTC.Utilities.Networking;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
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
using System.Text.RegularExpressions;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// item class for an item in the point of interest list view. this provides ui-friendly views of properties
    /// suitable for display in the ui via bindings.
    /// </summary>
    internal class PoIListItem
    {
        public PointOfInterest PoI { get; set; }

        public LLFormat LLDisplayFmt { get; set; }

        public string TagsUI
        {
            get
            {
                string tags = (string.IsNullOrEmpty(PoI.Tags)) ? "—" : PoI.Tags.Replace(";", ", ");
                return (string.IsNullOrEmpty(PoI.Campaign)) ? tags : $"{PoI.Campaign} : {tags}";
            }
        }

        public string LatUI => Coord.RemoveLLDegZeroFill(Coord.ConvertFromLatDD(PoI.Latitude, LLDisplayFmt));

        public string LonUI => Coord.RemoveLLDegZeroFill(Coord.ConvertFromLonDD(PoI.Longitude, LLDisplayFmt));

        public string Glyph
            => (PoI.Type == PointOfInterestType.USER) ? "\xE718"
                                                      : ((PoI.Type == PointOfInterestType.CAMPAIGN) ? "\xE7C1" : "");

        public PoIListItem(PointOfInterest poi, LLFormat fmt) => (PoI, LLDisplayFmt) = (poi, fmt);
    }

    // ================================================================================================================

    /// <summary>
    /// point of interest lat/lon helper. this provides for translation between the user-facing presentation of the
    /// lat/lon (where settings in the ui specify the lat/lon display format) and the internal decimal degress format.
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
                if (IsRegexFieldValid(value, Coord.LatRegexFor(Format)))
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
                if (IsRegexFieldValid(value, Coord.LonRegexFor(Format)))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lon = Coord.ConvertToLonDD(value, Format);
                SetProperty(ref _lonUI, value, error);
            }
        }

        public bool IsEmpty => (string.IsNullOrEmpty(Lat) && string.IsNullOrEmpty(Lon));

        public PoILL(LLFormat format) => (Format) = (format);

        public List<string> GetErrorsWithEmpty(bool isEmptyOK)
        {
            List<string> errors = new();
            if (!IsRegexFieldValid(LatUI, Coord.LatRegexFor(Format), isEmptyOK))
                errors.Add("LatUI");
            if (!IsRegexFieldValid(LonUI, Coord.LonRegexFor(Format), isEmptyOK))
                errors.Add("LonUI");
            return errors;
        }

        public void Reset()
        {
            LatUI = "";
            LonUI = "";
        }
    }

    // ================================================================================================================

    /// <summary>
    /// backing object for editing a point of interest.
    /// </summary>
    internal class PoIDetails : BindableObject
    {
        public int CurIndexLL { get; set; }

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
                if (IsIntegerFieldValid(value, -1500, 80000))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _alt, value, error);
            }
        }

        public bool IsEmpty
            => (string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Tags) && string.IsNullOrEmpty(Alt) &&
                LL[CurIndexLL].IsEmpty);

        // NOTE: format order of PoILL must be kept in sync with EditPointsOfInterestPage and xaml.
        //
        public PoIDetails()
            => (CurIndexLL, LL) = (0, new PoILL[3] { new(LLFormat.DDU), new(LLFormat.DMS), new(LLFormat.DDM_P3ZF) });

        public List<string> GetErrorsWithEmpty(bool isEmptyOK)
        {
            List<string> errors = LL[CurIndexLL].GetErrorsWithEmpty(isEmptyOK);
            if (!IsIntegerFieldValid(Alt, -1500, 80000, isEmptyOK))
                errors.Add("Alt");
            return errors;
        }

        public void Reset()
        {
            Name = "";
            Tags = "";
            Alt = "";
            for (int i = 0; i < LL.Length; i++)
                LL[i].Reset();
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

        private static readonly Regex _campaignNameRegex = new(@"^[a-zA-Z0-9 ]+$");

        private ObservableCollection<PoIListItem> CurPoIItems { get; set; }

        private List<PointOfInterest> CurPoI { get; set; }

        private PoIDetails EditPoI { get; set; }

        private bool IsEditPoINew { get; set; }

        private bool IsRebuildPending { get; set; }

        private LLFormat LLDisplayFmt { get; set; }

        private string FilterTheater { get; set; }

        private string FilterCampaign { get; set; }

        private string FilterTags { get; set; }

        private PointOfInterestTypeMask FilterIncludeTypes { get; set; }

        private bool IsFiltered => !(string.IsNullOrEmpty(FilterTheater) &&
                                     string.IsNullOrEmpty(FilterCampaign) && 
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
            for (int i = 0; i < EditPoI.LL.Length; i++)
                EditPoI.LL[i].ErrorsChanged += PoIField_DataValidationError;

            IsEditPoINew = true;

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

            LLDisplayFmt = LLFormat.DDM_P3ZF;
            EditPoI.CurIndexLL = _llFmtToIndexMap[LLDisplayFmt];
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
            field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBackgroundBrush"];
        }

        private void ValidateAllFields(Dictionary<string, TextBox> fields, IEnumerable errors)
        {
            Dictionary<string, bool> map = new();
            foreach (string error in errors)
                map[error] = true;
            foreach (KeyValuePair<string, TextBox> kvp in fields)
                SetFieldValidState(kvp.Value,
                                   !map.ContainsKey(kvp.Key) || EditPoI.IsEmpty);
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
                    SetFieldValidState(_curPoIFieldValueMap[args.PropertyName],
                                       isValid || EditPoI.IsEmpty);
            }
            RebuildInterfaceState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui utility
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// apply a function to a new campaign. prompt the user for a camapign name and then perform the operation
        /// given by a lambda when the user provides/accepts a valid name. the lambda takes a single string argument
        /// (the campaign name) and returns true on success, false on error. the what parameter should be uppercase
        /// description of what the function does ("Add", "Import").
        /// </summary>
        private async void CoreApplyFuncToNewCampaign(string what, Func<string, bool> operation)
        {
            GetNameDialog nameDialog = new(null, $"Select a Name for the Campaign to {what}")
            {
                XamlRoot = Content.XamlRoot,
                Title = $"{what} Campaign"
            };
            ContentDialog errDialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = "Invalid Name",
                PrimaryButtonText = "OK",
            };

            string campaignName;
            ContentDialogResult result;
            while (true)
            {
                result = await nameDialog.ShowAsync();
                campaignName = nameDialog.Value.Trim(' ');
                string message = null;
                foreach (string campaign in PointOfInterestDbase.Instance.KnownCampaigns)
                {
                    if (campaignName.ToLower() == campaign.ToLower())
                    {
                        message = $"There is already a campaign named '{campaignName}'. Please select a different name.";
                        break;
                    }
                }
                if (!_campaignNameRegex.IsMatch(campaignName))
                    message = $"Campaign name '{campaignName}' may only contain alphanumeric characters. Please select a different name.";
                if ((result == ContentDialogResult.None) || (message == null))
                    break;
                errDialog.Content = message;
                await errDialog.ShowAsync();
            }
            if (result == ContentDialogResult.Primary)
            {
                operation(campaignName);
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// apply a function to an existing campaign. prompt the user to select an existing camapign and then perform
        /// the operation given by a lambda when the user provides/accepts a valid campaign. the lambda takes a single
        /// string argument (the campaign name) and returns true on success, false on error. the what parameter should
        /// be uppercase description of what the function does ("Delete", "Export").
        /// </summary>
        private async void CoreApplyFuncToExistingCampaign(string what, Func<string, bool> operation)
        {
            GetListDialog listDialog = new(PointOfInterestDbase.Instance.KnownCampaigns, "Campaign")
            {
                XamlRoot = Content.XamlRoot,
                Title = $"{what} Campaign",
                PrimaryButtonText = what
            };
            if (await listDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                operation(listDialog.SelectedItem);
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// return a dictionary, keyed by PointOfInterestType that breaks the selection up by point of interest type.
        /// the dictionary will only contain a key of a given type if there were points of interest of that type in
        /// the selection.
        /// </summary>
        private Dictionary<PointOfInterestType, List<PointOfInterest>> CrackSelectedPoIsByType()
        {
            Dictionary<PointOfInterestType, List<PointOfInterest>> cracked = new();
            foreach (PoIListItem item in uiPoIListView.SelectedItems.Cast<PoIListItem>())
            {
                if (!cracked.ContainsKey(item.PoI.Type))
                    cracked[item.PoI.Type] = new List<PointOfInterest>();
                cracked[item.PoI.Type].Add(item.PoI);
            }
            return cracked;
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
                theater = PointOfInterest.TheaterForCoords(lat, lon);
            }
            return theater;
        }

        /// <summary>
        /// return a list of points of interest matching the current filter configuration with a name that
        /// containst the provided name fragment.
        /// </summary>
        private List<PointOfInterest> GetPoIsMatchingFilter(string name = null)
        {
            PointOfInterestDbQuery query = new(FilterIncludeTypes, FilterTheater, FilterCampaign, name, FilterTags,
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
                tbox.Visibility = Visibility.Collapsed;
            _curPoIFieldValueMap["LatUI"].Visibility = Visibility.Visible;
            _curPoIFieldValueMap["LonUI"].Visibility = Visibility.Visible;

            LLDisplayFmt = fmt;
            EditPoI.CurIndexLL = _llFmtToIndexMap[LLDisplayFmt];
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
                CurPoIItems.Add(new PoIListItem(poi, LLDisplayFmt));
        }

        /// <summary>
        /// rebuild the title of the action button based on the current values in the point of interest editor and
        /// the theater text. if name and theater match an existing user poi, we are updating; otherwise we are adding.
        /// </summary>
        private void RebuildActionButtonTitle()
        {
            string theater = GetTheaterFromEditor();
            uiPoITextTheater.Text = (string.IsNullOrEmpty(theater)) ? "Unknown Theater" : theater;

            string test = uiPoIValueName.Text;
            PointOfInterestDbQuery query = new(PointOfInterestTypeMask.USER, theater, null, uiPoIValueName.Text);
            List<PointOfInterest> pois = PointOfInterestDbase.Instance.Find(query);
            IsEditPoINew = pois.Count == 0;
            uiPoITextBtnAdd.Text = (IsEditPoINew) ? "Add" : "Update";
        }

        /// <summary>
        /// rebuild the enable state of controls on the page.
        /// </summary>
        private void RebuildEnableState()
        {
            JAFDTC.App curApp = Application.Current as JAFDTC.App;

            Dictionary<PointOfInterestType, List<PointOfInterest>> selectionByType = CrackSelectedPoIsByType();
            bool isUserInSel = selectionByType.ContainsKey(PointOfInterestType.USER);
            bool isCampaignInSel = selectionByType.ContainsKey(PointOfInterestType.CAMPAIGN);

            PointOfInterestDbQuery query = new(PointOfInterestTypeMask.ANY, null, null, uiPoIValueName.Text);
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
            Utilities.SetEnableState(uiPoIBtnCapture, curApp.IsDCSAvailable);

            if (!isPoIValid)
                uiPoITextBtnAdd.Text = "Add";

            bool isCampaigns = (PointOfInterestDbase.Instance.KnownCampaigns.Count > 0);

            Utilities.SetEnableState(uiBarBtnEdit, (uiPoIListView.SelectedItems.Count == 1) && isUserInSel);
            Utilities.SetEnableState(uiBarBtnDuplicate, uiPoIListView.SelectedItems.Count == 1);
            Utilities.SetEnableState(uiBarBtnCopyCampaign, isCampaigns && (uiPoIListView.SelectedItems.Count > 0));
            Utilities.SetEnableState(uiBarBtnDelete, isUserInSel || isCampaignInSel);
            Utilities.SetEnableState(uiBarBtnExport, (uiPoIListView.SelectedItems.Count > 0));

            Utilities.SetEnableState(uiBarBtnDeleteCamp, isCampaigns);

            List<string> errors = EditPoI.GetErrorsWithEmpty(EditPoI.IsEmpty);
            ValidateAllFields(_curPoIFieldValueMap, errors);
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
                    suitableItems.Add("No Matching Points of Interest Found");
                else
                    foreach (PointOfInterest poi in pois)
                        suitableItems.Add(poi.Name);
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
        /// filter command click: setup the filter setup.
        /// </summary>
        private async void CmdFilter_Click(object sender, RoutedEventArgs args)
        {
            AppBarToggleButton button = (AppBarToggleButton)sender;
            if (button.IsChecked != IsFiltered)
                button.IsChecked = IsFiltered;

            GetPoIFilterDialog filterDialog = new(FilterTheater, FilterCampaign, FilterTags, FilterIncludeTypes)
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
                FilterCampaign = filterDialog.Campaign;
                FilterTags = PointOfInterest.SanitizedTags(filterDialog.Tags);
                FilterIncludeTypes = filterDialog.IncludeTypes;
            }
            else if (result == ContentDialogResult.Secondary)
            {
                FilterTheater = "";
                FilterCampaign = "";
                FilterTags = "";
                FilterIncludeTypes = PointOfInterestTypeMask.ANY;
            }
            else
            {
                return;                                         // EXIT: cancelled, no change...
            }

            button.IsChecked = IsFiltered;

            Settings.LastPoIFilterTheater = FilterTheater;
            Settings.LastPoIFilterCampaign = FilterCampaign;
            Settings.LastPoIFilterTags = FilterTags;
            Settings.LastPoIFilterIncludeTypes = FilterIncludeTypes;

            uiPoIListView.SelectedItems.Clear();
            RebuildPoIList();
            RebuildInterfaceState();
        }

        /// <summary>
        /// edit command click: copy the name, latitude, longitude, and elevation of the currnetly selected point of
        /// interest into the poi editor fields and rebuild the interface state to reflect the change. this should
        /// only be called on editable user pois.
        /// </summary>
        private void CmdEdit_Click(object sender, RoutedEventArgs args)
        {
            PoIListItem poiListItem = (PoIListItem)(uiPoIListView.SelectedItems[0]);
            int index = _llFmtToIndexMap[LLDisplayFmt];
            EditPoI.Name = poiListItem.PoI.Name;
            EditPoI.Tags = PointOfInterest.SanitizedTags(poiListItem.PoI.Tags);
            EditPoI.LL[index].LatUI = Coord.ConvertFromLatDD(poiListItem.PoI.Latitude, LLDisplayFmt);
            EditPoI.LL[index].LonUI = Coord.ConvertFromLonDD(poiListItem.PoI.Longitude, LLDisplayFmt);
            EditPoI.Alt = poiListItem.PoI.Elevation;
            RebuildInterfaceState();
        }

        /// <summary>
        /// edit command click: copy the name, latitude, longitude, and elevation of the currnetly selected point of
        /// interest into the poi editor fields and rebuild the interface state to reflect the change. this should
        /// only be called on read-only campaign and system pois.
        /// </summary>
        private void CmdDuplicate_Click(object sender, RoutedEventArgs args)
        {
            PoIListItem poiListItem = (PoIListItem)(uiPoIListView.SelectedItems[0]);
            int index = _llFmtToIndexMap[LLDisplayFmt];
            EditPoI.Name = $"{poiListItem.PoI.Name} (User Copy)";
            EditPoI.Tags = PointOfInterest.SanitizedTags(poiListItem.PoI.Tags);
            EditPoI.LL[index].LatUI = Coord.ConvertFromLatDD(poiListItem.PoI.Latitude, LLDisplayFmt);
            EditPoI.LL[index].LonUI = Coord.ConvertFromLonDD(poiListItem.PoI.Longitude, LLDisplayFmt);
            EditPoI.Alt = poiListItem.PoI.Elevation;
            RebuildInterfaceState();
        }

        /// <summary>
        /// copy to campaign click: prompt the user for a campaign then copy the points of interest that are not
        /// already part of that campaign to the campaign.
        /// </summary>
        private void CmdCopyCampaign_Click(object sender, RoutedEventArgs args)
        {
            if (uiPoIListView.SelectedItems.Count > 0)
            {
                CoreApplyFuncToExistingCampaign("Copy to", (campaignName) =>
                {
                    foreach (PoIListItem item in uiPoIListView.SelectedItems.Cast<PoIListItem>())
                        if (string.IsNullOrEmpty(item.PoI.Campaign) || (item.PoI.Campaign != campaignName))
                            PointOfInterestDbase.Instance.AddPointOfInterest(new PointOfInterest(item.PoI)
                            {
                                Type = PointOfInterestType.CAMPAIGN,
                                Campaign = campaignName
                            });
                    RebuildPoIList();
                    return true;
                });
            }
        }

        /// <summary>
        /// delete command click: remove the selected points of interest from the points of interest database.
        /// dcs core pois are skipped.
        /// </summary>
        private async void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
            if (uiPoIListView.SelectedItems.Count > 0)
            {
                string message = "delete this point of interest?";
                if (uiPoIListView.SelectedItems.Count > 1)
                    message = "delete these points of interest?";
                ContentDialogResult result = await Utilities.Message2BDialog(
                    Content.XamlRoot,
                    "Delete Points of Interest?",
                    $"Are you sure you want to {message} This action cannot be undone.",
                    "Delete"
                );
                if (result == ContentDialogResult.Primary)
                {
                    bool isCampaignDeleted = false;
                    foreach (PoIListItem item in uiPoIListView.SelectedItems.Cast<PoIListItem>())
                        if (item.PoI.Type != PointOfInterestType.DCS_CORE)
                        {
                            if (PointOfInterestDbase.Instance.CountPoIInCampaign(item.PoI.Campaign) == 1)
                            {
                                PointOfInterestDbase.Instance.DeleteCampaign(item.PoI.Campaign);
                                isCampaignDeleted = true;
                            }
                            else
                            {
                                // TODO: ideally, save at end after delete is done, rather than doing N saves
                                PointOfInterestDbase.Instance.RemovePointOfInterest(item.PoI);
                            }
                        }
                    if (isCampaignDeleted)
                    {
                        await Utilities.Message1BDialog(
                            Content.XamlRoot,
                            $"Deleted Empty Campaigns",
                            $"The delete removed all points of interest from one or more campaigns." +
                             " These campaigns have been deleted as well.");
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
            // pick a .json or .csv file to import from and attempt to deserialize a list of points of interest
            // from the file.
            //
            FileOpenPicker picker = new()
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            picker.FileTypeFilter.Add(".json");
            picker.FileTypeFilter.Add(".csv");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as JAFDTC.App)?.Window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile file = await picker.PickSingleFileAsync();
            if (file == null)
                return;                                         // EXIT: cancelled picker...

            List<PointOfInterest> pois;
            try
            {
                string data = await FileIO.ReadTextAsync(file);
                if (file.FileType.ToLower() == ".json")
                    pois = JsonSerializer.Deserialize<List<PointOfInterest>>(data);
                else
                    pois = PointOfInterestDbase.ParseCSV(data);
            }
            catch (Exception ex)
            {
                FileManager.Log($"EditPointsOfInterestPage:CmdImport_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot,
                                                $"Import Failed",
                                                $"Unable to import points of interest from {file.Name}.");
                return;                                         // EXIT: file read failed
            }

            // catalog the pois just read from the file to determine how many campaign and user pois were
            // in the import along with which pois map to which campaign. this information is used to drive
            // user prompts on how to handle the import. we'll handle duplicates here as well (import will
            // over-write them).
            //
            Dictionary<PointOfInterestType, int> poiTypes = new();
            Dictionary<string, List<PointOfInterest>> poisCampaign = new();
            List<PointOfInterest> poisUser = new();
            List<PointOfInterest> poisDup = new();
            foreach (PointOfInterest poi in pois)
            {
                if (poiTypes.ContainsKey(poi.Type))
                    poiTypes[poi.Type]++;
                else
                    poiTypes[poi.Type] = 1;
                if (poi.Type == PointOfInterestType.USER)
                {
                    poisUser.Add(poi);
                    PointOfInterestDbQuery query = new(PointOfInterestTypeMask.USER, poi.Theater, null, poi.Name);
                    foreach (PointOfInterest poiDup in PointOfInterestDbase.Instance.Find(query))
                        poisDup.Add(poiDup);
                }
                else if ((poi.Type == PointOfInterestType.CAMPAIGN) && !string.IsNullOrEmpty(poi.Campaign))
                {
                    if (!poisCampaign.ContainsKey(poi.Campaign))
                        poisCampaign[poi.Campaign] = new List<PointOfInterest>();
                    poisCampaign[poi.Campaign].Add(poi);
                    PointOfInterestDbQuery query = new(PointOfInterestTypeMask.CAMPAIGN, poi.Theater, poi.Campaign,
                                                       poi.Name);
                    foreach (PointOfInterest poiDup in PointOfInterestDbase.Instance.Find(query))
                        poisDup.Add(poiDup);
                }
            }
            if (poisDup.Count > 0)
            {
                ContentDialogResult result = await Utilities.Message2BDialog(
                    Content.XamlRoot,
                    "Duplicate Points of Interest?",
                    $"The import contains points of interest that are already in the database. Do you want to update" +
                    $" these points of interest from the import or cancel?",
                    "Update"
                );
                if (result != ContentDialogResult.Primary)
                    return;                                     // EXIT: cancelled
                foreach (PointOfInterest poi in poisDup)
                    PointOfInterestDbase.Instance.RemovePointOfInterest(poi, false);
            }

            // add the pois to the database. user pois are added as is. campaign pois that match an existing campaign
            // prompt the user to "add" or "replace".
            //
            foreach (PointOfInterest poi in poisUser)
                PointOfInterestDbase.Instance.AddPointOfInterest(poi, false);
            PointOfInterestDbase.Instance.Save();

            foreach (string campaign in poisCampaign.Keys)
            {
                if (PointOfInterestDbase.Instance.CountPoIInCampaign(campaign) > 0)
                {
                    ContentDialogResult result = await Utilities.Message3BDialog(
                        Content.XamlRoot,
                        $"Campaign Exists",
                        $"Import file contains points of interest for a campaign '{campaign}' that is already in" +
                        $" the database. Would you like to merge the points of interest to this existing campaign or" +
                        $" replace the points of interest in this existing campaign with the imported points?",
                        "Add",
                        "Replace",
                        "Cancel");
                    if (result == ContentDialogResult.Secondary)
                        PointOfInterestDbase.Instance.DeleteCampaign(campaign, false);
                }
                if (PointOfInterestDbase.Instance.CountPoIInCampaign(campaign) == 0)
                    PointOfInterestDbase.Instance.AddCampaign(campaign, false);
                foreach (PointOfInterest poi in poisCampaign[campaign])
                    PointOfInterestDbase.Instance.AddPointOfInterest(poi, false);
                PointOfInterestDbase.Instance.Save(campaign);
            }

            string what = (pois.Count > 1) ? "points" : "point";
            await Utilities.Message1BDialog(Content.XamlRoot,
                                            "Success!", $"Imported {pois.Count} {what} of interest.");

            RebuildPoIList();
            RebuildInterfaceState();
        }

        /// <summary>
        /// export command click: prompt the user for a file to export the selected points of interest to and
        /// serialize the selected pois to the file. we will make some modifications to what is exported based
        /// on user responses: dcs pois are converted to user pois, and campaign pois may be added to export
        /// entire campaigns if not all campaign pois are selected.
        /// </summary>
        private async void CmdExport_Click(object sender, RoutedEventArgs args)
        {
            Dictionary<PointOfInterestType, List<PointOfInterest>> selectionByType = CrackSelectedPoIsByType();
            List<PointOfInterest> pois = new();
            ContentDialogResult result;

            // user pois are copied into the export set as is with no changes.
            //
            if (selectionByType.ContainsKey(PointOfInterestType.USER))
                foreach (PointOfInterest poi in selectionByType[PointOfInterestType.USER])
                    pois.Add(poi);

            // dcs pois are copied into the export set as user pois with an updated type and name. we'll ask the
            // user if this is ok before doing the deed.
            //
            if (selectionByType.ContainsKey(PointOfInterestType.DCS_CORE))
            {
                result = await Utilities.Message2BDialog(
                    Content.XamlRoot,
                    $"Exporting DCS Points of Interest",
                    $"The selection includes one or more DCS points of interest." +
                    $" These points will be exported as user points of interest.");
                if (result != ContentDialogResult.Primary)
                    return;                                     // EXIT: user cancels

                foreach (PointOfInterest poi in selectionByType[PointOfInterestType.DCS_CORE])
                    pois.Add(new PointOfInterest(poi)
                    {
                        Type = PointOfInterestType.USER,
                        Name = $"{poi.Name} (User Copy)"
                    });
            }

            // campaign pois are copied into the export set either as complete campaigns or selected pois. if the
            // user did not select all pois in a campaign, ask if they want to include all pois.
            //
            if (selectionByType.ContainsKey(PointOfInterestType.CAMPAIGN))
            {
                Dictionary<string, int> campaigns = new();
                foreach (PointOfInterest poi in selectionByType[PointOfInterestType.CAMPAIGN])
                    if (campaigns.ContainsKey(poi.Campaign))
                        campaigns[poi.Campaign]++;
                    else
                        campaigns[poi.Campaign] = 1;

                result = ContentDialogResult.Secondary;
                foreach (string name in campaigns.Keys)
                    if (PointOfInterestDbase.Instance.CountPoIInCampaign(name) != campaigns[name])
                    {
                        string plural1 = (campaigns.Count > 1) ? "one or more campaigns" : "a campaign";
                        string plural2 = (campaigns.Count > 1) ? "s" : "";
                        result = await Utilities.Message3BDialog(
                            Content.XamlRoot,
                            $"Exporting Partial Campaign{plural2}",
                            $"The selection does not include all points of interest from {plural1}." +
                            $" Would you like to include all points of interest from the selected" +
                            $" campaign{plural2} in the export?",
                            "All",
                            "Only Selected");
                        break;
                    }
                if (result == ContentDialogResult.None)
                    return;                                     // EXIT: user cancels
                else if (result == ContentDialogResult.Secondary)
                    foreach (PointOfInterest poi in selectionByType[PointOfInterestType.CAMPAIGN])
                        pois.Add(poi);
                else
                {
                    foreach (string name in campaigns.Keys)
                    {
                        PointOfInterestDbQuery query = new(PointOfInterestTypeMask.ANY, null, null, name);
                        foreach (PointOfInterest poi in PointOfInterestDbase.Instance.Find(query))
                            pois.Add(poi);
                    }
                }
            }

            // now that we have the export poi list, prompt the user for a save file, serialize the export poi list as
            // json, and save it to disk.
            //
            try
            {
                FileSavePicker picker = new()
                {
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    SuggestedFileName = "JAFDTC PoIs"
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
            catch (Exception ex)
            {
                FileManager.Log($"EditPointsOfInterestPage:CmdExport_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Export Failed", "Unable to export the points of interest.");
            }
        }

        // ---- campaign commands -------------------------------------------------------------------------------------

        /// <summary>
        /// add campaign command click: prompt the user for the name of a new campaign and add it to the point of
        /// interest database.
        /// </summary>
        private void CmdAddCampaign_Click(object sender, RoutedEventArgs args)
        {
            CoreApplyFuncToNewCampaign("Add", (campaignName) =>
            {
                PointOfInterestDbase.Instance.AddCampaign(campaignName);
                return true;
            });
        }

        /// <summary>
        /// delete campaign command click: prompt the user to select an existing campaign and delete it from the point
        /// of interest database.
        /// </summary>
        private void CmdDeleteCampaign_Click(object sender, RoutedEventArgs args)
        {
            CoreApplyFuncToExistingCampaign("Delete", (campaignName) =>
            {
                PointOfInterestDbase.Instance.DeleteCampaign(campaignName);
                RebuildPoIList();
                return true;
            });
        }

        // ---- coordinate setup --------------------------------------------------------------------------------------

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
            ListView listView = (ListView)sender;
            PoIListItem poi = (PoIListItem)((FrameworkElement)args.OriginalSource).DataContext;
            int index = CurPoIItems.IndexOf(poi);
            bool isTappedItemSelected = false;
            foreach (ItemIndexRange range in listView.SelectedRanges)
                if ((index >= range.FirstIndex) && (index <= range.LastIndex))
                {
                    isTappedItemSelected = true;
                    break;
                }
            if (!isTappedItemSelected)
            {
                listView.SelectedIndex = CurPoIItems.IndexOf(poi);
                RebuildInterfaceState();
            }

            bool isSelect = (uiPoIListView.SelectedItems.Count > 0);
            bool isOneUsrSel = false;
            bool isOneNonUsrSel = false;
            if (uiPoIListView.SelectedItems.Count == 1)
            {
                isOneUsrSel = (poi.PoI.Type == PointOfInterestType.USER);
                isOneNonUsrSel = !isOneUsrSel;
            }

            uiPoiListCtxMenuFlyout.Items[0].IsEnabled = isOneUsrSel;                                // edit
            uiPoiListCtxMenuFlyout.Items[1].IsEnabled = isOneNonUsrSel;                             // duplicate
            uiPoiListCtxMenuFlyout.Items[2].IsEnabled = isSelect;                                   // export
            uiPoiListCtxMenuFlyout.Items[4].IsEnabled = isSelect;                                   // copy to campaign
            uiPoiListCtxMenuFlyout.Items[6].IsEnabled = isSelect;                                   // delete

            uiPoiListCtxMenuFlyout.ShowAt((ListView)sender, args.GetPosition(listView));
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
            PointOfInterestDbQuery query = new(PointOfInterestTypeMask.USER, theater, null, EditPoI.Name);
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
                PointOfInterestDbase.Instance.AddPointOfInterest(poi);
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

        /// <summary>
        /// poi capture button click: launch jafdtc side of coordinate capture ui.
        /// </summary>
        private async void PoIBtnCapture_Click(object sender, RoutedEventArgs args)
        {
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived += PoIBtnCapture_WyptCaptureDataReceived;
            await Utilities.CaptureSingleDialog(Content.XamlRoot, "Steerpoint");
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived -= PoIBtnCapture_WyptCaptureDataReceived;
        }

        /// <summary>
        /// event handler for data received from the F10 waypoint capture. update the edited poi with the position of
        /// the location selected in dcs.
        /// </summary>
        private void PoIBtnCapture_WyptCaptureDataReceived(WyptCaptureData[] wypts)
        {
            // TODO: want to add multiple pois if multiple waypoints selected from f10?
            if ((wypts.Length > 0) && !wypts[0].IsTarget)
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    int index = _llFmtToIndexMap[LLDisplayFmt];
                    EditPoI.Name = "DCS Capture";
                    EditPoI.LL[index].LatUI = Coord.ConvertFromLatDD(wypts[0].Latitude, LLDisplayFmt);
                    EditPoI.LL[index].LonUI = Coord.ConvertFromLonDD(wypts[0].Longitude, LLDisplayFmt);
                    EditPoI.Alt = wypts[0].Elevation.ToString();
                });
            }
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
            FilterCampaign = Settings.LastPoIFilterCampaign;
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
