// ********************************************************************************************************************
//
// F16CEditDLNKPage.xaml.cs : ui c# for viper data link editor page
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
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.DLNK;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class F16CEditDLNKPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(DLNKSystem.SystemTag, "Datalink", "DLNK", Glyphs.DLNK, typeof(F16CEditDLNKPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditDLNK property.
        //
        private F16CConfiguration Config { get; set; }

        private DLNKSystem EditDLNK { get; set; }

        private string OwnshipDriverUID { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsRebuildingUI { get; set; }


        private List<ViperDriver> PilotDbase { get; set; }

        // ---- read-only properties

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        private readonly Dictionary<string, TextBox> _baseFieldValueMap;
        private readonly List<CheckBox> _tableTDOACkbxList;
        private readonly List<TextBox> _tableTNDLTextList;
        private readonly List<ComboBox> _tableCallsignComboList;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditDLNKPage()
        {
            InitializeComponent();

            EditDLNK = new DLNKSystem();
            for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
            {
                EditDLNK.TeamMembers[i].ErrorsChanged += TeamField_DataValidationError;
            }
            EditDLNK.ErrorsChanged += BaseField_DataValidationError;

            IsRebuildPending = false;
            IsRebuildingUI = false;

            PilotDbase = FileManager.LoadUserDbase<ViperDriver>(F16CConfigAuxCmdPilotDbase.PilotDbFilename);

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();

            _baseFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["OwnshipCallsign"] = uiOwnTextCallsign,
                ["OwnshipFENumber"] = uiOwnTextFENum
                
            };
            _tableTDOACkbxList = new List<CheckBox>()
            {
                uiTNDLCkbxTDOA1, uiTNDLCkbxTDOA2, uiTNDLCkbxTDOA3, uiTNDLCkbxTDOA4,
                uiTNDLCkbxTDOA5, uiTNDLCkbxTDOA6, uiTNDLCkbxTDOA7, uiTNDLCkbxTDOA8
            };
            _tableTNDLTextList = new List<TextBox>()
            {
                uiTNDLTextTNDL1, uiTNDLTextTNDL2, uiTNDLTextTNDL3, uiTNDLTextTNDL4,
                uiTNDLTextTNDL5, uiTNDLTextTNDL6, uiTNDLTextTNDL7, uiTNDLTextTNDL8
            };
            _tableCallsignComboList = new List<ComboBox>()
            {
                uiTNDLComboCallsign1, uiTNDLComboCallsign2, uiTNDLComboCallsign3, uiTNDLComboCallsign4,
                uiTNDLComboCallsign5, uiTNDLComboCallsign6, uiTNDLComboCallsign7, uiTNDLComboCallsign8
            };
            _defaultBorderBrush = uiTNDLTextTNDL1.BorderBrush;
            _defaultBkgndBrush = uiTNDLTextTNDL1.Background;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshal data from the datalink configuration to our local edit state.
        /// </summary>
        private void CopyConfigToEdit()
        {
            EditDLNK.Ownship = Config.DLNK.Ownship;
            EditDLNK.OwnshipCallsign = Config.DLNK.OwnshipCallsign;
            EditDLNK.OwnshipFENumber = Config.DLNK.OwnshipFENumber;
            EditDLNK.IsOwnshipLead = Config.DLNK.IsOwnshipLead;
            for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
            {
                EditDLNK.TeamMembers[i].TDOA = Config.DLNK.TeamMembers[i].TDOA;
                EditDLNK.TeamMembers[i].TNDL = new(Config.DLNK.TeamMembers[i].TNDL);
                EditDLNK.TeamMembers[i].DriverUID = new(Config.DLNK.TeamMembers[i].DriverUID);
            }
        }

        /// <summary>
        /// marshal data from our local edit state to the datalink configuration if there are no errors. configuration
        /// is optionally persisted.
        /// </summary>
        private void CopyEditToConfig(bool isPersist = false)
        {
            if (!CurStateHasErrors())
            {
                Config.DLNK.Ownship = new(EditDLNK.Ownship);
                //
                // OwnshipCallsign, OwnshipFENumber fields use text masks and can come back as "--" when empty. this
                // is really "" and, since that value is OK, remove the error.
                //
                Config.DLNK.OwnshipCallsign = (EditDLNK.OwnshipCallsign == "––") ? "" : EditDLNK.OwnshipCallsign;
                Config.DLNK.OwnshipFENumber = (EditDLNK.OwnshipFENumber == "––") ? "" : EditDLNK.OwnshipFENumber;
                Config.DLNK.IsOwnshipLead = EditDLNK.IsOwnshipLead;
                for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
                {
                    Config.DLNK.TeamMembers[i].TDOA = EditDLNK.TeamMembers[i].TDOA;
                    Config.DLNK.TeamMembers[i].TNDL = new(EditDLNK.TeamMembers[i].TNDL);
                    Config.DLNK.TeamMembers[i].DriverUID = new(EditDLNK.TeamMembers[i].DriverUID);
                }

                if (isPersist)
                {
                    Config.Save(this, DLNKSystem.SystemTag);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // field validation
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// set the border brush and background for a TextBox based on validity. valid fields use the defaults, invalid
        /// fields use ErrorFieldBorderBrush from the resources.
        /// </summary>
        private void SetFieldValidState(TextBox field, bool isValid)
        {
            field.BorderBrush = (isValid) ? _defaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
            field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
        }

        /// <summary>
        /// event handler for a validation error. check for errors in the base EditDLNK.TeamMembers fields and update
        /// the state of the ui element to indicate error state.
        /// 
        /// NOTE: of the properties we bind to the ui, only TeamMember.TNDL fields can raise an error.
        /// </summary>
        private void TeamField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            if (args.PropertyName == null)
            {
                for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
                {
                    SetFieldValidState(_tableTNDLTextList[i], EditDLNK.TeamMembers[i].HasErrors);
                }
            }
            else
            {
                for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
                {
                    if (sender.Equals(EditDLNK.TeamMembers[i]))
                    {
                        SetFieldValidState(_tableTNDLTextList[i], !EditDLNK.TeamMembers[i].HasErrors);
                        break;
                    }
                }
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// event handler for a validation error. check for errors in the base EditDLNK fields and update the state of
        /// the ui element to indicate error state.
        /// 
        /// NOTE: of the properties we bind to the ui, only TeamMember.TNDL fields can raise an error.
        /// </summary>
        private void BaseField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            if (args.PropertyName == null)
            {
                Dictionary<string, bool> map = new();
                foreach (string error in EditDLNK.GetErrors(null))
                {
                    map[error] = true;
                }
                foreach (KeyValuePair<string, TextBox> kvp in _baseFieldValueMap)
                {
                    SetFieldValidState(kvp.Value, !map.ContainsKey(kvp.Key));
                }
            }
            else if ((args.PropertyName == "OwnshipCallsign") && (EditDLNK.OwnshipCallsign == "––"))
            {
                // OwnshipCallsign field uses text mask and can come back as "--" when empty. this is really "" and,
                // since that value is OK, remove the error.
                //
                EditDLNK.ClearErrors("OwnshipCallsign");
                SetFieldValidState(_baseFieldValueMap[args.PropertyName], true);
            }
            else if ((args.PropertyName == "OwnshipFENumber") && (EditDLNK.OwnshipFENumber == "––"))
            {
                // OwnshipFENumber field uses text mask and can come back as "--" when empty. this is really "" and,
                // since that value is OK, remove the error.
                //
                EditDLNK.ClearErrors("OwnshipFENumber");
                SetFieldValidState(_baseFieldValueMap[args.PropertyName], true);
            }
            else
            {
                SetFieldValidState(_baseFieldValueMap[args.PropertyName],
                                   (((List<string>)EditDLNK.GetErrors(args.PropertyName)).Count == 0));
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// returns true if the current state in EditDLNK has errors, false otherwise.
        /// </summary>
        private bool CurStateHasErrors()
        {
            for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
            {
                if (EditDLNK.TeamMembers[i].HasErrors)
                {
                    return true;
                }
            }
            return EditDLNK.HasErrors;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // utilities
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// find the pilot in the pilot database by uid. returns the ViperDriver instance on success, null if not found.
        /// </summary>
        private ViperDriver FindPilotByUID(string UID)
        {
            // TODO: maybe optimize; but honestly, pilot list is likely short so prolly not worth the effort...
            foreach (ViperDriver pilot in PilotDbase)
            {
                if (pilot.UID.Equals(UID))
                {
                    return pilot;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns a StackPanel for use as a pilot item in the callsign combo boxes. items are a pilot name with the
        /// current callsign from the settings getting a bullet-prefix. the stack panel tag is set to the uid of the
        /// pilot. a pilot of null builds a panel with an empty name and "0" tag.
        /// </summary>
        private static StackPanel BuildPilotItemStackPanel(ViperDriver pilot)
        {
            bool isOwnship = ((pilot != null) && (pilot.Name == Settings.Callsign));
            StackPanel itemPanel = new()
            {
                Orientation = Orientation.Horizontal,
                Tag = (pilot != null) ? pilot.UID : "0",
            };
            FontIcon itemIcon = new()
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                Glyph = (isOwnship) ? "\xE915" : " "
            };
            TextBlock itemText = new()
            {
                Text = (pilot != null) ? pilot.Name : "",
                FontWeight = (isOwnship) ? FontWeights.Bold : FontWeights.Normal
            };
            itemIcon.Width = 20;
            itemText.Margin = new() { Left = 4 };
            itemPanel.Children.Add(itemIcon);
            itemPanel.Children.Add(itemText);
            return itemPanel;
        }

        /// <summary>
        /// returns a list of StackPanel instances to serve as the menu items for the callsign format combo controls.
        /// the tags are set to the uid of the pilot from the pilot database. first element is blank with a "0" tag.
        /// </summary>
        private IList<StackPanel> BuildCallsignComboItems()
        {
            List<StackPanel> pilotItems = new()
            {
                BuildPilotItemStackPanel(null)
            };
            for (int i = 0; i < PilotDbase.Count; i++)
            {
                pilotItems.Add(BuildPilotItemStackPanel(PilotDbase[i]));
            };
            return pilotItems;
        }

        /// <summary>
        /// rebuild the menus for the callsign combo boxes in the interaface. this should only need to be done upon
        /// init or when the pilot database changes.
        /// </summary>
        private void RebuildCallsignCombos()
        {
            IsRebuildingUI = true;
            Dictionary<string, int> uidToIndexMap = new();
            for (int i = 0; i < PilotDbase.Count; i++)
            {
                uidToIndexMap[PilotDbase[i].UID] = i + 1;
                if (PilotDbase[i].Name == Settings.Callsign)
                {
                    OwnshipDriverUID = PilotDbase[i].UID;
                }
            }

            for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
            {
                _tableCallsignComboList[i].ItemsSource = BuildCallsignComboItems();

                string uid = EditDLNK.TeamMembers[i].DriverUID;
                if (uidToIndexMap.ContainsKey(uid) && (_tableCallsignComboList[i].SelectedIndex != uidToIndexMap[uid]))
                {
                    _tableCallsignComboList[i].SelectedIndex = uidToIndexMap[uid];
                }
                else if (!uidToIndexMap.ContainsKey(uid) && !string.IsNullOrEmpty(uid))
                {
                    // tndl team member maps to a pilot that no longer is in the database, reset the pilot combo to
                    // the generic entry (index 0) and reset the team member to be empty.
                    //
                    _tableCallsignComboList[i].SelectedIndex = 0;
                    EditDLNK.TeamMembers[i].TDOA = false;
                    EditDLNK.TeamMembers[i].TNDL = "";
                    EditDLNK.TeamMembers[i].DriverUID = "";
                }
            }
            IsRebuildingUI = false;
            CopyEditToConfig(true);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildOwnshipMenu()
        {
            int indexPDbOwnship = -1;
            int indexCurOwnship = -1;
            List<string> list = new();
            for (int i = 0; i < EditDLNK.TeamMembers.Length; i++)
            {
                if (!string.IsNullOrEmpty(EditDLNK.TeamMembers[i].TNDL))
                {
                    list.Add((i + 1).ToString());
                    if (!string.IsNullOrEmpty(EditDLNK.Ownship) && (int.Parse(EditDLNK.Ownship) == (i + 1)))
                    {
                        indexCurOwnship = list.Count - 1;
                    }
                }
                if (EditDLNK.TeamMembers[i].DriverUID == OwnshipDriverUID)
                {
                    indexPDbOwnship = list.Count - 1;
                }
            }
            uiOwnComboEntry.ItemsSource = list;
            if (indexPDbOwnship != -1)
            {
                uiOwnComboEntry.SelectedIndex = indexPDbOwnship;
            }
            else if (indexCurOwnship != -1)
            {
                uiOwnComboEntry.SelectedIndex = indexCurOwnship;
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, DLNKSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        /// <summary>
        /// update the enable state on the ui elements based on the current settings. link controls must be set up
        /// vi RebuildLinkControls() prior to calling this function.
        /// </summary>
        public void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(DLNKSystem.SystemTag));

            bool isOwnInTable = false;
            bool isAnyInTable = false;
            for (int i = 0; i < _tableTDOACkbxList.Count; i++)
            {
                bool isEmpty = string.IsNullOrEmpty(_tableTNDLTextList[i].Text);
                Utilities.SetEnableState(_tableTDOACkbxList[i], isEditable && !isEmpty);
                bool isCallsignSelect = _tableCallsignComboList[i].SelectedIndex > 0;
                Utilities.SetEnableState(_tableTNDLTextList[i], isEditable && !isCallsignSelect);
                Utilities.SetEnableState(_tableCallsignComboList[i], isEditable);

                if (EditDLNK.TeamMembers[i].DriverUID == OwnshipDriverUID)
                {
                    isOwnInTable = true;
                }
                isAnyInTable |= !EditDLNK.TeamMembers[i].IsDefault;
            }

            Utilities.SetEnableState(uiOwnComboEntry, isEditable && !isOwnInTable && isAnyInTable);
            Utilities.SetEnableState(uiOwnCkbxLead, isEditable);
            Utilities.SetEnableState(uiPageBtnResetAll, !EditDLNK.IsDefault);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void RebuildInterfaceState()
        {
            if (!IsRebuildPending)
            {
                IsRebuildPending = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    IsRebuildingUI = true;
                    RebuildOwnshipMenu();
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

        // ---- page buttons ------------------------------------------------------------------------------------------

        /// <summary>
        /// reset all button click: reset all dlnk settings back to their defaults if the user consents.
        /// </summary>
        private async void PageBtnResetAll_Click(object sender, RoutedEventArgs args)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configuration?",
                "Are you sure you want to reset the datalink configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(DLNKSystem.SystemTag);
                Config.DLNK.Reset();
                Config.Save(this, DLNKSystem.SystemTag);
                CopyConfigToEdit();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, DLNKSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                Config.UnlinkSystem(DLNKSystem.SystemTag);
                Config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(DLNKSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit();
            }
        }

        // ---- ownship elements --------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void OwnCkbxLead_Click(object sender, RoutedEventArgs args)
        {
            // HACK: x:Bind doesn't work with bools? seems that way? this is a hack.
            //
            CheckBox cbox = (CheckBox)sender;
            EditDLNK.IsOwnshipLead = (bool)cbox.IsChecked;
            CopyEditToConfig(true);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void OwnComboEntry_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            ComboBox comboBox = (ComboBox)sender;
            if (!IsRebuildingUI)
            {
                if (!string.IsNullOrEmpty(EditDLNK.Ownship))
                {
                    EditDLNK.TeamMembers[int.Parse(EditDLNK.Ownship) - 1].TDOA = false;
                }
                EditDLNK.Ownship = (string)comboBox.SelectedItem;
                if (!string.IsNullOrEmpty(EditDLNK.Ownship))
                {
                    EditDLNK.TeamMembers[int.Parse(EditDLNK.Ownship) - 1].TDOA = true;
                }
                CopyEditToConfig(true);
            }
        }

        /// <summary>
        /// text box lost focus: copy the local backing values to the configuration (note this is predicated on error
        /// status) and rebuild the interface state.
        ///
        /// NOTE: though the text box has lost focus, the update may not yet have propagated into state. use the
        /// NOTE: dispatch queue to give in-flight state updates time to complete.
        /// </summary>
        private void OwnText_LostFocus(object sender, RoutedEventArgs args)
        {
            TextBox textBox = (TextBox)sender;
            if (((textBox == uiOwnTextCallsign) || (textBox == uiOwnTextFENum)) && (textBox.Text == "––"))
            {
                // callsign and flight/element fields uses text mask and can come back as "--" when empty. this
                // is really "" and, since that value is OK, remove the error. note that as we just lost focus,
                // the bound property in EditDLNK.OwnshipCallsign or .OwnshipFENumber may not yet be set up.
                //
                EditDLNK.ClearErrors((textBox == uiOwnTextCallsign) ? "OwnshipCallsign" : "OwnshipFENumber");
                SetFieldValidState(textBox, true);
            }

            // CONSIDER: may be better here to handle this in a property changed handler rather than here?
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CopyEditToConfig(true);
            });
        }

        // ---- member interface elements -----------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void TNDLCkbxTDOA_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(true);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void TNDLTextTNDL_LostFocus(object sender, RoutedEventArgs args)
        {
            TextBox tbox = (TextBox)sender;
            if (string.IsNullOrEmpty(tbox.Text))
            {
                int index = int.Parse((string)tbox.Tag);
                EditDLNK.TeamMembers[index].Reset();
                if (!string.IsNullOrEmpty(EditDLNK.Ownship) && (EditDLNK.Ownship == (index + 1).ToString()))
                {
                    uiOwnComboEntry.SelectedIndex = -1;
                }
            }
            CopyEditToConfig(true);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void TNDLComboCallsign_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (IsRebuildingUI)
            {
                return;
            }

            ComboBox comboBox = (ComboBox)sender;
            int index = int.Parse((string)comboBox.Tag);
            StackPanel item = (StackPanel)comboBox.SelectedItem;
            ViperDriver pilot = FindPilotByUID((string)item.Tag);

            bool isOldOwnByCallsign = (OwnshipDriverUID == EditDLNK.TeamMembers[index].DriverUID);
            bool isNewOwnByCallsign = false;

            int curPilotIndex = -1;
            if (pilot != null)
            {
                isNewOwnByCallsign = (OwnshipDriverUID == pilot.UID);
                for (int i = 0; i < _tableCallsignComboList.Count; i++)
                {
                    StackPanel otherItem = (StackPanel)_tableCallsignComboList[i].SelectedItem;
                    if ((i != index) && (otherItem != null) && otherItem.Tag.Equals(pilot.UID))
                    {
                        curPilotIndex = i;
                        break;
                    }
                }
            }

            if (!isOldOwnByCallsign && !isNewOwnByCallsign)
            {
                // we are not editing a team member entry that is ownship (either implicitly through the callsign or
                // explicitly through the ownship combo). either clear the entry (if we are setting the generic
                // callsign entry and the TNDL is not empty), or setup the entry from the pilot database. if we have
                // a pilot moving locations in the table, reset her old location (but preserve TDOA).
                //
                bool isCurTDOA = false;
                if (curPilotIndex != -1)
                {
                    IsRebuildingUI = true;
                    _tableCallsignComboList[curPilotIndex].SelectedIndex = 0;
                    IsRebuildingUI = false;
                    isCurTDOA = EditDLNK.TeamMembers[curPilotIndex].TDOA;
                    EditDLNK.TeamMembers[curPilotIndex].Reset();
                }
                if ((comboBox.SelectedIndex == 0) && !string.IsNullOrEmpty(EditDLNK.TeamMembers[index].TNDL))
                {
                    EditDLNK.TeamMembers[index].Reset();
                    CopyEditToConfig(true);
                }
                else if (comboBox.SelectedIndex != 0)
                {
                    // rebuild here to avoid some visual glitches from enabled -> disabled transition with the field
                    // contents updating around the same time.
                    //
                    RebuildEnableState();
                    EditDLNK.TeamMembers[index].TDOA = isCurTDOA;
                    EditDLNK.TeamMembers[index].TNDL = pilot.TNDL;
                    EditDLNK.TeamMembers[index].DriverUID = pilot.UID;
                    CopyEditToConfig(true);
                }
            }
            else if (isNewOwnByCallsign)
            {
                // we are making an edit that ends with an implicitly defined (ie, through callsign) ownship in the
                // team member table. clear any old entry associated with the callsign and update the new entry to
                // be ownship. if there was an ownship specified explicitly, disable TDOA. RebuildOwnshipMenu and
                // RebuildEnableState will handle the ownship menu post save.
                //
                if (curPilotIndex != -1)
                {
                    IsRebuildingUI = true;
                    _tableCallsignComboList[curPilotIndex].SelectedIndex = 0;
                    IsRebuildingUI = false;
                    EditDLNK.TeamMembers[curPilotIndex].Reset();
                }
                if (!isOldOwnByCallsign && !string.IsNullOrEmpty(EditDLNK.Ownship))
                {
                    EditDLNK.TeamMembers[int.Parse(EditDLNK.Ownship)-1].TDOA = false;
                }
                //
                // rebuild here to avoid some visual glitches from enabled -> disabled transition with the field
                // contents updating around the same time.
                //
                RebuildEnableState();
                EditDLNK.Ownship = (index + 1).ToString();
                EditDLNK.TeamMembers[index].TDOA = true;
                EditDLNK.TeamMembers[index].TNDL = pilot.TNDL;
                EditDLNK.TeamMembers[index].DriverUID = pilot.UID;
                CopyEditToConfig(true);
            }
            else if (isOldOwnByCallsign)
            {
                // we are making an edit that removes an implicitly defined (ie, through callsign) ownship in the
                // team member table. update the entry and clear the ownship.
                //
                // rebuild here to avoid some visual glitches from enabled -> disabled transition with the field
                // contents updating around the same time.
                //
                RebuildEnableState();
                EditDLNK.Ownship = "";
                EditDLNK.TeamMembers[index].TDOA = false;
                EditDLNK.TeamMembers[index].TNDL = (pilot != null) ? pilot.TNDL : "";
                EditDLNK.TeamMembers[index].DriverUID = (pilot != null) ? pilot.UID : "";
                CopyEditToConfig(true);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on configuration saved, rebuild the interface to ensure ui state is consistent with confiugration.
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
            PilotDbase = FileManager.LoadUserDbase<ViperDriver>(F16CConfigAuxCmdPilotDbase.PilotDbFilename);
            RebuildCallsignCombos();
        }

        /// <summary>
        /// on navigating to this page, set up internal and ui state.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;
            Config = (F16CConfiguration)NavArgs.Config;

            NavArgs.ConfigPage.AuxCommandInvoked += AuxCommandInvokedHandler;
            Config.ConfigurationSaved += ConfigurationSavedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, DLNKSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            CopyConfigToEdit();

            RebuildCallsignCombos();
            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }

        /// <summary>
        /// on navigating from this page, tear down internal and ui state. primarily, need to stop hooking config
        /// saved events as the configuration may outlive us.
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            NavArgs.ConfigPage.AuxCommandInvoked -= AuxCommandInvokedHandler;
            Config.ConfigurationSaved -= ConfigurationSavedHandler;

            base.OnNavigatedFrom(args);
        }
    }
}
