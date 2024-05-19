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

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class F16CEditCMDSPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(CMDSSystem.SystemTag, "Countermeasures", "CMDS", Glyphs.CMDS, typeof(F16CEditCMDSPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditCMDS/EditProgram properties.
        //
        private F16CConfiguration Config { get; set; }

        // NOTE: the ui always interacts with EditCMDS.Programs[0] when editing program values, EditProgram defines
        // NOTE: which program the ui is currently editing.
        //
        private CMDSSystem EditCMDS { get; set; }

        private int EditProgram { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsRebuildingUI { get; set; }

        // ---- read-only properties

        private readonly CMDSSystem _cmdsSysDefaults;

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        private readonly Dictionary<string, TextBox> _baseFieldValueMap;
        private readonly Dictionary<string, TextBox> _pgmChaffFieldValueMap;
        private readonly Dictionary<string, TextBox> _pgmFlareFieldValueMap;
        private readonly List<FontIcon> _pgmSelComboIcons;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditCMDSPage()
        {
            InitializeComponent();

            EditCMDS = new CMDSSystem();

            // NOTE: ui will operate on Programs[0], regardless of which program is actually selected.
            //
            EditCMDS.ErrorsChanged += BaseField_DataValidationError;
            EditCMDS.Programs[0].Chaff.ErrorsChanged += PgmChaffField_DataValidationError;
            EditCMDS.Programs[0].Flare.ErrorsChanged += PgmFlareField_DataValidationError;

            EditCMDS.PropertyChanged += EditField_PropertyChanged;
            EditCMDS.Programs[0].Chaff.PropertyChanged += EditField_PropertyChanged;
            EditCMDS.Programs[0].Flare.PropertyChanged += EditField_PropertyChanged;

            EditProgram = (int)ProgramNumbers.MAN1;

            IsRebuildPending = false;
            IsRebuildingUI = false;

            _cmdsSysDefaults = CMDSSystem.ExplicitDefaults;

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();

            _baseFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["BingoChaff"] = uiChaffValueBingo,
                ["BingoFlare"] = uiFlareValueBingo,
            };
            _pgmChaffFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["BQ"] = uiPgmChaffValueBQ,
                ["BI"] = uiPgmChaffValueBI,
                ["SQ"] = uiPgmChaffValueSQ,
                ["SI"] = uiPgmChaffValueSI,
            };
            _pgmFlareFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["BQ"] = uiPgmFlareValueBQ,
                ["BI"] = uiPgmFlareValueBI,
                ["SQ"] = uiPgmFlareValueSQ,
                ["SI"] = uiPgmFlareValueSI,
            };
            _pgmSelComboIcons = new List<FontIcon>()
            {
                uiPgmSelectItem1Icon, uiPgmSelectItem2Icon, uiPgmSelectItem3Icon,
                uiPgmSelectItem4Icon, uiPgmSelectItem5Icon, uiPgmSelectItem6Icon
            };
            _defaultBorderBrush = uiPgmChaffValueBQ.BorderBrush;
            _defaultBkgndBrush = uiPgmChaffValueBQ.Background;

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
            EditCMDS.BingoChaff = Config.CMDS.BingoChaff;
            EditCMDS.BingoFlare = Config.CMDS.BingoFlare;

            // NOTE: we don't marshall the program number here, it shouldn't change

            EditCMDS.Programs[0].Chaff.BQ = Config.CMDS.Programs[program].Chaff.BQ;
            EditCMDS.Programs[0].Chaff.BI = Config.CMDS.Programs[program].Chaff.BI;
            EditCMDS.Programs[0].Chaff.SQ = Config.CMDS.Programs[program].Chaff.SQ;
            EditCMDS.Programs[0].Chaff.SI = Config.CMDS.Programs[program].Chaff.SI;

            EditCMDS.Programs[0].Flare.BQ = Config.CMDS.Programs[program].Flare.BQ;
            EditCMDS.Programs[0].Flare.BI = Config.CMDS.Programs[program].Flare.BI;
            EditCMDS.Programs[0].Flare.SQ = Config.CMDS.Programs[program].Flare.SQ;
            EditCMDS.Programs[0].Flare.SI = Config.CMDS.Programs[program].Flare.SI;
        }

        private void CopyEditToConfig(int program, bool isPersist = false)
        {
            Debug.Assert(program == Config.CMDS.Programs[program].Number);

            if (!CurStateHasErrors())
            {
                Config.CMDS.BingoChaff = EditCMDS.BingoChaff;
                Config.CMDS.BingoFlare = EditCMDS.BingoFlare;

                // NOTE: we don't marshall the program number here, it shouldn't change

                Config.CMDS.Programs[program].Chaff.BQ = EditCMDS.Programs[0].Chaff.BQ;
                Config.CMDS.Programs[program].Chaff.BI = EditCMDS.Programs[0].Chaff.BI;
                Config.CMDS.Programs[program].Chaff.SQ = EditCMDS.Programs[0].Chaff.SQ;
                Config.CMDS.Programs[program].Chaff.SI = EditCMDS.Programs[0].Chaff.SI;

                Config.CMDS.Programs[program].Flare.BQ = EditCMDS.Programs[0].Flare.BQ;
                Config.CMDS.Programs[program].Flare.BI = EditCMDS.Programs[0].Flare.BI;
                Config.CMDS.Programs[program].Flare.SQ = EditCMDS.Programs[0].Flare.SQ;
                Config.CMDS.Programs[program].Flare.SI = EditCMDS.Programs[0].Flare.SI;

                if (isPersist)
                {
                    Config.Save(this, CMDSSystem.SystemTag);
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

        private void CoreDataValidationError(INotifyDataErrorInfo obj, string propertyName, Dictionary<string, TextBox> fields)
        {
            if (propertyName == null)
            {
                ValidateAllFields(fields, obj.GetErrors(null));
            }
            else
            {
                List<string> errors = (List<string>)obj.GetErrors(propertyName);
                SetFieldValidState(fields[propertyName], (errors.Count == 0));
            }
            RebuildInterfaceState();
        }

        // validation error: update ui state for the various components (base, chaff program, flare program)
        // that may have errors.
        //
        private void BaseField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            CoreDataValidationError(EditCMDS, args.PropertyName, _baseFieldValueMap);
        }

        private void PgmChaffField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            CoreDataValidationError(EditCMDS.Programs[0].Chaff, args.PropertyName, _pgmChaffFieldValueMap);
        }

        private void PgmFlareField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            CoreDataValidationError(EditCMDS.Programs[0].Flare, args.PropertyName, _pgmFlareFieldValueMap);
        }

        // property changed: rebuild interface state to account for configuration changes.
        //
        private void EditField_PropertyChanged(object sender, EventArgs args)
        {
            RebuildInterfaceState();
        }
        
        // returns true if the current state has errors, false otherwise.
        //
        private bool CurStateHasErrors()
        {
            return EditCMDS.HasErrors || EditCMDS.Programs[0].Chaff.HasErrors || EditCMDS.Programs[0].Flare.HasErrors;
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
            for (int i = 0; i < Config.CMDS.Programs.Length; i++)
            {
                Visibility viz = Visibility.Collapsed;
                if (((EditProgram == i) && !EditCMDS.Programs[0].IsDefault) ||
                    ((EditProgram != i) && !Config.CMDS.Programs[i].IsDefault))
                {
                    viz = Visibility.Visible;
                }
                _pgmSelComboIcons[i].Visibility = viz;
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private CMProgramCanvasParams ConvertCfgCMDStoCMCanvas(CMDSProgramCore pgm, CMDSProgramCore pgmDflt, bool isChaff)
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
            CMDSProgram pgm = _cmdsSysDefaults.Programs[EditProgram];
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
            CMDSProgram pgm = _cmdsSysDefaults.Programs[EditProgram];
            CMProgramCanvasParams chaff = ConvertCfgCMDStoCMCanvas(EditCMDS.Programs[0].Chaff, pgm.Chaff, true);
            CMProgramCanvasParams flare = ConvertCfgCMDStoCMCanvas(EditCMDS.Programs[0].Flare, pgm.Flare, false);

            uiCMPgmFlareCanvas.SetProgram(flare, chaff);
            uiCMPgmChaffCanvas.SetProgram(chaff, flare);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, CMDSSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        // update the enable state on the ui elements based on the current settings. link controls must be set up
        // vi RebuildLinkControls() prior to calling this function.
        //
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(CMDSSystem.SystemTag));

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

            bool isDefault = EditCMDS.IsDefault;
            for (int i = 0; i < 6; i++)
            {
                if (((EditProgram == i) && !EditCMDS.Programs[0].IsDefault) ||
                    ((EditProgram != i) && !Config.CMDS.Programs[i].IsDefault))
                {
                    isDefault = false;
                    break;
                }
            }
            Utilities.SetEnableState(uiPageBtnResetAll, !isDefault);

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);

            bool isNoErrs = !CurStateHasErrors();
            Utilities.SetEnableState(uiPgmNextBtn, (isNoErrs && (EditProgram != (int)ProgramNumbers.BYPASS)));
            Utilities.SetEnableState(uiPgmPrevBtn, (isNoErrs && (EditProgram != (int)ProgramNumbers.MAN1)));

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
                "Reset Configruation?",
                "Are you sure you want to reset the CMDS configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(CMDSSystem.SystemTag);
                Config.CMDS.Reset();
                Config.Save(this, CMDSSystem.SystemTag);
                CopyConfigToEdit(EditProgram);
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, CMDSSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                Config.UnlinkSystem(CMDSSystem.SystemTag);
                Config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(CMDSSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit(EditProgram);
            }
        }

        /// <summary>
        /// reset chaff program click: set all chaff program values to default.
        /// </summary>
        private void PgmChaffBtnReset_Click(object sender, RoutedEventArgs args)
        {
            EditCMDS.Programs[0].Chaff.Reset();
            CopyEditToConfig(EditProgram, true);
        }

        /// <summary>
        /// reset flare program click: set all flare program values to default.
        /// </summary>
        private void PgmFlareBtnReset_Click(object sender, RoutedEventArgs args)
        {
            EditCMDS.Programs[0].Flare.Reset();
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
        private void CMDSTextBox_LostFocus(object sender, RoutedEventArgs args)
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
            Config = (F16CConfiguration)NavArgs.Config;

            Config.ConfigurationSaved += ConfigurationSavedHandler;

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, CMDSSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            CopyConfigToEdit(EditProgram);

            ValidateAllFields(_baseFieldValueMap, EditCMDS.GetErrors(null));
            ValidateAllFields(_pgmChaffFieldValueMap, EditCMDS.Programs[0].Chaff.GetErrors(null));
            ValidateAllFields(_pgmFlareFieldValueMap, EditCMDS.Programs[0].Flare.GetErrors(null));

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
