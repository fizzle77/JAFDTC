// ********************************************************************************************************************
//
// F16CEditHTSPage.xaml.cs : ui c# for viper hts editor page
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
using JAFDTC.Models.F16C.HTS;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// Page obejct for the system editor page that handles the ui for the viper hts man table editor. this handles
    /// setup for the manual table as well as the threat classes enabled in the hts.
    /// </summary>
    public sealed partial class F16CEditHTSPage : SystemEditorPageBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(HTSSystem.SystemTag, "HTS Threats", "HTS", Glyphs.HTS, typeof(F16CEditHTSPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPage properties

        protected override SystemBase SystemConfig => ((F16CConfiguration)Config).HTS;

        protected override String SystemTag => HTSSystem.SystemTag;

        protected override string SystemName => "HTS manual table";

        protected override bool IsPageStateDefault => CurStateIsDefault();

        // ---- internal properties

        // NOTE: the ui always interacts with EditHTS when editing program values.
        //
        private HTSSystem EditHTS { get; set; }

        // ---- readonly properties

        private readonly List<string> _emitterTitles;
        private readonly Dictionary<string, Emitter> _emitterTitleMap;

        private readonly List<FontIcon> _tableEditFields;
        private readonly List<List<TextBlock>> _tableFields;
        private readonly Brush _brushEnabledText;
        private readonly Brush _brushDisabledText;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditHTSPage()
        {
            EditHTS = new HTSSystem();

            InitializeComponent();
            InitializeBase(EditHTS, uiT1ValueCode, uiCtlLinkResetBtns);

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

            _tableEditFields = new List<FontIcon>()
            {
                uiT1IconEdit, uiT2IconEdit, uiT3IconEdit, uiT4IconEdit,
                uiT5IconEdit, uiT6IconEdit, uiT7IconEdit, uiT8IconEdit
            };
            _tableFields = new List<List<TextBlock>>()
            {
                new() { uiT1RWRText, uiT1ValueCountry, uiT1ValueType, uiT1ValueName },
                new() { uiT2RWRText, uiT2ValueCountry, uiT2ValueType, uiT2ValueName },
                new() { uiT3RWRText, uiT3ValueCountry, uiT3ValueType, uiT3ValueName },
                new() { uiT4RWRText, uiT4ValueCountry, uiT4ValueType, uiT4ValueName },
                new() { uiT5RWRText, uiT5ValueCountry, uiT5ValueType, uiT5ValueName },
                new() { uiT6RWRText, uiT6ValueCountry, uiT6ValueType, uiT6ValueName },
                new() { uiT7RWRText, uiT7ValueCountry, uiT7ValueType, uiT7ValueName },
                new() { uiT8RWRText, uiT8ValueCountry, uiT8ValueType, uiT8ValueName }
            };

            // HACK: this is a stupid hack because i'm too lazy to figure out how to get this from a resource. fix
            // HACK: this at some point...
            //
            _brushEnabledText = uiT1ValueCode.Foreground;
            _brushDisabledText = uiHeaderText.Foreground;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Find and return the property information and encapsulating object corresponding to the provided control
        /// in the EditState object.
        /// </summary>
        protected override void GetControlEditStateProperty(FrameworkElement ctrl,
                                                            out PropertyInfo prop, out BindableObject obj)
        {
            // ui state corresponds to one of several entries in the config state. do that mapping here based on the
            // property of the form "<entry>.<property>"
            //
            string propName = ctrl.Tag.ToString();
            int entryNum = int.Parse(propName[..1]);
            prop = EditHTS.MANTable[entryNum].GetType().GetProperty(propName[2..]);
            obj = EditHTS.MANTable[entryNum];

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
            // property of the form "<entry>.<property>"
            //
            HTSSystem hts = (HTSSystem)SystemConfig;
            string propName = ctrl.Tag.ToString();
            int entryNum = int.Parse(propName[..1]);
            prop = hts.MANTable[entryNum].GetType().GetProperty(propName[2..]);
            obj = hts.MANTable[entryNum];

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
                for (int i = 0; i < EditHTS.MANTable.Count; i++)
                {
                    List<string> errors = (List<string>)EditHTS.MANTable[i].GetErrors(args.PropertyName);
                    SetFieldValidVisualState(PageTextBoxes[$"{i}.Code"], (errors.Count == 0));
                }
            }
        }

        /// <summary>
        /// returns true if the current state is default, false otherwise.
        /// </summary>
        private bool CurStateIsDefault()
        {
            for (int i = 0; i < EditHTS.MANTable.Count; i++)
                if (!EditHTS.MANTable[i].IsDefault)
                    return false;
            return true;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// rebuild the content in the man threat table definition using the emitter database to lookup the details
        /// on the programmed emitters.
        /// </summary>
        private void RebuildTableRowContent(int tableRow)
        {
            string curCode = EditHTS.MANTable[tableRow].Code;

            if (int.TryParse((string.IsNullOrEmpty(curCode)) ? "0" : curCode, out int alicCode))
            {
                List<Emitter> emitterList = EmitterDbase.Instance.Find(alicCode);

                _tableEditFields[tableRow].Visibility = (string.IsNullOrEmpty(curCode))
                                                      ? Visibility.Collapsed : Visibility.Visible;

                List<TextBlock> fields = _tableFields[tableRow];
                fields[0].Text = (emitterList.Count != 0) ? emitterList[0].F16RWR : "–";
                fields[1].Text = (emitterList.Count != 0) ? emitterList[0].Country : "–";
                fields[2].Text = (emitterList.Count != 0) ? emitterList[0].Type : "–";
                string name = (string.IsNullOrEmpty(curCode)) ? "No Emitter Programmed" : "Unknown Emitter";
                if (emitterList.Count != 0)
                {
                    name = emitterList[0].Name;
                    if (!string.IsNullOrEmpty(emitterList[0].NATO))
                        name += $" ({emitterList[0].NATO})";
                }
                fields[3].Text = name;

                for (int i = 0; i < fields.Count; i++)
                    fields[i].Foreground = (emitterList.Count != 0) ? _brushEnabledText : _brushDisabledText;
            }
        }

        /// <summary>
        /// update the summary content in the threat list based on the selected threat tables.
        /// </summary>
        private void RebuildThreatListContent()
        {
            List<string> threats = new();
            for (int threat = 1; threat < EditHTS.EnabledThreats.Length; threat++)
                if (EditHTS.EnabledThreats[threat])
                    threats.Add($"T{threat}");
            if (EditHTS.EnabledThreats[0])
                threats.Add("MAN");
            uiThreatTextList.Text = (threats.Count > 0) ? General.JoinList(threats) : "No HTS threat classes are enabled.";
        }

        /// <summary>
        /// update the ui state based on current setup.
        /// </summary>
        protected override void UpdateUICustom(bool isEditable)
        {
            for (int row = 0; row < EditHTS.MANTable.Count; row++)
                RebuildTableRowContent(row);
            RebuildThreatListContent();

            Utilities.SetEnableState(uiT1ValueCode, isEditable);
            Utilities.SetEnableState(uiT2ValueCode, isEditable);
            Utilities.SetEnableState(uiT3ValueCode, isEditable);
            Utilities.SetEnableState(uiT4ValueCode, isEditable);
            Utilities.SetEnableState(uiT5ValueCode, isEditable);
            Utilities.SetEnableState(uiT6ValueCode, isEditable);
            Utilities.SetEnableState(uiT7ValueCode, isEditable);
            Utilities.SetEnableState(uiT8ValueCode, isEditable);

            Utilities.SetEnableState(uiT1BtnAdd, isEditable);
            Utilities.SetEnableState(uiT2BtnAdd, isEditable);
            Utilities.SetEnableState(uiT3BtnAdd, isEditable);
            Utilities.SetEnableState(uiT4BtnAdd, isEditable);
            Utilities.SetEnableState(uiT5BtnAdd, isEditable);
            Utilities.SetEnableState(uiT6BtnAdd, isEditable);
            Utilities.SetEnableState(uiT7BtnAdd, isEditable);
            Utilities.SetEnableState(uiT8BtnAdd, isEditable);

            Utilities.SetEnableState(uiMANBtnResetTable, EditHTS.IsMANTablePopulated && isEditable);
        }

        protected override void ResetConfigToDefault()
        {
            ((F16CConfiguration)Config).HTS.Reset();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- page buttons ------------------------------------------------------------------------------------------

        /// <summary>
        /// reset table click: reset table to defaults. this implicitly disables man table enable as without a man
        /// table, the option to enable man table threats is not available in the hts format.
        /// </summary>
        private void MANBtnResetTable_Click(object sender, RoutedEventArgs _)
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            for (int row = 0; row < config.HTS.MANTable.Count; row++)
                config.HTS.MANTable[row].Reset();
            config.HTS.EnabledThreats[0] = false;
            config.Save(this, HTSSystem.SystemTag);
            CopyConfigToEditState();
        }

        // --- threat selection ---------------------------------------------------------------------------------------

        /// <summary>
        /// select threat click: push changes to the configuration and navigate to the threat selection page to update
        /// that state in the configuration.
        /// </summary>
        private void ThreatBtnSelect_Click(object sender, RoutedEventArgs _)
        {
            SaveEditStateToConfig();
            UpdateUIFromEditState();

            F16CConfiguration config = (F16CConfiguration)Config;
            NavArgs.BackButton.IsEnabled = false;
            bool isUnlinked = string.IsNullOrEmpty(Config.SystemLinkedTo(HTSSystem.SystemTag));
            Frame.Navigate(typeof(F16CEditHTSThreatsPage), new F16CEditHTSThreatsPageNavArgs(this, config, isUnlinked),
                           new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        // --- add emitter --------------------------------------------------------------------------------------------

        /// <summary>
        /// add button click: open the ui to prompt to select an emitter.
        /// </summary>
        private async void MANBtnAdd_Click(object sender, RoutedEventArgs _)
        {
            Button btnAdd = (Button)sender;
            GetListDialog dialog = new(_emitterTitles, "Emitter", 425)
            {
                XamlRoot = Content.XamlRoot,
                Title = $"Select an Emitter for HTS MAN Table, Entry {int.Parse((string)btnAdd.Tag) + 1}",
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                Button button = (Button)sender;
                Emitter emitter = _emitterTitleMap[(string)dialog.SelectedItem];

                int row = int.Parse((string)button.Tag);
                EditHTS.MANTable[row].Code = emitter.ALICCode.ToString("0");

                SaveEditStateToConfig();
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            base.OnNavigatedTo(args);

            for (int i = 0; i < EditHTS.MANTable.Count; i++)
            {
                EditHTS.MANTable[i].ErrorsChanged += EditState_ErrorsChanged;
                EditHTS.MANTable[i].PropertyChanged += EditState_PropertyChanged;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            for (int i = 0; i < EditHTS.MANTable.Count; i++)
            {
                EditHTS.MANTable[i].ErrorsChanged -= EditState_ErrorsChanged;
                EditHTS.MANTable[i].PropertyChanged -= EditState_PropertyChanged;
            }

            base.OnNavigatedFrom(args);
        }
    }
}
