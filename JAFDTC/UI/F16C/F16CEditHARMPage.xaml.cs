// ********************************************************************************************************************
//
// F16CEditHARMPage.xaml.cs : ui c# for viper harm alic editor page
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
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class F16CEditHARMPage : SystemEditorPageBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(HARMSystem.SystemTag, "HARM ALIC Tables", "HARM ALIC", Glyphs.HARM, typeof(F16CEditHARMPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPage properties

        protected override SystemBase SystemConfig => ((F16CConfiguration)Config).HARM;

        protected override String SystemTag => HARMSystem.SystemTag;

        protected override string SystemName => "HARM ALIC table";

        protected override bool IsPageStateDefault => CurStateIsDefault();

        // ---- internal properties

        // NOTE: the ui always interacts with EditTable when editing program values, EditTableNum defines which
        // NOTE: table the ui is currently editing.
        //
        private ALICTable EditTable { get; set; }

        private int EditTableNum { get; set; }

        // ---- readonly properties

        private readonly HARMSystem _harmSysDefaults;


        private readonly List<string> _emitterTitles;
        private readonly Dictionary<string, Emitter> _emitterTitleMap;

        private readonly List<TextBox> _tableCodeFields;
        private readonly List<FontIcon> _tableEditFields;
        private readonly List<List<TextBlock>> _tableFields;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditHARMPage()
        {
            EditTable = new ALICTable();
            EditTableNum = (int)TableNumbers.TABLE1;

            _harmSysDefaults = HARMSystem.ExplicitDefaults;

            _emitterTitles = new();
            _emitterTitleMap = new();
            foreach (Emitter emitter in EmitterDbase.Instance.Find())
            {
                string item = $"{emitter.Country} – {emitter.Type} – {emitter.Name}";
                if (!string.IsNullOrEmpty(emitter.NATO))
                    item += $" ({emitter.NATO})";
                _emitterTitles.Add(item);
                _emitterTitleMap[item] = emitter;
            }

            InitializeComponent();
            InitializeBase(EditTable, uiT1ValueCode, uiCtlLinkResetBtns);

            _tableCodeFields = new List<TextBox>()
            {
                uiT1ValueCode, uiT2ValueCode, uiT3ValueCode, uiT4ValueCode, uiT5ValueCode
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
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Find and return the property information and encapsulating object corresponding to the provided control
        /// in the EditState object. Base implementation returns data on the property named by the control's Tag in
        /// the EditState object. Derived classes may override this method if they require a more complex mapping
        /// between the tag and the returned (property, encapsulating object) tuple.
        /// </summary>
        protected override void GetControlEditStateProperty(FrameworkElement ctrl,
                                                            out PropertyInfo prop, out BindableObject obj)
        {
            // ui state corresponds to one of several entries in the config state. do that mapping here based on the
            // value in EditTableNum and the property of the form "<entry>.<property>"
            //
            string propName = ctrl.Tag.ToString();
            int entryNum = int.Parse(propName[..1]);
            prop = EditTable.Table[entryNum].GetType().GetProperty(propName[2..]);
            obj = EditTable.Table[entryNum];

            if (prop == null)
                throw new ApplicationException($"Unexpected {ctrl.GetType()}: Tag {ctrl.Tag}");
        }

        /// <summary>
        /// Find and return the property information and encapsulating object corresponding to the provided control
        /// in the persisted configuration object.
        /// </summary>
        protected override void GetControlConfigProperty(FrameworkElement ctrl,
                                                         out PropertyInfo prop, out BindableObject obj)
        {
            // ui state corresponds to one of several entries in the config state. do that mapping here based on the
            // value in EditTableNum and the property of the form "<entry>.<property>"
            //
            HARMSystem alic = (HARMSystem)SystemConfig;
            string propName = ctrl.Tag.ToString();
            int entryNum = int.Parse(propName[..1]);
            prop = alic.Tables[EditTableNum].Table[entryNum].GetType().GetProperty(propName[2..]);
            obj = alic.Tables[EditTableNum].Table[entryNum];

            if (prop == null)
                throw new ApplicationException($"Unexpected {ctrl.GetType()}: Tag {ctrl.Tag}");
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // field validation
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Override to perform custom logic when a property changes.
        /// </summary>
        protected override void EditState_ErrorsChanged(object sender, DataErrorsChangedEventArgs args)
        {
            if (args.PropertyName != null)
            {
                for (int i = 0; i < EditTable.Table.Count; i++)
                {
                    List<string> errors = (List<string>)EditTable.Table[i].GetErrors(args.PropertyName);
                    SetFieldValidVisualState(PageTextBoxes[$"{i}.Code"], (errors.Count == 0));
                }
            }
        }

        /// <summary>
        /// returns true if the current state has errors, false otherwise.
        /// </summary>
        private bool CurStateHasErrors()
        {
            for (int i = 0; i < EditTable.Table.Count; i++)
                if (EditTable.Table[i].HasErrors)
                    return true;
            return false;
        }

        /// <summary>
        /// returns true if the current table being edited is default, false otherwise.
        /// </summary>
        private bool EditTableIsDefault()
        {
            ObservableCollection<TableCode> table = EditTable.Table;
            ObservableCollection<TableCode> defaultTable = _harmSysDefaults.Tables[EditTableNum].Table;
            for (int i = 0; i < table.Count; i++)
                if (!string.IsNullOrEmpty(table[i].Code) && (table[i].Code != defaultTable[i].Code))
                    return false;
            return true;
        }

        /// <summary>
        /// returns true if the current state is default, false otherwise.
        /// </summary>
        private bool CurStateIsDefault()
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            ObservableCollection<TableCode> table = EditTable.Table;
            for (int i = 0; i < config.HARM.Tables.Length; i++)
                if (((EditTableNum == i) && !EditTableIsDefault()) ||
                    ((EditTableNum != i) && !config.HARM.Tables[i].IsDefault))
                {
                    return false;
                }
            return true;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// change the selected alic table and update various ui and model state.
        /// </summary>
        private void SelectTable(int table)
        {
            if (table != EditTableNum)
            {
                SaveEditStateToConfig();
                EditTableNum = table;
                CopyConfigToEditState();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildTableSelectMenu()
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            Utilities.SetBulletsInBulletComboBox(uiALICSelectCombo,
                                                 (int i) => (((EditTableNum == i) && !EditTableIsDefault()) ||
                                                             ((EditTableNum != i) && !config.HARM.Tables[i].IsDefault)));
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildTableRowContent(int tableRow)
        {
            string curCode = _tableCodeFields[tableRow].Text;
            string dfltCode = _harmSysDefaults.Tables[EditTableNum].Table[tableRow].Code;

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
                        name += $" ({emitterList[0].NATO})";
                }
                fields[3].Text = name;
            }
        }

        /// <summary>
        /// update the placeholder text to match the defaults for the selected table.
        /// </summary>
        private void RebuildFieldPlaceholders()
        {
            var table = _harmSysDefaults.Tables[EditTableNum];
            for (int i = 0; i < table.Table.Count; i++)
                _tableCodeFields[i].PlaceholderText = table.Table[i].Code;
        }

        /// <summary>
        /// update the ui state based on current setup.
        /// </summary>
        protected override void UpdateUICustom(bool isEditable)
        {
            RebuildTableSelectMenu();
            for (int row = 0; row < 5; row++)
                RebuildTableRowContent(row);

            RebuildFieldPlaceholders();

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

            Utilities.SetEnableState(uiALICBtnResetTable, isEditable && !EditTableIsDefault());

            bool isNoErrs = !CurStateHasErrors();
            Utilities.SetEnableState(uiALICSelectCombo, isNoErrs);
            Utilities.SetEnableState(uiALICBtnNext, (isNoErrs && (EditTableNum != (int)TableNumbers.TABLE3)));
            Utilities.SetEnableState(uiALICBtnPrev, (isNoErrs && (EditTableNum != (int)TableNumbers.TABLE1)));
        }

        protected override void ResetConfigToDefault()
        {
            ((F16CConfiguration)Config).HARM.Reset();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- page buttons ------------------------------------------------------------------------------------------

        /// <summary>
        /// reset table click: reset table to defaults.
        /// </summary>
        private void ALICBtnResetTable_Click(object sender, RoutedEventArgs args)
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            config.HARM.Tables[EditTableNum].Reset();
            config.Save(this, HARMSystem.SystemTag);
            CopyConfigToEditState();
        }

        // --- add emitter --------------------------------------------------------------------------------------------

        /// <summary>
        /// add button click: open the ui to prompt to select an emitter.
        /// </summary>
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
                EditTable.Table[entry].Code = emitter.ALICCode.ToString("000");

                SaveEditStateToConfig();
            }
        }

        // --- table selection ----------------------------------------------------------------------------------------

        /// <summary>
        /// previous program button click: advance to the previous table.
        /// </summary>
        private void ALICBtnPrev_Click(object sender, RoutedEventArgs _)
        {
            SelectTable(EditTableNum - 1);
            uiALICSelectCombo.SelectedIndex = EditTableNum;
        }

        /// <summary>
        /// next program button click: advance to the next table.
        /// </summary>
        private void ALICBtnNext_Click(object sender, RoutedEventArgs _)
        {
            SelectTable(EditTableNum + 1);
            uiALICSelectCombo.SelectedIndex = EditTableNum;
        }

        /// <summary>
        /// on selection changed in the table select combo, switch alic tables and update the ui.
        /// </summary>
        private void ALICSelectCombo_SelectionChanged(object sender, RoutedEventArgs _)
        {
            Grid item = (Grid)((ComboBox)sender).SelectedItem;
            if (!IsUIRebuilding && (item != null) && (item.Tag != null))
            {
                // NOTE: assume tag == index here...
                SelectTable(int.Parse((string)item.Tag));
            }
        }
        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            List<FrameworkElement> items = new();
            for (int i = (int)TableNumbers.TABLE1; i <= (int)TableNumbers.TABLE3; i++)
                items.Add(Utilities.BulletComboBoxItem($"Table {i + 1}", i.ToString()));
            uiALICSelectCombo.ItemsSource = items;

            base.OnNavigatedTo(args);

            for (int i = 0; i < EditTable.Table.Count; i++)
            {
                EditTable.Table[i].ErrorsChanged += EditState_ErrorsChanged;
                EditTable.Table[i].PropertyChanged += EditState_PropertyChanged;
            }

            uiALICSelectCombo.SelectedIndex = EditTableNum;
            SelectTable(EditTableNum);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            for (int i = 0; i < EditTable.Table.Count; i++)
            {
                EditTable.Table[i].ErrorsChanged -= EditState_ErrorsChanged;
                EditTable.Table[i].PropertyChanged -= EditState_PropertyChanged;
            }

            base.OnNavigatedFrom(e);
        }
    }
}
