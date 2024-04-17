// ********************************************************************************************************************
//
// F15EEditSteerpointListPage.xaml.cs : ui c# for mudhen steerpoint list editor page
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
using JAFDTC.Models.F15E;
using JAFDTC.Models.F15E.STPT;
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

namespace JAFDTC.UI.F15E
{
    /// <summary>
    /// user interface for the page that allows you to edit the steerpoint list from the mudhen steerpoint system
    /// configuration.
    /// </summary>
    public sealed partial class F15EEditSteerpointListPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(STPTSystem.SystemTag, "Steerpoints", "STPT", Glyphs.STPT, typeof(F15EEditSteerpointListPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditSTPT property.
        //
        private F15EConfiguration Config { get; set; }

        private STPTSystem EditSTPT { get; set; }

        private int StartingStptNum { get; set; }

        private bool IsClipboardValid { get; set; }

        private bool IsRebuildingUI { get; set; }

        private int CaptureIndex { get; set; }

        // ---- read-only properties

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F15EEditSteerpointListPage()
        {
            InitializeComponent();

            EditSTPT = new STPTSystem();

            IsRebuildingUI = false;

            uiCmdComboSelRoute.SelectedIndex = 0;

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshall data from the stpt system configuration to our local editable copy. as we edit outside the config,
        /// we will make a deep copy. we cannot Clone() here as the UI is tied to the specific EditSTPT instance we
        /// set up at load.
        /// </summary>
        private void CopyConfigToEdit()
        {
            // NOTE: Copnfig.STPT contains steerpoints for *all* routes intermingled. when we marshall into the edit
            // NOTE: mirror in EditSTPT, we will only grab those steerpoints that correspond to the current route.

            EditSTPT.Points.Clear();
            foreach (SteerpointInfo stpt in Config.STPT.Points)
            {
                if (stpt.Route == EditSTPT.ActiveRoute)
                {
                    EditSTPT.Add(new SteerpointInfo(stpt));
                }
            }
            for (int i = 1; i < EditSTPT.Count; i++)
            {
                if (!EditSTPT.Points[i - 1].IsTarget && EditSTPT.Points[i].IsTarget)
                {
                    EditSTPT.Points[i - 1].IsInitialUI = true;
                }
            }
        }

        /// <summary>
        /// marshall data from our local editable stpt system copy to the stpt system configuration, optionally
        /// persisting the configuration.
        /// </summary>
        private void CopyEditToConfig(bool isPersist = false)
        {
            // the route a/b/c implementation can lead to this being called when looking at a linked system. we don't
            // want to update the config in that case.
            //
            if (string.IsNullOrEmpty(Config.SystemLinkedTo(STPTSystem.SystemTag)))
            {
                // NOTE: EditSTPT only contins the steerpoints from the current route, so we need to reassemble the
                // NOTE: complete set of steerpoints from EditSTPT (for current route) and Config.STPT (for other
                // NOTE: routes).

                List<SteerpointInfo> stpts = new();
                foreach (SteerpointInfo stpt in EditSTPT.Points)
                {
                    stpts.Add(stpt);
                }
                foreach (SteerpointInfo stpt in Config.STPT.Points)
                {
                    if (stpt.Route != EditSTPT.ActiveRoute)
                    {
                        stpts.Add(stpt);
                    }
                }
                stpts.Sort((a, b) =>
                {
                    int routeCmp = a.Route.CompareTo(b.Route);
                    return (routeCmp == 0) ? a.Number.CompareTo(b.Number) : routeCmp;
                });

                Config.STPT = (STPTSystem)EditSTPT.Clone();
                Config.STPT.Points.Clear();
                foreach (SteerpointInfo stpt in stpts)
                {
                    Config.STPT.Add(new SteerpointInfo(stpt));
                }

                if (isPersist)
                {
                    Config.Save(this, STPTSystem.SystemTag);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // utility
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// launch the F15EEditSteerpointPage to edit the specified steerpoint.
        /// 
        /// NOTE: the index we hand off in the page nav args is within the Config list of steerpoints that contains all
        /// NOTE: routes. this index is given by the index of the route we are currently editing plus the index of the
        /// NOTE: steerpoint we are editing within the route.
        /// </summary>
        private void EditSteerpoint(SteerpointInfo stpt)
        {
            int iEditing = EditSTPT.IndexOf(stpt);
            int iBase;
            for (iBase = 0; (iBase < Config.STPT.Count) &&
                            (EditSTPT.Points[iEditing].Route != Config.STPT.Points[iBase].Route);
                 iBase++)
                ;
            NavArgs.BackButton.IsEnabled = false;
            bool isUnlinked = string.IsNullOrEmpty(Config.SystemLinkedTo(STPTSystem.SystemTag));
            Frame.Navigate(typeof(F15EEditSteerpointPage),
                           new F15EEditStptPageNavArgs(this, Config, iBase + iEditing, isUnlinked),
                           new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        /// <summary>
        /// renumber steerpoints sequentially starting from StartingStptNum. update the initial state to attach the
        /// appropriate glyphs in the ui.
        /// 
        /// NOTE: for the f-15e, "renumber" has the semantics of only operating within the current route. steerpoints
        /// NOTE: for other routes will not change.
        /// </summary>
        private void RenumberSteerpoints()
        {
            EditSTPT.RenumberFrom(StartingStptNum);
            for (int i = 1; i < EditSTPT.Count; i++)
            {
                EditSTPT.Points[i-1].IsInitialUI = (EditSTPT.Points[i].IsTarget && !EditSTPT.Points[i-1].IsTarget);
            }
            EditSTPT.Points[EditSTPT.Count - 1].IsInitialUI = false;
            CopyEditToConfig(true);
        }

        /// <summary>
        /// rebuild the link controls on the page based on where the configuration is linked to.
        /// </summary>
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, STPTSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        /// <summary>
        /// update the enable state on the ui elements based on the current settings. link controls must be set up
        /// via RebuildLinkControls() prior to calling this function.
        /// </summary>
        private void RebuildEnableState()
        {
            JAFDTC.App curApp = Application.Current as JAFDTC.App;

            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(STPTSystem.SystemTag));
            bool isDCSListening = curApp.IsDCSAvailable && (curApp.DCSActiveAirframe == Config.Airframe);

            Utilities.SetEnableState(uiBarAdd, isEditable);
            Utilities.SetEnableState(uiBarEdit, isEditable && (uiStptListView.SelectedItems.Count == 1));
            Utilities.SetEnableState(uiBarCopy, isEditable && (uiStptListView.SelectedItems.Count > 0));
            Utilities.SetEnableState(uiBarPaste, isEditable && IsClipboardValid);
            Utilities.SetEnableState(uiBarDelete, isEditable && (uiStptListView.SelectedItems.Count > 0));

            Utilities.SetEnableState(uiBarCapture, isEditable && isDCSListening);
            Utilities.SetEnableState(uiBarImport, isEditable);
            Utilities.SetEnableState(uiBarExport, isEditable && (EditSTPT.Count > 0));
            Utilities.SetEnableState(uiBarRenumber, isEditable && (EditSTPT.Count > 0));

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);

            Utilities.SetEnableState(uiPageBtnResetAll, (EditSTPT.Count > 0));

            uiStptListView.CanReorderItems = isEditable;
            uiStptListView.ReorderMode = (isEditable) ? ListViewReorderMode.Enabled : ListViewReorderMode.Disabled;
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

        // ---- command bar / commands --------------------------------------------------------------------------------

        /// <summary>
        /// open steerpoint button or context menu edit click: open the selected steerpoint.
        /// </summary>
        private void CmdOpen_Click(object sender, RoutedEventArgs args)
        {
            if (uiStptListView.SelectedItem is SteerpointInfo stpt)
            {
                EditSteerpoint(stpt);
            }
        }

        /// <summary>
        /// add steerpoint: append a new steerpoint and save the configuration.
        /// </summary>
        private void CmdAdd_Click(object sender, RoutedEventArgs args)
        {
            EditSTPT.Add();
            CopyEditToConfig(true);
            RebuildInterfaceState();
        }

        /// <summary>
        /// copy button or context menu copy click: serialize the selected steerpoints to json and put the text on
        /// the clipboard.
        /// </summary>
        private void CmdCopy_Click(object sender, RoutedEventArgs args)
        {
            General.DataToClipboard(STPTSystem.STPTListTag,
                                    JsonSerializer.Serialize(uiStptListView.SelectedItems, Configuration.JsonOptions));
        }

        /// <summary>
        /// paste button: deserialize the steerpoints on the clipboard from json and append them to the end of the
        /// steerpoint list.
        /// 
        /// NOTE: for the f-15e, there is really no way in the ui to select steerpoints from different routes at
        /// NOTE: the same time. for this reason, we'll always assume we're pasting whatever is on the clipboard into
        /// NOTE: the currently active route.
        /// </summary>
        private async void CmdPaste_Click(object sender, RoutedEventArgs args)
        {
            ClipboardData cboard = await General.ClipboardDataAsync();
            if (cboard?.SystemTag == STPTSystem.STPTListTag)
            {
                List<SteerpointInfo> list = JsonSerializer.Deserialize<List<SteerpointInfo>>(cboard.Data);
                foreach (SteerpointInfo stpt in list)
                {
                    stpt.Route = EditSTPT.ActiveRoute;          // force paste to target current route
                    EditSTPT.Add(stpt);
                }
                CopyEditToConfig(true);
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// delete button or context menu edit click: delete the selected steerpoint(s), clear the selection, renumber
        /// the remaining steerpoints, and save the updated configuration.
        /// </summary>
        private async void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
            Debug.Assert(uiStptListView.SelectedItems.Count > 0);

            if (await NavpointUIHelper.DeleteDialog(Content.XamlRoot, "Steerpoint", uiStptListView.SelectedItems.Count))
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

        /// <summary>
        /// renumber button click: prompt the user for the new starting steerpoint number, renumber the steerpoints
        /// and save the updated configuration.
        /// </summary>
        private async void CmdRenumber_Click(object sender, RoutedEventArgs args)
        {
            // TODO: check navpoint min/max range
            int newStartNum = await NavpointUIHelper.RenumberDialog(Content.XamlRoot, "Steerpoint", 1, 700);
            if (newStartNum != -1)
            {
                StartingStptNum = newStartNum;
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
                    if (CaptureIndex < EditSTPT.Count)
                    {
                        EditSTPT.Points[CaptureIndex].Name = $"WP{i + 1} DCS Capture";
                        EditSTPT.Points[CaptureIndex].Lat = wypts[i].Latitude;
                        EditSTPT.Points[CaptureIndex].Lon = wypts[i].Longitude;
                        EditSTPT.Points[CaptureIndex].Alt = wypts[i].Elevation;
                        EditSTPT.Points[CaptureIndex].IsTarget = wypts[i].IsTarget;
                        CaptureIndex++;
                    }
                    else
                    {
                        SteerpointInfo stpt = new()
                        {
                            Name = $"WP{i + 1} DCS Capture",
                            Route = EditSTPT.ActiveRoute,
                            Lat = wypts[i].Latitude,
                            Lon = wypts[i].Longitude,
                            Alt = wypts[i].Elevation,
                            IsTarget = wypts[i].IsTarget
                        };
                        EditSTPT.Add(stpt);
                        CaptureIndex++;
                    }
                }
            });
        }

        /// <summary>
        /// import button click: run the import ui to select file to import from and use the common import code with
        /// appropriate helpers to actually do the import.
        /// </summary>
        private async void CmdImport_Click(object sender, RoutedEventArgs args)
        {
            // TODO: should import operate on all steerpoints, or just current route?
            if (await NavpointUIHelper.Import(Content.XamlRoot, AirframeTypes.F15E, EditSTPT, "Steerpoint"))
            {
                Config.Save(this, STPTSystem.SystemTag);
                CopyConfigToEdit();
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// export command: prompt the user for a path to save the steerpoints to, then serialize the steerpoints and
        /// save them to the requested file.
        /// </summary>
        private async void CmdExport_Click(object sender, RoutedEventArgs args)
        {
            // TODO: should export operate on all steerpoints, or just current route?
            await NavpointUIHelper.Export(Content.XamlRoot, Config.Name, EditSTPT.SerializeNavpoints(), "Steerpoint");
        }

        /// <summary>
        /// route selection changed: if the route is really changing, update the user interface to only show the
        /// steerpoints assigned to the selected route.
        /// </summary>
        public void CmdComboSelRoute_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            ComboBox cbox = (ComboBox)sender;
            TextBlock tblock = (TextBlock)cbox.SelectedItem;
            if (!IsRebuildingUI && (tblock != null) && (EditSTPT.ActiveRoute != (string)tblock.Tag))
            {
                CopyEditToConfig(true);
                
                EditSTPT.ActiveRoute = (string)tblock.Tag;
                Config.STPT.ActiveRoute = (string)tblock.Tag;

                CopyConfigToEdit();
                RebuildEnableState();
            }
        }

        // ---- buttons -----------------------------------------------------------------------------------------------

        /// <summary>
        /// reset all button click: remove all steerpoints from the configuration and save it.
        /// </summary>
        private async void PageBtnResetAll_Click(object sender, RoutedEventArgs e)
        {
            if (await NavpointUIHelper.ResetDialog(Content.XamlRoot, "Steerpoint"))
            {
                EditSTPT.ActiveRoute = "A";
                Config.STPT.ActiveRoute = "A";

                IsRebuildingUI = true;
                uiCmdComboSelRoute.SelectedIndex = 0;
                IsRebuildingUI = false;

                Config.UnlinkSystem(STPTSystem.SystemTag);
                Config.STPT.Reset();
                Config.Save(this, STPTSystem.SystemTag);
                CopyConfigToEdit();
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// system link click: manage the ui to link this configuration to another steerpoint configuration.
        /// </summary>
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, STPTSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                Config.UnlinkSystem(STPTSystem.SystemTag);
                Config.Save(this);
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

        /// <summary>
        /// steerpoint list selection change: update ui state to be consistent.
        /// </summary>
        private void StptList_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// steerpoint list right click: bring up the steerpoint context menu to select from.
        /// </summary>
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

        /// <summary>
        /// steerpoint list double click: open the selected steerpoint in the editor.
        /// </summary>
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

        /// <summary>
        /// on clipboard changes, check for clipboard content changes and update state as necessary.
        /// </summary>
        private async void ClipboardChangedHandler(object sender, object args)
        {
            ClipboardData cboard = await General.ClipboardDataAsync();
            IsClipboardValid = ((cboard != null) && (cboard.SystemTag.StartsWith(STPTSystem.STPTListTag)));
            RebuildInterfaceState();
        }

        /// <summary>
        /// on collection changes, renumber the steerpoints just to be safe.
        /// </summary>
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
        /// on configuration saved, rebuild the interface state to align with the latest save (assuming we go here
        /// through a CopyEditToConfig).
        /// </summary>
        private void ConfigurationSavedHandler(object sender, ConfigurationSavedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// on aux command invoked, update the state of the editor based on the command.
        /// </summary>
        private void AuxCommandInvokedHandler(object sender, ConfigAuxCommandInfo args)
        {
        }

        /// <summary>
        /// on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        /// we are editing.
        ///
        /// we do not use page caching here as we're just tracking the configuration state.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;

            Config = (F15EConfiguration)NavArgs.Config;
            StartingStptNum = (Config.STPT.Points.Count > 0) ? Config.STPT.Points[0].Number : 1;
            CopyConfigToEdit();

            NavArgs.BackButton.IsEnabled = true;
            NavArgs.ConfigPage.AuxCommandInvoked += AuxCommandInvokedHandler;

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
            NavArgs.ConfigPage.AuxCommandInvoked -= AuxCommandInvokedHandler;
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
