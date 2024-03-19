// ********************************************************************************************************************
//
// F15EEditMFDPage.xaml.cs : ui c# for mudhen mpd/mpcd setup editor page
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
using JAFDTC.Models.F15E.MPD;
using JAFDTC.Models.F16C.MFD;
using JAFDTC.UI.App;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace JAFDTC.UI.F15E
{
    /// <summary>
    /// user interface for the page that allows you to edit the mudhen mpd/mpcd system configurations.
    /// </summary>
    public sealed partial class F15EEditMPDPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(MPDSystem.SystemTag, "Displays", "MPD", Glyphs.MPD, typeof(F15EEditMPDPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- private properties

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditMisc property.
        //
        private F15EConfiguration Config { get; set; }

        private MPDSystem EditMPD { get; set; }

        private int EditCrewMember { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsRebuildingUI { get; set; }

        // ---- private properties, read-only

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        private readonly Dictionary<string, Dictionary<string, ComboBox>> _formatComboMap;
        private readonly Dictionary<string, Dictionary<string, ComboBox>> _modeComboMap;
        private readonly Dictionary<string, Button> _resetDisplayMap;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F15EEditMPDPage()
        {
            this.InitializeComponent();

            EditMPD = new MPDSystem();

            IsRebuildPending = false;
            IsRebuildingUI = false;

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();

            _formatComboMap = new()
            {
                [((int)MPDSystem.CockpitDisplays.PILOT_L_MPD).ToString()] = new Dictionary<string, ComboBox>()
                {
                    ["0"] = uiComboFormatSelPLMPD1,
                    ["1"] = uiComboFormatSelPLMPD2,
                    ["2"] = uiComboFormatSelPLMPD3
                },
                [((int)MPDSystem.CockpitDisplays.PILOT_MPCD).ToString()] = new Dictionary<string, ComboBox>()
                {
                    ["0"] = uiComboFormatSelPMPCD1,
                    ["1"] = uiComboFormatSelPMPCD2,
                    ["2"] = uiComboFormatSelPMPCD3
                },
                [((int)MPDSystem.CockpitDisplays.PILOT_R_MPD).ToString()] = new Dictionary<string, ComboBox>()
                {
                    ["0"] = uiComboFormatSelPRMPD1,
                    ["1"] = uiComboFormatSelPRMPD2,
                    ["2"] = uiComboFormatSelPRMPD3
                },
                [((int)MPDSystem.CockpitDisplays.WSO_L_MPCD).ToString()] = new Dictionary<string, ComboBox>()
                {
                    ["0"] = uiComboFormatSelWLMPCD1,
                    ["1"] = uiComboFormatSelWLMPCD2,
                    ["2"] = uiComboFormatSelWLMPCD3
                },
                [((int)MPDSystem.CockpitDisplays.WSO_L_MPD).ToString()] = new Dictionary<string, ComboBox>()
                {
                    ["0"] = uiComboFormatSelWLMPD1,
                    ["1"] = uiComboFormatSelWLMPD2,
                    ["2"] = uiComboFormatSelWLMPD3
                },
                [((int)MPDSystem.CockpitDisplays.WSO_R_MPD).ToString()] = new Dictionary<string, ComboBox>()
                {
                    ["0"] = uiComboFormatSelWRMPD1,
                    ["1"] = uiComboFormatSelWRMPD2,
                    ["2"] = uiComboFormatSelWRMPD3
                },
                [((int)MPDSystem.CockpitDisplays.WSO_R_MPCD).ToString()] = new Dictionary<string, ComboBox>()
                {
                    ["0"] = uiComboFormatSelWRMPCD1,
                    ["1"] = uiComboFormatSelWRMPCD2,
                    ["2"] = uiComboFormatSelWRMPCD3
                }
            };
            _modeComboMap = new()
            {
                [((int)MPDSystem.CockpitDisplays.PILOT_L_MPD).ToString()] = new Dictionary<string, ComboBox>()
                {
                    ["0"] = uiComboModeSelPLMPD1,
                    ["1"] = uiComboModeSelPLMPD2,
                    ["2"] = uiComboModeSelPLMPD3
                },
                [((int)MPDSystem.CockpitDisplays.PILOT_MPCD).ToString()] = new Dictionary<string, ComboBox>()
                {
                    ["0"] = uiComboModeSelPMPCD1,
                    ["1"] = uiComboModeSelPMPCD2,
                    ["2"] = uiComboModeSelPMPCD3
                },
                [((int)MPDSystem.CockpitDisplays.PILOT_R_MPD).ToString()] = new Dictionary<string, ComboBox>()
                {
                    ["0"] = uiComboModeSelPRMPD1,
                    ["1"] = uiComboModeSelPRMPD2,
                    ["2"] = uiComboModeSelPRMPD3
                },
                [((int)MPDSystem.CockpitDisplays.WSO_L_MPCD).ToString()] = new Dictionary<string, ComboBox>()
                {
                    ["0"] = uiComboModeSelWLMPCD1,
                    ["1"] = uiComboModeSelWLMPCD2,
                    ["2"] = uiComboModeSelWLMPCD3
                },
                [((int)MPDSystem.CockpitDisplays.WSO_L_MPD).ToString()] = new Dictionary<string, ComboBox>()
                {
                    ["0"] = uiComboModeSelWLMPD1,
                    ["1"] = uiComboModeSelWLMPD2,
                    ["2"] = uiComboModeSelWLMPD3
                },
                [((int)MPDSystem.CockpitDisplays.WSO_R_MPD).ToString()] = new Dictionary<string, ComboBox>()
                {
                    ["0"] = uiComboModeSelWRMPD1,
                    ["1"] = uiComboModeSelWRMPD2,
                    ["2"] = uiComboModeSelWRMPD3
                },
                [((int)MPDSystem.CockpitDisplays.WSO_R_MPCD).ToString()] = new Dictionary<string, ComboBox>()
                {
                    ["0"] = uiComboModeSelWRMPCD1,
                    ["1"] = uiComboModeSelWRMPCD2,
                    ["2"] = uiComboModeSelWRMPCD3
                }
            };
            _resetDisplayMap = new()
            {
                [((int)MPDSystem.CockpitDisplays.PILOT_L_MPD).ToString()] = uiResetPLMPDBtn,
                [((int)MPDSystem.CockpitDisplays.PILOT_MPCD).ToString()] = uiResetPMPCDBtn,
                [((int)MPDSystem.CockpitDisplays.PILOT_R_MPD).ToString()] = uiResetPRMPDBtn,
                [((int)MPDSystem.CockpitDisplays.WSO_L_MPCD).ToString()] = uiResetWLMPCDBtn,
                [((int)MPDSystem.CockpitDisplays.WSO_L_MPD).ToString()] = uiResetWLMPDBtn,
                [((int)MPDSystem.CockpitDisplays.WSO_R_MPD).ToString()] = uiResetWRMPCDBtn,
                [((int)MPDSystem.CockpitDisplays.WSO_R_MPCD).ToString()] = uiResetWRMPDBtn
            };

            // for whatever reason, x:Array went away, so we get to do set format items outside xaml.
            //
            foreach (KeyValuePair<string, Dictionary<string, ComboBox>> display in _formatComboMap)
            {
                foreach (KeyValuePair<string, ComboBox> sequence in display.Value)
                {
                    sequence.Value.ItemsSource = BuildMPDFormatComboItems();
                }
            }
            foreach (KeyValuePair<string, Dictionary<string, ComboBox>> display in _modeComboMap)
            {
                foreach (KeyValuePair<string, ComboBox> sequence in display.Value)
                {
                    sequence.Value.ItemsSource = BuildMPDModeComboItems();
                }
            }

            // wait for final setup of the ui until we navigate to the page (at which point we will have a
            // configuration to display).
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshall data from the configuration into our local misc setup.
        /// </summary>
        private void CopyConfigToEdit()
        {
            EditCrewMember = (string.IsNullOrEmpty(Config.MPD.CurCrewPosition)) ? 0 : int.Parse(Config.MPD.CurCrewPosition);
            for (int i = 0; i < (int)MPDSystem.CockpitDisplays.NUM_DISPLAYS; i++)
            {
                for (int j = 0; j < MPDConfiguration.NUM_SEQUENCES; j++)
                {
                    EditMPD.Displays[i].Formats[j] = Config.MPD.Displays[i].Formats[j];
                    EditMPD.Displays[i].Modes[j] = Config.MPD.Displays[i].Modes[j];
                }
            }
        }

        /// <summary>
        /// marshall data from the local misc setup into the configuration, persisting the configuration if directed.
        /// </summary>
        private void CopyEditToConfig(bool isPersist = false)
        {
            Config.MPD.CurCrewPosition = EditCrewMember.ToString();
            for (int i = 0; i < (int)MPDSystem.CockpitDisplays.NUM_DISPLAYS; i++)
            {
                for (int j = 0; j < MPDConfiguration.NUM_SEQUENCES; j++)
                {
                    Config.MPD.Displays[i].Formats[j] = EditMPD.Displays[i].Formats[j];
                    Config.MPD.Displays[i].Modes[j] = EditMPD.Displays[i].Modes[j];
                }
            }
            if (isPersist)
            {
                Config.Save(this, MPDSystem.SystemTag);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // utilities
        //
        // ------------------------------------------------------------------------------------------------------------

        private static TextBlock BuildFormatItem(string text, MPDConfiguration.DisplayFormats format)
            => new() { Text = text, Tag = ((int)format).ToString(), HorizontalAlignment = HorizontalAlignment.Center };

        private static TextBlock BuildModeItem(string text, MPDConfiguration.MasterModes mode)
            => new() { Text = text, Tag = ((int)mode).ToString(), HorizontalAlignment = HorizontalAlignment.Center };

        /// <summary>
        /// return a list of TextBlock instances to serve as the menu items for the mpd/mpcd format combo controls.
        /// the tags are JAFDTC.Models.F15E.MPD.MPDConfiguration.DisplayFormats.
        /// </summary>
        private static IList<TextBlock> BuildMPDFormatComboItems()
        {
            return new List<TextBlock>()
            {
                BuildFormatItem("", MPDConfiguration.DisplayFormats.NONE),
                BuildFormatItem("A/A RDR", MPDConfiguration.DisplayFormats.AA_RDR),
                BuildFormatItem("A/G RDR", MPDConfiguration.DisplayFormats.AG_RDR),
                BuildFormatItem("A/G DLVRY", MPDConfiguration.DisplayFormats.AG_DLVRY),
                BuildFormatItem("ADI", MPDConfiguration.DisplayFormats.ADI),
                BuildFormatItem("ARMT", MPDConfiguration.DisplayFormats.ARMT),
                BuildFormatItem("ENG", MPDConfiguration.DisplayFormats.ENG),
                BuildFormatItem("HSI", MPDConfiguration.DisplayFormats.HSI),
                BuildFormatItem("HUD", MPDConfiguration.DisplayFormats.HUD),
                BuildFormatItem("SMRT WPNS", MPDConfiguration.DisplayFormats.SMRT_WPNS),
                BuildFormatItem("TEWS", MPDConfiguration.DisplayFormats.TEWS),
                BuildFormatItem("TF", MPDConfiguration.DisplayFormats.TF),
                BuildFormatItem("TPOD", MPDConfiguration.DisplayFormats.TPOD),
                BuildFormatItem("TSD", MPDConfiguration.DisplayFormats.TSD)
            };
        }

        /// <summary>
        /// return a list of TextBlock instances to serve as the menu items for the mpd/mpcd mode combo controls.
        /// the tags are JAFDTC.Models.F15E.MPD.MPDConfiguration.MasterModes.
        /// </summary>
        private static IList<TextBlock> BuildMPDModeComboItems()
        {
            return new List<TextBlock>()
            {
                BuildModeItem("", MPDConfiguration.MasterModes.NONE),
                BuildModeItem("A/G", MPDConfiguration.MasterModes.A2G),
                BuildModeItem("A/A", MPDConfiguration.MasterModes.A2A),
                BuildModeItem("NAV", MPDConfiguration.MasterModes.NAV)
            };
        }

        /// <summary>
        /// TODO: documemtn
        /// </summary>
        private bool IsDisplaySequenceNone(int disp, int seq)
            => string.IsNullOrEmpty(EditMPD.Displays[disp].Formats[seq]) ||
               (EditMPD.Displays[disp].Formats[seq] == ((int)MPDConfiguration.DisplayFormats.NONE).ToString());

        /// <summary>
        /// change the selected crew member and update various ui and model state.
        /// </summary>
        private void SelectCrewMember(int member, bool isForce = false)
        {
            if ((member != EditCrewMember) || isForce)
            {
                if (member == (int)MPDSystem.CrewPositions.PILOT)
                {
                    uiGridPilotFormats.Visibility = Visibility.Visible;
                    uiGridWizzoFormats.Visibility = Visibility.Collapsed;
                }
                else
                {
                    uiGridPilotFormats.Visibility = Visibility.Collapsed;
                    uiGridWizzoFormats.Visibility = Visibility.Visible;
                }
                EditCrewMember = member;
                CopyEditToConfig(true);
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildCrewSelectMenu()
        {
            Visibility viz = Visibility.Collapsed;
            for (int i = (int)MPDSystem.CockpitDisplays.PILOT_L_MPD; i <= (int)MPDSystem.CockpitDisplays.PILOT_R_MPD; i++)
            {
                if (!EditMPD.Displays[i].IsDefault)
                {
                    viz = Visibility.Visible;
                }
            }
            uiMemberSelectItem0Icon.Visibility = viz;
            
            viz = Visibility.Collapsed;
            for (int i = (int)MPDSystem.CockpitDisplays.WSO_L_MPCD; i <= (int)MPDSystem.CockpitDisplays.WSO_R_MPCD; i++)
            {
                if (!EditMPD.Displays[i].IsDefault)
                {
                    viz = Visibility.Visible;
                }
            }
            uiMemberSelectItem1Icon.Visibility = viz;

            IsRebuildingUI = true;
            uiMemberSelectCombo.SelectedIndex = EditCrewMember;
            IsRebuildingUI = false;
        }

        /// <summary>
        /// update the selected display formats for the mpd/mpcd based on the current settings.
        /// </summary>
        private void RebuildFormatSelects()
        {
            IsRebuildingUI = true;
            foreach (KeyValuePair<string, Dictionary<string, ComboBox>> display in _formatComboMap)
            {
                int displayNum = int.Parse(display.Key);
                foreach (KeyValuePair<string, ComboBox> sequence in display.Value)
                {
                    int sequenceNum = int.Parse(sequence.Key);
                    string formatStr = EditMPD.Displays[displayNum].Formats[sequenceNum];
                    sequence.Value.SelectedIndex = (string.IsNullOrEmpty(formatStr)) ? 0 : int.Parse(formatStr);
                }
            }
            IsRebuildingUI = false;
        }

        /// <summary>
        /// update the selected master modes for the mpd/mpcd based on the current settings.
        /// </summary>
        private void RebuildModeSelects()
        {
            IsRebuildingUI = true;
            foreach (KeyValuePair<string, Dictionary<string, ComboBox>> display in _modeComboMap)
            {
                int displayNum = int.Parse(display.Key);
                foreach (KeyValuePair<string, ComboBox> sequence in display.Value)
                {
                    int sequenceNum = int.Parse(sequence.Key);
                    string formatStr = EditMPD.Displays[displayNum].Modes[sequenceNum];
                    sequence.Value.SelectedIndex = (string.IsNullOrEmpty(formatStr)) ? 0 : int.Parse(formatStr);
                }
            }
            IsRebuildingUI = false;
        }

        /// <summary>
        /// rebuild the link controls on the page based on where the configuration is linked to.
        /// </summary>
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, MFDSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        /// <summary>
        /// update the enable state on the ui elements based on the current settings. link controls must be set up
        /// vi RebuildLinkControls() prior to calling this function.
        /// </summary>
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(MFDSystem.SystemTag));
            foreach (KeyValuePair<string, Dictionary<string, ComboBox>> display in _formatComboMap)
            {
                foreach (KeyValuePair<string, ComboBox> sequence in display.Value)
                {
                    Utilities.SetEnableState(sequence.Value, isEditable);
                }
            }
            foreach (KeyValuePair<string, Dictionary<string, ComboBox>> display in _modeComboMap)
            {
                int displayNum = int.Parse(display.Key);
                foreach (KeyValuePair<string, ComboBox> sequence in display.Value)
                {
                    bool isNone = IsDisplaySequenceNone(displayNum, int.Parse(sequence.Key));
                    Utilities.SetEnableState(sequence.Value, isEditable && !isNone);                    
                }
            }
            foreach (KeyValuePair<string, Button> button in _resetDisplayMap)
            {
                int display = int.Parse(button.Key);
                bool isDefault = EditMPD.Displays[display].IsDefault;
                Utilities.SetEnableState(button.Value, isEditable && !isDefault);
            }

            for (int i = 0; i < (int)MPDSystem.CockpitDisplays.NUM_DISPLAYS; i++)
            {
                for (int j = 0; j < MPDConfiguration.NUM_SEQUENCES; j++)
                {
                    Visibility viz = Visibility.Visible;
                    if (j > 0)
                    {
                        viz = (IsDisplaySequenceNone(i, j - 1)) ? Visibility.Collapsed : Visibility.Visible;
                        _formatComboMap[i.ToString()][j.ToString()].Visibility = viz;
                        _modeComboMap[i.ToString()][j.ToString()].Visibility = viz;
                    }
                    if ((viz == Visibility.Visible) && IsDisplaySequenceNone(i, j))
                    {
                        _modeComboMap[i.ToString()][j.ToString()].Visibility = Visibility.Collapsed;
                    }
                    else if (viz == Visibility.Visible)
                    {
                        _modeComboMap[i.ToString()][j.ToString()].Visibility = Visibility.Visible;
                    }
                }
            }

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);
            Utilities.SetEnableState(uiPageBtnReset, !EditMPD.IsDefault);

            Utilities.SetEnableState(uiMemberPrevBtn, (EditCrewMember != (int)MPDSystem.CrewPositions.PILOT));
            Utilities.SetEnableState(uiMemberNextBtn, (EditCrewMember != (int)MPDSystem.CrewPositions.WSO));
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
                    RebuildCrewSelectMenu();
                    RebuildFormatSelects();
                    RebuildModeSelects();
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

        /// <summary>
        /// reset all button clicked: reset all settings back to default.
        /// </summary>
        private async void PageBtnReset_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configruation?",
                "Are you sure you want to reset the MPD/MPCD system configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(MPDSystem.SystemTag);
                Config.MPD.Reset();
                Config.Save(this, MPDSystem.SystemTag);
                CopyConfigToEdit();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, MPDSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                CopyEditToConfig(true);
                Config.UnlinkSystem(MPDSystem.SystemTag);
                Config.Save(this);
                CopyConfigToEdit();
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(MPDSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit();
            }
        }

        // ---- crew member selection ---------------------------------------------------------------------------------

        /// <summary>
        /// previous member click: advance to previous crew member position
        /// </summary>
        private void BtnMemberPrev_Click(object sender, RoutedEventArgs args)
        {
            SelectCrewMember(EditCrewMember - 1);
            uiMemberSelectCombo.SelectedIndex = EditCrewMember;
        }

        /// <summary>
        /// next member click: advance to previous crew member position
        /// </summary>
        private void BtnMemberNext_Click(object sender, RoutedEventArgs args)
        {
            SelectCrewMember(EditCrewMember + 1);
            uiMemberSelectCombo.SelectedIndex = EditCrewMember;
        }

        /// <summary>
        /// member combo selection changed: switch crew member positions and update the ui
        /// </summary>
        private void MemberSelectCombo_SelectionChanged(object sender, RoutedEventArgs args)
        {
            Grid item = (Grid)((ComboBox)sender).SelectedItem;
            if ((item != null) && (item.Tag != null))
            {
                SelectCrewMember(int.Parse((string)item.Tag));
            }
        }

        /// <summary>
        /// format combo selection changed: determine which sequence on which display was updated and mirror the
        /// change in the editor state.
        /// </summary>
        private void ComboFormatSel_SelectionChanged(object sender, RoutedEventArgs args)
        {
            if (!IsRebuildingUI)
            {
                ComboBox combo = (ComboBox)sender;
                string[] fields = combo.Tag.ToString().Split(',');
                int display = int.Parse(fields[0]);
                int sequence = int.Parse(fields[1]);
                EditMPD.Displays[display].Formats[sequence] = combo.SelectedIndex.ToString();
                if (combo.SelectedIndex == (int)MPDConfiguration.DisplayFormats.NONE)
                {
                    EditMPD.Displays[display].Modes[sequence] = ((int)MPDConfiguration.MasterModes.NONE).ToString();

                    for (int i = sequence + 1; i < MPDConfiguration.NUM_SEQUENCES; i++)
                    {
                        if (!IsDisplaySequenceNone(display, i))
                        {
                            EditMPD.Displays[display].Formats[i - 1] = EditMPD.Displays[display].Formats[i];
                            EditMPD.Displays[display].Modes[i - 1] = EditMPD.Displays[display].Modes[i];
                            EditMPD.Displays[display].Formats[i] = ((int)MPDConfiguration.DisplayFormats.NONE).ToString();
                            EditMPD.Displays[display].Modes[i] = ((int)MPDConfiguration.MasterModes.NONE).ToString();
                        }
                    }
                }
                CopyEditToConfig(true);
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// mode combo selection changed: determine which sequence on which display was updated and mirror the
        /// change in the editor state. note that a given master mode may be associated with at most one sequnces
        /// on each display.
        /// </summary>
        private void ComboModeSel_SelectionChanged(object sender, RoutedEventArgs args)
        {
            if (!IsRebuildingUI)
            {
                ComboBox combo = (ComboBox)sender;
                string[] fields = combo.Tag.ToString().Split(',');
                int display = int.Parse(fields[0]);
                int sequence = int.Parse(fields[1]);
                EditMPD.Displays[display].Modes[sequence] = combo.SelectedIndex.ToString();
                for (int i = 0; i < MPDConfiguration.NUM_SEQUENCES; i++)
                {
                    if ((i != sequence) &&
                        (EditMPD.Displays[display].Modes[sequence] == EditMPD.Displays[display].Modes[i]))
                    {
                        EditMPD.Displays[display].Modes[i] = ((int)MPDConfiguration.MasterModes.NONE).ToString();
                    }
                }
                CopyEditToConfig(true);
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public void BtnResetDisplay_Click(object sender, RoutedEventArgs args)
        {
            Button button = (Button)sender;
            int display = int.Parse((string)button.Tag);
            for (int i = 0; i < MPDConfiguration.NUM_SEQUENCES; i++)
            {
                EditMPD.Displays[display].Formats[i] = ((int)MPDConfiguration.DisplayFormats.NONE).ToString();
                EditMPD.Displays[display].Modes[i] = ((int)MPDConfiguration.MasterModes.NONE).ToString();
            }
            RebuildInterfaceState();
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
        /// on navigating to this page, set up our internal and ui state based on the configuration we are editing.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;
            Config = (F15EConfiguration)NavArgs.Config;

            Config.ConfigurationSaved += ConfigurationSavedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, MPDSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            CopyConfigToEdit();

            SelectCrewMember(EditCrewMember, true);
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }

        /// <summary>
        /// on navigating from this page, tear down our internal and ui state.
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            Config.ConfigurationSaved -= ConfigurationSavedHandler;

            base.OnNavigatedFrom(args);
        }
    }
}
