// ********************************************************************************************************************
//
// AV8BEditWaypointListPage.xaml.cs : ui c# for harrier steerpoint list editor page
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
using JAFDTC.Models.AV8B;
using JAFDTC.Models.Base;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
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

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.AV8B
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class AV8BEditWaypointListPage : SystemEditorPageBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPage properties

        protected override SystemBase SystemConfig => null;

        protected override String SystemTag => PageHelper.SystemTag;

        protected override string SystemName => PageHelper.NavptName;

        protected override bool IsPageStateDefault => (EditNavpt.Count == 0);

        // ---- internal properties

        private IEditNavpointListPageHelper PageHelper { get; set; }

        private ObservableCollection<INavpointInfo> EditNavpt { get; set; }

        private int StartingNavptNum { get; set; }

        private bool IsClipboardValid { get; set; }

        private bool IsMarshalling { get; set; }

        private int CaptureIndex { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public AV8BEditWaypointListPage()
        {
            EditNavpt = new ObservableCollection<INavpointInfo>();

            InitializeComponent();
            InitializeBase(new PrivateEditState(), null, uiCtlLinkResetBtns);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshall data from the configuration navpoint system to the local edit state.
        /// </summary>
        protected override void CopyConfigToEditState()
        {
            IsMarshalling = true;
            PageHelper.CopyConfigToEdit(Config, EditNavpt);
            uiMiscCkbxAddMode.IsChecked = ((AV8BConfiguration)Config).WYPT.IsAppendMode;
            IsMarshalling = false;
        }

        /// <summary>
        /// marshall data from the local edit state to the configuration navpoint system.
        /// </summary>
        protected override void SaveEditStateToConfig()
        {
            IsMarshalling = true;
            if (PageHelper.CopyEditToConfig(EditNavpt, Config))
            {
                ((AV8BConfiguration)Config).WYPT.IsAppendMode = (bool)uiMiscCkbxAddMode.IsChecked;
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
            SaveEditStateToConfig();
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
            SaveEditStateToConfig();
        }

        /// <summary>
        /// update the enable state on the ui elements based on the current settings. link controls must be set up
        /// vi RebuildLinkControls() prior to calling this function.
        /// </summary>
        protected override void UpdateUICustom(bool isEditable)
        {
            JAFDTC.App curApp = Application.Current as JAFDTC.App;

            bool isFull = EditNavpt.Count >= PageHelper.NavptMaxCount;
            bool isDCSListening = curApp.IsDCSAvailable && (curApp.DCSActiveAirframe == Config.Airframe);

            Utilities.SetEnableState(uiBarAdd, isEditable && !isFull);
            Utilities.SetEnableState(uiBarEdit, isEditable && (uiNavptListView.SelectedItems.Count == 1));
            Utilities.SetEnableState(uiBarCopy, isEditable && (uiNavptListView.SelectedItems.Count > 0));
            Utilities.SetEnableState(uiBarPaste, isEditable && IsClipboardValid);
            Utilities.SetEnableState(uiBarDelete, isEditable && (uiNavptListView.SelectedItems.Count > 0));

            Utilities.SetEnableState(uiBarCapture, isEditable && isDCSListening);
            Utilities.SetEnableState(uiBarImport, isEditable);
            Utilities.SetEnableState(uiBarExport, isEditable && (EditNavpt.Count > 0));
            Utilities.SetEnableState(uiBarRenumber, isEditable && (EditNavpt.Count > 0));

            Utilities.SetEnableState(uiMiscCkbxAddMode, isEditable && (EditNavpt.Count > 0));

            uiNavptListView.CanReorderItems = isEditable;
            uiNavptListView.ReorderMode = (isEditable) ? ListViewReorderMode.Enabled : ListViewReorderMode.Disabled;
        }

        protected override void ResetConfigToDefault()
        {
            PageHelper.ResetSystem(Config);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- command bar / commands --------------------------------------------------------------------------------

        /// <summary>
        /// open navpoint button or context menu edit click: open the selected navpoint.
        /// </summary>
        private void CmdOpen_Click(object sender, RoutedEventArgs args)
        {
            if (uiNavptListView.SelectedItem is INavpointInfo navpt)
                EditNavpoint(navpt);
        }

        /// <summary>
        /// add navpoint: append a new navpoint and save the configuration.
        /// </summary>
        private void CmdAdd_Click(object sender, RoutedEventArgs args)
        {
            PageHelper.AddNavpoint(Config);
            Config.Save(this, PageHelper.SystemTag);
            CopyConfigToEditState();
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
                CopyConfigToEditState();
            }
        }

        /// <summary>
        /// delete button or context menu edit click: delete the selected waypoint(s), clear the selection, renumber
        /// the remaining waypoints, and save the updated configuration.
        /// </summary>
        private async void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
            Debug.Assert(uiNavptListView.SelectedItems.Count > 0);

            if (await NavpointUIHelper.DeleteDialog(Content.XamlRoot, PageHelper.NavptName,
                                                    uiNavptListView.SelectedItems.Count))
            {
                List<INavpointInfo> deleteList = new();
                foreach (INavpointInfo navpt in uiNavptListView.SelectedItems.Cast<INavpointInfo>())
                    deleteList.Add(navpt);
                uiNavptListView.SelectedItems.Clear();
                foreach (INavpointInfo navpt in deleteList)
                    EditNavpt.Remove(navpt);
                SaveEditStateToConfig();
            }
        }

        /// <summary>
        /// renumber button click: prompt the user for the new starting navpoint number, renumber the navpoints and
        /// save the updated configuration.
        /// </summary>
        private async void CmdRenumber_Click(object sender, RoutedEventArgs args)
        {
            // TODO: check navpoint min/max range
            int newStartNum = await NavpointUIHelper.RenumberDialog(Content.XamlRoot, "Steerpoint", 1, 700);
            if (newStartNum != -1)
            {
                StartingNavptNum = newStartNum;
                RenumberWaypoints();
                UpdateUIFromEditState();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void CmdCapture_Click(object sender, RoutedEventArgs args)
        {
            CaptureIndex = EditNavpt.Count;
            if (EditNavpt.Count > 0)
            {
                ContentDialogResult result = await Utilities.CaptureActionDialog(Content.XamlRoot, PageHelper.NavptName);
                if (result != ContentDialogResult.Primary)
                    CaptureIndex = (uiNavptListView.SelectedIndex >= 0) ? uiNavptListView.SelectedIndex : 0;
            }

            SaveEditStateToConfig();

            WyptCaptureDataRx.Instance.WyptCaptureDataReceived += CmdCapture_WyptCaptureDataReceived;
            await Utilities.CaptureMultipleDialog(Content.XamlRoot, PageHelper.NavptName);
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived -= CmdCapture_WyptCaptureDataReceived;
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
            if (await NavpointUIHelper.Import(Content.XamlRoot, AirframeTypes.AV8B,
                                              PageHelper.NavptSystem(Config), PageHelper.NavptName))
            {
                Config.Save(this, PageHelper.SystemTag);
                CopyConfigToEditState();
            }
        }

        /// <summary>
        /// export command: prompt the user for a path to save the waypoints to, then serialize the waypoints and
        /// save them to the requested file.
        /// </summary>
        private async void CmdExport_Click(object sender, RoutedEventArgs args)
        {
            await NavpointUIHelper.Export(Content.XamlRoot, Config.Name,
                                          PageHelper.ExportNavpoints(Config), PageHelper.NavptName);
        }

        // ---- navigation point list ---------------------------------------------------------------------------------

        /// <summary>
        /// steerpoint list selection change: update ui state to be consistent.
        /// </summary>
        private void NavptList_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            UpdateUIFromEditState();
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
                UpdateUIFromEditState();
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
            SaveEditStateToConfig();
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
            UpdateUIFromEditState();
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
                RenumberWaypoints();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void WindowActivatedHandler(object sender, WindowActivatedEventArgs args)
        {
            if ((args.WindowActivationState == WindowActivationState.PointerActivated) ||
                (args.WindowActivationState == WindowActivationState.CodeActivated))
            {
                UpdateUIFromEditState();
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on navigating to this page, set up our internal and ui state based on the configuration we are editing.
        ///
        /// we do not use page caching here as we're just tracking the configuration state.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            // HACK: fixes circular dependency where base.OnNavigatedTo needs PageHelper, but PageHelper needs
            // NavArgs which are built inside base.OnNavigatedTo.
            ConfigEditorPageNavArgs navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            PageHelper = (IEditNavpointListPageHelper)Activator.CreateInstance(navArgs.EditorHelperType);
            PageHelper.SetupUserInterface(navArgs.Config, uiNavptListView);

            base.OnNavigatedTo(args);

            CopyConfigToEditState();
            StartingNavptNum = (EditNavpt.Count > 0) ? EditNavpt[0].Number : 1;

            NavArgs.BackButton.IsEnabled = true;

            EditNavpt.CollectionChanged += CollectionChangedHandler;
            Clipboard.ContentChanged += ClipboardChangedHandler;
            ((Application.Current as JAFDTC.App)?.Window).Activated += WindowActivatedHandler;

            ClipboardChangedHandler(null, null);
        }

        /// <summary>
        /// on navigating from this page, tear down our internal and ui state based on the configuration we are
        /// editing.
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            EditNavpt.CollectionChanged -= CollectionChangedHandler;
            Clipboard.ContentChanged -= ClipboardChangedHandler;
            ((Application.Current as JAFDTC.App)?.Window).Activated -= WindowActivatedHandler;

            SaveEditStateToConfig();

            base.OnNavigatedFrom(args);
        }
    }
}
