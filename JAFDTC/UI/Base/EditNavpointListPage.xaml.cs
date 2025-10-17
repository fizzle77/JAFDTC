// ********************************************************************************************************************
//
// EditNavpointListPage.cs : ui c# for general navigation point list editor page
//
// Copyright(C) 2023-2025 ilominar/raven
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
using JAFDTC.Models.DCS;
using JAFDTC.UI.App;
using JAFDTC.UI.Controls.Map;
using JAFDTC.Utilities;
using JAFDTC.Utilities.Networking;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.Json;
using Windows.ApplicationModel.DataTransfer;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// system editor page to present a list of navigation points to the user and allow individual navpoints to be
    /// edited and manipulated. this is a general-purpose class that is instatiated in combination with a
    /// IEditNavpointListHelper class to provide airframe-specific specialization.
    /// 
    /// using IEditNavpointListHelper, this class can support other navigation point systems that go beyond the basic
    /// functionality in NavpointInfoBase and NavpointSystemBase.
    /// </summary>
    public sealed partial class EditNavpointListPage : SystemEditorPageBase, IMapControlVerbHandler, IMapControlMarkerExplainer
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        public readonly static string ROUTE_NAME = "Primary";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPage properties

        protected override SystemBase SystemConfig => null;

        protected override string SystemTag => PageHelper.SystemTag;

        protected override string SystemName => PageHelper.NavptName;

        protected override bool IsPageStateDefault => (EditNavpt.Count == 0);

        // ---- internal properties

        private MapWindow _mapWindow;
        private MapWindow MapWindow
        {
            get => _mapWindow;
            set
            {
                if (_mapWindow != value)
                {
                    _mapWindow = value;
                    VerbMirror = value;
                }
            }
        }

        private IEditNavpointListPageHelper PageHelper { get; set; }

        private ObservableCollection<INavpointInfo> EditNavpt { get; set; }

        private EditNavpointPage EditNavptDetailPage { get; set; }

        private int _startingNavptNum;
        private bool _isVerbEvent;
        private bool _isClipboardValid;
        private bool _isMarshalling;
        private int _captureIndex;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public EditNavpointListPage()
        {
            EditNavpt = [ ];

            InitializeComponent();
            InitializeBase(null, null, uiCtlLinkResetBtns);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Copy data from the system configuration object to the edit object the page interacts with.
        /// </summary>
        protected override void CopyConfigToEditState()
        {
            _isMarshalling = true;
            PageHelper.CopyConfigToEdit(Config, EditNavpt);
            _isMarshalling = false;
            UpdateUIFromEditState();
        }

        /// <summary>
        /// Copy data from the edit object the page interacts with to the system configuration object and persist the
        /// updated configuration to disk.
        /// </summary>
        protected override void SaveEditStateToConfig()
        {
            _isMarshalling = true;
            if (PageHelper.CopyEditToConfig(EditNavpt, Config))
                Config.Save(this, PageHelper.SystemTag);
            _isMarshalling = false;
            UpdateUIFromEditState();
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
            this.Frame.Navigate(PageHelper.NavptEditorType,
                                PageHelper.NavptEditorArg(this, VerbMirror, Config, EditNavpt.IndexOf(navpt)),
                                new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        /// <summary>
        /// renumber waypoints sequentially starting from _startingNavptNum.
        /// </summary>
        private void RenumberWaypoints()
        {
            for (int i = 0; i < EditNavpt.Count; i++)
                EditNavpt[i].Number = _startingNavptNum + i;
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
            Utilities.SetEnableState(uiBarPaste, isEditable && _isClipboardValid);
            Utilities.SetEnableState(uiBarDelete, isEditable && (uiNavptListView.SelectedItems.Count > 0));

            Utilities.SetEnableState(uiBarCapture, isEditable && isDCSListening);
            Utilities.SetEnableState(uiBarImport, isEditable);
            Utilities.SetEnableState(uiBarMap, true);
            Utilities.SetEnableState(uiBarRenumber, isEditable && (EditNavpt.Count > 0));

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
        private async void CmdAdd_Click(object sender, RoutedEventArgs args)
        {
            Tuple<string, string> ll = await NavpointUIHelper.ProposeNewNavptLatLon(Content.XamlRoot, [.. EditNavpt ]);
            if (ll != null)
            {
                int index = PageHelper.AddNavpoint(Config);
                CopyConfigToEditState();
                EditNavpt[index].Lat = ll.Item1;
                EditNavpt[index].Lon = ll.Item2;
                SaveEditStateToConfig();

                MapMarkerInfo info = new(MapMarkerInfo.MarkerType.NAVPT, ROUTE_NAME, index + 1, ll.Item1, ll.Item2);
                VerbMirror?.MirrorVerbMarkerAdded(this, info);
                VerbMirror?.MirrorVerbMarkerSelected(this, info);
            }
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
        /// navpoint list. implicitly closes any open map window to avoid having to do the coordination/coherency
        /// thing.
        /// </summary>
        private async void CmdPaste_Click(object sender, RoutedEventArgs args)
        {
// TODO: consider doing this without closing the map window?
            MapWindow?.Close();

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
                _isMarshalling = true;

                List<int> selectedIndices = [ ];
                foreach (ItemIndexRange range in uiNavptListView.SelectedRanges)
                    for (int i = range.FirstIndex; i <= range.LastIndex; i++)
                        selectedIndices.Add(i);
                selectedIndices.Sort((a, b) => b.CompareTo(a));
                uiNavptListView.SelectedItems.Clear();
                foreach (int index in selectedIndices)
                {
                    EditNavpt.RemoveAt(index);
                    VerbMirror?.MirrorVerbMarkerDeleted(this, new(MapMarkerInfo.MarkerType.NAVPT, ROUTE_NAME, index + 1));
                }
                VerbMirror?.MirrorVerbMarkerSelected(this, new());

                _isMarshalling = false;
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
            int newStartNum = await NavpointUIHelper.RenumberDialog(Content.XamlRoot, PageHelper.NavptName, 1, 700);
            if (newStartNum != -1)
            {
                _startingNavptNum = newStartNum;
                RenumberWaypoints();
                UpdateUIFromEditState();
            }
        }

        /// <summary>
        /// capture command: start the ui to allow coordinate capture from the dcs ui.  implicitly closes any open map
        /// window to avoid having to do the coordination/coherency thing.
        /// </summary>
        private async void CmdCapture_Click(object sender, RoutedEventArgs args)
        {
            MapWindow?.Close();

            _captureIndex = EditNavpt.Count;
            if (EditNavpt.Count > 0)
            {
                ContentDialogResult result = await Utilities.CaptureActionDialog(Content.XamlRoot, PageHelper.NavptName);
                if (result != ContentDialogResult.Primary)
                    _captureIndex = (uiNavptListView.SelectedIndex >= 0) ? uiNavptListView.SelectedIndex : 0;
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
                PageHelper.CaptureNavpoints(Config, wypts, _captureIndex);
                Config.Save(this, PageHelper.SystemTag);
                CopyConfigToEditState();
            });
        }

        /// <summary>
        /// import poi command: navigate to the poi list to pull in pois. implicitly closes any open map window to
        /// avoid having to do the coordination/coherency thing.
        /// </summary>
        private void CmdImportPOIs_Click(object sender, RoutedEventArgs args)
        {
            MapWindow?.Close();

            SaveEditStateToConfig();
            NavArgs.BackButton.IsEnabled = false;
            Frame.Navigate(typeof(AddNavpointsFromPOIsPage), new AddNavpointsFromPOIsPage.NavigationArg(this, Config, PageHelper),
                new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        /// <summary>
        /// import command: start the import flow to pull in navpoints. implicitly closes any open map window to
        /// avoid having to do the coordination/coherency thing.
        /// </summary>
        private async void CmdImport_Click(object sender, RoutedEventArgs args)
        {
            MapWindow?.Close();

            if (await NavpointUIHelper.Import(Content.XamlRoot, PageHelper.AirframeType,
                                              PageHelper.NavptSystem(Config), PageHelper.NavptName))
            {
                Config.Save(this, PageHelper.SystemTag);
                CopyConfigToEditState();
            }
        }

        /// <summary>
        /// map command: if the map window is not currently open, build out the data source and so on necessary for
        /// the window and create it. otherwise, activate the window.
        /// </summary>
        private void CmdMap_Click(object sender, RoutedEventArgs args)
        {
            if (MapWindow == null)
            {
                Dictionary<string, List<INavpointInfo>> routes = new()
                {
                    [ROUTE_NAME] = [.. EditNavpt ]
                };
                MapWindow = NavpointUIHelper.OpenMap(this, PageHelper.NavptMaxCount, PageHelper.NavptCoordFmt, routes);
                MapWindow.MarkerExplainer = this;
                MapWindow.Closed += MapWindow_Closed;
                NavArgs.ConfigPage.RegisterAuxWindow(MapWindow);

                if (uiNavptListView.SelectedIndex != -1)
                    VerbMirror?.MirrorVerbMarkerSelected(this, new(MapMarkerInfo.MarkerType.ANY, ROUTE_NAME,
                                                         uiNavptListView.SelectedIndex + 1));
            }
            else
            {
                MapWindow.Activate();
            }
        }

        // ---- navigation point list ---------------------------------------------------------------------------------

        /// <summary>
        /// steerpoint list selection change: update ui state to be consistent.
        /// </summary>
        private void NavptList_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (!_isMarshalling)
            {
                ListView list = sender as ListView;
                if (!_isVerbEvent)
                    VerbMirror?.MirrorVerbMarkerSelected(this, new(MapMarkerInfo.MarkerType.ANY,
                                                                   ROUTE_NAME, list.SelectedIndex + 1));
                UpdateUIFromEditState();
            }
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
                uiNavptListCtxMenuFlyout.Items[2].IsEnabled = isEditable && _isClipboardValid;              // paste
            }
            else
            {
                bool isNotEmpty = (uiNavptListView.SelectedItems.Count >= 1);
                uiNavptListCtxMenuFlyout.Items[0].IsEnabled = (uiNavptListView.SelectedItems.Count == 1);   // edit
                uiNavptListCtxMenuFlyout.Items[1].IsEnabled = isEditable && isNotEmpty;                     // copy
                uiNavptListCtxMenuFlyout.Items[2].IsEnabled = isEditable && _isClipboardValid;              // paste
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
                EditNavpoint((INavpointInfo)uiNavptListView.SelectedItems[0]);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlMarkerExplainer
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns the display name of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayName(MapMarkerInfo info)
        {
            string name = null;
            if (info.Type == MapMarkerInfo.MarkerType.NAVPT)
            {
                if (EditNavptDetailPage != null)
                    CopyConfigToEditState();                            // just in case editor is FA, so it won't FO
                name = EditNavpt[info.TagInt - 1].Name;
                if (string.IsNullOrEmpty(name))
                    name = $"{PageHelper.NavptName} {info.TagInt}";
            }
            else if ((info.Type == MapMarkerInfo.MarkerType.DCS_CORE) ||
                     (info.Type == MapMarkerInfo.MarkerType.USER) ||
                     (info.Type == MapMarkerInfo.MarkerType.CAMPAIGN))
            {
                string[] fields = info.TagStr.Split('|');
                name = info.Type switch
                {
                    MapMarkerInfo.MarkerType.DCS_CORE => $"POI: {fields[2]}",
                    MapMarkerInfo.MarkerType.USER => $"U: {fields[2]}",
                    MapMarkerInfo.MarkerType.CAMPAIGN => $"{fields[1]}: {fields[2]}",
                    _ => throw new NotImplementedException(),
                };
            }
            return name;
        }

        /// <summary>
        /// returns the elevation of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
        {
            string elev = null;
            if (info.Type == MapMarkerInfo.MarkerType.NAVPT)
            {
                if (EditNavptDetailPage != null)
                    CopyConfigToEditState();                            // just in case editor is FA, so it won't FO
                elev = EditNavpt[info.TagInt - 1].Alt;
                if (string.IsNullOrEmpty(elev))
                    elev = "0";
                elev = $"{elev}{units}";
            }
            else if ((info.Type == MapMarkerInfo.MarkerType.DCS_CORE) ||
                     (info.Type == MapMarkerInfo.MarkerType.USER) ||
                     (info.Type == MapMarkerInfo.MarkerType.CAMPAIGN))
            {
                string[] fields = info.TagStr.Split('|');
                PointOfInterestDbQuery query = new((PointOfInterestTypeMask)(1 << (int)info.Type), fields[0], fields[1], fields[2]);
                List<PointOfInterest> pois = PointOfInterestDbase.Instance.Find(query);
                if (pois.Count == 1)
                    elev = $"{pois[0].Elevation}{units}";
            }
            return elev;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IWorldMapControlVerbHandler
        //
        // ------------------------------------------------------------------------------------------------------------

        public string VerbHandlerTag => "EditNavpointListPage";

        public IMapControlVerbMirror VerbMirror { get; set; }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerSelected(IMapControlVerbHandler sender, MapMarkerInfo info)
        {
            Debug.WriteLine($"ENLP:VerbMarkerSelected {info.Type}, {info.TagStr}, {info.TagInt}");
            if ((info.Type == MapMarkerInfo.MarkerType.UNKNOWN) || (info.TagStr != ROUTE_NAME))
            {
                _isVerbEvent = true;
                uiNavptListView.SelectedIndex = -1;
                _isVerbEvent = false;
            }
            else if (info.TagStr == ROUTE_NAME)
            {
                _isVerbEvent = true;
                uiNavptListView.SelectedIndex = info.TagInt - 1;
                _isVerbEvent = false;

                EditNavptDetailPage?.ChangeToEditNavpointAtIndex(info.TagInt - 1);
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerOpened(IMapControlVerbHandler sender, MapMarkerInfo info)
        {
            Debug.WriteLine($"ENLP:MarkerOpen {info.Type}, {info.TagStr}, {info.TagInt}");
            if (info.TagStr == ROUTE_NAME)
            {
                if (EditNavptDetailPage == null)
                    EditNavpoint(EditNavpt[info.TagInt - 1]);
                else
                    EditNavptDetailPage?.ChangeToEditNavpointAtIndex(info.TagInt - 1);
            }
// TODO: handle other types of markers (user pois?)
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerMoved(IMapControlVerbHandler sender, MapMarkerInfo info)
        {
            Debug.WriteLine($"ENLP:VerbMarkerMoved {info.Type}, {info.TagStr}, {info.TagInt}, {info.Lat}, {info.Lon}");
            if (info.TagStr == ROUTE_NAME)
            {
                EditNavpt[info.TagInt - 1].Lat = info.Lat;
                EditNavpt[info.TagInt - 1].Lon = info.Lon;
                SaveEditStateToConfig();

                EditNavptDetailPage?.CopyConfigToEditIfEditingNavpointAtIndex(info.TagInt - 1);
            }
// TODO: handle other types of markers (user pois?)
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerAdded(IMapControlVerbHandler sender, MapMarkerInfo info)
        {
            Debug.WriteLine($"ENLP:VerbMarkerAdded {info.Type}, {info.TagStr}, {info.TagInt}, {info.Lat}, {info.Lon}");
            if (info.TagStr == ROUTE_NAME)
            {
                int index = PageHelper.AddNavpoint(Config, info.TagInt - 1);
                CopyConfigToEditState();
                EditNavpt[index].Lat = info.Lat;
                EditNavpt[index].Lon = info.Lon;
                SaveEditStateToConfig();

                RenumberWaypoints();
                UpdateUIFromEditState();
            }
// TODO: handle other types of markers (user pois?)
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerDeleted(IMapControlVerbHandler sender, MapMarkerInfo info)
        {
            Debug.WriteLine($"ENLP:VerbMarkerDeleted {info.Type}, {info.TagStr}, {info.TagInt}");
            if (info.TagStr == ROUTE_NAME)
            {
                uiNavptListView.SelectedIndex = -1;

                EditNavpt.RemoveAt(info.TagInt - 1);
                SaveEditStateToConfig();

                EditNavptDetailPage?.CancelIfEditingNavpointAtIndex(info.TagInt - 1);

                RenumberWaypoints();
                UpdateUIFromEditState();
            }
// TODO: handle other types of markers (user pois?)
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
            _isClipboardValid = ((cboard != null) && (cboard.SystemTag.StartsWith(PageHelper.NavptListTag)));
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
            if (!_isMarshalling && (list.Count > 0))
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

        /// <summary>
        /// TODO: document
        /// </summary>
        private void MapWindow_Closed(object sender, WindowEventArgs args)
        {
            MapWindow.Closed -= MapWindow_Closed;
            MapWindow = null;
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
            // HACK: NavArgs which are built inside base.OnNavigatedTo.
            ConfigEditorPageNavArgs navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            PageHelper = (IEditNavpointListPageHelper)Activator.CreateInstance(navArgs.EditorHelperType);
            PageHelper.SetupUserInterface(navArgs.Config, uiNavptListView);

            base.OnNavigatedTo(args);

            EditNavptDetailPage = null;

            _startingNavptNum = (EditNavpt.Count > 0) ? EditNavpt[0].Number : 1;

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
            // we can navigate from here by pushing to a navpoint detail editor (EditNavpointPage) or by pushing
            // to a add poi list page (AddNavpointsFromPOIsPage). we use EditNavptDetailPage to determine when
            // we have pushed the former and not the latter so we can correctly work with any map window.
            //
            EditNavptDetailPage = args.Content as EditNavpointPage;

            EditNavpt.CollectionChanged -= CollectionChangedHandler;
            Clipboard.ContentChanged -= ClipboardChangedHandler;
            ((Application.Current as JAFDTC.App)?.Window).Activated -= WindowActivatedHandler;

            SaveEditStateToConfig();

            base.OnNavigatedFrom(args);
        }
    }
}
