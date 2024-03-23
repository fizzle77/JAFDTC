// ********************************************************************************************************************
//
// F16CEditMFDPage.xaml.cs : ui c# for viper mfd setup editor page
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
using JAFDTC.Models.F16C.MFD;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class F16CEditMFDPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(MFDSystem.SystemTag, "Displays", "MFD", Glyphs.MFD, typeof(F16CEditMFDPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditMFDModeConfig/EditMode properties.
        //
        private F16CConfiguration Config { get; set; }

        private MFDModeConfiguration EditMFDModeConfig { get; set; }

        private int EditMode { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsRebuildingUI { get; set; }

        // ---- read-only properties

        private readonly MFDSystem _mfdSysDefault;

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        private readonly List<FontIcon> _modeSelComboIcons;
        private readonly IList<ComboBox> _formatCombos;
        private readonly IList<ToggleButton> _leftToggles;
        private readonly IList<ToggleButton> _rightToggles;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditMFDPage()
        {
            InitializeComponent();

            EditMode = (int)MFDSystem.MasterModes.NAV;
            EditMFDModeConfig = new();

            IsRebuildPending = false;
            IsRebuildingUI = false;

            _mfdSysDefault = MFDSystem.ExplicitDefaults;

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();

            _modeSelComboIcons = new List<FontIcon>()
            {
                uiModeSelectItem1Icon, uiModeSelectItem2Icon, uiModeSelectItem3Icon, uiModeSelectItem4Icon,
                uiModeSelectItem5Icon
            };
            _formatCombos = new List<ComboBox>()
            {
                uiFormatSelComboLOSB14, uiFormatSelComboLOSB13, uiFormatSelComboLOSB12,
                uiFormatSelComboROSB14, uiFormatSelComboROSB13, uiFormatSelComboROSB12
            };
            _leftToggles = new List<ToggleButton>()
            {
                uiInitLOSB14Toggle, uiInitLOSB13Toggle, uiInitLOSB12Toggle
            };
            _rightToggles = new List<ToggleButton>()
            {
                uiInitROSB14Toggle, uiInitROSB13Toggle, uiInitROSB12Toggle
            };

            // for whatever reason, x:Array went away, so we get to do set format items outside xaml.
            //
            // Tag is enum JAFDTC.Models.F16C.MFD.Modes
            //
            uiFormatSelComboLOSB14.ItemsSource = BuildMFDFormatComboItems();
            uiFormatSelComboLOSB13.ItemsSource = BuildMFDFormatComboItems();
            uiFormatSelComboLOSB12.ItemsSource = BuildMFDFormatComboItems();

            uiFormatSelComboROSB14.ItemsSource = BuildMFDFormatComboItems();
            uiFormatSelComboROSB13.ItemsSource = BuildMFDFormatComboItems();
            uiFormatSelComboROSB12.ItemsSource = BuildMFDFormatComboItems();

            // wait for final setup of the ui until we navigate to the page (at which point we will have a
            // configuration to display).
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        // marshall data between our local mfd mode setting and the appropriate mode in the cmds configuration.
        // note that data is kept in EditMFDModeConfig in "explict" form (where actual values are placed in fields,
        // not "" for defaults). we must use care when using IsDefault and friends on our state.
        //
        // NOTE: we assume Clone() performs a deep copy.
        //
        private void CopyConfigToEdit(int mode)
        {
            MFDConfiguration cfgLeftMC = Config.MFD.ModeConfigs[mode].LeftMFD;
            MFDConfiguration cfgRightMC = Config.MFD.ModeConfigs[mode].RightMFD;

            EditMFDModeConfig.LeftMFD.SelectedOSB = cfgLeftMC.SelectedOSB;
            EditMFDModeConfig.LeftMFD.OSB12 = cfgLeftMC.OSB12;
            EditMFDModeConfig.LeftMFD.OSB13 = cfgLeftMC.OSB13;
            EditMFDModeConfig.LeftMFD.OSB14 = cfgLeftMC.OSB14;

            EditMFDModeConfig.RightMFD.SelectedOSB = cfgRightMC.SelectedOSB;
            EditMFDModeConfig.RightMFD.OSB12 = cfgRightMC.OSB12;
            EditMFDModeConfig.RightMFD.OSB13 = cfgRightMC.OSB13;
            EditMFDModeConfig.RightMFD.OSB14 = cfgRightMC.OSB14;

            // replace "" fields in EditMFDModeConfig with actual default values.
            //
            MFDConfiguration curLeftMC = EditMFDModeConfig.LeftMFD;
            MFDConfiguration curRightMC = EditMFDModeConfig.RightMFD;

            MFDConfiguration dfltLeft = _mfdSysDefault.ModeConfigs[mode].LeftMFD;
            MFDConfiguration dfltRight = _mfdSysDefault.ModeConfigs[mode].RightMFD;

            curLeftMC.SelectedOSB = (curLeftMC.SelectedOSB != "") ? curLeftMC.SelectedOSB : dfltLeft.SelectedOSB;
            curLeftMC.OSB12 = (curLeftMC.OSB12 != "") ? curLeftMC.OSB12 : dfltLeft.OSB12;
            curLeftMC.OSB13 = (curLeftMC.OSB13 != "") ? curLeftMC.OSB13 : dfltLeft.OSB13;
            curLeftMC.OSB14 = (curLeftMC.OSB14 != "") ? curLeftMC.OSB14 : dfltLeft.OSB14;

            curRightMC.SelectedOSB = (curRightMC.SelectedOSB != "") ? curRightMC.SelectedOSB : dfltRight.SelectedOSB;
            curRightMC.OSB12 = (curRightMC.OSB12 != "") ? curRightMC.OSB12 : dfltRight.OSB12;
            curRightMC.OSB13 = (curRightMC.OSB13 != "") ? curRightMC.OSB13 : dfltRight.OSB13;
            curRightMC.OSB14 = (curRightMC.OSB14 != "") ? curRightMC.OSB14 : dfltRight.OSB14;
        }

        private void CopyEditToConfig(int mode, bool isPersist = false)
        {
            // copy EditMFDModeConfig to the MFD configuration replacing fields matching defaults in EditMFDModeConfig
            // with "" values to indicate no change from default.
            //
            MFDConfiguration dfltLeft = _mfdSysDefault.ModeConfigs[mode].LeftMFD;
            MFDConfiguration dfltRight = _mfdSysDefault.ModeConfigs[mode].RightMFD;

            MFDConfiguration cfgLeftMC = Config.MFD.ModeConfigs[mode].LeftMFD;
            MFDConfiguration cfgRightMC = Config.MFD.ModeConfigs[mode].RightMFD;

            MFDConfiguration curLeftMC = EditMFDModeConfig.LeftMFD;
            MFDConfiguration curRightMC = EditMFDModeConfig.RightMFD;

            cfgLeftMC.SelectedOSB = (curLeftMC.SelectedOSB != dfltLeft.SelectedOSB) ? curLeftMC.SelectedOSB : "";
            cfgLeftMC.OSB12 = (curLeftMC.OSB12 != dfltLeft.OSB12) ? curLeftMC.OSB12 : "";
            cfgLeftMC.OSB13 = (curLeftMC.OSB13 != dfltLeft.OSB13) ? curLeftMC.OSB13 : "";
            cfgLeftMC.OSB14 = (curLeftMC.OSB14 != dfltLeft.OSB14) ? curLeftMC.OSB14 : "";

            cfgRightMC.SelectedOSB = (curRightMC.SelectedOSB != dfltRight.SelectedOSB) ? curRightMC.SelectedOSB : "";
            cfgRightMC.OSB12 = (curRightMC.OSB12 != dfltRight.OSB12) ? curRightMC.OSB12 : "";
            cfgRightMC.OSB13 = (curRightMC.OSB13 != dfltRight.OSB13) ? curRightMC.OSB13 : "";
            cfgRightMC.OSB14 = (curRightMC.OSB14 != dfltRight.OSB14) ? curRightMC.OSB14 : "";

            if (isPersist)
            {
                Config.Save(this, MFDSystem.SystemTag);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // utilities
        //
        // ------------------------------------------------------------------------------------------------------------

        // return a list of TextBlock instances to serve as the menu items for the mfd format combo control. the
        // tags are set to the position in the list.
        //
        private static IList<TextBlock> BuildMFDFormatComboItems()
        {
            return new List<TextBlock>()
            {
                new TextBlock { Text = "", Tag = "0", HorizontalAlignment = HorizontalAlignment.Center },
                new TextBlock { Text = "DTE", Tag = "1", HorizontalAlignment = HorizontalAlignment.Center },
                new TextBlock { Text = "FCR", Tag = "2", HorizontalAlignment = HorizontalAlignment.Center },
                new TextBlock { Text = "FLCS", Tag = "3", HorizontalAlignment = HorizontalAlignment.Center },
                new TextBlock { Text = "HAD", Tag = "4", HorizontalAlignment = HorizontalAlignment.Center },
                new TextBlock { Text = "HSD", Tag = "5", HorizontalAlignment = HorizontalAlignment.Center },
                new TextBlock { Text = "SMS", Tag = "6", HorizontalAlignment = HorizontalAlignment.Center },
                new TextBlock { Text = "TEST", Tag = "7", HorizontalAlignment = HorizontalAlignment.Center },
                new TextBlock { Text = "TGP", Tag = "8", HorizontalAlignment = HorizontalAlignment.Center },
                new TextBlock { Text = "WPN", Tag = "9", HorizontalAlignment = HorizontalAlignment.Center }
            };
        }

        // TODO: document
        private bool IsEditLeftMFDConfigDefault()
        {
            MFDConfiguration dfltLeft = _mfdSysDefault.ModeConfigs[EditMode].LeftMFD;

            MFDConfiguration curLeftMC = EditMFDModeConfig.LeftMFD;

            return ((string.IsNullOrEmpty(curLeftMC.SelectedOSB) || (curLeftMC.SelectedOSB == dfltLeft.SelectedOSB)) &&
                    (string.IsNullOrEmpty(curLeftMC.OSB12) || (curLeftMC.OSB12 == dfltLeft.OSB12)) &&
                    (string.IsNullOrEmpty(curLeftMC.OSB13) || (curLeftMC.OSB13 == dfltLeft.OSB13)) &&
                    (string.IsNullOrEmpty(curLeftMC.OSB14) || (curLeftMC.OSB14 == dfltLeft.OSB14)));
        }

        // TODO: document
        private bool IsEditRightMFDConfigDefault()
        {
            MFDConfiguration dfltRight = _mfdSysDefault.ModeConfigs[EditMode].RightMFD;

            MFDConfiguration curRightMC = EditMFDModeConfig.RightMFD;

            return ((string.IsNullOrEmpty(curRightMC.SelectedOSB) || (curRightMC.SelectedOSB == dfltRight.SelectedOSB)) &&
                    (string.IsNullOrEmpty(curRightMC.OSB12) || (curRightMC.OSB12 == dfltRight.OSB12)) &&
                    (string.IsNullOrEmpty(curRightMC.OSB13) || (curRightMC.OSB13 == dfltRight.OSB13)) &&
                    (string.IsNullOrEmpty(curRightMC.OSB14) || (curRightMC.OSB14 == dfltRight.OSB14)));
        }

        // TODO: document
        private bool IsEditMFDConfigDefault()
        {
            return (IsEditLeftMFDConfigDefault() && IsEditRightMFDConfigDefault());
        }

        // sets the mfd format in EditMFDModeConfig for the given format (as identified by a tag from the ComboBox that
        // displays the format) to the given value.
        //
        private void SetFormatForOSB(object tag, int value)
        {
            switch ((string)tag)
            {
                case "L.14": EditMFDModeConfig.LeftMFD.OSB14 = value.ToString(); break;
                case "L.13": EditMFDModeConfig.LeftMFD.OSB13 = value.ToString(); break;
                case "L.12": EditMFDModeConfig.LeftMFD.OSB12 = value.ToString(); break;
                case "R.14": EditMFDModeConfig.RightMFD.OSB14 = value.ToString(); break;
                case "R.13": EditMFDModeConfig.RightMFD.OSB13 = value.ToString(); break;
                case "R.12": EditMFDModeConfig.RightMFD.OSB12 = value.ToString(); break;
            }
        }

        // change the selected cmds program and update various ui and model state.
        //
        private void SelectMode(int mode)
        {
            if (mode != EditMode)
            {
                CopyEditToConfig(EditMode, true);
                EditMode = mode;
                CopyConfigToEdit(EditMode);
                RebuildInterfaceState();
            }
        }

        // TODO: document
        private void RebuildModeSelectMenu()
        {
            for (int i = 0; i < Config.MFD.ModeConfigs.Length; i++)
            {
                Visibility viz = Visibility.Collapsed;
                if (((EditMode == i) && !IsEditMFDConfigDefault()) ||
                    ((EditMode != i) && !Config.MFD.ModeConfigs[i].IsDefault))
                {
                    viz = Visibility.Visible;
                }
                _modeSelComboIcons[i].Visibility = viz;
            }
        }

        // update the selected formats for the mfds/osbs based on the current settings.
        //
        private void RebuildFormatSelects()
        {
            IsRebuildingUI = true;
            uiFormatSelComboLOSB14.SelectedIndex = int.Parse(EditMFDModeConfig.LeftMFD.OSB14);
            uiFormatSelComboLOSB13.SelectedIndex = int.Parse(EditMFDModeConfig.LeftMFD.OSB13);
            uiFormatSelComboLOSB12.SelectedIndex = int.Parse(EditMFDModeConfig.LeftMFD.OSB12);

            uiFormatSelComboROSB14.SelectedIndex = int.Parse(EditMFDModeConfig.RightMFD.OSB14);
            uiFormatSelComboROSB13.SelectedIndex = int.Parse(EditMFDModeConfig.RightMFD.OSB13);
            uiFormatSelComboROSB12.SelectedIndex = int.Parse(EditMFDModeConfig.RightMFD.OSB12);
            IsRebuildingUI = false;
        }

        // update the toggle buttons that select inital formats based on current settings.
        //
        private void RebuildSelToggleButtons()
        {
            foreach (ToggleButton toggle in _leftToggles)
            {
                toggle.IsChecked = ((string) toggle.Tag == EditMFDModeConfig.LeftMFD.SelectedOSB);
            }
            foreach (ToggleButton toggle in _rightToggles)
            {
                toggle.IsChecked = ((string)toggle.Tag == EditMFDModeConfig.RightMFD.SelectedOSB);
            }
        }

        // TODO: document
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, MFDSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        // update the enable state on the ui elements based on the current settings. link controls must be set up
        // vi RebuildLinkControls() prior to calling this function.
        //
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(MFDSystem.SystemTag));
            Utilities.SetEnableState(uiFormatSelComboLOSB12, isEditable);
            Utilities.SetEnableState(uiFormatSelComboLOSB13, isEditable);
            Utilities.SetEnableState(uiFormatSelComboLOSB14, isEditable);

            Utilities.SetEnableState(uiFormatSelComboROSB12, isEditable);
            Utilities.SetEnableState(uiFormatSelComboROSB13, isEditable);
            Utilities.SetEnableState(uiFormatSelComboROSB14, isEditable);

            Utilities.SetEnableState(uiInitLOSB12Toggle, isEditable);
            Utilities.SetEnableState(uiInitLOSB13Toggle, isEditable);
            Utilities.SetEnableState(uiInitLOSB14Toggle, isEditable);

            Utilities.SetEnableState(uiInitROSB12Toggle, isEditable);
            Utilities.SetEnableState(uiInitROSB13Toggle, isEditable);
            Utilities.SetEnableState(uiInitROSB14Toggle, isEditable);

            Utilities.SetEnableState(uiResetLMFDBtn, isEditable && !IsEditLeftMFDConfigDefault());
            Utilities.SetEnableState(uiResetRMFDBtn, isEditable && !IsEditRightMFDConfigDefault());

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);

            Utilities.SetEnableState(uiResetAllBtn, !Config.MFD.IsDefault || !IsEditMFDConfigDefault());

            Utilities.SetEnableState(uiModePrevBtn, (EditMode != (int)MFDSystem.MasterModes.NAV));
            Utilities.SetEnableState(uiModeNextBtn, (EditMode != (int)MFDSystem.MasterModes.DGFT_DGFT));
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
                    RebuildModeSelectMenu();
                    RebuildFormatSelects();
                    RebuildSelToggleButtons();
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

        // on clicks of the reset all button, reset all settings back to default.
        //
        private async void BtnResetAll_Click(object sender, RoutedEventArgs args)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configruation?",
                "Are you sure you want to reset the MFD configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(MFDSystem.SystemTag);
                Config.MFD.Reset();
                Config.Save(this, MFDSystem.SystemTag);
                CopyConfigToEdit(EditMode);
            }
        }

        // TODO: document
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, MFDSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                Config.UnlinkSystem(MFDSystem.SystemTag);
                Config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(MFDSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit(EditMode);
            }
        }

        // on clicks of the reset left mfd button, reset the mfd formats and initial button for
        // the left mfd.
        //
        private void BtnResetLMFD_Click(object sender, RoutedEventArgs args)
        {
            EditMFDModeConfig.LeftMFD.SelectedOSB = _mfdSysDefault.ModeConfigs[EditMode].LeftMFD.SelectedOSB;
            EditMFDModeConfig.LeftMFD.OSB12 = _mfdSysDefault.ModeConfigs[EditMode].LeftMFD.OSB12;
            EditMFDModeConfig.LeftMFD.OSB13 = _mfdSysDefault.ModeConfigs[EditMode].LeftMFD.OSB13;
            EditMFDModeConfig.LeftMFD.OSB14 = _mfdSysDefault.ModeConfigs[EditMode].LeftMFD.OSB14;
            CopyEditToConfig(EditMode, true);
        }

        // on clicks of the reset right mfd button, reset the mfd formats and initial button for
        // the left mfd.
        //
        private void BtnResetRMFD_Click(object sender, RoutedEventArgs args)
        {
            EditMFDModeConfig.RightMFD.SelectedOSB = _mfdSysDefault.ModeConfigs[EditMode].RightMFD.SelectedOSB;
            EditMFDModeConfig.RightMFD.OSB12 = _mfdSysDefault.ModeConfigs[EditMode].RightMFD.OSB12;
            EditMFDModeConfig.RightMFD.OSB13 = _mfdSysDefault.ModeConfigs[EditMode].RightMFD.OSB13;
            EditMFDModeConfig.RightMFD.OSB14 = _mfdSysDefault.ModeConfigs[EditMode].RightMFD.OSB14;
            CopyEditToConfig(EditMode, true);
        }

        // on clicks of the previous program button, advance to the previous program.
        //
        private void BtnModePrev_Click(object sender, RoutedEventArgs args)
        {
            SelectMode(EditMode - 1);
            uiModeSelectCombo.SelectedIndex = EditMode;
        }

        // on clicks of the next program button, advance to the next program.
        //
        private void BtnModeNext_Click(object sender, RoutedEventArgs args)
        {
            SelectMode(EditMode + 1);
            uiModeSelectCombo.SelectedIndex = EditMode;
        }

        // on selection changed in the mode select combo, switch master modes and update the ui.
        //
        private void ModeSelectCombo_SelectionChanged(object sender, RoutedEventArgs args)
        {
            Grid item = (Grid)((ComboBox)sender).SelectedItem;
            if ((item != null) && (item.Tag != null))
            {
                SelectMode(int.Parse((string)item.Tag));
            }
        }

        // on selection changed in a format selection combo, update the formats and ui. note that
        // a format may appear in at most one osb on one display.
        //
        private void FormatSelectCombo_SelectionChanged(object sender, RoutedEventArgs args)
        {
            if (!IsRebuildingUI)
            {
                ComboBox combo = (ComboBox)sender;

                foreach (ComboBox item in _formatCombos)
                {
                    if ((((string)item.Tag) != ((string)combo.Tag)) &&
                        (combo.SelectedIndex == item.SelectedIndex))
                    {
                        item.SelectedIndex = 0;
                        SetFormatForOSB(item.Tag, 0);
                    }
                }
                SetFormatForOSB(combo.Tag, combo.SelectedIndex);
                CopyEditToConfig(EditMode, true);
            }
        }

        // on clicks of the left-side mfd selected osb button, uncheck all other buttons and update
        // the configuration.
        //
        private void InitLeftOSB_Click(object sender, RoutedEventArgs args)
        {
            ToggleButton toggleClicked = (ToggleButton)sender;
            foreach (ToggleButton toggle in _leftToggles)
            {
                toggle.IsChecked = ((string)toggle.Tag == (string)toggleClicked.Tag);
            }
            EditMFDModeConfig.LeftMFD.SelectedOSB = (string)toggleClicked.Tag;
            CopyEditToConfig(EditMode, true);
        }

        // on clicks of the right-side mfd selected osb button, uncheck all other buttons and update
        // the configuration.
        //
        private void InitRightOSB_Click(object sender, RoutedEventArgs args)
        {
            ToggleButton toggleClicked = (ToggleButton)sender;
            foreach (ToggleButton toggle in _rightToggles)
            {
                toggle.IsChecked = ((string)toggle.Tag == (string)toggleClicked.Tag);
            }
            EditMFDModeConfig.RightMFD.SelectedOSB = (string)toggleClicked.Tag;
            CopyEditToConfig(EditMode, true);
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
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;
            Config = (F16CConfiguration)NavArgs.Config;

            Config.ConfigurationSaved += ConfigurationSavedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, MFDSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            uiModeSelectCombo.SelectedIndex = EditMode;
            SelectMode(EditMode);

            CopyConfigToEdit(EditMode);

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
