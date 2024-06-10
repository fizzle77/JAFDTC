// ********************************************************************************************************************
//
// F16CEditCMDSPage.xaml.cs : ui c# for viper cmds editor page
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
using JAFDTC.Models.F16C.CMDS;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Windows.Networking.NetworkOperators;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class F16CEditCMDSPage : SystemEditorPageBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(CMDSSystem.SystemTag, "Countermeasures", "CMDS", Glyphs.CMDS, typeof(F16CEditCMDSPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPage properties

        protected override SystemBase SystemConfig => ((F16CConfiguration)Config).CMDS;

        protected override String SystemTag => CMDSSystem.SystemTag;

        protected override string SystemName => "countermeasure management";

        protected override bool IsPageStateDefault => CurStateIsDefault();

        // ---- internal properties

        // NOTE: the ui always interacts with EditCMDS.Programs[0] when editing program values, EditProgram defines
        // NOTE: which program the ui is currently editing.
        //
        private CMDSSystem EditCMDS { get; set; }

        private int EditProgNum { get; set; }

        // ---- read-only properties

        private readonly CMDSSystem _cmdsSysDefaults;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditCMDSPage()
        {
            EditCMDS = new CMDSSystem();
            EditProgNum = (int)ProgramNumbers.MAN1;

            _cmdsSysDefaults = CMDSSystem.ExplicitDefaults;

            InitializeComponent();
            InitializeBase(EditCMDS, uiChaffValueBingo, uiCtlLinkResetBtns);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Copy data from the edit object the page interacts with to the system configuration object and persist the
        /// updated configuration to disk.
        /// </summary>
        protected override bool EditStateHasErrors()
            => (EditCMDS.HasErrors || EditCMDS.Programs[0].Chaff.HasErrors || EditCMDS.Programs[0].Flare.HasErrors);

        /// <summary>
        /// Find and return the property information and encapsulating object corresponding to the provided control
        /// in the EditState object.
        /// </summary>
        protected override void GetControlEditStateProperty(FrameworkElement ctrl,
                                                            out PropertyInfo prop, out BindableObject obj)
        {
            string propName = ctrl.Tag.ToString();

            // CMDSSystem object layout is complicated. find the right property and object based on the property name
            // and the program number of the program being edited.
            //
            if ((propName == "BingoChaff") || (propName == "BingoFlare"))
            {
                prop = EditCMDS.GetType().GetProperty(propName);
                obj = EditCMDS;
            }
            else if (propName.StartsWith("Chaff."))
            {
                prop = EditCMDS.Programs[0].Chaff.GetType().GetProperty(propName["Chaff.".Length..]);
                obj = EditCMDS.Programs[0].Chaff;
            }
            else if (propName.StartsWith("Flare."))
            {
                prop = EditCMDS.Programs[0].Flare.GetType().GetProperty(propName["Flare.".Length..]);
                obj = EditCMDS.Programs[0].Flare;
            }
            else
            {
                prop = null;
                obj = null;
            }

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
            CMDSSystem cmds = (CMDSSystem)SystemConfig;
            string propName = ctrl.Tag.ToString();

            // CMDSSystem object layout is complicated. find the right property and object based on the property name
            // and the program number of the program being edited.
            //
            if ((propName == "BingoChaff") || (propName == "BingoFlare"))
            {
                prop = cmds.GetType().GetProperty(propName);
                obj = cmds;
            }
            else if (propName.StartsWith("Chaff."))
            {
                prop = cmds.Programs[EditProgNum].Chaff.GetType().GetProperty(propName["Chaff.".Length..]);
                obj = cmds.Programs[EditProgNum].Chaff;
            }
            else if (propName.StartsWith("Flare."))
            {
                prop = cmds.Programs[EditProgNum].Flare.GetType().GetProperty(propName["Flare.".Length..]);
                obj = cmds.Programs[EditProgNum].Flare;
            }
            else
            {
                prop = null;
                obj = null;
            }

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
            if ((args.PropertyName == "BingoChaff") || (args.PropertyName == "BingoFlare"))
            {
                ValidateEditState(EditCMDS, args.PropertyName);
            }
            else if (args.PropertyName != null)
            {
                List<string> errors = (List<string>)EditCMDS.Programs[0].Chaff.GetErrors(args.PropertyName);
                SetFieldValidVisualState(PageTextBoxes[$"Chaff.{args.PropertyName}"], (errors.Count == 0));

                errors = (List<string>)EditCMDS.Programs[0].Flare.GetErrors(args.PropertyName);
                SetFieldValidVisualState(PageTextBoxes[$"Flare.{args.PropertyName}"], (errors.Count == 0));
            }
        }

        /// <summary>
        /// return true if current state is default.
        /// </summary>
        private bool CurStateIsDefault()
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            for (int i = (int)ProgramNumbers.MAN1; i <= (int)ProgramNumbers.BYPASS; i++)
                if (((EditProgNum == i) && !EditCMDS.Programs[i].IsDefault) ||
                    ((EditProgNum != i) && !config.CMDS.Programs[i].IsDefault))
                {
                    return false;
                }
            return true;
        }

        /// <summary>
        /// returns true if the current state has errors, false otherwise.
        /// </summary>
        private bool CurStateHasErrors()
        {
            return EditStateHasErrors() || ((F16CConfiguration)Config).CMDS.HasErrors;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// change the selected cmds program and update various ui and model state.
        /// </summary>
        private void SelectProgram(int program)
        {
            if (program != EditProgNum)
            {
                SaveEditStateToConfig();
                EditProgNum = program;
                CopyConfigToEditState();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildProgramSelectMenu()
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            Utilities.SetBulletsInBulletComboBox(uiPgmSelectCombo,
                                                 (int i) => (((EditProgNum == i) && !EditCMDS.Programs[0].IsDefault) ||
                                                             ((EditProgNum != i) && !config.CMDS.Programs[i].IsDefault)));
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private static CMProgramCanvasParams ConvertCfgCMDStoCMCanvas(CMDSProgramCore pgm, CMDSProgramCore pgmDflt,
                                                                      bool isChaff)
        {
            int sq = 0, bq = 0;
            double si = 0.0, bi = 0.0;

            if (!pgm.HasErrors)
            {
                sq = int.Parse((!string.IsNullOrEmpty(pgm.SQ)) ? pgm.SQ : pgmDflt.SQ);
                bq = int.Parse((!string.IsNullOrEmpty(pgm.BQ)) ? pgm.BQ : pgmDflt.BQ);
                si = double.Parse((!string.IsNullOrEmpty(pgm.SI)) ? pgm.SI : pgmDflt.SI);
                bi = double.Parse((!string.IsNullOrEmpty(pgm.BI)) ? pgm.BI : pgmDflt.BI);
            }
            if ((sq == 0) || (bq == 0))
            {
                sq = bq = 0;
                si = bi = 0.0;
            }
            return new(isChaff, bq, bi, sq, si);
        }

        /// <summary>
        /// update the placeholder text in the programming fields to match the defaults for the selected program.
        /// </summary>
        private void RebuildFieldPlaceholders()
        {
            CMDSProgram pgm = _cmdsSysDefaults.Programs[EditProgNum];
            uiChaffValueBingo.PlaceholderText = _cmdsSysDefaults.BingoChaff;
            uiPgmChaffValueBQ.PlaceholderText = pgm.Chaff.BQ;
            uiPgmChaffValueBI.PlaceholderText = pgm.Chaff.BI;
            uiPgmChaffValueSQ.PlaceholderText = pgm.Chaff.SQ;
            uiPgmChaffValueSI.PlaceholderText = pgm.Chaff.SI;

            uiFlareValueBingo.PlaceholderText = _cmdsSysDefaults.BingoFlare;
            uiPgmFlareValueBQ.PlaceholderText = pgm.Flare.BQ;
            uiPgmFlareValueBI.PlaceholderText = pgm.Flare.BI;
            uiPgmFlareValueSQ.PlaceholderText = pgm.Flare.SQ;
            uiPgmFlareValueSI.PlaceholderText = pgm.Flare.SI;
        }

        /// <summary>
        /// update the canvai that show the chaff and flare programs visually.
        /// </summary>
        private void RebuildProgramCanvi()
        {
            CMDSProgram pgm = _cmdsSysDefaults.Programs[EditProgNum];
            CMProgramCanvasParams chaff = ConvertCfgCMDStoCMCanvas(EditCMDS.Programs[0].Chaff, pgm.Chaff, true);
            CMProgramCanvasParams flare = ConvertCfgCMDStoCMCanvas(EditCMDS.Programs[0].Flare, pgm.Flare, false);

            uiCMPgmFlareCanvas.SetProgram(flare, chaff);
            uiCMPgmChaffCanvas.SetProgram(chaff, flare);
        }

        /// <summary>
        /// update the ui state based on current setup.
        /// </summary>
        protected override void UpdateUICustom(bool isEditable)
        {
            RebuildProgramSelectMenu();
            RebuildFieldPlaceholders();
            RebuildProgramCanvi();

            Utilities.SetEnableState(uiChaffValueBingo, isEditable);
            Utilities.SetEnableState(uiFlareValueBingo, isEditable);

            Utilities.SetEnableState(uiPgmChaffValueBQ, isEditable);
            Utilities.SetEnableState(uiPgmChaffValueBI, isEditable);
            Utilities.SetEnableState(uiPgmChaffValueSQ, isEditable);
            Utilities.SetEnableState(uiPgmChaffValueSI, isEditable);

            Utilities.SetEnableState(uiPgmFlareValueBQ, isEditable);
            Utilities.SetEnableState(uiPgmFlareValueBI, isEditable);
            Utilities.SetEnableState(uiPgmFlareValueSQ, isEditable);
            Utilities.SetEnableState(uiPgmFlareValueSI, isEditable);

            Utilities.SetEnableState(uiPgmChaffBtnReset, isEditable && !EditCMDS.Programs[0].Chaff.IsDefault);
            Utilities.SetEnableState(uiPgmFlareBtnReset, isEditable && !EditCMDS.Programs[0].Flare.IsDefault);

            bool isNoErrs = !CurStateHasErrors();
            Utilities.SetEnableState(uiPgmNextBtn, (isNoErrs && (EditProgNum != (int)ProgramNumbers.BYPASS)));
            Utilities.SetEnableState(uiPgmPrevBtn, (isNoErrs && (EditProgNum != (int)ProgramNumbers.MAN1)));

            Utilities.SetEnableState(uiPgmSelectCombo, isNoErrs);
        }

        protected override void ResetConfigToDefault()
        {
            ((F16CConfiguration)Config).CMDS.Reset();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- page buttons ------------------------------------------------------------------------------------------

        /// <summary>
        /// reset chaff program click: set all chaff program values to default.
        /// </summary>
        private void PgmChaffBtnReset_Click(object sender, RoutedEventArgs args)
        {
            EditCMDS.Programs[0].Chaff.Reset();
            SaveEditStateToConfig();
        }

        /// <summary>
        /// reset flare program click: set all flare program values to default.
        /// </summary>
        private void PgmFlareBtnReset_Click(object sender, RoutedEventArgs args)
        {
            EditCMDS.Programs[0].Flare.Reset();
            SaveEditStateToConfig();
        }

        // ---- program selection -------------------------------------------------------------------------------------

        /// <summary>
        /// previous program button click: advance to the previous program.
        /// </summary>
        private void PgmBtnPrev_Click(object sender, RoutedEventArgs args)
        {
            SelectProgram(EditProgNum - 1);
            uiPgmSelectCombo.SelectedIndex = EditProgNum;
        }

        /// <summary>
        /// next program button click: advance to the next program.
        /// </summary>
        private void PgmBtnNext_Click(object sender, RoutedEventArgs args)
        {
            SelectProgram(EditProgNum + 1);
            uiPgmSelectCombo.SelectedIndex = EditProgNum;
        }

        /// <summary>
        /// program select combo click: switch to the selected program. the tag of the sender (a TextBlock) gives us
        /// the program number to select.
        /// </summary>
        private void PgmSelectCombo_SelectionChanged(object sender, RoutedEventArgs args)
        {
            Grid item = (Grid)((ComboBox)sender).SelectedItem;
            if (!IsUIRebuilding && (item != null) && (item.Tag != null))
            {
                // NOTE: assume tag == index here...
                SelectProgram(int.Parse((string)item.Tag));
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        /// we are editing.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            List<FrameworkElement> items = new();
            for (int i = (int)ProgramNumbers.MAN1; i <= (int)ProgramNumbers.MAN4; i++)
                items.Add(Utilities.BulletComboBoxItem($"PROG {i + 1}", i.ToString()));
            items.Add(Utilities.BulletComboBoxItem("PANIC", ((int)ProgramNumbers.PANIC).ToString()));
            items.Add(Utilities.BulletComboBoxItem("BYPASS", ((int)ProgramNumbers.BYPASS).ToString()));
            uiPgmSelectCombo.ItemsSource = items;

            base.OnNavigatedTo(args);

            // base class will take care of subscribing to Property/ErrorsChanged for the top EditCMDS object. we will
            // also subscribe to events from Program[0], the program that all editing takes place in.
            //
            EditCMDS.Programs[0].Chaff.ErrorsChanged += this.EditState_ErrorsChanged;
            EditCMDS.Programs[0].Chaff.PropertyChanged += this.EditState_PropertyChanged;

            EditCMDS.Programs[0].Flare.ErrorsChanged += this.EditState_ErrorsChanged;
            EditCMDS.Programs[0].Flare.PropertyChanged += this.EditState_PropertyChanged;

            uiPgmSelectCombo.SelectedIndex = EditProgNum;
            SelectProgram(EditProgNum);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs args)
        {
            EditCMDS.Programs[0].Chaff.ErrorsChanged -= EditState_ErrorsChanged;
            EditCMDS.Programs[0].Chaff.PropertyChanged -= EditState_PropertyChanged;

            EditCMDS.Programs[0].Flare.ErrorsChanged -= EditState_ErrorsChanged;
            EditCMDS.Programs[0].Flare.PropertyChanged -= EditState_PropertyChanged;

            base.OnNavigatingFrom(args);
        }
    }
}
