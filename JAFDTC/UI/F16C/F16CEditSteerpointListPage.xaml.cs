// ********************************************************************************************************************
//
// F16CEditSteerpointListPage.xaml.cs : ui c# for viper steerpoint list editor page
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
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.STPT;
using JAFDTC.Models.Import;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using JAFDTC.Utilities.Networking;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// editor for steerpoints in the viper.
    /// </summary>
    public sealed partial class F16CEditSteerpointListPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(STPTSystem.SystemTag, "Steerpoints", "STPT", Glyphs.STPT, typeof(F16CEditSteerpointListPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditSTPT property.
        //
        private F16CConfiguration Config { get; set; }

        private STPTSystem EditSTPT { get; set; }

        private int StartingStptNum { get; set; }

        private bool IsClipboardValid { get; set; }

        private int CaptureIndex { get; set; }

        // ---- read-only properties

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditSteerpointListPage()
        {
            InitializeComponent();

            EditSTPT = new STPTSystem();

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        // marshall data between our local state and the steerpoint configuration.
        //
        private void CopyConfigToEdit()
        {
            EditSTPT.Points.Clear();
            foreach (SteerpointInfo stpt in Config.STPT.Points)
            {
                EditSTPT.Add(new SteerpointInfo(stpt));
            }
        }

        private void CopyEditToConfig(bool isPersist = false)
        {
            Config.STPT = (STPTSystem)EditSTPT.Clone();

            if (isPersist)
            {
                Config.Save(this, STPTSystem.SystemTag);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // utility
        //
        // ------------------------------------------------------------------------------------------------------------

        // launch the F16CEditSteerpointPage to edit the specified steerpoint.
        //
        private void EditSteerpoint(SteerpointInfo stpt)
        {
            NavArgs.BackButton.IsEnabled = false;
            bool isUnlinked = string.IsNullOrEmpty(Config.SystemLinkedTo(STPTSystem.SystemTag));
            Frame.Navigate(typeof(F16CEditSteerpointPage),
                           new F16CEditStptPageNavArgs(this, Config, EditSTPT.IndexOf(stpt), isUnlinked),
                           new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        // renumber steerpoints sequentially starting from StartingStptNum.
        //
        private void RenumberSteerpoints()
        {
            for (int i = 0; i < EditSTPT.Points.Count; i++)
            {
                EditSTPT.Points[i].Number = StartingStptNum + i;
            }
            CopyEditToConfig(true);
        }

        // TODO: document
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, STPTSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        // update the enable state on the ui elements based on the current settings. link controls must be set up
        // vi RebuildLinkControls() prior to calling this function.
        //
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(STPTSystem.SystemTag));

            Utilities.SetEnableState(uiBarAdd, isEditable);
            Utilities.SetEnableState(uiBarEdit, isEditable && (uiStptListView.SelectedItems.Count == 1));
            Utilities.SetEnableState(uiBarCopy, isEditable && (uiStptListView.SelectedItems.Count > 0));
            Utilities.SetEnableState(uiBarPaste, isEditable && IsClipboardValid);
            Utilities.SetEnableState(uiBarDelete, isEditable && (uiStptListView.SelectedItems.Count > 0));

            Utilities.SetEnableState(uiBarCapture, isEditable);
            Utilities.SetEnableState(uiBarImport, isEditable);
            Utilities.SetEnableState(uiBarExport, isEditable && (EditSTPT.Count > 0));
            Utilities.SetEnableState(uiBarRenumber, isEditable && (EditSTPT.Count > 0));

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);

            Utilities.SetEnableState(uiPageBtnResetAll, (EditSTPT.Count > 0));

            uiStptListView.CanReorderItems = isEditable;
            uiStptListView.ReorderMode = (isEditable) ? ListViewReorderMode.Enabled : ListViewReorderMode.Disabled;
        }

        // rebuild the state of controls on the page in response to a change in the configuration.
        //
        private void RebuildInterfaceState()
        {
            RebuildLinkControls();
            RebuildEnableState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- command bar / commands --------------------------------------------------------------------------------

        // open steerpoint button or context menu edit click: open the selected steerpoint.
        //
        private void CmdOpen_Click(object sender, RoutedEventArgs args)
        {
            if (uiStptListView.SelectedItem is SteerpointInfo stpt)
            {
                EditSteerpoint(stpt);
            }
        }

        // add steerpoint: append a new steerpoint and save the configuration.
        //
        private void CmdAdd_Click(object sender, RoutedEventArgs args)
        {
            EditSTPT.Add();
            CopyEditToConfig(true);
            RebuildInterfaceState();
        }

        // copy button or context menu copy click: serialize the selected steerpoints to json and put the text on
        // the clipboard.
        //
        private void CmdCopy_Click(object sender, RoutedEventArgs args)
        {
            General.DataToClipboard(STPTSystem.STPTListTag,
                                    JsonSerializer.Serialize(uiStptListView.SelectedItems, Configuration.JsonOptions));
        }

        // paste button: deserialize the steerpoints on the clipboard from json and append them to the end of the
        // steerpoint list.
        //
        private async void CmdPaste_Click(object sender, RoutedEventArgs args)
        {
            ClipboardData cboard = await General.ClipboardDataAsync();
            if (cboard?.SystemTag == STPTSystem.STPTListTag)
            {
                List<SteerpointInfo> list = JsonSerializer.Deserialize<List<SteerpointInfo>>(cboard.Data);
                foreach (SteerpointInfo stpt in list)
                {
                    EditSTPT.Add(stpt);
                }
                CopyEditToConfig(true);
                RebuildInterfaceState();
            }
        }

        // delete button or context menu edit click: delete the selected steerpoint(s), clear the selection, renumber
        // the remaining steerpoints, and save the updated configuration.
        //
        private async void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
            Debug.Assert(uiStptListView.SelectedItems.Count > 0);

            string title = (uiStptListView.SelectedItems.Count == 1) ? "Delete Steerpoint?" : "Delete Steerpoints?";
            string content = (uiStptListView.SelectedItems.Count == 1)
                ? "Are you sure you want to delete this steerpoint? This action cannot be undone."
                : "Are you sure you want to delete these steerpoints? This action cannot be undone.";
            ContentDialogResult result = await Utilities.Message2BDialog(Content.XamlRoot, title, content, "Delete");
            if (result == ContentDialogResult.Primary)
            {
                List<SteerpointInfo> deleteList = new();
                foreach (SteerpointInfo item in uiStptListView.SelectedItems.Cast<SteerpointInfo>())
                {
                    deleteList.Add(item);
                }
                uiStptListView.SelectedItems.Clear();
                foreach (SteerpointInfo stpt in deleteList)
                {
                    EditSTPT.Delete(stpt);
                }
                //
                // steerpoint renumbering should be handled by observer to EditSTPT changes...
                //
                CopyEditToConfig(true);
            }
        }

        // renumber button click: prompt the user for the new starting steerpoint number, renumber
        // the steerpoints and save the updated configuration.
        //
        private async void CmdRenumber_Click(object sender, RoutedEventArgs args)
        {
            GetNumberDialog dialog = new(null, null, 1, 700)
            {
                XamlRoot = Content.XamlRoot,
                Title = "Select New Starting Steerpoint Number",
                PrimaryButtonText = "Renumber",
                CloseButtonText = "Cancel",
            };
            if (await dialog.ShowAsync(ContentDialogPlacement.Popup) == ContentDialogResult.Primary)
            {
                StartingStptNum = dialog.Value;
                RenumberSteerpoints();
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void CmdCapture_Click(object sender, RoutedEventArgs args)
        {
            CaptureIndex = EditSTPT.Count;
            if (EditSTPT.Count > 0)
            {
                ContentDialogResult result = await Utilities.CaptureActionDialog(Content.XamlRoot, "Steerpoint");
                if (result != ContentDialogResult.Primary)
                {
                    CaptureIndex = (uiStptListView.SelectedIndex >= 0) ? uiStptListView.SelectedIndex : 0;
                }
            }

            CopyEditToConfig(true);

            WyptCaptureDataRx.Instance.WyptCaptureDataReceived += CmdCapture_WyptCaptureDataReceived;
            await Utilities.CaptureMultipleDialog(Content.XamlRoot, "Steerpoint");
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived -= CmdCapture_WyptCaptureDataReceived;

            CopyConfigToEdit();
            RebuildInterfaceState();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void CmdCapture_WyptCaptureDataReceived(WyptCaptureData[] wypts)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                for (int i = 0; i < wypts.Length; i++)
                {
                    if (!wypts[i].IsTarget && (CaptureIndex < EditSTPT.Count))
                    {
                        EditSTPT.Points[CaptureIndex].Name = $"WP{i + 1} DCS Capture";
                        EditSTPT.Points[CaptureIndex].Lat = wypts[i].Latitude;
                        EditSTPT.Points[CaptureIndex].Lon = wypts[i].Longitude;
                        EditSTPT.Points[CaptureIndex].Alt = wypts[i].Elevation;
                        CaptureIndex++;
                    }
                    else if (!wypts[i].IsTarget)
                    {
                        SteerpointInfo stpt = new()
                        {
                            Name = $"WP{i + 1} DCS Capture",
                            Lat = wypts[i].Latitude,
                            Lon = wypts[i].Longitude,
                            Alt = wypts[i].Elevation
                        };
                        EditSTPT.Add(stpt);
                        CaptureIndex++;
                    }
                }
            });
        }

        // TODO: document
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
                picker.FileTypeFilter.Add(".cf");
                picker.FileTypeFilter.Add(".miz");
                var hwnd = WindowNative.GetWindowHandle((Application.Current as JAFDTC.App)?.Window);
                InitializeWithWindow.Initialize(picker, hwnd);

                StorageFile file = await picker.PickSingleFileAsync();

                // ---- do the import

                IImportHelper importer;
                if ((file != null) && (file.FileType.ToLower() == ".json"))
                {
                    ContentDialogResult action = await Utilities.Message3BDialog(Content.XamlRoot,
                        "Import Steerpoints",
                        "Do you want to replace or append to the steerpoints currently in the configuration?",
                        "Replace",
                        "Append",
                        "Cancel");
                    if (action != ContentDialogResult.None)
                    {
                        string json = FileManager.ReadFile(file.Path);
                        if ((json != null) &&
                            !EditSTPT.DeserializeNavpoints(json, (action == ContentDialogResult.Primary)))
                        {
                            await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed", "Unable to read the steerpoints from the JSON file.");
                        }
                        CopyEditToConfig(true);
                    }
                    return;
                }
                else if ((file != null) && (file.FileType.ToLower() == ".cf"))
                {
                    importer = new ImportHelperCF(AirframeTypes.F16C, file.Path);
                }
                else if ((file != null) && (file.FileType.ToLower() == ".miz"))
                {
                    importer = new ImportHelperMIZ(AirframeTypes.F16C, file.Path);
                }
                else
                {
                    return;
                }

                // ---- use the helper

                GetListDialog flightList = new(importer.Flights())
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "Select a Flight to Import Steerpoints From",
                    PrimaryButtonText = "Replace",
                    SecondaryButtonText = "Append",
                    CloseButtonText = "Cancel"
                };
                ContentDialogResult result = await flightList.ShowAsync(ContentDialogPlacement.Popup);
                bool isReplace = (result == ContentDialogResult.Primary);
                if (result != ContentDialogResult.None)
                {
                    List<Dictionary<string, string>> importStpts = importer.Waypoints(flightList.SelectedItem);
                    if ((importStpts != null) && (importStpts.Count > 0))
                    {
                        if (result == ContentDialogResult.Primary)
                        {
                            EditSTPT.Reset();
                        }
                        foreach (Dictionary<string, string> importStpt in importStpts)
                        {
                            SteerpointInfo stpt = new()
                            {
                                Name = (importStpt.ContainsKey("name")) ? importStpt["name"] : "",
                                Lat = (importStpt.ContainsKey("lat")) ? importStpt["lat"] : "",
                                Lon = (importStpt.ContainsKey("lon")) ? importStpt["lon"] : "",
                                Alt = (importStpt.ContainsKey("alt")) ? importStpt["alt"] : ""
                            };
                            EditSTPT.Add(stpt);
                        }
                        CopyEditToConfig(true);
                    }
                    else
                    {
                        await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed",
                                                        "Unable to read the steerpoints from the file.");
                    }
                    RebuildInterfaceState();
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"F16CEditSteerpointListPage:CmdImport_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed", "Unable to import the steerpoints.");
            }
        }

        // export command: prompt the user for a path to save the steerpoints to, then serialize the steerpoints and
        // save them to the requested file.
        //
        private async void CmdExport_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                string filename = ((Config.Name.Length > 0) ? Config.Name + " " : "") + "Steerpoints";
                FileSavePicker picker = new()
                {
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    SuggestedFileName = filename
                };
                picker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });
                var hwnd = WindowNative.GetWindowHandle((Application.Current as JAFDTC.App)?.Window);
                InitializeWithWindow.Initialize(picker, hwnd);

                StorageFile file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    FileManager.WriteFile(file.Path, EditSTPT.SerializeNavpoints());
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"F16CEditSteerpointListPage:CmdExport_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Export Failed", "Unable to export the steerpoints.");
            }
        }

        // ---- buttons -----------------------------------------------------------------------------------------------

        // reset all button click: remove all steerpoints from the configuration and save it.
        //
        private async void PageBtnResetAll_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "Reset Steerpoints?",
                Content = "Are you sure you want to delete all steerpoints? This action cannot be undone.",
                PrimaryButtonText = "Delete All",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };
            ContentDialogResult result = await dialog.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(STPTSystem.SystemTag);
                Config.STPT.Reset();
                Config.Save(this, STPTSystem.SystemTag);
                CopyConfigToEdit();
                RebuildInterfaceState();
            }
        }

        // TODO: document
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, STPTSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                CopyEditToConfig(true);
                Config.UnlinkSystem(STPTSystem.SystemTag);
                Config.Save(this);
                CopyConfigToEdit();
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(STPTSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit();
            }
            RebuildInterfaceState();
        }

        // ---- steerpoint list ---------------------------------------------------------------------------------------

        // steerpoint list selection change: update ui state to be consistent.
        //
        private void StptList_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        // steerpoint list right click: bring up the steerpoint context menu to select from.
        //
        private void StptList_RightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            ListView listView = (ListView)sender;
            SteerpointInfo stpt = (SteerpointInfo)((FrameworkElement)args.OriginalSource).DataContext;
            if (!uiStptListView.SelectedItems.Contains(stpt))
            {
                listView.SelectedItem = stpt;
                RebuildInterfaceState();
            }

            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(STPTSystem.SystemTag));
            uiStptListCtxMenuFlyout.Items[0].IsEnabled = false;     // edit
            uiStptListCtxMenuFlyout.Items[1].IsEnabled = false;     // copy
            uiStptListCtxMenuFlyout.Items[2].IsEnabled = false;     // paste
            uiStptListCtxMenuFlyout.Items[4].IsEnabled = false;     // delete
            if (stpt == null)
            {
                uiStptListCtxMenuFlyout.Items[2].IsEnabled = isEditable && IsClipboardValid;                // paste
            }
            else
            {
                bool isNotEmpty = (uiStptListView.SelectedItems.Count >= 1);
                uiStptListCtxMenuFlyout.Items[0].IsEnabled = (uiStptListView.SelectedItems.Count == 1);     // edit
                uiStptListCtxMenuFlyout.Items[1].IsEnabled = isEditable && isNotEmpty;                      // copy
                uiStptListCtxMenuFlyout.Items[2].IsEnabled = isEditable && IsClipboardValid;                // paste
                uiStptListCtxMenuFlyout.Items[4].IsEnabled = isEditable && isNotEmpty;                      // delete
            }
            uiStptListCtxMenuFlyout.ShowAt(listView, args.GetPosition(listView));
        }

        // steertpoint list double click: open the selected steerpoint in the editor.
        //
        private void StptList_DoubleTapped(object sender, RoutedEventArgs args)
        {
            if (uiStptListView.SelectedItems.Count > 0)
            {
                EditSteerpoint((SteerpointInfo)uiStptListView.SelectedItems[0]);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // handlers
        //
        // ------------------------------------------------------------------------------------------------------------

        // check for clipboard content changes and update state as necessary.
        //
        private async void ClipboardChangedHandler(object sender, object args)
        {
            ClipboardData cboard = await General.ClipboardDataAsync();
            IsClipboardValid = ((cboard != null) && (cboard.SystemTag.StartsWith(STPTSystem.STPTListTag)));
            RebuildInterfaceState();
        }

        // when collection changes, renumber. just in case.
        //
        private void CollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs args)
        {
            // TODO: this is a bit of a hack since there's no clear way to know when a re-order via drag has completed
            // TODO: other than looking for these changes.
            //
            ObservableCollection<SteerpointInfo> list = (ObservableCollection<SteerpointInfo>)sender;
            if (list.Count > 0)
            {
                RenumberSteerpoints();
            }
        }

        // TODO: document
        private void WindowActivatedHandler(object sender, WindowActivatedEventArgs args)
        {
            if ((args.WindowActivationState == WindowActivationState.PointerActivated) ||
                (args.WindowActivationState == WindowActivationState.CodeActivated))
            {
                RebuildInterfaceState();
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        // on configuration saved, rebuild the interface state to align with the latest save (assuming we go here
        // through a CopyEditToConfig).
        //
        private void ConfigurationSavedHandler(object sender, ConfigurationSavedEventArgs args)
        {
            RebuildInterfaceState();
        }

        // on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        // we are editing.
        //
        // we do not use page caching here as we're just tracking the configuration state.
        //
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;

            Config = (F16CConfiguration)NavArgs.Config;
            StartingStptNum = (Config.STPT.Points.Count > 0) ? Config.STPT.Points[0].Number : 1;
            CopyConfigToEdit();

            NavArgs.BackButton.IsEnabled = true;

            Config.ConfigurationSaved += ConfigurationSavedHandler;
            EditSTPT.Points.CollectionChanged += CollectionChangedHandler;
            Clipboard.ContentChanged += ClipboardChangedHandler;
            ((Application.Current as JAFDTC.App)?.Window).Activated += WindowActivatedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, STPTSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            ClipboardChangedHandler(null, null);
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            Config.ConfigurationSaved -= ConfigurationSavedHandler;
            EditSTPT.Points.CollectionChanged -= CollectionChangedHandler;
            Clipboard.ContentChanged -= ClipboardChangedHandler;
            ((Application.Current as JAFDTC.App)?.Window).Activated -= WindowActivatedHandler;

            CopyEditToConfig(true);
            RebuildInterfaceState();

            base.OnNavigatedFrom(args);
        }
    }
}
