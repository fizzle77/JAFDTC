// ********************************************************************************************************************
//
// EditSimulatorDTCPage.cs : ui c# for for general radio setup editor page
//
// Copyright(C) 2025 ilominar/raven
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
using JAFDTC.Models.Base;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// object representing the user interface view of a system that can be included in the dtc tape.
    /// </summary>
    public sealed partial class DTCSystemItem : BindableObject
    {
        public string Tag { get; set; }

        public string Glyph { get; set; }

        public string Name { get; set; }

        public Boolean IsChecked { get; set; }

        public DTCSystemItem(string tag, string glyph, string name, Boolean isChecked)
            => (Tag, Glyph, Name, IsChecked) = (tag, glyph, name, isChecked);
    }

    // ================================================================================================================

    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class EditSimulatorDTCPage : SystemEditorPageBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPage properties

        protected override SystemBase SystemConfig => PageHelper.GetSystemConfig(Config);

        protected override string SystemTag => SimDTCSystem.SystemTag;

        protected override string SystemName => "DCS DTC Tape";

        protected override bool IsPageStateDefault => EditDTC.IsDefault;

        // ---- internal properties

        private IEditSimulatorDTCPageHelper PageHelper { get; set;  }

        private readonly SimDTCSystem EditDTC;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public EditSimulatorDTCPage()
        {
            EditDTC = new();

            InitializeComponent();
            InitializeBase(EditDTC, null, uiCtlLinkResetBtns, null);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshall data between the dtc configuration and our local dtc state.
        /// </summary>
        protected override void CopyConfigToEditState()
        {
            if (EditState != null)
            {
                PageHelper.CopyConfigToEdit(Config, EditDTC);
                CopyAllSettings(SettingLocation.Config, SettingLocation.Edit);
            }
            UpdateUIFromEditState();
        }

        /// <summary>
        /// marshall data between our local dtc state and the dtc configuration.
        /// </summary>
        protected override void SaveEditStateToConfig()
        {
            if ((EditState != null) && !IsUIRebuilding)
            {
                PageHelper.CopyEditToConfig(EditDTC, Config);
                CopyAllSettings(SettingLocation.Edit, SettingLocation.Config, true);
                Config.Save(this, SimDTCSystem.SystemTag);
            }
            UpdateUIFromEditState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// update the dtc output path if it is changing from what is stored in our local dtc state. if the path is
        /// changing and the old path is valid, ask if the luser wants to delete the old path.
        /// </summary>
        private async void UpdateDTCOutputPath(string newPath)
        {
            string oldPath = EditDTC.OutputPath;
            if (EditDTC.OutputPath != newPath)
            {
                EditDTC.OutputPath = newPath;
                SaveEditStateToConfig();
            }
            if ((oldPath.Length > 0) && (newPath != oldPath) && File.Exists(oldPath))
            {
                ContentDialogResult result = await Utilities.Message2BDialog(Content.XamlRoot,
                    "Save Location Changing",
                    $"You are changing the save location, would you like to delete the output previously saved at “{oldPath}”?",
                    "No",
                    "Delete"
                );
                try
                {
                    if (result == ContentDialogResult.None)
                        File.Delete(oldPath);
                }
                catch (Exception ex)
                {
                    FileManager.Log($"EditSimulatorDTCPage:UpdateDTCOutputPath exception {ex}");
                }
            }
        }

        /// <summary>
        /// rebuild the template list from the known templates and update the selection to be consistent with the
        /// edit state.
        /// </summary>
        private void RebuildTemplateList()
        {
            List<string> templates = FileManager.ListDTCTemplates(Config.Airframe);
            templates.Sort();

            uiComboTemplate.Items.Clear();
            uiComboTemplate.Items.Add($"Default {Globals.AirframeNames[Config.Airframe]} DTC Template");
            foreach (string template in templates)
                uiComboTemplate.Items.Add(template);

            int selIndex = 0;
            if (!string.IsNullOrEmpty(EditDTC.Template))
                foreach (string template in templates)
                {
                    selIndex++;
                    if (template == EditDTC.Template)
                        break;
                }
            uiComboTemplate.SelectedIndex = selIndex;
        }

        /// <summary>
        /// rebuild the enable state of the buttons in the ui based on current configuration setup.
        /// </summary>
        private void RebuildEnableState()
        {
            Utilities.SetEnableState(uiBtnDelTmplt, (uiComboTemplate.SelectedIndex > 0));
            Utilities.SetEnableState(uiBtnClearOutput, (uiValueOutput.Text.Length > 0));
        }

        /// <summary>
        /// rebuild the state of controls on the page in response to a change in the configuration.
        /// </summary>
        protected override void UpdateUICustom(bool isEditable)
        {
            RebuildTemplateList();
            RebuildEnableState();
        }

        /// <summary>
        /// reset to defaults. uncheck all of the systems buttons.
        /// </summary>
        protected override void ResetConfigToDefault()
        {
            SystemConfig.Reset();
            //
            // HACK: can't get the bindings to work for some reason, have to do this the brute force way...
            //
            List<ToggleButton> tbtns = new();
            Utilities.FindDescendantControls<ToggleButton>(tbtns, uiGridSystemItems);
            foreach (ToggleButton tbtn in tbtns)
                tbtn.IsChecked = false;
            foreach (DTCSystemItem item in uiGridSystemItems.Items.Cast<DTCSystemItem>())
                item.IsChecked = false;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- buttons -----------------------------------------------------------------------------------------------

        /// <summary>
        /// add template click: add a dcs dtc template to the known templates available for the airframe. the
        /// template file is copied into the dtc area if necessary. the selected template is not changed.
        /// </summary>
        private async void BtnAddTmplt_Click(object sender, RoutedEventArgs args)
        {
            FileOpenPicker picker = new()
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            picker.FileTypeFilter.Add(".dtc");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as JAFDTC.App)?.Window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                string name = Path.GetFileNameWithoutExtension(file.Path);
                try
                {
                    string json = FileManager.ReadFile(file.Path);
                    using (JsonDocument data = JsonDocument.Parse(json))
                        if (data.RootElement.GetProperty("type").ToString() != Globals.AirframeDTCTypes[Config.Airframe])
                            throw new Exception($"The template “{name}” is not suitable for use as a DTC" +
                                                $" template for the {Globals.AirframeShortNames[Config.Airframe]}.");

                    ContentDialogResult result = ContentDialogResult.Primary;
                    if (FileManager.IsValidDTCTemplate(Config.Airframe, name))
                    {
                        result = await Utilities.Message2BDialog(Content.XamlRoot,
                            "Template Exists",
                            $"There is already a DTC template with the name “{name}”. Would you like to replace it?",
                            "Replace"
                        );
                    }
                    if (result == ContentDialogResult.Primary)
                    {
                        FileManager.ImportDTCTemplate(Config.Airframe, file.Path);
                        RebuildTemplateList();
                    }
                }
                catch (Exception ex)
                {
                    string msg = (ex.Source == "JAFDTC") ? ex.Message
                                                         : $"The template “{name}” is not a valid DTC template.";
                    FileManager.Log($"EditSimulatorDTCPage:BtnAddTmplt_Click exception {ex}");
                    await Utilities.Message1BDialog(Content.XamlRoot, "DTC Template Import Failed", msg);
                }
            }
        }

        /// <summary>
        /// delete template click: remove the currently selected dtc template from the known templates. the
        /// template file is removed from the dtc area if the user approves. the default template is selected
        /// upon deletions.
        /// </summary>
        private async void BtnDelTmplt_Click(object sender, RoutedEventArgs args)
        {
            if (uiComboTemplate.SelectedIndex > 0)
            {
                string name = uiComboTemplate.SelectedItem as string;
                ContentDialogResult result = await Utilities.Message2BDialog(Content.XamlRoot,
                    "Delete DTC Template",
                    $"Are you sure you want to delete the {Globals.AirframeShortNames[Config.Airframe]} DTC template “{name}”?",
                    "Cancel",
                    "Delete"
                );
                if (result == ContentDialogResult.None)
                {
                    FileManager.DeleteDTCTemplate(Config.Airframe, name);
                    EditDTC.Template = "";
                    SaveEditStateToConfig();
                }
            }
        }

        /// <summary>
        /// set output click: if there is no output path selected, use the picker to specify a file for the
        /// merged tape. save the merged tape to the specified file.
        /// </summary>
        private async void BtnSetOutput_Click(object sender, RoutedEventArgs args)
        {
            bool shouldMerge = true;
            if (string.IsNullOrEmpty(EditDTC.OutputPath))
            {
                try
                {
                    FileSavePicker picker = new()
                    {
                        SuggestedStartLocation = PickerLocationId.Desktop,
                        SuggestedFileName = "JAFDTC DTC Tape.dtc"
                    };
                    picker.FileTypeChoices.Add("DTC", new List<string>() { ".dtc" });
                    var hwnd = WindowNative.GetWindowHandle((Application.Current as JAFDTC.App)?.Window);
                    InitializeWithWindow.Initialize(picker, hwnd);

                    StorageFile file = await picker.PickSaveFileAsync();
                    if (file != null)
                        UpdateDTCOutputPath(file.Path);
                    else
                        shouldMerge = false;
                }
                catch (Exception ex)
                {
                    FileManager.Log($"EditSimulatorDTCPage:BtnSetOutput_Click exception {ex}");
                    await Utilities.Message1BDialog(Content.XamlRoot, "Selection Failed", "Unable to select that file for output.");
                }
            }

            if (shouldMerge)
            {
                try
                {
                    Config.SaveMergedSimDTC(EditDTC.Template, EditDTC.OutputPath);
                    await Utilities.Message1BDialog(Content.XamlRoot, "Tape Merged",
                                                    $"Successfully generated the merged tape at “{EditDTC.OutputPath}”");
                }
                catch (Exception ex)
                {
                    FileManager.Log($"EditSimulatorDTCPage:BtnSetOutput_Click exception {ex}");
                    await Utilities.Message1BDialog(Content.XamlRoot, "Tape Merge Failed", "TODO");
                }
            }
        }

        /// <summary>
        /// clear output click: clear the output file.
        /// </summary>
        private void BtnClearOutput_Click(object sender, RoutedEventArgs args)
        {
            UpdateDTCOutputPath("");
        }

        /// <summary>
        /// system item click: change the state of the merged system item.
        /// </summary>
        private void BtnSystemItem_Click(object sender, RoutedEventArgs args)
        {
            ToggleButton tbtn = (ToggleButton)sender;
            if ((tbtn.IsChecked == true) && !EditDTC.MergedSystemTags.Contains(tbtn.Tag.ToString()))
            {
                EditDTC.MergedSystemTags.Add(tbtn.Tag.ToString());
                SaveEditStateToConfig();
            }
            else if ((tbtn.IsChecked == false) && EditDTC.MergedSystemTags.Contains((string)tbtn.Tag.ToString()))
            {
                EditDTC.MergedSystemTags.Remove(tbtn.Tag.ToString());
                SaveEditStateToConfig();
            }
        }

        // ---- combos ------------------------------------------------------------------------------------------------

        /// <summary>
        /// combo selection: update the selection for the dcs dtc template combo.
        /// </summary>
        private void ComboTemplate_SelectionChanged(object sender, RoutedEventArgs args)
        {
            if (!IsUIRebuilding)
            {
                string template = (uiComboTemplate.SelectedIndex > 0) ? uiComboTemplate.SelectedItem.ToString() : "";
                EditDTC.Template = template;
                SaveEditStateToConfig();
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        /// we are editing.
        /// 
        /// we do not use page caching here as we're just tracking the configuration state.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            ConfigEditorPageNavArgs navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            PageHelper = (IEditSimulatorDTCPageHelper)Activator.CreateInstance(navArgs.EditorHelperType);

            base.OnNavigatedTo(args);

            PageHelper.ValidateDTCSystem(Config);

            CopyConfigToEditState();

            ObservableCollection<DTCSystemItem> items = new();
            foreach (ConfigEditorPageInfo info in PageHelper.MergableSystems)
                items.Add(new DTCSystemItem(info.Tag, info.Glyph, info.Label, EditDTC.MergedSystemTags.Contains(info.Tag)));
            uiGridSystemItems.ItemsSource = items;
        }
    }
}
