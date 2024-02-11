// ********************************************************************************************************************
//
// F16CEditHARMPage.cs : ui c# for viper harm alic editor page
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
using JAFDTC.Models.DCS;
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.HARM;
using JAFDTC.UI.App;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class F16CEditHARMPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(HARMSystem.SystemTag, "HARM ALIC Tables", "HARM ALIC", Glyphs.HARM, typeof(F16CEditHARMPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditHARM/EditTable properties.
        //
        private F16CConfiguration Config { get; set; }

        // NOTE: the ui always interacts with EditHARM.Tables[0] when editing ALIC, EditTable defines which table the ui is currently
        // NOTE: editing.
        //
        private HARMSystem EditHARM { get; set; }

        private int EditTable { get; set; }

        private bool IsRebuildPending { get; set; }

        // ---- readonly properties

        private readonly HARMSystem _harmSysDefaults;

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        private readonly List<string> _emitterTitles;
        private readonly Dictionary<string, Emitter> _emitterTitleMap;

        private readonly Dictionary<string, TextBox> _baseFieldValueMap;
        private readonly List<TextBox> _tableCodeFields;
        private readonly List<FontIcon> _tableSelComboIcons;
        private readonly List<FontIcon> _tableEditFields;
        private readonly List<List<TextBlock>> _tableFields;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditHARMPage()
        {
            InitializeComponent();

            EditHARM = new HARMSystem();

            // NOTE: ui will operate on Tables[0], regardless of which table is actually selected.
            //
            EditHARM.Tables[0].ErrorsChanged += CodeField_DataValidationError;
            EditHARM.Tables[0].PropertyChanged += CodeField_PropertyChanged;

            EditTable = (int)TableNumbers.TABLE1;

            IsRebuildPending = false;

            _harmSysDefaults = HARMSystem.ExplicitDefaults;

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();

            _emitterTitles = new();
            _emitterTitleMap = new();
            foreach (Emitter emitter in EmitterDbase.Instance.Find())
            {
                string item = $"{emitter.Country} – {emitter.Type} – {emitter.Name}";
                if (!string.IsNullOrEmpty(emitter.NATO))
                {
                    item += $" ({emitter.NATO})";
                }
                _emitterTitles.Add(item);
                _emitterTitleMap[item] = emitter;
            }

            _baseFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["CodeT1"] = uiT1ValueCode,
                ["CodeT2"] = uiT2ValueCode,
                ["CodeT3"] = uiT3ValueCode,
                ["CodeT4"] = uiT4ValueCode,
                ["CodeT5"] = uiT5ValueCode
            };
            _tableCodeFields = new List<TextBox>()
            {
                uiT1ValueCode, uiT2ValueCode, uiT3ValueCode, uiT4ValueCode, uiT5ValueCode
            };
            _tableSelComboIcons = new List<FontIcon>()
            {
                uiALICSelectItem1Icon, uiALICSelectItem2Icon, uiALICSelectItem3Icon
            };
            _tableEditFields = new List<FontIcon>()
            {
                uiT1IconEdit, uiT2IconEdit, uiT3IconEdit, uiT4IconEdit, uiT5IconEdit
            };
            _tableFields = new List<List<TextBlock>>()
            {
                new() { uiT1RWRText, uiT1ValueCountry, uiT1ValueType, uiT1ValueName },
                new() { uiT2RWRText, uiT2ValueCountry, uiT2ValueType, uiT2ValueName },
                new() { uiT3RWRText, uiT3ValueCountry, uiT3ValueType, uiT3ValueName },
                new() { uiT4RWRText, uiT4ValueCountry, uiT4ValueType, uiT4ValueName },
                new() { uiT5RWRText, uiT5ValueCountry, uiT5ValueType, uiT5ValueName }
            };
            _defaultBorderBrush = uiT1ValueCode.BorderBrush;
            _defaultBkgndBrush = uiT1ValueCode.Background;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        // marshall data between our local alic table and the appropriate table in the harm configuration. to
        // simplify bindings, note that the local copy of the program being edited is always in EditHARM.Table[0]
        // regardless of which table we are editing. note that data is kept in EditHARM in "explict" form (where the
        // actual values are placed in fields, not "" for defaults). we must use care when using IsDefault and
        // friends on our state.
        //
        private void CopyConfigToEdit(int table)
        {
            // NOTE: we don't marshall the program number here, it shouldn't change

            for (int i = 0; i < EditHARM.Tables[0].Table.Count; i++)
            {
                EditHARM.Tables[0].Table[i].Code = Config.HARM.Tables[table].Table[i].Code;
            }

            ALICTable curTable = EditHARM.Tables[0];
            ALICTable dfltTable = _harmSysDefaults.Tables[table];
            for (int i = 0; i < curTable.Table.Count; i++)
            {
                curTable.Table[i].Code = (curTable.Table[i].Code != "") ? curTable.Table[i].Code : dfltTable.Table[i].Code;
            }
        }

        private void CopyEditToConfig(int table, bool isPersist = false)
        {
            Debug.Assert(table == Config.HARM.Tables[table].Number);

            if (!CurStateHasErrors())
            {
                // NOTE: we don't marshall the program number here, it shouldn't change

                ALICTable curTable = EditHARM.Tables[0];
                ALICTable dfltTable = _harmSysDefaults.Tables[table];
                for (int i = 0; i < curTable.Table.Count; i++)
                {
                    Config.HARM.Tables[table].Table[i].Code = (curTable.Table[i].Code != dfltTable.Table[i].Code) ? curTable.Table[i].Code : "";
                }

                if (isPersist)
                {
                    Config.Save(this, HARMSystem.SystemTag);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // field validation
        //
        // ------------------------------------------------------------------------------------------------------------

        private void SetFieldValidState(TextBox field, bool isValid)
        {
            field.BorderBrush = (isValid) ? _defaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
            field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
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
                SetFieldValidState(kvp.Value, !map.ContainsKey(kvp.Key));
            }
        }

        // validation error: update ui state for the various components (base, chaff program, flare program)
        // that may have errors.
        //
        private void CodeField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            if (args.PropertyName == null)
            {
                ValidateAllFields(_baseFieldValueMap, EditHARM.Tables[0].GetErrors(null));
            }
            else
            {
                List<string> errors = (List<string>)EditHARM.Tables[0].GetErrors(args.PropertyName);
                SetFieldValidState(_baseFieldValueMap[args.PropertyName], (errors.Count == 0));
            }
            RebuildInterfaceState();
        }

        // property changed: rebuild interface state to account for configuration changes.
        //
        private void CodeField_PropertyChanged(object sender, EventArgs args)
        {
            for (int row = 0; row < 5; row++)
            {
                RebuildTableRowContent(row);
            }
        }

        // returns true if the current state has errors, false otherwise.
        //
        private bool CurStateHasErrors()
        {
            return EditHARM.Tables[0].HasErrors;
        }

        // returns true if the current table being edited is default, false otherwise.
        private bool EditTableIsDefault()
        {
            ObservableCollection<TableCode> table = EditHARM.Tables[0].Table;
            ObservableCollection<TableCode> defaultTable = _harmSysDefaults.Tables[EditTable].Table;
            for (int i = 0; i < table.Count; i++)
            {
                if (!string.IsNullOrEmpty(table[i].Code) && (table[i].Code != defaultTable[i].Code))
                {
                    return false;
                }
            }
            return true;
        }

        // returns true if the current state is default, false otherwise.
        private bool PageIsDefault()
        {
            ObservableCollection<TableCode> table = EditHARM.Tables[0].Table;
            for (int i = 0; i < EditHARM.Tables.Length; i++)
            {
                if (((EditTable == i) && !EditTableIsDefault()) ||
                    ((EditTable != i) && !Config.HARM.Tables[i].IsDefault))
                {
                    return false;
                }
            }
            return true;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        // change the selected alic table and update various ui and model state.
        //
        private void SelectTable(int table)
        {
            if (table != EditTable)
            {
                CopyEditToConfig(EditTable, true);
                EditTable = table;
                CopyConfigToEdit(EditTable);
                RebuildInterfaceState();
            }
        }

        // TODO: document
        private void RebuildTableSelectMenu()
        {
            ObservableCollection<TableCode> table = EditHARM.Tables[0].Table;
            for (int i = 0; i < Config.HARM.Tables.Length; i++)
            {
                Visibility viz = Visibility.Collapsed;
                if (((EditTable == i) && !EditTableIsDefault()) ||
                    ((EditTable != i) && !Config.HARM.Tables[i].IsDefault))
                {
                    viz = Visibility.Visible;
                }
                _tableSelComboIcons[i].Visibility = viz;
            }
        }

        // TODO: document
        private void RebuildTableRowContent(int tableRow)
        {
            string curCode = _tableCodeFields[tableRow].Text;
            string dfltCode = _harmSysDefaults.Tables[EditTable].Table[tableRow].Code;

            if (int.TryParse((string.IsNullOrEmpty(curCode)) ? dfltCode : curCode, out int alicCode))
            {
                List<Emitter> emitterList = EmitterDbase.Instance.Find(alicCode);

                _tableEditFields[tableRow].Visibility = (string.IsNullOrEmpty(curCode) || (curCode == dfltCode))
                                                      ? Visibility.Collapsed : Visibility.Visible;

                List<TextBlock> fields = _tableFields[tableRow];
                fields[0].Text = (emitterList.Count != 0) ? emitterList[0].F16RWR : "–";
                fields[1].Text = (emitterList.Count != 0) ? emitterList[0].Country : "–";
                fields[2].Text = (emitterList.Count != 0) ? emitterList[0].Type : "–";
                string name = "Unknown";
                if (emitterList.Count != 0)
                {
                    name = emitterList[0].Name;
                    if (!string.IsNullOrEmpty(emitterList[0].NATO))
                    {
                        name += $" ({emitterList[0].NATO})";
                    }
                }
                fields[3].Text = name;
            }
        }

        // update the placeholder text to match the defaults for the selected table.
        //
        private void RebuildFieldPlaceholders()
        {
            var table = _harmSysDefaults.Tables[EditTable];
            for (int i = 0; i < table.Table.Count; i++)
            {
                _tableCodeFields[i].PlaceholderText = table.Table[i].Code;
            }
        }

        // TODO: document
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, HARMSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        // update the enable state on the ui elements based on the current settings. link controls must be set up
        // vi RebuildLinkControls() prior to calling this function.
        //
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(HARMSystem.SystemTag));
            Utilities.SetEnableState(uiT1ValueCode, isEditable);
            Utilities.SetEnableState(uiT2ValueCode, isEditable);
            Utilities.SetEnableState(uiT3ValueCode, isEditable);
            Utilities.SetEnableState(uiT4ValueCode, isEditable);
            Utilities.SetEnableState(uiT5ValueCode, isEditable);

            Utilities.SetEnableState(uiT1BtnAdd, isEditable);
            Utilities.SetEnableState(uiT2BtnAdd, isEditable);
            Utilities.SetEnableState(uiT3BtnAdd, isEditable);
            Utilities.SetEnableState(uiT4BtnAdd, isEditable);
            Utilities.SetEnableState(uiT5BtnAdd, isEditable);

            bool isEditTableDefault = true;
            for (int i = 0; i < EditHARM.Tables[0].Table.Count; i++)
            {
                if (EditHARM.Tables[0].Table[i].Code != _harmSysDefaults.Tables[EditTable].Table[i].Code)
                {
                    isEditTableDefault = false;
                    break;
                }
            }
            Utilities.SetEnableState(uiALICBtnResetTable, !isEditTableDefault && isEditable);

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);

            Utilities.SetEnableState(uiPageBtnResetAll, !PageIsDefault());

            bool isNoErrs = !CurStateHasErrors();
            Utilities.SetEnableState(uiALICSelectCombo, isNoErrs);
            Utilities.SetEnableState(uiALICBtnNext, (isNoErrs && (EditTable != (int)TableNumbers.TABLE3)));
            Utilities.SetEnableState(uiALICBtnPrev, (isNoErrs && (EditTable != (int)TableNumbers.TABLE1)));
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
                    RebuildTableSelectMenu();
                    for (int row = 0; row < 5; row++)
                    {
                        RebuildTableRowContent(row);
                    }
                    RebuildFieldPlaceholders();
                    RebuildLinkControls();
                    RebuildEnableState();
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

        // reset all button click: reset all harm alic settings back to their defaults.
        //
        private async void PageBtnResetAll_Click(object sender, RoutedEventArgs args)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configruation?",
                "Are you sure you want to reset the HARM ALIC configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(HARMSystem.SystemTag);
                Config.HARM.Reset();
                Config.Save(this, HARMSystem.SystemTag);
                CopyConfigToEdit(EditTable);
            }
        }

        // TODO: document
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, HARMSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                CopyEditToConfig(EditTable, true);
                Config.UnlinkSystem(HARMSystem.SystemTag);
                Config.Save(this);
                CopyConfigToEdit(EditTable);
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(HARMSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit(EditTable);
            }
        }

        // reset table click: reset table to defaults.
        //
        private void ALICBtnResetTable_Click(object sender, RoutedEventArgs args)
        {
            Config.HARM.Tables[EditTable].Reset();
            Config.Save(this, HARMSystem.SystemTag);
            CopyConfigToEdit(EditTable);
        }

        // --- add emitter --------------------------------------------------------------------------------------------

        // add button click: open the ui to prompt to select an emitter.
        //
        private async void ALICBtnAdd_Click(object sender, RoutedEventArgs args)
        {
            Button btnAdd = (Button)sender;
            string te = $"T{int.Parse((string)btnAdd.Tag) + 1}";
            string table = $"Table {int.Parse((string)((Grid)uiALICSelectCombo.SelectedItem).Tag) + 1}";
            GetListDialog dialog = new(_emitterTitles, "Emitter", 425)
            {
                XamlRoot = Content.XamlRoot,
                Title = $"Select an Emitter for ALIC {table}, Entry {te}",
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                Button button = (Button)sender;
                Emitter emitter = _emitterTitleMap[(string)dialog.SelectedItem];
                int entry = int.Parse((string)button.Tag);
                EditHARM.Tables[0].Table[entry].Code = emitter.ALICCode.ToString("000");

                CopyEditToConfig(EditTable, true);
            }
        }

        // --- table selection ----------------------------------------------------------------------------------------

        // previous program button click: advance to the previous table.
        //
        private void ALICBtnPrev_Click(object sender, RoutedEventArgs args)
        {
            SelectTable(EditTable - 1);
            uiALICSelectCombo.SelectedIndex = EditTable;
        }

        // next program button click: advance to the next table.
        //
        private void ALICBtnNext_Click(object sender, RoutedEventArgs args)
        {
            SelectTable(EditTable + 1);
            uiALICSelectCombo.SelectedIndex = EditTable;
        }

        // on selection changed in the table select combo, switch alic tables and update the ui.
        //
        private void ALICSelectCombo_SelectionChanged(object sender, RoutedEventArgs args)
        {
            Grid item = (Grid)((ComboBox)sender).SelectedItem;
            if ((item != null) && (item.Tag != null))
            {
                SelectTable (int.Parse((string)item.Tag));
            }
        }
        // ---- text field changes ------------------------------------------------------------------------------------

        // text box lost focus: copy the local backing values to the configuration (note this is predicated on error
        // status) and rebuild the interface state.
        //
        // NOTE: though the text box has lost focus, the update may not yet have propagated into state. use the
        // NOTE: dispatch queue to give in-flight state updates time to complete.
        //
        private void ALICTextBox_LostFocus(object sender, RoutedEventArgs args)
        {
            // CONSIDER: may be better here to handle this in a property changed handler rather than here?
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CopyEditToConfig(EditTable, true);
            });
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

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, HARMSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            CopyConfigToEdit(EditTable);

            uiALICSelectCombo.SelectedIndex = EditTable;
            SelectTable(EditTable);

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
