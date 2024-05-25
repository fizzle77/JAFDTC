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
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace JAFDTC.UI.FA18C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class FA18CEditCMSPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(CMSSystem.SystemTag, "Countermeasures", "CMS", Glyphs.CMS, typeof(FA18CEditCMSPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditCMDS/EditProgram properties.
        //
        private FA18CConfiguration Config { get; set; }

        // NOTE: the ui always interacts with EditCMS.Programs[0] when editing program values, EditProgram defines
        // NOTE: which program the ui is currently editing.
        //
        private CMSSystem EditCMS { get; set; }

        private int EditProgram { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsRebuildingUI { get; set; }

        // ---- read-only properties

        private readonly CMSSystem _cmdsSysDefaults;

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        private readonly Dictionary<string, TextBox> _baseFieldValueMap;
        private readonly List<FontIcon> _pgmSelComboIcons;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public FA18CEditCMSPage()
        {
            InitializeComponent();

            EditCMS = new CMSSystem();

            // NOTE: ui will operate on Programs[0], regardless of which program is actually selected.
            //
            EditCMS.Programs[0].ErrorsChanged += EditField_DataValidationError;
            EditCMS.Programs[0].PropertyChanged += EditField_PropertyChanged;

            EditProgram = (int)ProgramNumbers.PROG1;

            IsRebuildPending = false;
            IsRebuildingUI = false;

            _cmdsSysDefaults = CMSSystem.ExplicitDefaults;

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();

            _baseFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["ChaffQ"] = uiPgmValueChaffQ,
                ["flareQ"] = uiPgmValueFlareQ,
                ["SQ"] = uiPgmValueSQ,
                ["SI"] = uiPgmValueSI,
            };
            _pgmSelComboIcons = new List<FontIcon>()
            {
                uiPgmSelectItem1Icon, uiPgmSelectItem2Icon, uiPgmSelectItem3Icon,
                uiPgmSelectItem4Icon, uiPgmSelectItem5Icon
            };
            _defaultBorderBrush = uiPgmValueChaffQ.BorderBrush;
            _defaultBkgndBrush = uiPgmValueChaffQ.Background;

            // wait for final setup of the ui until we navigate to the page (at which point we will have a
            // configuration to display).
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        // marshall data between our local cmds state and the appropriate program in the cmds configuration. to
        // simplify bindings, note that the local copy of the program being edited is always in EditCMDS.Programs[0]
        // regardless of which program we are editing.
        //
        private void CopyConfigToEdit(int program)
        {
            // NOTE: we don't marshall the program number here, it shouldn't change

            EditCMS.Programs[0].ChaffQ = Config.CMS.Programs[program].ChaffQ;
            EditCMS.Programs[0].FlareQ = Config.CMS.Programs[program].FlareQ;
            EditCMS.Programs[0].SQ = Config.CMS.Programs[program].SQ;
            EditCMS.Programs[0].SI = Config.CMS.Programs[program].SI;
        }

        private void CopyEditToConfig(int program, bool isPersist = false)
        {
            Debug.Assert(program == Config.CMS.Programs[program].Number);

            if (!CurStateHasErrors())
            {
                // NOTE: we don't marshall the program number here, it shouldn't change

                Config.CMS.Programs[program].ChaffQ = EditCMS.Programs[0].ChaffQ;
                Config.CMS.Programs[program].FlareQ = EditCMS.Programs[0].FlareQ;
                Config.CMS.Programs[program].SQ = EditCMS.Programs[0].SQ;
                Config.CMS.Programs[program].SI = EditCMS.Programs[0].SI;

                if (isPersist)
                {
                    Config.Save(this, CMSSystem.SystemTag);
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

        /// <summary>
        /// validation error: update ui state for the various components (base, chaff program, flare program)
        /// that may have errors.
        /// </summary>
        private void EditField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            if (args.PropertyName == null)
            {
                ValidateAllFields(_baseFieldValueMap, EditCMS.Programs[0].GetErrors(null));
            }
            else
            {
                List<string> errors = (List<string>)EditCMS.Programs[0].GetErrors(args.PropertyName);
                SetFieldValidState(_baseFieldValueMap[args.PropertyName], (errors.Count == 0));
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// property changed: rebuild interface state to account for configuration changes.
        /// </summary>
        private void EditField_PropertyChanged(object sender, EventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// returns true if the current state has errors, false otherwise.
        /// </summary>
        private bool CurStateHasErrors()
        {
            return EditCMS.HasErrors || EditCMS.Programs[0].HasErrors;
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
            if (program != EditProgram)
            {
                CopyEditToConfig(EditProgram, true);
                EditProgram = program;
                CopyConfigToEdit(EditProgram);
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildProgramSelectMenu()
        {
            for (int i = 0; i < Config.CMS.Programs.Length; i++)
            {
                Visibility viz = Visibility.Collapsed;
                if (((EditProgram == i) && !EditCMS.Programs[0].IsDefault) ||
                    ((EditProgram != i) && !Config.CMS.Programs[i].IsDefault))
                {
                    viz = Visibility.Visible;
                }
                _pgmSelComboIcons[i].Visibility = viz;
            }
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
            CMProgram pgm = _cmdsSysDefaults.Programs[EditProgram];
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
            CMProgram pgm = _cmdsSysDefaults.Programs[EditProgram];
            CMProgramCanvasParams chaff = ConvertCfgCMDStoCMCanvas(EditCMS.Programs[0], pgm, true);
            CMProgramCanvasParams flare = ConvertCfgCMDStoCMCanvas(EditCMS.Programs[0], pgm, false);

            uiCMPgmFlareCanvas.SetProgram(flare, chaff);
            uiCMPgmChaffCanvas.SetProgram(chaff, flare);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, CMSSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        // update the enable state on the ui elements based on the current settings. link controls must be set up
        // vi RebuildLinkControls() prior to calling this function.
        //
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(CMSSystem.SystemTag));

            Utilities.SetEnableState(uiPgmValueChaffQ, isEditable);
            Utilities.SetEnableState(uiPgmValueFlareQ, isEditable);
            Utilities.SetEnableState(uiPgmValueSQ, isEditable);
            Utilities.SetEnableState(uiPgmValueSI, isEditable);

            Utilities.SetEnableState(uiPgmBtnReset, isEditable && !EditCMS.Programs[0].IsDefault);

            bool isDefault = EditCMS.IsDefault;
            for (int i = 0; i < EditCMS.Programs.Length; i++)
            {
                if (((EditProgram == i) && !EditCMS.Programs[0].IsDefault) ||
                    ((EditProgram != i) && !Config.CMS.Programs[i].IsDefault))
                {
                    isDefault = false;
                    break;
                }
            }
            Utilities.SetEnableState(uiPageBtnResetAll, !isDefault);

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);

            bool isNoErrs = !CurStateHasErrors();
            Utilities.SetEnableState(uiPgmNextBtn, (isNoErrs && (EditProgram != (int)ProgramNumbers.PROG5)));
            Utilities.SetEnableState(uiPgmPrevBtn, (isNoErrs && (EditProgram != (int)ProgramNumbers.PROG1)));

            Utilities.SetEnableState(uiPgmSelectCombo, isNoErrs);
        }

        /// <summary>
        /// rebuild the state of controls on the page in response to a change in the configuration.
        /// </summary>
        private void RebuildInterfaceState()
        {
            if (!IsRebuildPending)
            {
                IsRebuildPending = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    IsRebuildingUI = true;
                    RebuildProgramSelectMenu();
                    RebuildFieldPlaceholders();
                    RebuildProgramCanvi();
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
        /// reset all button click: reset all cmds settings back to their defaults.
        /// </summary>
        private async void PageBtnResetAll_Click(object sender, RoutedEventArgs args)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configuration?",
                "Are you sure you want to reset the CMS configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(CMSSystem.SystemTag);
                Config.CMS.Reset();
                Config.Save(this, CMSSystem.SystemTag);
                CopyConfigToEdit(EditProgram);
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, CMSSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                Config.UnlinkSystem(CMSSystem.SystemTag);
                Config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(CMSSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit(EditProgram);
            }
        }

        /// <summary>
        /// reset chaff program click: set all chaff program values to default.
        /// </summary>
        private void PgmBtnReset_Click(object sender, RoutedEventArgs args)
        {
            EditCMS.Programs[0].Reset();
            CopyEditToConfig(EditProgram, true);
        }

        // ---- program selection -------------------------------------------------------------------------------------

        /// <summary>
        /// previous program button click: advance to the previous program.
        /// </summary>
        private void PgmBtnPrev_Click(object sender, RoutedEventArgs args)
        {
            SelectProgram(EditProgram - 1);
            uiPgmSelectCombo.SelectedIndex = EditProgram;
        }

        /// <summary>
        /// next program button click: advance to the next program.
        /// </summary>
        private void PgmBtnNext_Click(object sender, RoutedEventArgs args)
        {
            SelectProgram(EditProgram + 1);
            uiPgmSelectCombo.SelectedIndex = EditProgram;
        }

        /// <summary>
        /// program select combo click: switch to the selected program. the tag of the sender (a TextBlock) gives us
        /// the program number to select.
        /// </summary>
        private void PgmSelectCombo_SelectionChanged(object sender, RoutedEventArgs args)
        {
            Grid item = (Grid)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                // NOTE: assume tag == index here...
                SelectProgram(int.Parse((string)item.Tag));
            }
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        /// <summary>
        /// text box lost focus: copy the local backing values to the configuration (note this is predicated on error
        /// status) and rebuild the interface state.
        ///
        /// NOTE: though the text box has lost focus, the update may not yet have propagated into state. use the
        /// NOTE: dispatch queue to give in-flight state updates time to complete.
        /// 
        /// </summary>
        private void CMSTextBox_LostFocus(object sender, RoutedEventArgs args)
        {
            // CONSIDER: may be better here to handle this in a property changed handler rather than here?
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CopyEditToConfig(EditProgram, true);
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
            Config = (FA18CConfiguration)NavArgs.Config;

            Config.ConfigurationSaved += ConfigurationSavedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, CMSSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            CopyConfigToEdit(EditProgram);

            ValidateAllFields(_baseFieldValueMap, EditCMS.Programs[0].GetErrors(null));

            uiPgmSelectCombo.SelectedIndex = EditProgram;
            SelectProgram(EditProgram);

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
