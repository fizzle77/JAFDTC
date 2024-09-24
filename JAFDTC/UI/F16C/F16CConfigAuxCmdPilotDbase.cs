// ********************************************************************************************************************
//
// F16CConfigAuxCmdPilotDbase.cs : configuration auxiliary command handler for viper pilot database
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

using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.DLNK;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// TODO
    /// </summary>
    internal class F16CConfigAuxCmdPilotDbase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly App.MainWindow _window;

        private readonly XamlRoot _xamlRoot;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CConfigAuxCmdPilotDbase(App.MainWindow window, XamlRoot root)
            => (_window, _xamlRoot) = (window, root);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// run the ui for the pilot dbase editor until the user dismisses it. will export or import the database
        /// as requested. returns true if accepted, false if cancelled.
        /// </summary>
        public async void RunPilotDbEditorUI(ConfigurationPage configPage, ConfigAuxCommandInfo cmd)
        {
            List<ViperDriver> pilotDbase = F16CPilotsDbase.LoadDbase();
            while (true)
            {
                F16CPilotDbaseDialog dialog = new(_xamlRoot, pilotDbase);
                ContentDialogResult result = await dialog.ShowAsync();
                if (dialog.IsExportRequested)
                {
                    pilotDbase = new List<ViperDriver>(dialog.Pilots);
                    F16CPilotsDbase.UpdateDbase(pilotDbase);
                    PilotDbExport(dialog.SelectedDrivers);
                }
                else if (dialog.IsImportRequested)
                {
                    pilotDbase = await PilotDbImport(new List<ViperDriver>(dialog.Pilots));
                    F16CPilotsDbase.UpdateDbase(pilotDbase);
                }
                else if (result == ContentDialogResult.Primary)
                {
                    pilotDbase = new List<ViperDriver>(dialog.Pilots);
                    F16CPilotsDbase.UpdateDbase(pilotDbase);
                    configPage.RaiseAuxCommandInvoked(cmd);
                    break;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// handle imports into the pilot database. open a file picker to select the file to import from, then
        /// attempt to read the file and deserialize it to either replace or append-to the existing database.
        /// returns the new database (this will be the current database on errors).
        /// </summary>
        private async Task<List<ViperDriver>> PilotDbImport(List<ViperDriver> curDbase)
        {
            List<ViperDriver> newDbase = new(curDbase);
            try
            {
                // ---- pick file

                FileOpenPicker picker = new()
                {
                    SuggestedStartLocation = PickerLocationId.Desktop
                };
                picker.FileTypeFilter.Add(".json");
                var hwnd = WindowNative.GetWindowHandle(_window);
                InitializeWithWindow.Initialize(picker, hwnd);

                StorageFile file = await picker.PickSingleFileAsync();

                // ---- do the import

                if ((file != null) && (file.FileType.ToLower() == ".json"))
                {
                    ContentDialogResult action = await Utilities.Message3BDialog(_xamlRoot,
                        "Import Pilots",
                        "Do you want to replace or append to the pilots currently in the database?",
                        "Replace",
                        "Append",
                        "Cancel");
                    if (action != ContentDialogResult.None)
                    {
                        List<ViperDriver> fileDrivers = FileManager.LoadUserDbase<ViperDriver>(file.Path);
                        if (fileDrivers != null)
                        {
                            if (action == ContentDialogResult.Primary)
                                newDbase.Clear();
                            foreach (ViperDriver driver in fileDrivers)
                                newDbase.Add(driver);
                        }
                        else
                        {
                            await Utilities.Message1BDialog(_xamlRoot, "Import Failed", "Unable to read the pilots from the database file.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"F16CEditDLNKPage:PilotDbImport exception {ex}");
                await Utilities.Message1BDialog(_xamlRoot, "Import Failed", "Unable to import pilots.");
            }
            return newDbase;
        }

        /// <summary>
        /// handle exports from the pilot database. open a file picker to select the file to save to, then
        /// serialize the selected pilots.
        /// </summary>
        private async void PilotDbExport(List<ViperDriver> exportDrivers)
        {
            try
            {
                FileSavePicker picker = new()
                {
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    SuggestedFileName = "Viper Drivers"
                };
                picker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });
                var hwnd = WindowNative.GetWindowHandle(_window);
                InitializeWithWindow.Initialize(picker, hwnd);

                StorageFile file = await picker.PickSaveFileAsync();
                if (file != null)
                    FileManager.SaveUserDbase<ViperDriver>(file.Path, exportDrivers);
            }
            catch (Exception ex)
            {
                FileManager.Log($"F16CEditDLNKPage:PilotDbExport exception {ex}");
                await Utilities.Message1BDialog(_xamlRoot, "Export Failed", "Unable to export pilots.");
            }
        }
    }
}
