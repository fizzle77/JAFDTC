// ********************************************************************************************************************
//
// NavpointUIHelper.cs : helper classes for navpoint ui
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

using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.Models.Import;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// wrapper around the point of interest object to present the ui view of the point of interest in the selection
    /// list from the poi filter box in navpoint editors.
    /// </summary>
    public sealed class PoIListItem
    {
        public PointOfInterest PoI { get; set; }

        public string Name => PoI.Name;

        public string Info
        {
            get
            {
                string tags = (string.IsNullOrEmpty(PoI.Tags)) ? "—" : PoI.Tags.Replace(";", ", ");
                return (string.IsNullOrEmpty(PoI.Campaign)) ? $"{PoI.Theater} : {tags}"
                                                            : $"{PoI.Theater} [{PoI.Campaign}] : {tags}";
            }
        }

        public string Glyph => (PoI.Type == PointOfInterestType.USER)
                           ? "\xE718" : ((PoI.Type == PointOfInterestType.CAMPAIGN) ? "\xE7C1" : "");

        public PoIListItem(PointOfInterest poi) => (PoI) = (poi);
    }

    // ================================================================================================================

    /// <summary>
    /// TODO
    /// </summary>
    public sealed class PoIFilterSpec
    {
        public string Theater { get; set; }

        public string Campaign { get; set; }

        public string Tags { get; set; }

        public PointOfInterestTypeMask IncludeTypes { get; set; }

        public bool IsFiltered => !(string.IsNullOrEmpty(Theater) &&
                                    string.IsNullOrEmpty(Campaign) &&
                                    string.IsNullOrEmpty(Tags) &&
                                    IncludeTypes.HasFlag(PointOfInterestTypeMask.DCS_CORE) &&
                                    IncludeTypes.HasFlag(PointOfInterestTypeMask.USER) &&
                                    IncludeTypes.HasFlag(PointOfInterestTypeMask.CAMPAIGN));

        public PoIFilterSpec(string theater = null, string campaign = null, string tags = null,
                             PointOfInterestTypeMask types = PointOfInterestTypeMask.ANY)
            => (Theater, Campaign, Tags, IncludeTypes) = (theater, campaign, tags, types);
    }

    // ================================================================================================================

    /// <summary>
    /// helper class to provide a number of static support functions for use in the navpoint user interface. this
    /// includes things like common dialogs, import/export core operations, etc.
    /// </summary>
    public class NavpointUIHelper
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // navpoint poi functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// display the filter dialog and gather a new filter spec to use.
        /// </summary>
        public static async Task<PoIFilterSpec> FilterSpecDialog(XamlRoot root, PoIFilterSpec spec, ToggleButton button)
        {
            if (button.IsChecked != spec.IsFiltered)
            {
                button.IsChecked = spec.IsFiltered;
            }

            GetPoIFilterDialog filterDialog = new(spec.Theater, spec.Campaign, spec.Tags, spec.IncludeTypes)
            {
                XamlRoot = root,
                Title = $"Set a Filter for Points of Interest",
                PrimaryButtonText = "Set",
                SecondaryButtonText = "Clear Filters",
                CloseButtonText = "Cancel",
            };
            ContentDialogResult result = await filterDialog.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
            {
                spec.Theater = filterDialog.Theater;
                spec.Tags = PointOfInterest.SanitizedTags(filterDialog.Tags);
                spec.Tags = filterDialog.Tags;
                spec.IncludeTypes = filterDialog.IncludeTypes;
            }
            else if (result == ContentDialogResult.Secondary)
            {
                spec.Theater = "";
                spec.Tags = "";
                spec.IncludeTypes = PointOfInterestTypeMask.ANY;
            }
            else
            {
                return null;                                    // EXIT: cancelled, no change...
            }

            button.IsChecked = spec.IsFiltered;
            return spec;
        }

        /// <summary>
        /// return the point of interest list to display in the filter box candidates list.
        /// </summary>
        public static List<PoIListItem> RebuildPointsOfInterest(PoIFilterSpec spec, string name = null)
        {
            List<PoIListItem> suitableItems = new();
            PointOfInterestDbQuery query = new(spec.IncludeTypes, spec.Theater, null, name, spec.Tags,
                                               PointOfInterestDbQueryFlags.NAME_PARTIAL_MATCH);
            foreach (PointOfInterest poi in PointOfInterestDbase.Instance.Find(query, true))
            {
                suitableItems.Add(new PoIListItem(poi));
            }
            return suitableItems;
        }

        /// <summary>
        /// create a poi from a navpoint with the specified name and position. if the poi exists and can be edited,
        /// it is updated to match the name/position based on user feedback; if the poi does not exist, it is added.
        /// returns true if changes were made to the poi database, false otherwise. the function returns false if the
        /// name or position are invalid and will inform the user if unable to add/update due to name collision.
        /// </summary>
        public static async Task<bool> CreatePoIAt(XamlRoot root, string name, string lat, string lon, string elev)
        {
            string theater = PointOfInterest.TheaterForCoords(lat, lon);
            if (string.IsNullOrEmpty(name) || (theater == null))
            {
                await Utilities.Message1BDialog(
                    root,
                    "Unable to Create Point of Interest",
                    $"The name or coordinates are invalid or do not match a known DCS theater.");
            }
            else
            {
                PointOfInterestDbQuery query = new(PointOfInterestTypeMask.ANY, null, name);
                List<PointOfInterest> pois = PointOfInterestDbase.Instance.Find(query);
                if (pois.Count == 0)
                {
                    PointOfInterest poi = new(PointOfInterestType.USER, theater, "", name, "", lat, lon, elev);
                    PointOfInterestDbase.Instance.AddPointOfInterest(poi);
                    return true;
                }
                else if ((pois.Count == 1) && (pois[0].IsMatchTypeMask(PointOfInterestTypeMask.USER)))
                {
                    ContentDialogResult result = await Utilities.Message2BDialog(
                        root,
                        "Point of Interest Already Defined",
                        $"The database already contains a point of interest with the name “{name}”. Would you like to replace it?",
                        "Replace");
                    if (result == ContentDialogResult.Primary)
                    {
                        pois[0].Name = name;
                        pois[0].Latitude = lat;
                        pois[0].Longitude = lon;
                        pois[0].Elevation = elev;
                        return true;
                    }
                }
                else
                {
                    await Utilities.Message1BDialog(
                        root,
                        "Point of Interest Already Defined",
                        $"The database already contains a point of interest with the name “{name}”.");
                }
            }
            return false;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // navpoint command functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// display a reset navpoints dialog and return the result. the return value is true if the user accepts
        /// the reset, false if they cancel. the what parameter should be capitalized and singular.
        /// </summary>
        public static async Task<bool> ResetDialog(XamlRoot root, string what)
        {
            return await new ContentDialog()
            {
                XamlRoot = root,
                Title = $"Reset {what}s?",
                Content = $"Are you sure you want to delete all {what.ToLower()}s? This action cannot be undone.",
                PrimaryButtonText = "Delete All",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            }.ShowAsync(ContentDialogPlacement.Popup) == ContentDialogResult.Primary;
        }

        /// <summary>
        /// display a delete navpoint dialog and return the result. the return value is true if the user accepts
        /// the delete, false if they cancel. the what parameter should be capitalized and singular.
        /// </summary>
        public static async Task<bool> DeleteDialog(XamlRoot root, string what, int count)
        {
            string title = (count == 1) ? $"Delete {what}?" : $"Delete {what}s?";
            string content = (count == 1)
                ? $"Are you sure you want to delete this {what.ToLower()}? This action cannot be undone."
                : $"Are you sure you want to delete these {what.ToLower()}?s? This action cannot be undone.";
            return await Utilities.Message2BDialog(root, title, content, "Delete") == ContentDialogResult.Primary;
        }

        /// <summary>
        /// display a renumber navpoints dialog and return the result. the return value is the starting navpoint
        /// number (if accepted) or -1 (if cancelled). the what parameter should be capitalized and singular.
        /// </summary>
        public static async Task<int> RenumberDialog(XamlRoot root, string what, int min, int max)
        {
            GetNumberDialog dlg = new(null, null, min, max)
            {
                XamlRoot = root,
                Title = $"Select New Starting {what} Number",
                PrimaryButtonText = "Renumber",
                CloseButtonText = "Cancel",
            };
            return (await dlg.ShowAsync(ContentDialogPlacement.Popup) == ContentDialogResult.Primary) ? dlg.Value : -1;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // navpoint list import functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// present and handle a file open picker to select a .json/.cf/.miz file to import from. returns the result
        /// of FileOpenPicker.PickSingleFileAsync.
        /// </summary>
        private static async Task<StorageFile> ImportFileOpenPicker()
        {
            FileOpenPicker picker = new()
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            picker.FileTypeFilter.Add(".json");
            picker.FileTypeFilter.Add(".cf");
            picker.FileTypeFilter.Add(".miz");
            var hwnd = WindowNative.GetWindowHandle((Application.Current as JAFDTC.App)?.Window);
            InitializeWithWindow.Initialize(picker, hwnd);

            return await picker.PickSingleFileAsync();
        }

        /// <summary>
        /// present and handle an import fail dialog that indicates jafdtc was unable to read/import from the file.
        /// what parameter should be capitalized and singular (e.g., "Steerpoints").
        /// </summary>
        private static async void ImportFailDialog(XamlRoot root, string what)
        {
            await Utilities.Message1BDialog(root, "Import Failed", $"Unable to read {what.ToLower()}s from the file.");
        }

        /// <summary>
        /// present and handle the ui to import from the contents of a .json/.cf/.miz file. the navpoints either
        /// replace or are appended to the current list of navpoints. what parameter should be capitalized and
        /// singular (e.g., "Steerpoints"). returns true on success, false on failure (user is notified on failures).
        /// </summary>
        public static async Task<bool> Import(XamlRoot root, AirframeTypes airframe, INavpointSystemImport navptSys,
                                              string what)
        {
            try
            {
                StorageFile file = await ImportFileOpenPicker();
                if (file == null)
                {
                    return false;                                           // exit, picker cancelled
                }
                IImportHelper importer = file.FileType.ToLower() switch
                {
                    ".json" => new ImportHelperJSON(airframe, file.Path),
                    ".miz" => new ImportHelperMIZ(airframe, file.Path),
                    ".cf" => new ImportHelperCF(airframe, file.Path),
                    _ => null
                };
                if (importer == null)
                {
                    await Utilities.Message1BDialog(root, "Unable to Import", "File is of unkown type.");
                    return false;                                           // exit, bogus file type
                }

                ContentDialogResult result = ContentDialogResult.Primary;
                string flightName = null;
                Dictionary<string, object> options = null;
                List<string> flights = importer.Flights();
                if (importer.HasFlights && ((flights == null) || (flights.Count == 0)))
                {
                    await Utilities.Message1BDialog(root,
                        $"No Flights Available",
                        $"There are no flights for the {Globals.AirframeNames[airframe]} airframe in the file.");
                    return false;                                           // exit, no matching flights
                }
                else if (importer.HasFlights)
                {
                    ImportParamsDialog flightList = new(flights, importer.OptionTitles(what), importer.OptionDefaults)
                    {
                        XamlRoot = root,
                        Title = $"Select a Flight to Import {what}s From",
                        PrimaryButtonText = "Replace",
                        SecondaryButtonText = "Append",
                        CloseButtonText = "Cancel"
                    };
                    result = await flightList.ShowAsync(ContentDialogPlacement.Popup);
                    flightName = flightList.SelectedItem;
                    options = flightList.Options;
                }
                else
                {
                    result = await Utilities.Message3BDialog(root,
                        $"Import {what}s",
                        $"Would you like to replace the existing {what.ToLower()}s or append to the current list?",
                        "Replace",
                        "Append");
                }
                if (result == ContentDialogResult.None)
                {
                    return false;                                           // exit, flight selection cancelled
                }
                if (!importer.Import(navptSys, flightName, (result == ContentDialogResult.Primary), options))
                {
                    throw new Exception();                                  // exit, import error
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"NavpointUIHdlper:Import exception {ex}");
                ImportFailDialog(root, what);
                return false;                                               // exit, import error
            }
            return true;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // navpoint list export functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// present and handle a file save picker to select a .json/.cf/.miz file to export to. returns the result of
        /// FileSavePicker.PickSaveFileAsync. what parameter should be capitalized and singular (e.g., "Steerpoints").

        /// </summary>
        private static async Task<StorageFile> ExportFileSavePicker(string name, string what)
        {
            FileSavePicker picker = new()
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                SuggestedFileName = ((name.Length > 0) ? name + " " : "") + $"{what}s"
            };
            picker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });
            var hwnd = WindowNative.GetWindowHandle((Application.Current as JAFDTC.App)?.Window);
            InitializeWithWindow.Initialize(picker, hwnd);

            return await picker.PickSaveFileAsync();
        }

        /// <summary>
        /// present and handle an export fail dialog that indicates jafdtc was unable to write/export to the file.
        /// what parameter should be capitalized and singular (e.g., "Steerpoints").
        /// </summary>
        private static async void ExportFailDialog(XamlRoot root, string what)
        {
            await Utilities.Message1BDialog(root, "Export Failed", $"Unable to export {what.ToLower()}s to the file.");
        }

        /// <summary>
        /// present and handle the ui to export (serialize) all current navpoints to a .json file. what parameter
        /// should be capitalized and singular (e.g., "Steerpoints"). returns true on success, false on failure
        /// (user is notified on failures).
        /// </summary>
        public static async Task<bool> Export(XamlRoot root, string name, string json, string what)
        {
            try
            {
                StorageFile file = await ExportFileSavePicker(name, what);
                if (file != null)
                {
                    FileManager.WriteFile(file.Path, json);
                }
                return true;
            }
            catch (Exception ex)
            {
                FileManager.Log($"NavpointUIHdlper:Export exception {ex}");
                ExportFailDialog(root, what);
            }
            return false;
        }
    }
}
