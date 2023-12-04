// ********************************************************************************************************************
//
// EditNavpointListPage.cs : ui c# for general navigation point list editor page
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
using JAFDTC.Models.Base;
using JAFDTC.Models.Import;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
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

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// page to present a list of navigation points to the user and allow individual navpoints to be edited and
    /// manipulated. this is a general-purpose class that is instatiated in combination with a IEditNavpointListHelper
    /// class to provide airframe-specific specialization.
    /// 
    /// using IEditNavpointListHelper, this class can support other navigation point systems that go beyond the basic
    /// functionality in NavpointInfoBase and NavpointSystemBase.
    /// </summary>
    public sealed partial class EditNavpointListPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        private IEditNavpointListPageHelper NavHelper { get; set; }

        // NOTE: changes to the Config object here may only occur through the marshall methods. bindings to and edits
        // NOTE: by the ui are always directed at the EditWYPT property.
        //
        private IConfiguration Config { get; set; }

        private ObservableCollection<INavpointInfo> EditNavpt { get; set; }

        private int StartingNavptNum { get; set; }

        private bool IsClipboardValid { get; set; }

        private bool IsMarshalling { get; set; }

        // ---- read-only properties

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public EditNavpointListPage()
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

        // marshall data between our local state and the steerpoint configuration.
        //
        private void CopyConfigToEdit()
        {
            IsMarshalling = true;
            NavHelper.CopyConfigToEdit(Config, EditNavpt);
            IsMarshalling = false;
        }

        private void CopyEditToConfig(bool isPersist = false)
        {
            IsMarshalling = true;
            if (NavHelper.CopyEditToConfig(EditNavpt, Config) && isPersist)
            {
                Config.Save(this, NavHelper.SystemTag);
            }
            IsMarshalling = false;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // utility
        //
        // ------------------------------------------------------------------------------------------------------------

        // launch the proper detail page to edit the specified navpoint.
        //
        private void EditNavpoint(INavpointInfo navpt)
        {
            CopyEditToConfig(true);
            NavArgs.BackButton.IsEnabled = false;
            this.Frame.Navigate(NavHelper.NavptEditorType, NavHelper.NavptEditorArg(this, Config, EditNavpt.IndexOf(navpt)),
                                new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        // renumber waypoints sequentially starting from StartingNavptNum.
        //
        private void RenumberWaypoints()
        {
            for (int i = 0; i < EditNavpt.Count; i++)
            {
                EditNavpt[i].Number = StartingNavptNum + i;
            }
            CopyEditToConfig(true);
        }

        // TODO: document
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, NavHelper.SystemTag, NavArgs.UIDtoConfigMap, uiPageBtnTxtLink, uiPageTxtLink);
        }

        // update the enable state on the ui elements based on the current settings. link controls must be set up
        // vi RebuildLinkControls() prior to calling this function.
        //
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(NavHelper.SystemTag));

            Utilities.SetEnableState(uiBarAdd, isEditable);
            Utilities.SetEnableState(uiBarEdit, isEditable && (uiNavptListView.SelectedItems.Count == 1));
            Utilities.SetEnableState(uiBarCopy, isEditable && (uiNavptListView.SelectedItems.Count > 0));
            Utilities.SetEnableState(uiBarPaste, isEditable && IsClipboardValid);
            Utilities.SetEnableState(uiBarDelete, isEditable && (uiNavptListView.SelectedItems.Count > 0));

            Utilities.SetEnableState(uiBarImport, isEditable && (EditNavpt.Count > 0));
            Utilities.SetEnableState(uiBarExport, isEditable && (EditNavpt.Count > 0));
            Utilities.SetEnableState(uiBarRenumber, isEditable && (EditNavpt.Count > 0));

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);

            Utilities.SetEnableState(uiPageBtnResetAll, (EditNavpt.Count > 0));

            uiNavptListView.CanReorderItems = isEditable;
            uiNavptListView.ReorderMode = (isEditable) ? ListViewReorderMode.Enabled : ListViewReorderMode.Disabled;
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

        // ---- buttons -----------------------------------------------------------------------------------------------

        // reset all button click: remove all steerpoints from the configuration and save it.
        //
        private async void PageBtnResetAll_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = $"Reset {NavHelper.NavptName}?",
                Content = $"Are you sure you want to delete all {NavHelper.NavptName.ToLower()}? This action cannot be undone.",
                PrimaryButtonText = "Delete All",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };
            if (await dialog.ShowAsync(ContentDialogPlacement.Popup) == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(NavHelper.SystemTag);
                NavHelper.ResetSystem(Config);
                Config.Save(this, NavHelper.SystemTag);
                CopyConfigToEdit();
            }
        }

        // TODO: document
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, NavHelper.SystemTag, _configNameList);
            if (selItem == null)
            {
                CopyEditToConfig(true);
                Config.UnlinkSystem(NavHelper.SystemTag);
                Config.Save(this);
                CopyConfigToEdit();
            }
            else if (selItem.Length > 0)
            {
                Config.LinkSystemTo(NavHelper.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selItem]]);
                Config.Save(this);
                CopyConfigToEdit();
            }
        }

        // ---- command bar / commands --------------------------------------------------------------------------------

        // open navpoint button or context menu edit click: open the selected navpoint.
        //
        private void CmdOpen_Click(object sender, RoutedEventArgs args)
        {
            if (uiNavptListView.SelectedItem is INavpointInfo navpt)
            {
                EditNavpoint(navpt);
            }
        }

        // add navpoint: append a new navpoint and save the configuration.
        //
        private void CmdAdd_Click(object sender, RoutedEventArgs args)
        {
            NavHelper.AddNavpoint(Config);
            Config.Save(this, NavHelper.SystemTag);
            CopyConfigToEdit();
        }

        // copy button or context menu copy click: serialize the selected navpoint to json and put the text on the
        // clipboard.
        //
        private void CmdCopy_Click(object sender, RoutedEventArgs args)
        {
            General.DataToClipboard(NavHelper.NavptListTag,
                                    JsonSerializer.Serialize(uiNavptListView.SelectedItems, Configuration.JsonOptions));
        }

        // paste button: deserialize the navpoints on the clipboard from json and append them to the end of the
        // navpoint list.
        //
        private async void CmdPaste_Click(object sender, RoutedEventArgs args)
        {
            ClipboardData cboard = await General.ClipboardDataAsync();
            if (cboard?.SystemTag == NavHelper.NavptListTag)
            {
                NavHelper.PasteNavpoints(Config, cboard.Data);
                Config.Save(this, NavHelper.SystemTag);
                CopyConfigToEdit();
            }
        }

        // delete button or context menu edit click: delete the selected waypoint(s), clear the selection, renumber
        // the remaining waypoints, and save the updated configuration.
        //
        private async void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
            Debug.Assert(uiNavptListView.SelectedItems.Count > 0);

            string title = (uiNavptListView.SelectedItems.Count == 1) ? $"Delete {NavHelper.NavptName}?"
                                                                      : $"Delete {NavHelper.NavptName}s?";
            string content = (uiNavptListView.SelectedItems.Count == 1)
                ? $"Are you sure you want to delete this {NavHelper.NavptName.ToLower()}? This action cannot be undone."
                : $"Are you sure you want to delete these {NavHelper.NavptName.ToLower()}? This action cannot be undone.";
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
            }
        }

        // renumber button click: prompt the user for the new starting navpoint number, renumber the navpoints and
        // save the updated configuration.
        //
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
                StartingNavptNum = dialog.Value;
                RenumberWaypoints();
                RebuildInterfaceState();
            }
        }

        // TODO: document
        private async void CmdImport_Click(object sender, RoutedEventArgs args)
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
                    $"Import {NavHelper.NavptName}",
                    $"Do you want to replace or append to the {NavHelper.NavptName} currently in the configuration?",
                    $"Replace",
                    $"Append",
                    $"Cancel");
                if (action != ContentDialogResult.None)
                {
                    string json = FileManager.ReadFile(file.Path);
                    if ((json != null) && !NavHelper.PasteNavpoints(Config, json, (action == ContentDialogResult.Primary)))
                    {
                        await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed",
                                                        $"Unable to read the {NavHelper.NavptName} from the JSON file.");
                    }
                    CopyEditToConfig(true);
                }
                return;
            }
            else if ((file != null) && (file.FileType.ToLower() == ".cf"))
            {
                importer = new ImportHelperCF(NavHelper.AirframeType, file.Path);
            }
            else if ((file != null) && (file.FileType.ToLower() == ".miz"))
            {
                importer = new ImportHelperMIZ(NavHelper.AirframeType, file.Path);
            }
            else
            {
                return;
            }

            // ---- use the helper

            GetListDialog flightList = new(importer.Flights())
            {
                XamlRoot = Content.XamlRoot,
                Title = $"Select a Flight to Import {NavHelper.NavptName} From",
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
                    NavHelper.ImportNavpoints(Config, importWypts, isReplace);
                    Config.Save(this, NavHelper.SystemTag);
                    CopyConfigToEdit();
                }
                else
                {
                    await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed",
                                                    $"Unable to read the {NavHelper.NavptName} from the file.");
                }
                RebuildInterfaceState();
            }
        }

        // export command: prompt the user for a path to save the waypoints to, then serialize the waypoints and
        // save them to the requested file.
        //
        private async void CmdExport_Click(object sender, RoutedEventArgs args)
        {
            string filename = ((Config.Name.Length > 0) ? Config.Name + " " : "") + NavHelper.NavptName;
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
                try
                {
                    FileManager.WriteFile(file.Path, NavHelper.ExportNavpoints(Config));
                }
                catch
                {
                    await Utilities.Message1BDialog(Content.XamlRoot, "Export Failed",
                                                    $"Unable to write the {NavHelper.NavptName} to the file.");
                }
            }
        }

        // ---- navigation point list ---------------------------------------------------------------------------------

        // steerpoint list selection change: update ui state to be consistent.
        //
        private void NavptList_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        // steerpoint list right click: bring up the steerpoint context menu to select from.
        //
        private void NavptList_RightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            ListView listView = (ListView)sender;
            INavpointInfo navpt = (INavpointInfo)((FrameworkElement)args.OriginalSource).DataContext;
            if (!uiNavptListView.SelectedItems.Contains(navpt))
            {
                listView.SelectedItem = navpt;
                RebuildInterfaceState();
            }

            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(NavHelper.SystemTag));
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

        // steertpoint list double click: open the selected steerpoint in the editor.
        //
        private void NavptList_DoubleTapped(object sender, RoutedEventArgs args)
        {
            if (uiNavptListView.SelectedItems.Count > 0)
            {
                EditNavpoint((INavpointInfo)uiNavptListView.SelectedItems[0]);
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
            IsClipboardValid = ((cboard != null) && (cboard.SystemTag.StartsWith(NavHelper.NavptListTag)));
            RebuildInterfaceState();
        }

        // when collection changes, renumber. just in case.
        //
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

        // on configuration saved, pull changes from the config if the save was done by someone else. rebuild the
        // interface state to align with the latest save (assuming we go here through a CopyEditToConfig).
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

            NavHelper = (IEditNavpointListPageHelper)Activator.CreateInstance(NavArgs.EditorHelperType);

            NavHelper.SetupUserInterface(NavArgs.Config, uiNavptListView);

            Config = NavArgs.Config;
            CopyConfigToEdit();
            StartingNavptNum = (EditNavpt.Count > 0) ? EditNavpt[0].Number : 1;

            NavArgs.BackButton.IsEnabled = true;

            Config.ConfigurationSaved += ConfigurationSavedHandler;
            EditNavpt.CollectionChanged += CollectionChangedHandler;
            Clipboard.ContentChanged += ClipboardChangedHandler;
            ((Application.Current as JAFDTC.App)?.Window).Activated += WindowActivatedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, NavHelper.SystemTag,
                                           _configNameList, _configNameToUID);

            ClipboardChangedHandler(null, null);
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }

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
