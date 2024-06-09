// ********************************************************************************************************************
//
// FA18CEditCMSPage.xaml.cs : ui c# for hornet cms editor page
//
// Copyright(C) 2024 ilominar/raven
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
using JAFDTC.Models.FA18C;
using JAFDTC.Models.FA18C.CMS;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace JAFDTC.UI.FA18C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class FA18CEditCMSPage : SystemEditorPageBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(CMSSystem.SystemTag, "Countermeasures", "CMS", Glyphs.CMS, typeof(FA18CEditCMSPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPage properties

        protected override SystemBase SystemConfig => ((FA18CConfiguration)Config).CMS;

        protected override String SystemTag => CMSSystem.SystemTag;

        protected override string SystemName => "countermeasure management";

        protected override bool IsPageStateDefault => CurStateIsDefault();

        // ---- internal properties

        // NOTE: the ui always interacts with EditProg when editing program values, EditProgNum defines which program
        // NOTE: the ui is currently editing.
        //
        private CMProgram EditProg { get; set; }

        private int EditProgNum { get; set; }

        // ---- read-only properties

        private readonly CMSSystem _cmdsSysDefaults;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public FA18CEditCMSPage()
        {
            EditProg = new CMProgram();
            EditProgNum = (int)ProgramNumbers.PROG1;

            _cmdsSysDefaults = CMSSystem.ExplicitDefaults;

            InitializeComponent();
            InitializeBase(EditProg, uiPgmValueChaffQ, uiCtlLinkResetBtns);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Find and return the property information and encapsulating object corresponding to the provided control
        /// in the persisted configuration object.
        /// </summary>
        protected override void GetControlConfigProperty(FrameworkElement ctrl,
                                                         out PropertyInfo prop, out BindableObject obj)
        {
            // ui state corresponds to one of several programs in the config state. do that mapping here based on the
            // value in EditProgNum.
            //
            CMSSystem cms = (CMSSystem)SystemConfig;
            prop = cms.Programs[EditProgNum].GetType().GetProperty(ctrl.Tag.ToString());
            obj = cms.Programs[EditProgNum];

            if (prop == null)
                throw new ApplicationException($"Unexpected {ctrl.GetType()}: Tag {ctrl.Tag}");
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // field validation
        //
        // ------------------------------------------------------------------------------------------------------------

        private bool CurStateIsDefault()
        {
            FA18CConfiguration config = (FA18CConfiguration)Config;
            for (int i = (int)ProgramNumbers.PROG1; i <= (int)ProgramNumbers.PROG5; i++)
                if (((EditProgNum == i) && !EditProg.IsDefault) ||
                    ((EditProgNum != i) && !config.CMS.Programs[i].IsDefault))
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
            return EditProg.HasErrors || ((FA18CConfiguration)Config).CMS.HasErrors;
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
                UpdateUIFromEditState();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildProgramSelectMenu()
        {
            FA18CConfiguration config = (FA18CConfiguration)Config;
            Utilities.SetBulletsInBulletComboBox(uiPgmSelectCombo,
                                                 (int i) => (((EditProgNum == i) && !EditProg.IsDefault) ||
                                                             ((EditProgNum != i) && !config.CMS.Programs[i].IsDefault)));
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private static CMProgramCanvasParams ConvertCfgCMDStoCMCanvas(CMProgram pgm, CMProgram pgmDflt, bool isChaff)
        {
            int sq = 0, bq = 0;
            double si = 0.0, bi = 0.0;
            if (!pgm.HasErrors)
            {
                sq = int.Parse((!string.IsNullOrEmpty(pgm.SQ)) ? pgm.SQ : pgmDflt.SQ);
                si = double.Parse((!string.IsNullOrEmpty(pgm.SI)) ? pgm.SI : pgmDflt.SI);
                if (isChaff)
                {
                    bq = int.Parse((!string.IsNullOrEmpty(pgm.ChaffQ)) ? pgm.ChaffQ : pgmDflt.ChaffQ);
                    bi = 0.075;
                }
                else
                {
                    bq = int.Parse((!string.IsNullOrEmpty(pgm.FlareQ)) ? pgm.FlareQ : pgmDflt.FlareQ);
                    bi = 0.075;
                }
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
            CMProgram pgm = _cmdsSysDefaults.Programs[EditProgNum];
            uiPgmValueChaffQ.PlaceholderText = pgm.ChaffQ;
            uiPgmValueFlareQ.PlaceholderText = pgm.FlareQ;
            uiPgmValueSQ.PlaceholderText = pgm.SQ;
            uiPgmValueSI.PlaceholderText = pgm.SI;
        }

        /// <summary>
        /// update the canvai that show the chaff and flare programs visually.
        /// </summary>
        private void RebuildProgramCanvi()
        {
            CMProgram pgm = _cmdsSysDefaults.Programs[EditProgNum];
            CMProgramCanvasParams chaff = ConvertCfgCMDStoCMCanvas(EditProg, pgm, true);
            CMProgramCanvasParams flare = ConvertCfgCMDStoCMCanvas(EditProg, pgm, false);

            uiCMPgmFlareCanvas.SetProgram(flare, chaff);
            uiCMPgmChaffCanvas.SetProgram(chaff, flare);
        }

        // update the enable state on the ui elements based on the current settings. link controls must be set up
        // vi RebuildLinkControls() prior to calling this function.
        //
        protected override void UpdateUICustom(bool isEditable)
        {
            RebuildProgramSelectMenu();
            RebuildFieldPlaceholders();
            RebuildProgramCanvi();

            Utilities.SetEnableState(uiPgmValueChaffQ, isEditable);
            Utilities.SetEnableState(uiPgmValueFlareQ, isEditable);
            Utilities.SetEnableState(uiPgmValueSQ, isEditable);
            Utilities.SetEnableState(uiPgmValueSI, isEditable);

            Utilities.SetEnableState(uiPgmBtnReset, isEditable && !EditProg.IsDefault);

            bool isNoErrs = !CurStateHasErrors();
            Utilities.SetEnableState(uiPgmNextBtn, (isNoErrs && (EditProgNum != (int)ProgramNumbers.PROG5)));
            Utilities.SetEnableState(uiPgmPrevBtn, (isNoErrs && (EditProgNum != (int)ProgramNumbers.PROG1)));

            Utilities.SetEnableState(uiPgmSelectCombo, isNoErrs);
        }

        protected override void ResetConfigToDefault()
        {
            ((FA18CConfiguration)Config).CMS.Reset();
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
        private void PgmBtnReset_Click(object sender, RoutedEventArgs args)
        {
            EditProg.Reset();
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

        // on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        // we are editing.
        //
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            List<FrameworkElement> items = new();
            for (int i = (int)ProgramNumbers.PROG1; i <= (int)ProgramNumbers.PROG5 ; i++)
                items.Add(Utilities.BulletComboBoxItem($"PROG {i+1}", i.ToString()));
            uiPgmSelectCombo.ItemsSource = items;

            base.OnNavigatedTo(args);

            uiPgmSelectCombo.SelectedIndex = EditProgNum;
            SelectProgram(EditProgNum);
        }
    }
}
