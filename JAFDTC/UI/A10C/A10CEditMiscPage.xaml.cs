using JAFDTC.UI.App;
using JAFDTC.Models;
using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.Misc;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using Microsoft.UI.Dispatching;
using static JAFDTC.Models.F15E.UFC.UFCSystem;
using System.Collections;
using System.ComponentModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class A10CEditMiscPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(MiscSystem.SystemTag, "Miscellaneous", "Miscellaneous", Glyphs.MISC, typeof(A10CEditMiscPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditMisc property.
        //
        private A10CConfiguration Config { get; set; }

        private MiscSystem EditMisc { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsRebuildingUI { get; set; }

        // ---- read-only properties

        private readonly MiscSystem _miscSysDefault;

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;
        private readonly Dictionary<string, TextBox> _miscTextFieldPropertyMap;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public A10CEditMiscPage()
        {
            this.InitializeComponent();
            EditMisc = new MiscSystem();

            IsRebuildPending = false;
            IsRebuildingUI = false;

            _miscSysDefault = MiscSystem.ExplicitDefaults;

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();

            EditMisc.ErrorsChanged += BaseField_DataValidationError;
            EditMisc.PropertyChanged += BaseField_PropertyChanged;
            // For mapping property errors to corresponding text field.
            _miscTextFieldPropertyMap = new Dictionary<string, TextBox>()
            {
                ["TACANChannel"] = uiTextTACANChannel,
                ["IFFMode3Code"] = uiTextIFFMode3Code
            };
            _defaultBorderBrush = uiTextTACANChannel.BorderBrush;
            _defaultBkgndBrush = uiTextTACANChannel.Background;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        // marshall data between our local misc setup and the configuration.
        //
        private void CopyConfigToEdit()
        {
            EditMisc.CoordSystem = Config.Misc.CoordSystem;
            EditMisc.BullseyeOnHUD = Config.Misc.BullseyeOnHUD;
            EditMisc.FlightPlan1Manual = Config.Misc.FlightPlan1Manual;
            EditMisc.SpeedDisplay = Config.Misc.SpeedDisplay;
            EditMisc.AapSteerPt = Config.Misc.AapSteerPt;
            EditMisc.AapPage = Config.Misc.AapPage;
            EditMisc.AutopilotMode = Config.Misc.AutopilotMode;
            EditMisc.TACANChannel = Config.Misc.TACANChannel;
            EditMisc.TACANBand = Config.Misc.TACANBand;
            EditMisc.TACANMode = Config.Misc.TACANMode;
            EditMisc.IFFMasterMode = Config.Misc.IFFMasterMode;
            EditMisc.IFFMode4On = Config.Misc.IFFMode4On;
            EditMisc.IFFMode3Code = Config.Misc.IFFMode3Code;
        }

        private void CopyEditToConfig(bool isPersist = false)
        {
            if (!EditMisc.HasErrors)
            {
                Config.Misc.CoordSystem = EditMisc.CoordSystem;
                Config.Misc.BullseyeOnHUD = EditMisc.BullseyeOnHUD;
                Config.Misc.FlightPlan1Manual = EditMisc.FlightPlan1Manual;
                Config.Misc.SpeedDisplay = EditMisc.SpeedDisplay;
                Config.Misc.AapSteerPt = EditMisc.AapSteerPt;
                Config.Misc.AapPage = EditMisc.AapPage;
                Config.Misc.AutopilotMode = EditMisc.AutopilotMode;
                Config.Misc.TACANChannel = EditMisc.TACANChannel;
                Config.Misc.TACANBand = EditMisc.TACANBand;
                Config.Misc.TACANMode = EditMisc.TACANMode;
                Config.Misc.IFFMasterMode = EditMisc.IFFMasterMode;
                Config.Misc.IFFMode4On = EditMisc.IFFMode4On;
                Config.Misc.IFFMode3Code = EditMisc.IFFMode3Code;

                if (isPersist)
                {
                    Config.Save(this, MiscSystem.SystemTag);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        // rebuild the setup of the coordinate system according to the current settings. 
        //
        private void RebuildCoordSystemSetup()
        {
            int coordSystem = (string.IsNullOrEmpty(EditMisc.CoordSystem)) ? int.Parse(_miscSysDefault.CoordSystem)
                                                                  : int.Parse(EditMisc.CoordSystem);
            if (uiComboCoordSystem.SelectedIndex != coordSystem)
            {
                uiComboCoordSystem.SelectedIndex = coordSystem;
            }
        }

        // rebuild the setup of the bullseye on HUD setting according to the current settings. 
        //
        private void RebuildBullseyeOnHUDSetup()
        {
            uiCkboxBullsOnHUD.IsChecked = EditMisc.IsBullseyeOnHUDValue;
        }

        // rebuild the setup of the first flight plan's manual setting according to the current settings. 
        //
        private void RebuildFlightPlan1ManualSetup()
        {
            int manualOrAuto = (string.IsNullOrEmpty(EditMisc.FlightPlan1Manual)) ? int.Parse(_miscSysDefault.FlightPlan1Manual)
                                                                          : int.Parse(EditMisc.FlightPlan1Manual);
            if (uiComboFlightPlan1Manual.SelectedIndex != manualOrAuto)
            {
                uiComboFlightPlan1Manual.SelectedIndex = manualOrAuto;
            }
        }

        // rebuild the setup of the speed display setting according to the current settings. 
        //
        private void RebuildSpeedDisplaySetup()
        {
            int speedDisplay = (string.IsNullOrEmpty(EditMisc.SpeedDisplay)) ? int.Parse(_miscSysDefault.SpeedDisplay)
                                                                             : int.Parse(EditMisc.SpeedDisplay);
            if (uiComboSpeedDisplay.SelectedIndex != speedDisplay)
            {
                uiComboSpeedDisplay.SelectedIndex = speedDisplay;
            }
        }

        // rebuild the setup of the aap steer point according to the current settings. 
        //
        private void RebuildAapSteerPtSetup()
        {
            int aapSteerPt = (string.IsNullOrEmpty(EditMisc.AapSteerPt)) ? int.Parse(_miscSysDefault.AapSteerPt)
                                                                             : int.Parse(EditMisc.AapSteerPt);
            if (uiComboSteerPt.SelectedIndex != aapSteerPt)
            {
                uiComboSteerPt.SelectedIndex = aapSteerPt;
            }
        }

        // rebuild the setup of the aap page according to the current settings. 
        //
        private void RebuildAapPageSetup()
        {
            int aapPage = (string.IsNullOrEmpty(EditMisc.AapPage)) ? int.Parse(_miscSysDefault.AapPage)
                                                                             : int.Parse(EditMisc.AapPage);
            if (uiComboAapPage.SelectedIndex != aapPage)
            {
                uiComboAapPage.SelectedIndex = aapPage;
            }
        }

        // rebuild the setup of the autopilot mode according to the current settings. 
        //
        private void RebuildAutopilotModeSetup()
        {
            int autopilotMode = (string.IsNullOrEmpty(EditMisc.AutopilotMode)) ? int.Parse(_miscSysDefault.AutopilotMode)
                                                                             : int.Parse(EditMisc.AutopilotMode);
            foreach (TextBlock item in uiComboAutopilotMode.Items)
            {
                if ( int.Parse((string)item.Tag) == autopilotMode)
                {
                    uiComboAutopilotMode.SelectedItem = item;
                    return;
                }
            }
        }

        // rebuild the setup of the TACAN band according to the current settings. 
        //
        private void RebuildTACANBandSetup()
        {
            int tacanBand = (string.IsNullOrEmpty(EditMisc.TACANBand)) ? int.Parse(_miscSysDefault.TACANBand)
                                                                           : int.Parse(EditMisc.TACANBand);
            foreach (TextBlock item in uiComboTACANBand.Items)
            {
                if (int.Parse((string)item.Tag) == tacanBand)
                {
                    uiComboTACANBand.SelectedItem = item;
                    return;
                }
            }
        }

        // rebuild the setup of the TACAN mode according to the current settings. 
        //
        private void RebuildTACANModeSetup()
        {
            int tacanMode = (string.IsNullOrEmpty(EditMisc.TACANMode)) ? int.Parse(_miscSysDefault.TACANMode)
                                                                       : int.Parse(EditMisc.TACANMode);
            foreach (TextBlock item in uiComboTACANMode.Items)
            {
                if (int.Parse((string)item.Tag) == tacanMode)
                {
                    uiComboTACANMode.SelectedItem = item;
                    return;
                }
            }
        }

        // rebuild the setup of the IFF master mode according to the current settings. 
        //
        private void RebuildIFFMasterModeSetup()
        {
            int tacanMode = (string.IsNullOrEmpty(EditMisc.IFFMasterMode)) ? int.Parse(_miscSysDefault.IFFMasterMode)
                                                                           : int.Parse(EditMisc.IFFMasterMode);
            foreach (TextBlock item in uiComboIFFMasterMode.Items)
            {
                if (int.Parse((string)item.Tag) == tacanMode)
                {
                    uiComboIFFMasterMode.SelectedItem = item;
                    return;
                }
            }
        }

        // rebuild the setup of the IFF mode 4 ON setting according to the current settings. 
        //
        private void RebuildIFFMode4OnSetup()
        {
            uiCheckIFFMode4On.IsChecked = EditMisc.IffMode4OnValue;
        }

        // TODO: document
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, MiscSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }


        // update the enable state on the ui elements based on the current settings. link controls must be set up
        // via RebuildLinkControls() prior to calling this function.
        //
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(MiscSystem.SystemTag));
            Utilities.SetEnableState(uiComboCoordSystem, isEditable);
            Utilities.SetEnableState(uiCkboxBullsOnHUD, isEditable);
            Utilities.SetEnableState(uiComboFlightPlan1Manual, isEditable);
            Utilities.SetEnableState(uiComboSpeedDisplay, isEditable);
            Utilities.SetEnableState(uiComboSteerPt, isEditable);
            Utilities.SetEnableState(uiComboAapPage, isEditable);
            Utilities.SetEnableState(uiComboAutopilotMode, isEditable);
            Utilities.SetEnableState(uiTextTACANChannel, isEditable);
            Utilities.SetEnableState(uiComboTACANBand, isEditable);
            Utilities.SetEnableState(uiComboTACANMode, isEditable);
            Utilities.SetEnableState(uiComboIFFMasterMode, isEditable);
            Utilities.SetEnableState(uiCheckIFFMode4On, isEditable);
            Utilities.SetEnableState(uiTextIFFMode3Code, isEditable);

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);
            Utilities.SetEnableState(uiPageBtnReset, !EditMisc.IsDefault);
        }

        // rebuild the state of controls on the page in response to a change in the configuration.
        //
        private void RebuildInterfaceState()
        {
            if (!IsRebuildPending)
            {
                IsRebuildPending = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    IsRebuildingUI = true;
                    RebuildCoordSystemSetup();
                    RebuildBullseyeOnHUDSetup();
                    RebuildFlightPlan1ManualSetup();
                    RebuildSpeedDisplaySetup();
                    RebuildAapSteerPtSetup();
                    RebuildAapPageSetup();
                    RebuildAutopilotModeSetup();
                    RebuildTACANBandSetup();
                    RebuildTACANModeSetup();
                    RebuildIFFMasterModeSetup();
                    RebuildIFFMode4OnSetup();

                    RebuildLinkControls();
                    RebuildEnableState();
                    IsRebuildingUI = false;
                    IsRebuildPending = false;
                });
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- page settings -----------------------------------------------------------------------------------------

        // on clicks of the reset all button, reset all settings back to default.
        //
        private async void PageBtnReset_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configruation?",
                "Are you sure you want to reset the miscellaneous configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(MiscSystem.SystemTag);
                Config.Misc.Reset();
                Config.Save(this, MiscSystem.SystemTag);
                CopyConfigToEdit();
            }
        }

        // TODO: document
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, MiscSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                Config.UnlinkSystem(MiscSystem.SystemTag);
                Config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(MiscSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit();
            }
        }

        private void uiTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CopyEditToConfig(true);
            });
        }

        // ---- coordinate system setup -------------------------------------------------------------------------------------------

        private void ComboCoordSystem_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                EditMisc.CoordSystem = (string)item.Tag;
                CopyEditToConfig(true);
            }
        }

        // ---- bullseye on hud setup -------------------------------------------------------------------------------------------

        private void uiCkboxBullsOnHUD_Click(object sender, RoutedEventArgs args)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (!IsRebuildingUI && (checkBox != null))
            {
                EditMisc.BullseyeOnHUD = checkBox.IsChecked.ToString();
                CopyEditToConfig(true);
            }
        }

        // ---- flight plan 1 manual/auto setup -------------------------------------------------------------------------------------------

        private void ComboFlightPlan1Manual_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                EditMisc.FlightPlan1Manual = (string)item.Tag;
                CopyEditToConfig(true);
            }
        }

        // ---- steer page speed display setup -------------------------------------------------------------------------------------------

        private void ComboSpeedDisplay_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                EditMisc.SpeedDisplay = (string)item.Tag;
                CopyEditToConfig(true);
            }
        }

        // ---- aap steer pt knob setup -------------------------------------------------------------------------------------------

        private void ComboSteerPt_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                EditMisc.AapSteerPt = (string)item.Tag;
                CopyEditToConfig(true);
            }

        }

        // ---- aap page knob setup -------------------------------------------------------------------------------------------

        private void ComboPage_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                EditMisc.AapPage = (string)item.Tag;
                CopyEditToConfig(true);
            }
        }

        // ---- autopilot mode switch setup -------------------------------------------------------------------------------------------

        private void ComboAutopilotMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                EditMisc.AutopilotMode = (string)item.Tag;
                CopyEditToConfig(true);
            }
        }


        // ---- TACAN setup -------------------------------------------------------------------------------------------

        private void uiComboTACANBand_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                EditMisc.TACANBand = (string)item.Tag;
                CopyEditToConfig(true);
            }
        }

        private void uiComboTACANMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                EditMisc.TACANMode = (string)item.Tag;
                CopyEditToConfig(true);
            }
        }

        // ---- IFF setup -------------------------------------------------------------------------------------------

        private void uiComboIFFMasterMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextBlock item = (TextBlock)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                EditMisc.IFFMasterMode = (string)item.Tag;
                CopyEditToConfig(true);
            }
        }

        private void uiCheckIFFMode4On_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (!IsRebuildingUI && (checkBox != null))
            {
                EditMisc.IFFMode4On = checkBox.IsChecked.ToString();
                CopyEditToConfig(true);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- field validation -------------------------------------------------------------------------------------------

        private void SetFieldValidVisualState(TextBox field, bool isValid)
        {
            field.BorderBrush = (isValid) ? _defaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
            field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBackgroundBrush"];
        }

        private void ValidateAllFields(Dictionary<string, TextBox> fields, IEnumerable errors)
        {
            Dictionary<string, bool> map = new();
            foreach (string error in errors)
            {
                map[error] = true;
            }
            foreach (KeyValuePair<string, TextBox> kvp in fields)
            {
                SetFieldValidVisualState(kvp.Value, !map.ContainsKey(kvp.Key));
            }
        }

        // validation error: update ui state for the various components that may have errors.
        //
        private void BaseField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            if (args.PropertyName == null)
            {
                ValidateAllFields(_miscTextFieldPropertyMap, EditMisc.GetErrors(null));
            }
            else
            {
                List<string> errors = (List<string>)EditMisc.GetErrors(args.PropertyName);
                if (_miscTextFieldPropertyMap.ContainsKey(args.PropertyName))
                    SetFieldValidVisualState(_miscTextFieldPropertyMap[args.PropertyName], (errors.Count == 0));
            }
            RebuildInterfaceState();
        }

        // property changed: rebuild interface state to account for configuration changes.
        //
        private void BaseField_PropertyChanged(object sender, EventArgs args)
        {
            RebuildInterfaceState();
        }

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
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;
            Config = (A10CConfiguration)NavArgs.Config;

            Config.ConfigurationSaved += ConfigurationSavedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, MiscSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            CopyConfigToEdit();

            //ValidateAllFields(_baseFieldValueMap, EditMisc.GetErrors(null));
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            Config.ConfigurationSaved -= ConfigurationSavedHandler;

            base.OnNavigatedFrom(args);
        }
    }
}