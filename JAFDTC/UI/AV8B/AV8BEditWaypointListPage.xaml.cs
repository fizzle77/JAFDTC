// ********************************************************************************************************************
//
// AV8BEditWaypointListPage.xaml.cs : ui c# for harrier steerpoint list editor page
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

using JAFDTC.Models.AV8B;
using JAFDTC.Models.Base;
using JAFDTC.Models.Import;
using JAFDTC.Models;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.Utilities.Networking;
using JAFDTC.Utilities;
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
using System.IO;
using System.Linq;
using System.Text.Json;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.AV8B
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class AV8BEditWaypointListPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        private IEditNavpointListPageHelper PageHelper { get; set; }

        // NOTE: changes to the Config object here may only occur through the marshall methods. bindings to and edits
        // NOTE: by the ui are always directed at the EditWYPT property.
        //
        private AV8BConfiguration Config { get; set; }

        private ObservableCollection<INavpointInfo> EditNavpt { get; set; }

        private int StartingNavptNum { get; set; }

        private bool IsClipboardValid { get; set; }

        private bool IsMarshalling { get; set; }

        private int CaptureIndex { get; set; }

        // ---- read-only properties

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public AV8BEditWaypointListPage()
        {
            InitializeComponent();

            EditNavpt = new ObservableCollection<INavpointInfo>();

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshall data from the configuration navpoint system to the local edit state.
        /// </summary>
        private void CopyConfigToEdit()
        {
            IsMarshalling = true;
            PageHelper.CopyConfigToEdit(Config, EditNavpt);
            uiMiscCkbxAddMode.IsChecked = Config.WYPT.IsAppendMode;
            IsMarshalling = false;
        }

        /// <summary>
        /// marshall data from the local edit state to the configuration navpoint system.
        /// </summary>
        private void CopyEditToConfig(bool isPersist = false)
        {
            IsMarshalling = true;
            if (PageHelper.CopyEditToConfig(EditNavpt, Config) && isPersist)
            {
                Config.WYPT.IsAppendMode = (bool)uiMiscCkbxAddMode.IsChecked;
                Config.Save(this, PageHelper.SystemTag);
            }
            IsMarshalling = false;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // utility
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// launch the proper detail page to edit the specified navpoint.
        /// </summary>
        private void EditNavpoint(INavpointInfo navpt)
        {
            CopyEditToConfig(true);
            NavArgs.BackButton.IsEnabled = false;
            this.Frame.Navigate(PageHelper.NavptEditorType, PageHelper.NavptEditorArg(this, Config, EditNavpt.IndexOf(navpt)),
                                new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        /// <summary>
        /// renumber waypoints sequentially starting from StartingNavptNum.
        /// </summary>
        private void RenumberWaypoints()
        {
            for (int i = 0; i < EditNavpt.Count; i++)
            {
                EditNavpt[i].Number = StartingNavptNum + i;
            }
            CopyEditToConfig(true);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, PageHelper.SystemTag, NavArgs.UIDtoConfigMap, uiPageBtnTxtLink, uiPageTxtLink);
        }

        /// <summary>
        /// update the enable state on the ui elements based on the current settings. link controls must be set up
        /// vi RebuildLinkControls() prior to calling this function.
        /// </summary>
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(PageHelper.SystemTag));
            bool isFull = EditNavpt.Count >= PageHelper.NavptMaxCount;

            Utilities.SetEnableState(uiBarAdd, isEditable && !isFull);
            Utilities.SetEnableState(uiBarEdit, isEditable && (uiNavptListView.SelectedItems.Count == 1));
            Utilities.SetEnableState(uiBarCopy, isEditable && (uiNavptListView.SelectedItems.Count > 0));
            Utilities.SetEnableState(uiBarPaste, isEditable && IsClipboardValid);
            Utilities.SetEnableState(uiBarDelete, isEditable && (uiNavptListView.SelectedItems.Count > 0));

            Utilities.SetEnableState(uiBarCapture, isEditable);
            Utilities.SetEnableState(uiBarImport, isEditable);
            Utilities.SetEnableState(uiBarExport, isEditable && (EditNavpt.Count > 0));
            Utilities.SetEnableState(uiBarRenumber, isEditable && (EditNavpt.Count > 0));

            Utilities.SetEnableState(uiMiscCkbxAddMode, isEditable && (EditNavpt.Count > 0));

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);
            Utilities.SetEnableState(uiPageBtnResetAll, (EditNavpt.Count > 0));

            uiNavptListView.CanReorderItems = isEditable;
            uiNavptListView.ReorderMode = (isEditable) ? ListViewReorderMode.Enabled : ListViewReorderMode.Disabled;
        }

        /// <summary>
        /// rebuild the state of controls on the page in response to a change in the configuration.
        /// </summary>
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

        // ---- buttons -----------------------------------------------------------------------------------------------

        /// <summary>
        /// reset all button click: remove all steerpoints from the configuration and save it.
        /// </summary>
        private async void PageBtnResetAll_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = $"Reset {PageHelper.NavptName}?",
                Content = $"Are you sure you want to delete all {PageHelper.NavptName.ToLower()}? This action cannot be undone.",
                PrimaryButtonText = "Delete All",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };
            if (await dialog.ShowAsync(ContentDialogPlacement.Popup) == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(PageHelper.SystemTag);
                PageHelper.ResetSystem(Config);
                Config.Save(this, PageHelper.SystemTag);
                CopyConfigToEdit();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, PageHelper.SystemTag, _configNameList);
            if (selItem == null)
            {
                CopyEditToConfig(true);
                Config.UnlinkSystem(PageHelper.SystemTag);
                Config.Save(this);
                CopyConfigToEdit();
            }
            else if (selItem.Length > 0)
            {
                Config.LinkSystemTo(PageHelper.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selItem]]);
                Config.Save(this);
                CopyConfigToEdit();
            }
        }

        // ---- command bar / commands --------------------------------------------------------------------------------

        /// <summary>
        /// open navpoint button or context menu edit click: open the selected navpoint.
        /// </summary>
        private void CmdOpen_Click(object sender, RoutedEventArgs args)
        {
            if (uiNavptListView.SelectedItem is INavpointInfo navpt)
            {
                EditNavpoint(navpt);
            }
        }

        /// <summary>
        /// add navpoint: append a new navpoint and save the configuration.
        /// </summary>
        private void CmdAdd_Click(object sender, RoutedEventArgs args)
        {
            PageHelper.AddNavpoint(Config);
            Config.Save(this, PageHelper.SystemTag);
            CopyConfigToEdit();
            RebuildInterfaceState();
        }

        /// <summary>
        /// copy button or context menu copy click: serialize the selected navpoint to json and put the text on the
        /// clipboard.
        /// </summary>
        private void CmdCopy_Click(object sender, RoutedEventArgs args)
        {
            General.DataToClipboard(PageHelper.NavptListTag,
                                    JsonSerializer.Serialize(uiNavptListView.SelectedItems, Configuration.JsonOptions));
        }

        /// <summary>
        /// paste button: deserialize the navpoints on the clipboard from json and append them to the end of the
        /// navpoint list.
        /// </summary>
        private async void CmdPaste_Click(object sender, RoutedEventArgs args)
        {
            // TODO: need to check paste against maximum navpoint count
            ClipboardData cboard = await General.ClipboardDataAsync();
            if (cboard?.SystemTag == PageHelper.NavptListTag)
            {
                PageHelper.PasteNavpoints(Config, cboard.Data);
                Config.Save(this, PageHelper.SystemTag);
                CopyConfigToEdit();
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// delete button or context menu edit click: delete the selected waypoint(s), clear the selection, renumber
        /// the remaining waypoints, and save the updated configuration.
        /// </summary>
        private async void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
            Debug.Assert(uiNavptListView.SelectedItems.Count > 0);

            string title = (uiNavptListView.SelectedItems.Count == 1) ? $"Delete {PageHelper.NavptName}?"
                                                                      : $"Delete {PageHelper.NavptName}s?";
            string content = (uiNavptListView.SelectedItems.Count == 1)
                ? $"Are you sure you want to delete this {PageHelper.NavptName.ToLower()}? This action cannot be undone."
                : $"Are you sure you want to delete these {PageHelper.NavptName.ToLower()}? This action cannot be undone.";
            if (await Utilities.Message2BDialog(Content.XamlRoot, title, content, "Delete") == ContentDialogResult.Primary)
            {
                List<INavpointInfo> deleteList = new();
                foreach (INavpointInfo navpt in uiNavptListView.SelectedItems.Cast<INavpointInfo>())
                {
                    deleteList.Add(navpt);
                }
                uiNavptListView.SelectedItems.Clear();
                foreach (INavpointInfo navpt in deleteList)
                {
                    int index = EditNavpt.IndexOf(navpt);
                    EditNavpt.Remove(navpt);
                }
                CopyEditToConfig(true);
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// renumber button click: prompt the user for the new starting navpoint number, renumber the navpoints and
        /// save the updated configuration.
        /// </summary>
        private async void CmdRenumber_Click(object sender, RoutedEventArgs args)
        {
            GetNumberDialog dialog = new(null, null, 1, 700)
            {
                XamlRoot = Content.XamlRoot,
                Title = "Select New Starting Waypoint Number",
                PrimaryButtonText = "Renumber",
                CloseButtonText = "Cancel",
            };
            if (await dialog.ShowAsync(ContentDialogPlacement.Popup) == ContentDialogResult.Primary)
            {
                // TODO: need to check renumbering against maximum navpoint number
                StartingNavptNum = dialog.Value;
                RenumberWaypoints();
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void CmdCapture_Click(object sender, RoutedEventArgs args)
        {
            ContentDialogResult result = ContentDialogResult.Primary;
            if (EditNavpt.Count > 0)
            {
                result = await Utilities.Message2BDialog(
                    Content.XamlRoot,
                    $"Capture {PageHelper.NavptName} from DCS",
                    $"Would you like to append coordiantes captured from DCS to the end of the " +
                    $"{PageHelper.NavptName.ToLower()} list or replace starting from the current selection?",
                    $"Append",
                    $"Replace");
            }
            if (result == ContentDialogResult.Primary)
            {
                CaptureIndex = EditNavpt.Count;
            }
            else
            {
                CaptureIndex = (uiNavptListView.SelectedIndex >= 0) ? uiNavptListView.SelectedIndex : 0;
            }

            CopyEditToConfig(true);

            WyptCaptureDataRx.Instance.WyptCaptureDataReceived += CmdCapture_WyptCaptureDataReceived;
            await Utilities.Message1BDialog(
                Content.XamlRoot,
                $"Capturing {PageHelper.NavptName} in DCS",
                $"From DCS, type [CTRL]-[SHIFT]-J to show the coordinate selection dialog, then move the crosshair over " +
                $"the desired point in the F10 map. Click “Done” below when finished.",
                $"Done");
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
                PageHelper.CaptureNavpoints(Config, wypts, CaptureIndex);
            });
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void CmdImport_Click(object sender, RoutedEventArgs args)
        {
            // TODO: need to check import against maximum navpoint count
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
                        $"Import {PageHelper.NavptName}",
                        $"Do you want to replace or append to the {PageHelper.NavptName} currently in the configuration?",
                        $"Replace",
                        $"Append",
                        $"Cancel");
                    if (action != ContentDialogResult.None)
                    {
                        string json = FileManager.ReadFile(file.Path);
                        if ((json != null) && !PageHelper.PasteNavpoints(Config, json, (action == ContentDialogResult.Primary)))
                        {
                            await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed",
                                                            $"Unable to read the {PageHelper.NavptName} from the JSON file.");
                        }
                        CopyEditToConfig(true);
                    }
                    return;
                }
                else if ((file != null) && (file.FileType.ToLower() == ".cf"))
                {
                    importer = new ImportHelperCF(PageHelper.AirframeType, file.Path);
                }
                else if ((file != null) && (file.FileType.ToLower() == ".miz"))
                {
                    importer = new ImportHelperMIZ(PageHelper.AirframeType, file.Path);
                }
                else
                {
                    return;
                }

                // ---- use the helper

                GetListDialog flightList = new(importer.Flights())
                {
                    XamlRoot = Content.XamlRoot,
                    Title = $"Select a Flight to Import {PageHelper.NavptName} From",
                    PrimaryButtonText = "Replace",
                    SecondaryButtonText = "Append",
                    CloseButtonText = "Cancel"
                };
                ContentDialogResult result = await flightList.ShowAsync(ContentDialogPlacement.Popup);
                bool isReplace = (result == ContentDialogResult.Primary);
                if (result != ContentDialogResult.None)
                {
                    List<Dictionary<string, string>> importWypts = importer.Waypoints(flightList.SelectedItem);
                    if ((importWypts != null) && (importWypts.Count > 0))
                    {
                        PageHelper.ImportNavpoints(Config, importWypts, isReplace);
                        Config.Save(this, PageHelper.SystemTag);
                        CopyConfigToEdit();
                    }
                    else
                    {
                        await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed",
                                                        $"Unable to read the {PageHelper.NavptName} from the file.");
                    }
                    RebuildInterfaceState();
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"EditNavpointListPAge:CmdImport_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed",
                                                $"Unable to import the {PageHelper.NavptName.ToLower()}s.");
            }
        }

        /// <summary>
        /// export command: prompt the user for a path to save the waypoints to, then serialize the waypoints and
        /// save them to the requested file.
        /// </summary>
        private async void CmdExport_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                string filename = ((Config.Name.Length > 0) ? Config.Name + " " : "") + PageHelper.NavptName;
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
                    FileManager.WriteFile(file.Path, PageHelper.ExportNavpoints(Config));
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"EditNavpointListPage:CmdExport_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Export Failed",
                                                $"Unable to export the {PageHelper.NavptName.ToLower()}s.");
            }
        }

        // ---- navigation point list ---------------------------------------------------------------------------------

        /// <summary>
        /// steerpoint list selection change: update ui state to be consistent.
        /// </summary>
        private void NavptList_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// steerpoint list right click: bring up the steerpoint context menu to select from.
        /// </summary>
        private void NavptList_RightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            ListView listView = (ListView)sender;
            INavpointInfo navpt = (INavpointInfo)((FrameworkElement)args.OriginalSource).DataContext;
            if (!uiNavptListView.SelectedItems.Contains(navpt))
            {
                listView.SelectedItem = navpt;
                RebuildInterfaceState();
            }

            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(PageHelper.SystemTag));
            uiNavptListCtxMenuFlyout.Items[0].IsEnabled = false;     // edit
            uiNavptListCtxMenuFlyout.Items[1].IsEnabled = false;     // copy
            uiNavptListCtxMenuFlyout.Items[2].IsEnabled = false;     // paste
            uiNavptListCtxMenuFlyout.Items[4].IsEnabled = false;     // delete
            if (navpt == null)
            {
                uiNavptListCtxMenuFlyout.Items[2].IsEnabled = isEditable && IsClipboardValid;               // paste
            }
            else
            {
                bool isNotEmpty = (uiNavptListView.SelectedItems.Count >= 1);
                uiNavptListCtxMenuFlyout.Items[0].IsEnabled = (uiNavptListView.SelectedItems.Count == 1);   // edit
                uiNavptListCtxMenuFlyout.Items[1].IsEnabled = isEditable && isNotEmpty;                     // copy
                uiNavptListCtxMenuFlyout.Items[2].IsEnabled = isEditable && IsClipboardValid;               // paste
                uiNavptListCtxMenuFlyout.Items[4].IsEnabled = isEditable && isNotEmpty;                     // delete
            }
            uiNavptListCtxMenuFlyout.ShowAt(listView, args.GetPosition(listView));
        }

        /// <summary>
        /// steertpoint list double click: open the selected steerpoint in the editor.
        /// </summary>
        private void NavptList_DoubleTapped(object sender, RoutedEventArgs args)
        {
            if (uiNavptListView.SelectedItems.Count > 0)
            {
                EditNavpoint((INavpointInfo)uiNavptListView.SelectedItems[0]);
            }
        }

        // ---- miscellaneous controls --------------------------------------------------------------------------------

        private void MiscCkbxAddMode_Clicked(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(true);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // handlers
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// check for clipboard content changes and update state as necessary.
        /// </summary>
        private async void ClipboardChangedHandler(object sender, object args)
        {
            ClipboardData cboard = await General.ClipboardDataAsync();
            IsClipboardValid = ((cboard != null) && (cboard.SystemTag.StartsWith(PageHelper.NavptListTag)));
            RebuildInterfaceState();
        }

        /// <summary>
        /// when collection changes, renumber. just in case.
        /// </summary>
        private void CollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs args)
        {
            // TODO: this is a bit of a hack since there's no clear way to know when a re-order via drag has completed
            // TODO: other than looking for these changes.
            //
            ObservableCollection<INavpointInfo> list = (ObservableCollection<INavpointInfo>)sender;
            if (!IsMarshalling && (list.Count > 0))
            {
                RenumberWaypoints();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
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

        /// <summary>
        /// on configuration saved, pull changes from the config if the save was done by someone else. rebuild the
        /// interface state to align with the latest save (assuming we go here through a CopyEditToConfig).
        /// </summary>
        private void ConfigurationSavedHandler(object sender, ConfigurationSavedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// on navigating to this page, set up our internal and ui state based on the configuration we are editing.
        ///
        /// we do not use page caching here as we're just tracking the configuration state.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;

            PageHelper = (IEditNavpointListPageHelper)Activator.CreateInstance(NavArgs.EditorHelperType);

            PageHelper.SetupUserInterface(NavArgs.Config, uiNavptListView);

            Config = (AV8BConfiguration)NavArgs.Config;
            CopyConfigToEdit();
            StartingNavptNum = (EditNavpt.Count > 0) ? EditNavpt[0].Number : 1;

            NavArgs.BackButton.IsEnabled = true;

            Config.ConfigurationSaved += ConfigurationSavedHandler;
            EditNavpt.CollectionChanged += CollectionChangedHandler;
            Clipboard.ContentChanged += ClipboardChangedHandler;
            ((Application.Current as JAFDTC.App)?.Window).Activated += WindowActivatedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, PageHelper.SystemTag,
                                           _configNameList, _configNameToUID);

            ClipboardChangedHandler(null, null);
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }

        /// <summary>
        /// on navigating from this page, tear down our internal and ui state based on the configuration we are
        /// editing.
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            Config.ConfigurationSaved -= ConfigurationSavedHandler;
            EditNavpt.CollectionChanged -= CollectionChangedHandler;
            Clipboard.ContentChanged -= ClipboardChangedHandler;
            ((Application.Current as JAFDTC.App)?.Window).Activated -= WindowActivatedHandler;

            CopyEditToConfig(true);
            RebuildInterfaceState();

            base.OnNavigatedFrom(args);
        }
    }
}
