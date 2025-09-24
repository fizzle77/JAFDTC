// ********************************************************************************************************************
//
// ConfigurationListPage.xaml.cs -- ui c# for configuration list page that provides the top-level ui
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
using JAFDTC.Utilities.Networking;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// top level page in ui: list of configurations along with controls to add/delete/etc.
    /// </summary>
    public sealed partial class ConfigurationListPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // windoze interfaces & data structs
        //
        // ------------------------------------------------------------------------------------------------------------

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // the AutoSuggestBox used for configuration filtering is elsewhere in the visual tree. we, at some point,
        // will get a reference to that control and hook interesting events here.

        private AutoSuggestBox _configFilterBox;
        public AutoSuggestBox ConfigFilterBox
        {
            get => _configFilterBox;
            set
            {
                if (_configFilterBox != value)
                {
                    if (_configFilterBox != null)
                    {
                        _configFilterBox.TextChanged -= ConfigFilterBox_TextChanged;
                        _configFilterBox.QuerySubmitted -= ConfigFilterBox_QuerySubmitted;
                        _configFilterBox.SuggestionChosen -= ConfigFilterBox_SuggestionChosen;
                    }
                    _configFilterBox = value;
                    if (_configFilterBox != null)
                    {
                        _configFilterBox.TextChanged += ConfigFilterBox_TextChanged;
                        _configFilterBox.QuerySubmitted += ConfigFilterBox_QuerySubmitted;
                        _configFilterBox.SuggestionChosen += ConfigFilterBox_SuggestionChosen;
                    }
                }
            }
        }

        private JAFDTC.App CurApp { get; set; }

        private ConfigurationList ConfigList { get; set; }

        private AirframeTypes CurAirframe { get; set; }

        private IConfiguration CurConfigEditing { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsNavPending { get; set; }

        private bool IsClipboardValid { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ConfigurationListPage()
        {
            CurApp = Application.Current as JAFDTC.App;
            CurApp.PropertyChanged += AppPropertyChangedHandler;

            Clipboard.ContentChanged += ClipboardChangedHandler;

            InitializeComponent();

            CurConfigEditing = null;
            IsRebuildPending = false;
            IsNavPending = false;

            if (Settings.LastAirframeSelection >= uiBarComboAirframe.Items.Count)
            {
                Settings.LastAirframeSelection = 0;
            }

            TextBlock item = (TextBlock)uiBarComboAirframe.Items[Settings.LastAirframeSelection];
            CurAirframe = (AirframeTypes)int.Parse((string)item.Tag);

            ConfigList = new ConfigurationList(CurAirframe);

            uiBarComboAirframe.SelectedIndex = Settings.LastAirframeSelection;

            // this will rebuild interface state...
            //
            ClipboardChangedHandler(null, null);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // configuration operations
        //
        // ------------------------------------------------------------------------------------------------------------

        public delegate bool ConfigOpHandler(string name, object arg = null);

        /// <summary>
        /// ConfigOpHandler to add a new configuration for the selected airframe with the given name. the name is
        /// assumed to be legal (e.g., not a duplicate, non-zero length, etc.).
        /// 
        /// </summary>
        private bool AddConfigNameOpHandler(string name, object arg = null)
        {
            return (ConfigList.Add(CurAirframe, name) != null);
        }

        /// <summary>
        /// ConfigOpHandler to add a new configuration for the selected airframe with the given name that is cloned
        /// from a json description. the name is assumed to be legal (e.g., not a duplicate, non-zero length, etc.).
        /// </summary>
        private bool AddJSONConfigNameOpHandler(string name, object json)
        {
            return (ConfigList.AddJSON(CurAirframe, name, (string)json) != null);
        }

        /// <summary>
        /// ConfigOpHandler to rename the selected configuration to the given name. the name is assumed to be legal
        /// (e.g., not a duplicate, non-zero length, etc.).
        /// </summary>
        private bool RenameConfigNameOpHandler(string name, object arg = null)
        {
            ConfigList.Rename((IConfiguration)uiCfgListView.SelectedItem, name);
            return true;
        }

        /// <summary>
        /// ConfigOpHandler to create a new configuration that is a copy of the selected configuration with the given
        /// name. the name is assumed to be legal (e.g., not a duplicate, non-zero length, etc.).
        /// </summary>
        private bool CopyConfigNameOpHandler(string name, object arg = null)
        {
            ConfigList.Copy((IConfiguration)uiCfgListView.SelectedItem, name);
            return true;
        }

        /// <summary>
        /// check to make sure a configuration name is valid. valid names should be unique across all existing
        /// configurations for the airframe and should not be zero length. returns null if valid, a non-null error
        // message if not.
        /// </summary>
        private string ValidateConfigName(string name)
        {
            string message = null;
            if ((name == null) || (name.Length == 0))
            {
                return "The name may not be empty";
            }
            else if (ConfigList.IsNameUnique(name) == false)
            {
                message = "There is already a configuration with that name.";
            }
            return message;
        }

        /// <summary>
        /// prompt for a configuration name, validate it, and perform an operation on the configuration list (e.g.,
        /// update an existing configuration, add a new configuration, etc.). the operation uses the validated
        /// name and is provided by the ConfigOpHandler callback.
        /// </summary>
        private async void PromptForConfigName(string title, string prompt, string initialName,
                                               ConfigOpHandler handler, object cfgHandlerArg = null)
        {
            GetNameDialog nameDialog = new(initialName, prompt)
            {
                XamlRoot = Content.XamlRoot,
                Title = title
            };
            ContentDialog errDialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = "Invalid Name",
                PrimaryButtonText = "OK",
            };
            string message;

            ContentDialogResult result;
            while (true)
            {
                result = await nameDialog.ShowAsync();
                if ((result == ContentDialogResult.None) ||
                    ((message = ValidateConfigName(nameDialog.Value)) == null))
                {
                    break;
                }
                errDialog.Content = message;
                await errDialog.ShowAsync();
            }
            if (result == ContentDialogResult.Primary)
            {
                if (!handler(nameDialog.Value, cfgHandlerArg))
                {
                    await Utilities.Message1BDialog(Content.XamlRoot, "Something Went Wrong?", "That operation failed, bruh. Bummer.");
                }
                RebuildInterfaceState();
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui utility
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        public void NextConfiguration(bool isFirst)
        {
            if ((uiCfgListView.SelectedIndex == -1) && (ConfigList.ConfigsFiltered.Count > 0))
            {
                uiCfgListView.SelectedIndex = 0;
            }
            if ((uiCfgListView.SelectedIndex == -1) ||
                (!isFirst && (uiCfgListView.SelectedIndex == (ConfigList.ConfigsFiltered.Count - 1))))
            {
                General.PlayAudio("ux_error.wav");
            }
            else
            {
                if (!isFirst)
                {
                    uiCfgListView.SelectedIndex += 1;
                }
                string name = ((IConfiguration)uiCfgListView.SelectedItem).Name;
                StatusMessageTx.Send(name);
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void PreviousConfiguration(bool isFirst)
        {
            if ((uiCfgListView.SelectedIndex == -1) && (ConfigList.ConfigsFiltered.Count > 0))
            {
                uiCfgListView.SelectedIndex = 0;
            }
            if ((uiCfgListView.SelectedIndex == -1) ||(!isFirst && (uiCfgListView.SelectedIndex == 0)))
            {
                General.PlayAudio("ux_error.wav");
            }
            else
            {
                if (!isFirst)
                {
                    uiCfgListView.SelectedIndex -= 1;
                }
                string name = ((IConfiguration)uiCfgListView.SelectedItem).Name;
                StatusMessageTx.Send(name);
            }
        }

        /// <summary>
        /// navigate to editor page for configuration currently selected in the configuration list view.
        /// </summary>
        private void OpenConfigurationEditor()
        {
            if (!IsNavPending && (uiCfgListView.SelectedItem is IConfiguration config))
            {
                config.CleanupSystemLinks(new List<string>(ConfigList.UIDtoConfigMap.Keys));

                IsNavPending = true;
                CurConfigEditing = config;
                MainWindow mainWindow = (MainWindow)(Application.Current as JAFDTC.App)?.Window;
                mainWindow.SetConfigFilterBoxVisibility(Visibility.Collapsed);
                ConfigEditorPageNavArgs navArgs = new(null, config, null, ConfigList.UIDtoConfigMap, null);
                Frame.Navigate(typeof(ConfigurationPage), navArgs, new DrillInNavigationTransitionInfo());
            }
        }

        /// <summary>
        /// core dcs status ok/error icon setup.
        /// </summary>
        private void DCSStatusIconSetup(FontIcon icon, bool isOK)
        {
            icon.Glyph = (isOK) ? "\xE73E" : "\xE894";
            icon.Foreground = (SolidColorBrush)((isOK) ? Resources["StatusOKBrush"] : Resources["StatusErrorBrush"]);
        }

        /// <summary>
        /// rebuild user interface state sucha as theenable state of the command bars.
        /// </summary>
        public void RebuildInterfaceState()
        {
            if (!IsRebuildPending)
            {
                IsRebuildPending = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    DCSStatusIconSetup(uiStatsIconLua, DCSLuaManager.IsLuaInstalled());
                    DCSStatusIconSetup(uiStatsIconLaunch, CurApp.IsDCSRunning);
                    DCSStatusIconSetup(uiStatsIconExport,
                                       CurApp.IsDCSAvailable && (CurAirframe == CurApp.DCSActiveAirframe));

                    uiStatsAirframe.Text = Globals.AirframeShortNames[CurApp.DCSActiveAirframe];

                    string sep = (!string.IsNullOrEmpty(Settings.WingName) &&
                                  !string.IsNullOrEmpty(Settings.Callsign)) ? " | " : "";
                    if (!string.IsNullOrEmpty(Settings.WingName) || !string.IsNullOrEmpty(Settings.Callsign))
                    {
                        uiStatsValuePilot.Text = $"{Settings.WingName}{sep}“{Settings.Callsign}”";
                    }
                    else
                    {
                        uiStatsValuePilot.Text = "Set Your Callsign Through Settings";
                    }

                    IConfiguration config = (IConfiguration)uiCfgListView.SelectedItem;
                    bool isEnabled = (config != null);
                    bool isJetMatched = (config != null) && (CurApp.DCSActiveAirframe == config.Airframe);
                    uiBarBtnEdit.IsEnabled = isEnabled;
                    uiBarBtnCopy.IsEnabled = isEnabled;
                    uiBarBtnDelete.IsEnabled = isEnabled;
                    uiBarBtnRename.IsEnabled = isEnabled;
                    uiBarBtnExport.IsEnabled = isEnabled;
                    uiBarBtnLoadJet.IsEnabled = (isEnabled && CurApp.IsDCSAvailable && isJetMatched && 
                                                 !CurApp.IsDCSUploadInFlight);
                    uiBarBtnLoadJetIcon.Glyph = (CurApp.IsDCSUploadInFlight) ? "\xEB4C" : "\xE709";
                    uiBarBtnFocusDCS.IsEnabled = CurApp.IsDCSAvailable;
                    IsRebuildPending = false;
                });
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- configuration filter search box -----------------------------------------------------------------------

        // NOTE: this widget is not part of our layout for... reasons... we'll get a reference to it from someone
        // NOTE: else and manage it here.

        /// <summary>
        /// text changed in filter box: update the list of "suitable" items to present to the uer.
        /// </summary>
        private void ConfigFilterBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                List<string> suitableItems = ConfigList.FilterHits(sender.Text);
                if (suitableItems.Count == 0)
                {
                    suitableItems.Add("No Matching Configurations Found");
                }
                sender.ItemsSource = suitableItems;
            }
        }

        /// <summary>
        /// query submitted in filter box: filters the list of configurations by the submitted query.
        /// </summary>
        private void ConfigFilterBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            ConfigList.FilterConfigs(sender.Text);
        }

        /// <summary>
        /// query suggestion chosen from filter box: if the user selects the "no matching" sentinel, map the suggestion
        /// back onto "no filter".
        /// </summary>
        private void ConfigFilterBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem.Equals("No Matching Configurations Found"))
            {
                ConfigFilterBox.Text = "";
            }
        }

        // ---- airframe selection ------------------------------------------------------------------------------------

        /// <summary>
        /// airframe selection changed: if we're on a different airframe, use the config list model to reload with the
        /// configurations for the newly-selected airframe.
        /// </summary>
        private void BarComboAirframe_SelectionChanged(object sender, RoutedEventArgs args)
        {
            TextBlock item = (TextBlock)uiBarComboAirframe.SelectedItem;
            AirframeTypes newAirframe = (item != null) ? (AirframeTypes)int.Parse((string)item.Tag) : CurAirframe;
            if (newAirframe != CurAirframe)
            {
                ConfigFilterBox.Text = "";
                ConfigList.FilterConfigs(ConfigFilterBox.Text);
                CurAirframe = newAirframe;
                ConfigList.LoadConfigurationsForAirframe(CurAirframe);
                Settings.LastAirframeSelection = uiBarComboAirframe.SelectedIndex;
                RebuildInterfaceState();
            }
        }

        // ---- command bar / commands --------------------------------------------------------------------------------

        /// <summary>
        /// open command: open the selected configuration for editing.
        /// </summary>
        private void CmdOpen_Click(object sender, RoutedEventArgs args)
        {
            OpenConfigurationEditor();
        }

        /// <summary>
        /// add command: prompt for a name and create a new configuration with that name.
        /// </summary>
        private void CmdAdd_Click(object sender, RoutedEventArgs args)
        {
            PromptForConfigName("Create New Configuration", null, null, AddConfigNameOpHandler);
        }

        /// <summary>
        /// copy command: prompt for a name for a copy of the selected configuration and create a copy with the
        /// specified name.
        /// </summary>
        private void CmdCopy_Click(object sender, RoutedEventArgs args)
        {
            if (uiCfgListView.SelectedItem is IConfiguration config)
            {
                PromptForConfigName("Duplicate Configuration", "Enter Name of Duplicate:",
                                    $"{config.Name} Copy", CopyConfigNameOpHandler);
            }
        }

        /// <summary>
        /// paste command: check if we have a valid clipbaoard and, if so, paste it into the selected configuration.
        /// the only ui updates are at list level; these are handled by a ConfigurationSavedHandler.
        /// </summary>
        private async void CmdPaste_Click(object sender, RoutedEventArgs args)
        {
            if (IsClipboardValid && uiCfgListView.SelectedItem is IConfiguration config)
            {
                ClipboardData cboard = await General.ClipboardDataAsync();
                if (config.Deserialize(cboard?.SystemTag, cboard?.Data))
                {
                    config.Save(this);

                    // HACK: seriously msft, wtaf? why don't bindings work as they are documented? we use the ugly
                    // HACK: "Reinsert" hack to cause the ListView to reload. There appears to be some kinda race
                    // HACK: going on though, so put the reinsert on the dispatch queue with low priority.
                    //
                    DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        ConfigList.Reinsert(config);
                        uiCfgListView.SelectedItem = config;
                    });
                }
            }
        }

        /// <summary>
        /// rename command: prompt for a new name and rename the selected configuration.
        /// </summary>
        private void CmdRename_Click(object sender, RoutedEventArgs args)
        {
            if (uiCfgListView.SelectedItem is IConfiguration config)
            {
                PromptForConfigName("Rename Configuration", "Enter New Name:", config.Name, RenameConfigNameOpHandler);
            }
        }

        /// <summary>
        /// delete command: put up a confirmation dialog and delete the selected configuration if confirmed.
        /// </summary>
        private async void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
            if (uiCfgListView.SelectedItem is IConfiguration config)
            {
                ContentDialogResult result = await Utilities.Message2BDialog(
                    Content.XamlRoot,
                    "Delete Configuration?",
                    "Are you sure you want to delete this configuration? This action cannot be undone.",
                    "Delete"
                );
                if (result == ContentDialogResult.Primary)
                {
                    ConfigList.Delete(config);
                    RebuildInterfaceState();
                }
            }
        }

        /// <summary>
        /// favorite command: toggle favorite status of selected configuration.
        /// </summary>
        private void CmdFavorite_Click(object sender, RoutedEventArgs args)
        {
            IConfiguration config = (IConfiguration)uiCfgListView.SelectedItem;
            config.IsFavorite = !config.IsFavorite;
            config.Save();
            ConfigList.FilterConfigs(ConfigFilterBox.Text, true);
        }

        /// <summary>
        /// import command: prompt for a configuration name and import file, deserialize a new configuration from the
        /// specified json.
        /// </summary>
        private async void CmdImport_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                FileOpenPicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
                {
                    // SettingsIdentifier = "JAFDTC_ImportCfg"
                    CommitButtonText = "Import Configuration",
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    ViewMode = PickerViewMode.List
                };

                picker.FileTypeFilter.Add(".json");
                PickFileResult resultPick = await picker.PickSingleFileAsync();
                if (resultPick != null)
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(resultPick.Path);
                    IConfiguration config = (IConfiguration)uiCfgListView.SelectedItem;
                    string json = await FileIO.ReadTextAsync(file);
                    PromptForConfigName("Name Imported Configuration", null, null, AddJSONConfigNameOpHandler, json);
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"ConfigurationListPage:CmdImport_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed", "Unable to import into a new configuration.");
            }
        }

        /// <summary>
        /// export command: prompt for a filename, serialize the selected configuration to json, and write it to the
        /// specified file.
        /// </summary>
        private async void CmdExport_Click(object sender, RoutedEventArgs argse)
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
                picker.FileTypeChoices.Add("JSON", [ ".json" ]);

                PickFileResult resultPick = await picker.PickSaveFileAsync();
                if (resultPick != null)
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(resultPick.Path);
                    IConfiguration config = (IConfiguration)uiCfgListView.SelectedItem;
                    await FileIO.WriteTextAsync(file, config.Serialize());
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"ConfigurationListPage:CmdExport_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Export Failed", "Unable to export the configuration.");
            }
        }

        /// <summary>
        /// upload command: send the selected configuration to the jet.
        /// </summary>
        private void CmdLoadJet_Click(object sender, RoutedEventArgs args)
        {
            CurApp.UploadConfigurationToJet((IConfiguration)uiCfgListView.SelectedItem);
        }

        /// <summary>
        /// focus dcs command: bring dcs to the foreground and give it focus.
        /// </summary>
        private void CmdFocusDCS_Click(object sender, RoutedEventArgs args)
        {
            // TODO: reset cockpit "always on top" control to "not always on top" (eg, FLIR GAIN/LVL/AUTO in viper)?
            CurApp.Window.AppWindow.MoveInZOrderAtBottom();
            Process[] arrProcesses = Process.GetProcessesByName("DCS");
            if (arrProcesses.Length > 0)
            {
                IntPtr ipHwnd = arrProcesses[0].MainWindowHandle;
                Thread.Sleep(100);
                SetForegroundWindow(ipHwnd);
            }
        }

        /// <summary>
        /// points of interest command: navigate to the points of interest editor page to edit pois.
        /// </summary>
        private void CmdPoI_Click(object sender, RoutedEventArgs args)
        {
            if (!IsNavPending)
            {
                IsNavPending = true;
                MainWindow mainWindow = (MainWindow)(Application.Current as JAFDTC.App)?.Window;
                mainWindow.SetConfigFilterBoxVisibility(Visibility.Collapsed);
                Frame.Navigate(typeof(EditPointsOfInterestPage), null, new DrillInNavigationTransitionInfo());
            }
        }

        /// <summary>
        /// settings command: manage the settings dialog, updating the actual settings or handling lua support
        /// as necessary.
        /// </summary>
        private async void CmdSettings_Click(object sender, RoutedEventArgs args)
        {
            MainWindow mainWindow = (MainWindow)(Application.Current as JAFDTC.App)?.Window;

            SettingsDialog dialog = new()
            {
                XamlRoot = Content.XamlRoot,
            };
            ContentDialogResult result = await dialog.ShowAsync();
            if (dialog.IsLuaInstallRequested)
            {
                result = ContentDialogResult.Primary;
                Settings.IsSkipDCSLuaInstall = false;
                mainWindow.Sploosh(DCSLuaManager.LuaCheck());
            }
            else if (dialog.ISLuaUninstallRequested)
            {
                result = ContentDialogResult.Primary;
                await Utilities.Message1BDialog(Content.XamlRoot, "Sad Trombone", "Not yet supported, you'll have to do it the old-fashioned way...");
                foreach (string dcsPath in Settings.VersionDCSLua.Keys)
                {
                    DCSLuaManager.UninstallLua(dcsPath);
                }
                Settings.IsSkipDCSLuaInstall = false;
            }
            if (result == ContentDialogResult.Primary)
            {
                Settings.WingName = dialog.WingName;
                Settings.Callsign = dialog.Callsign;
                Settings.UploadFeedback = dialog.UploadFeedback;
                Settings.IsNavPtImportIgnoreAirframe = dialog.IsNavPtImportIgnoreAirframe;
                Settings.IsAlwaysOnTop = dialog.IsAppOnTop;
                ((OverlappedPresenter)CurApp.Window.AppWindow.Presenter).IsAlwaysOnTop = Settings.IsAlwaysOnTop;
                Settings.IsNewVersCheckDisabled = dialog.IsNewVersCheckDisabled;
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// about command: aboot box for what's this aboot?
        /// </summary>
        private async void CmdAbout_Click(object sender, RoutedEventArgs args)
        {
            string content = $"Just Another #%*@^!& DTC, {Globals.BuildJAFDTC}\n" +
                             $"\n" +
                             $"Standing on the shoulders of DCS-DTC, DCSWE, and many others. You can find " +
                             $"us on github. JAFDTC is free software released under GPLv3 (see gnu.org for " +
                             $"more), share and enjoy!\n" +
                             $"\n" +
                             $"On borrowed time until ED releases their native solution (2 weeks!)...\n" +
                             $"\n" +
                             $"A Raven & fizzle / 51st VFW fork";
            await Utilities.Message1BDialog(Content.XamlRoot, $"JAFDTC {Settings.VersionJAFDTC}", content);
        }

        // ---- configuration list ------------------------------------------------------------------------------------

        /// <summary>
        /// config list right-click: open the context menu at the click location and handle any commands.
        /// </summary>
        private void CfgListView_RightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            ListView listView = (ListView)sender;
            IConfiguration config = (IConfiguration)((FrameworkElement)args.OriginalSource).DataContext;
            int index = ConfigList.ConfigsFiltered.IndexOf(config);
            if (listView.SelectedIndex != index)
            {
                listView.SelectedIndex = ConfigList.ConfigsFiltered.IndexOf(config);
                RebuildInterfaceState();
            }
            bool isJetMatched = (config != null) && (CurApp.DCSActiveAirframe == config.Airframe);
            uiCfgListCtxMenuFlyout.Items[2].IsEnabled = IsClipboardValid;                           // paste
            if (config.IsFavorite)
            {
                uiCfgListCtxMenuFlyout.Items[5].Visibility = Visibility.Collapsed;                  // add favorite
                uiCfgListCtxMenuFlyout.Items[6].Visibility = Visibility.Visible;                    // del favorite
            }
            else
            {
                uiCfgListCtxMenuFlyout.Items[5].Visibility = Visibility.Visible;                    // add favorite
                uiCfgListCtxMenuFlyout.Items[6].Visibility = Visibility.Collapsed;                  // del favorite
            }
            uiCfgListCtxMenuFlyout.Items[9].IsEnabled = CurApp.IsDCSAvailable && isJetMatched;      // load to jet
            uiCfgListCtxMenuFlyout.ShowAt(listView, args.GetPosition(listView));
        }

        /// <summary>
        /// config list double-click: open selected configuration for editing
        /// </summary>
        private void CfgListView_DoubleClick(object sender, RoutedEventArgs args)
        {
            OpenConfigurationEditor();
        }

        /// <summary>
        /// config list selection changed: update ui state such as enabled properties.
        /// </summary>
        private void CfgListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            ListView cfgList = (ListView)sender;
            CurApp.CurrentConfig = cfgList.SelectedItem as IConfiguration;
            RebuildInterfaceState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events / handlers
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// check for clipboard content changes and update state as necessary.
        /// </summary>
        private async void ClipboardChangedHandler(object sender, object args)
        {
            IsClipboardValid = false;
            if (uiCfgListView.SelectedItem is IConfiguration config)
            {
                ClipboardData cboard = await General.ClipboardDataAsync();
                IsClipboardValid = config.CanAcceptPasteForSystem(cboard?.SystemTag);
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void AppPropertyChangedHandler(object sender, object args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// on navigating to this page, set up and tear down our internal and ui state.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            IsNavPending = false;
            if (ConfigFilterBox != null)
            {
                MainWindow mainWindow = (MainWindow)(Application.Current as JAFDTC.App)?.Window;
                mainWindow.SetConfigFilterBoxVisibility(Visibility.Visible);
            }

            if (CurConfigEditing != null)
            {
                // HACK: seriously, wtaf? why don't bindings work as they are documented? we use the ugly
                // HACK: "Reinsert" hack to cause the ListView to reload. There appears to be a race going
                // HACK: on too, so put the reinsert on the dispatch queue with low priority.
                //
                CurConfigEditing.ConfigurationUpdated();
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,() =>
                {
                    ConfigList.Reinsert(CurConfigEditing);
                    uiCfgListView.SelectedItem = CurConfigEditing;
                    CurConfigEditing = null;
                });
            }
            RebuildInterfaceState();
            base.OnNavigatedTo(args);
        }
    }
}
