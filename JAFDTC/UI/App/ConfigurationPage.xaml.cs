// ********************************************************************************************************************
//
// ConfigurationPage.xaml.cs -- ui c# for configuration page that enables editing of some configuration
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
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Storage;
using Windows.UI;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// holds information on the editor page for a section of an airframe configuration. ConfigurationPage
    /// uses this data to dynamically build the content of the configuration editor page navigation list.
    /// </summary>
    public sealed partial class ConfigEditorPageInfo : BindableObject
    {
        public string Tag { get; }

        public string Label { get; }

        public string ShortName { get; }

        public string Glyph { get; }

        public Type EditorPageType { get; }

        public Type EditorHelperType { get; }

        // this property is bound by the ConfigurationPage ui to provide the foreground color for the icon in the nav
        // list used to select the configuration.

        private Brush _editorPageIconFg;
        public Brush EditorPageIconFg
        {
            get => _editorPageIconFg;
            set => SetProperty(ref _editorPageIconFg, value);
        }

        // this property is bound by the ConfigurationPage ui to provide the foreground color for the badge in the nav
        // list used to select the configuration.

        private Brush _editorPageBadgeFg;
        public Brush EditorPageBadgeFg
        {
            get => _editorPageBadgeFg;
            set => SetProperty(ref _editorPageBadgeFg, value);
        }

        public ConfigEditorPageInfo(string tag, string label, string name, string glyph, Type pageType, Type helpType = null)
            => (Tag, Label, ShortName, Glyph,
                EditorPageType, EditorHelperType,
                EditorPageIconFg, EditorPageBadgeFg) = (tag, label, name, glyph, pageType, helpType, null, null);
    }

    // ================================================================================================================

    /// <summary>
    /// holds information on an auxiliary command from the aux list of an airframe configuration. ConfigurationPage
    /// uses this data to dynamically build the content of the configuration editor page.
    /// </summary>
    public sealed partial class ConfigAuxCommandInfo : BindableObject
    {
        public string Tag { get; }

        public string Label { get; }

        public string Glyph { get; }

        public ConfigAuxCommandInfo(string tag, string label, string glyph)
            => (Tag, Label, Glyph) = (tag, label, glyph);
    }

    // ================================================================================================================

    /// <summary>
    /// class encapsulating arguments/parameters to pass in to a system editor page.
    /// </summary>
    public sealed partial class ConfigEditorPageNavArgs
    {
        public ConfigurationPage ConfigPage { get; }

        public IConfiguration Config { get; }

        public Dictionary<string, IConfiguration> UIDtoConfigMap { get; }

        public Type EditorHelperType { get; }

        public AppBarButton BackButton { get; }

        public ConfigEditorPageNavArgs(ConfigurationPage cfgPage, IConfiguration cfg, Type type, Dictionary<string,
                                       IConfiguration> map, AppBarButton backButton)
            => (ConfigPage, Config, EditorHelperType, UIDtoConfigMap, BackButton) = (cfgPage, cfg, type, map, backButton);
    }

    // ================================================================================================================

    /// <summary>
    /// top level airframe-independed naviagation page to edit sections of the configuration. the various editors are
    /// dynamically defined through ConfigurationEditorPage instances.
    /// </summary>
    public sealed partial class ConfigurationPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// AuxCommandInvoked event is raised when an aux command is invoked. the handler has a ConfigAuxCommandInfo
        /// argument that describes the command invoked.
        /// </summary>
        public event EventHandler<ConfigAuxCommandInfo> AuxCommandInvoked;

        private JAFDTC.App CurApp { get; set; }

        private ObservableCollection<ConfigEditorPageInfo> EditorPages { get; set; }

        private ObservableCollection<ConfigAuxCommandInfo> AuxCommands { get; set; }

        private IConfiguration Config { get; set; }

        private Dictionary<string, IConfiguration> UIDtoConfigMap { get; set; }

        private IConfigurationEditor ConfigEditor { get; set; }

        private bool IsRefreshingNavList { get; set; }

        private readonly List<Window> _auxWindowList = [];

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ConfigurationPage()
        {
            CurApp = Application.Current as JAFDTC.App;
            CurApp.PropertyChanged += App_PropertyChanged;

            CurApp.Window.Closed += AppWindow_Closed;

            InitializeComponent();

            IsRefreshingNavList = false;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// registers an auxiliary  window for use by an editor page. these windows are automatically closed when
        /// the configuration editor moves off of the configuration editor page on which they were registered. closing
        /// the window via Close() will unregister an auxiliary  window.
        /// </summary>
        public void RegisterAuxWindow(Window window)
        {
            _auxWindowList.Add(window);
            window.Closed += AuxWindow_Closed;
        }

        /// <summary>
        /// close the auxiliary  window with the given tag.
        /// </summary>
        public void CloseAuxWindows(Window window = null)
        {
            if (window == null)
            {
                foreach (Window wind in _auxWindowList)
                {
                    wind.Closed -= AuxWindow_Closed;
                    wind.Close();
                }
                _auxWindowList.Clear();
            }
            else
            {
                window.Closed -= AuxWindow_Closed;
                _auxWindowList.Remove(window);
            }
        }

        /// <summary>
        /// rebuild the editor icon foregrounds to implement the link/unlink badge on the editor icon.
        /// </summary>
        private void RebuildIconForeground(ConfigEditorPageInfo info)
        {
            info.EditorPageIconFg = (Config.IsDefault(info.Tag))
                                    ? (SolidColorBrush)Resources["EditorListIconNormalBrush"]
                                    : (SolidColorBrush)Resources["EditorListIconHighlightBrush"];
            info.EditorPageBadgeFg = (Config.IsLinked(info.Tag))
                                    ? new SolidColorBrush(Color.FromArgb(0xFF, 0xB8, 0x86, 0x0B))       // DarkGoldenrod
                                    : new SolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00));      // Transparent
        }

        /// <summary>
        /// rebuild the user interface state in response to a state change.
        /// </summary>
        private void RebuildInterfaceState()
        {
            foreach (ConfigEditorPageInfo info in EditorPages)
                RebuildIconForeground(info);

            if (CurApp.IsDCSAvailable && (CurApp.DCSActiveAirframe == Config.Airframe) && !CurApp.IsDCSUploadInFlight)
            {
                uiNavListLoadToJet.IsItemClickEnabled = true;
                uiIconLoadToJet.Glyph = "\xE709";
                uiIconLoadToJet.Foreground = (SolidColorBrush)Resources["ItemEnabled"];
                uiTextLoadToJet.Foreground = (SolidColorBrush)Resources["ItemEnabled"];
            }
            else
            {
                uiNavListLoadToJet.IsItemClickEnabled = false;
                uiIconLoadToJet.Glyph = (CurApp.IsDCSUploadInFlight) ? "\xEB4C" : "\xE709";
                uiIconLoadToJet.Foreground = (SolidColorBrush)Resources["ItemDisabled"];
                uiTextLoadToJet.Foreground = (SolidColorBrush)Resources["ItemDisabled"];
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- buttons -----------------------------------------------------------------------------------------------

        /// <summary>
        /// back button click: navigate back to the configuration list, closing any open auxiliary  windows.
        /// </summary>
        private void HdrBtnBack_Click(object sender, RoutedEventArgs args)
        {
            CloseAuxWindows();
            Frame.GoBack();
        }

        /// <summary>
        /// export button: prompt for a filename, serialize the selected configuration to json, and write it to the
        /// specified file.
        /// </summary>
        private async void HdrBtnExport_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                FileSavePicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
                {
                    // SettingsIdentifier = "JAFDTC_ExportCfg",
                    CommitButtonText = "Export Configuration",
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    SuggestedFileName = "Configuration"
                };
                picker.FileTypeChoices.Add("JSON", [".json"]);

                PickFileResult resultPick = await picker.PickSaveFileAsync();
                if (resultPick != null)
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(resultPick.Path);
                    await FileIO.WriteTextAsync(file, Config.Serialize());
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"ConfigurationListPage:CmdExport_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Export Failed", "Unable to export the configuration.");
            }
        }

        /// <summary>
        /// copy click: serialize the selected system and copy it to the clipboard.
        /// </summary>
        private void CmdCopy_Click(object sender, RoutedEventArgs args)
        {
            if (uiNavListEditors.SelectedItem is ConfigEditorPageInfo info)
                General.DataToClipboard(info.Tag, Config.Serialize(info.Tag));
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void CmdPaste_Click(object sender, RoutedEventArgs args)
        {
            if (uiNavListEditors.SelectedItem is ConfigEditorPageInfo info)
            {
                ClipboardData cboard = await General.ClipboardDataAsync();
                if (Config.CanAcceptPasteForSystem(cboard?.SystemTag, info.Tag) &&
                    Config.Deserialize(cboard?.SystemTag, cboard?.Data))
                {
                    Config.Save(info);
                }
            }
        }

        /// <summary>
        /// reset click: reset the avionics component to their default values if the user confirms. updates will be
        /// persisted to storage.
        /// </summary>
        private async void CmdReset_Click(object sender, RoutedEventArgs args)
        {
            if (uiNavListEditors.SelectedItem is ConfigEditorPageInfo info)
            {
                ContentDialogResult result = await Utilities.Message2BDialog(
                    Content.XamlRoot,
                    "Reset Configuration?",
                    "Are you sure you want to reset this to avionics defaults? This action cannot be undone.",
                    "Reset"
                );
                if (result == ContentDialogResult.Primary)
                {
                    ISystem system = Config.SystemForTag(info.Tag);
                    if (system != null)
                    {
                        system.Reset();
                        Config.Save(info);
                    }
                }
            }
        }

        // ---- editor list -------------------------------------------------------------------------------------------

        /// <summary>
        /// nav left panel selection change: navigate to the corresponding editor page.
        ///
        /// NavigationEventArgs.Parameter will be the configuaraiont object conforming to IConfiguration.
        /// </summary>
        private void NavListEditors_SelectionChanged(object sender, RoutedEventArgs args)
        {
            ConfigEditorPageInfo info = (ConfigEditorPageInfo)uiNavListEditors.SelectedItem;
            if (!IsRefreshingNavList && (info != null))
            {
                ConfigEditorPageNavArgs navArgs = new(this, Config, info.EditorHelperType, UIDtoConfigMap, uiHdrBtnBack);
                ((Frame)uiNavSplitView.Content).Navigate(info.EditorPageType, navArgs);
                Config.LastSystemEdited = uiNavListEditors.SelectedIndex;

                CloseAuxWindows();
            }
        }

        /// <summary>
        /// nav list right-click: pull up and handle the context menu for the editor list.
        /// </summary>
        private async void NavListEditors_RightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            ListView listView = (ListView)sender;
            ConfigEditorPageInfo info = (ConfigEditorPageInfo)((FrameworkElement)args.OriginalSource).DataContext;
            if (uiNavListEditors.SelectedItem != info)
            {
                listView.SelectedItem = info;
                RebuildInterfaceState();
            }

            uiNavListEditorsCtxMenuFlyout.Items[0].IsEnabled = false;                   // copy
            uiNavListEditorsCtxMenuFlyout.Items[1].IsEnabled = false;                   // paste
            uiNavListEditorsCtxMenuFlyout.Items[3].IsEnabled = false;                   // reset

            // NOTE: make sure the child editor has not done further navigation (if it has, it should have disabled
            // NOTE: the header back button). this allows us to avoid rewinding the child's nav stack on reset/paste
            // NOTE: and avoids some confusion with what copy/paste means.
            //
            if (uiHdrBtnBack.IsEnabled && (info == null))
            {
                ClipboardData cboard = await General.ClipboardDataAsync();
                bool isPasteValid = (cboard != null) && Config.CanAcceptPasteForSystem(cboard.SystemTag);

                uiNavListEditorsCtxMenuFlyout.Items[1].IsEnabled = isPasteValid;        // paste
            }
            else if (uiHdrBtnBack.IsEnabled && (info != null))
            {
                ClipboardData cboard = await General.ClipboardDataAsync();
                bool isPasteValid = (cboard != null) && Config.CanAcceptPasteForSystem(cboard.SystemTag, info.Tag);
                bool isDefault = Config.IsDefault(info.Tag);

                uiNavListEditorsCtxMenuFlyout.Items[0].IsEnabled = !isDefault;          // copy
                uiNavListEditorsCtxMenuFlyout.Items[1].IsEnabled = isPasteValid;        // paste
                uiNavListEditorsCtxMenuFlyout.Items[3].IsEnabled = !isDefault;          // reset
            }
            uiNavListEditorsCtxMenuFlyout.ShowAt(listView, args.GetPosition(listView));
        }

        // ---- aux command list --------------------------------------------------------------------------------------

        /// <summary>
        /// handle an auxiliary  command by invoking the editor's HandleAuxCommand method. rebuild the contents of
        /// the aux command list if requested. raises an AuxCommandInvoked event.
        /// </summary>
        private void NavListAuxCmd_ItemClick(object sender, ItemClickEventArgs args)
        {
            if (ConfigEditor.HandleAuxCommand(this, (ConfigAuxCommandInfo)args.ClickedItem))
            {
                AuxCommands = ConfigEditor.ConfigAuxCommandInfo();
                uiNavListAuxCmd.ItemsSource = AuxCommands;
            }
        }

        /// <summary>
        /// called from a IConfigEditor.HandleAuxCommand once processing has finished if the handler wants the
        /// configuration page to raise a AuxCommandInvoked event to inform subscribers that the specified aux command
        /// has completed.
        /// </summary>
        public void RaiseAuxCommandInvoked(ConfigAuxCommandInfo cmd)
        {
            AuxCommandInvoked?.Invoke(this, cmd);
        }

        // ---- load to jet command list ------------------------------------------------------------------------------

        /// <summary>
        /// to jet button click: upload the current configuration to the jet.
        /// </summary>
        private void NavListLoadToJet_ItemClick(object sender, ItemClickEventArgs args)
        {
            CurApp.UploadConfigurationToJet(Config);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// when configuration is saved, rebuild the interface state to catch up with any changes.
        /// </summary>
        private void ConfigurationSavedHandler(object sender, ConfigurationSavedEventArgs args)
        {
            ConfigEditorPageInfo modifiedInfo = null;
            if (args.InvokedBy.GetType() == typeof(ConfigEditorPageInfo))
            {
                modifiedInfo = (ConfigEditorPageInfo)args.InvokedBy;
            }
            else
            {
                foreach (ConfigEditorPageInfo info in EditorPages)
                {
                    if (info.EditorPageType == args.InvokedBy.GetType())
                    {
                        modifiedInfo = info;
                        break;
                    }
                }
            }
            if (modifiedInfo != null)
            {
                // HACK: this is a total hack as bindings don't seem to work the way they are supposed to; e.g.,
                // HACK: as in
                // HACK: https://stackoverflow.com/questions/59473945/update-display-of-one-item-in-a-listviews-observablecollection/59506197#59506197
                // HACK: There appears to be some kinda race going on though, so put the reinsert on the dispatch
                // HACK: queue with low priority.
                //
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    RebuildIconForeground(modifiedInfo);
                    IsRefreshingNavList = true;
                    int index = uiNavListEditors.SelectedIndex;
                    EditorPages[EditorPages.IndexOf(modifiedInfo)] = modifiedInfo;
                    uiNavListEditors.SelectedIndex = index;
                    IsRefreshingNavList = false;
                });
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void App_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// on closing the main app window, close any open aux windows too.
        /// </summary>
        private void AppWindow_Closed(object sender, object args)
        {
            CloseAuxWindows();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void AuxWindow_Closed(object sender, WindowEventArgs args)
        {
            Window window = sender as Window;
            window.Closed -= AuxWindow_Closed;
            _auxWindowList.Remove(window);
        }

        /// <summary>
        /// on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        /// we are editing.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            ConfigEditorPageNavArgs navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            Config = navArgs.Config;
            UIDtoConfigMap = navArgs.UIDtoConfigMap;
            ConfigEditor = ConfigurationEditor.Factory(Config);

            Config.ConfigurationSaved += ConfigurationSavedHandler;

            EditorPages = ConfigEditor.ConfigEditorPageInfo();
            AuxCommands = ConfigEditor.ConfigAuxCommandInfo();
            uiNavListEditors.SelectedIndex = Config.LastSystemEdited;
            uiHdrTxtConfigName.Text = Config.Name;
            uiHdrTxtConfigIsFav.Visibility = (Config.IsFavorite) ? Visibility.Visible : Visibility.Collapsed;
            uiNavTxtAirframeName.Text = Globals.AirframeNames[Config.Airframe];

            // create a new frame to hold the editors and replace the split view content with the frame. setting
            // SelectedIndex above will trigger NavListEditors_SelectionChanged() and cause navigation to happen.
            //
            uiNavSplitView.Content = new Frame();

            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            CloseAuxWindows();

            Config.ConfigurationSaved -= ConfigurationSavedHandler;

            base.OnNavigatedFrom(args);
        }
    }
}
