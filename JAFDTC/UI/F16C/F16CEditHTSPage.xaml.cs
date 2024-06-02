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

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class F16CEditHTSPage : Page
    {
        public static ConfigEditorPageInfo PageInfo
            => new(HTSSystem.SystemTag, "HTS Threats", "HTS", Glyphs.HTS, typeof(F16CEditHTSPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------
        private ConfigEditorPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditHTS property.
        //
        private F16CConfiguration Config { get; set; }

        private HTSSystem EditHTS { get; set; }

        private bool IsRebuildPending { get; set; }

        // ---- readonly properties

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        private readonly List<string> _emitterTitles;
        private readonly Dictionary<string, Emitter> _emitterTitleMap;

        private readonly List<TextBox> _tableCodeFields;
        private readonly List<FontIcon> _tableEditFields;
        private readonly List<List<TextBlock>> _tableFields;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditHTSPage()
        {
            InitializeComponent();

            EditHTS = new HTSSystem();

            // NOTE: for this to work here, we need to not change the instances in MANTable[]. changes should
            // NOTE: be done by modifying fields in the MANTable[] instnaces.
            //
            for (int row = 0; row < EditHTS.MANTable.Count; row++)
            {
                EditHTS.MANTable[row].ErrorsChanged += CodeField_DataValidationError;
                EditHTS.MANTable[row].PropertyChanged += CodeField_PropertyChanged;
            }

            IsRebuildPending = false;

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

            _tableCodeFields = new List<TextBox>()
            {
                uiT1ValueCode, uiT2ValueCode, uiT3ValueCode, uiT4ValueCode,
                uiT5ValueCode, uiT6ValueCode, uiT7ValueCode, uiT8ValueCode
            };
            _tableEditFields = new List<FontIcon>()
            {
                uiT1IconEdit, uiT2IconEdit, uiT3IconEdit, uiT4IconEdit,
                uiT5IconEdit, uiT6IconEdit, uiT7IconEdit, uiT8IconEdit
            };
            _tableFields = new List<List<TextBlock>>()
            {
                new List<TextBlock>() { uiT1RWRText, uiT1ValueCountry, uiT1ValueType, uiT1ValueName },
                new List<TextBlock>() { uiT2RWRText, uiT2ValueCountry, uiT2ValueType, uiT2ValueName },
                new List<TextBlock>() { uiT3RWRText, uiT3ValueCountry, uiT3ValueType, uiT3ValueName },
                new List<TextBlock>() { uiT4RWRText, uiT4ValueCountry, uiT4ValueType, uiT4ValueName },
                new List<TextBlock>() { uiT5RWRText, uiT5ValueCountry, uiT5ValueType, uiT5ValueName },
                new List<TextBlock>() { uiT6RWRText, uiT6ValueCountry, uiT6ValueType, uiT6ValueName },
                new List<TextBlock>() { uiT7RWRText, uiT7ValueCountry, uiT7ValueType, uiT7ValueName },
                new List<TextBlock>() { uiT8RWRText, uiT8ValueCountry, uiT8ValueType, uiT8ValueName }
            };
            _defaultBorderBrush = uiT1ValueCode.BorderBrush;
            _defaultBkgndBrush = uiT1ValueCode.Background;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        // marshall data between our local state and the hts configuration. note we do not replace the TableCode
        // instance in MANTable[] to avoid disrupting bindings.
        //
        private void CopyConfigToEdit()
        {
            for (int i = 0; i < EditHTS.EnabledThreats.Length; i++)
            {
                EditHTS.EnabledThreats[i] = Config.HTS.EnabledThreats[i];
            }
            for (int i = 0; i < EditHTS.MANTable.Count; i++)
            {
                EditHTS.MANTable[i].Code = new(Config.HTS.MANTable[i].Code);
            }
        }

        private void CopyEditToConfig(bool isPersist = false)
        {
            if (!CurStateHasErrors())
            {
                for (int i = 0; i < Config.HTS.EnabledThreats.Length; i++)
                {
                    Config.HTS.EnabledThreats[i] = EditHTS.EnabledThreats[i];
                }
                for (int row = 0; row < Config.HTS.MANTable.Count; row++)
                {
                    Config.HTS.MANTable[row].Code = new(EditHTS.MANTable[row].Code);
                }

                if (isPersist)
                {
                    Config.Save(this, HTSSystem.SystemTag);
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
            field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBackgroundBrush"];
        }

        // validation error: update ui state for the various components (base, chaff program, flare program)
        // that may have errors.
        //
        private void CodeField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            int row = EditHTS.MANTable.IndexOf((TableCode)sender);
            if ((row != -1) && (args.PropertyName != null))
            {
                List<string> errors = (List<string>)EditHTS.MANTable[row].GetErrors(args.PropertyName);
                SetFieldValidState(_tableCodeFields[row], (errors.Count == 0));
            }
            RebuildInterfaceState();
        }

        // property changed: rebuild interface state to account for configuration changes.
        //
        private void CodeField_PropertyChanged(object sender, EventArgs args)
        {
            for (int row = 0; row < EditHTS.MANTable.Count; row++)
            {
                RebuildTableRowContent(row);
            }
        }

        // returns true if the current state has errors, false otherwise.
        //
        private bool CurStateHasErrors()
        {
            for (int row = 0; row < EditHTS.MANTable.Count; row++)
            {
                if (EditHTS.MANTable[row].HasErrors)
                {
                    return true;
                }
            }
            return false;
        }

        // returns true if the current state is default, false otherwise.
        //
        private bool PageIsDefault()
        {
            bool isDefault = !EditHTS.IsMANTablePopulated && !EditHTS.EnabledThreats[0];
            for (int threat = 1; threat < EditHTS.EnabledThreats.Length; threat++)
            {
                if (!EditHTS.EnabledThreats[threat])
                {
                    isDefault = false;
                    break;
                }
            }
            return isDefault;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        // rebuild the content in the man threat table definition using the emitter database to lookup the details
        // on the programmed emitters.
        //
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
                    {
                        name += $" ({emitterList[0].NATO})";
                    }
                }
                fields[3].Text = name;
            }
        }

        // update the summary content in the threat list based on the selected threat tables.
        //
        private void RebuildThreatListContent()
        {
            List<string> threats = new();
            for (int threat = 1; threat < EditHTS.EnabledThreats.Length; threat++)
            {
                if (EditHTS.EnabledThreats[threat])
                {
                    threats.Add($"T{threat}");
                }
            }
            if (EditHTS.EnabledThreats[0])
            {
                threats.Add("MAN");
            }
            uiThreatTextList.Text = (threats.Count > 0) ? General.JoinList(threats) : "No HTS threat classes are enabled.";
        }

        // TODO: document
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, HTSSystem.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        // update the enable state on the ui elements based on the current settings. link controls must be set up
        // vi RebuildLinkControls() prior to calling this function.
        //
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(HTSSystem.SystemTag));
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

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);

            Utilities.SetEnableState(uiPageBtnResetAll, !PageIsDefault());
            Utilities.SetEnableState(uiMANBtnResetTable, EditHTS.IsMANTablePopulated && isEditable);
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
                    for (int row = 0; row < EditHTS.MANTable.Count; row++)
                    {
                        RebuildTableRowContent(row);
                    }
                    RebuildThreatListContent();
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

        // reset all button click: reset all hts settings back to their defaults.
        //
        private async void PageBtnResetAll_Click(object sender, RoutedEventArgs args)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configuration?",
                "Are you sure you want to reset the HTS configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(HTSSystem.SystemTag);
                Config.HTS.Reset();
                Config.Save(this, HTSSystem.SystemTag);
                CopyConfigToEdit();
            }
        }

        // TODO: document
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, HTSSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                Config.UnlinkSystem(HTSSystem.SystemTag);
                Config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(HTSSystem.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit();
            }
        }

        // reset table click: reset table to defaults. this implicitly disables man table enable as without a man
        // table, the option to enable man table threats is not available in the hts format.
        //
        private void MANBtnResetTable_Click(object sender, RoutedEventArgs args)
        {
            for (int row = 0; row < Config.HTS.MANTable.Count; row++)
            {
                Config.HTS.MANTable[row].Reset();
            }
            Config.HTS.EnabledThreats[0] = false;
            Config.Save(this, HTSSystem.SystemTag);
            CopyConfigToEdit();
        }

        // --- threat selection ---------------------------------------------------------------------------------------

        // select threat click: push changes to the configuration and navigate to the threat selection page to update
        // that state in the configuration.
        //
        private void ThreatBtnSelect_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(true);
            RebuildInterfaceState();

            NavArgs.BackButton.IsEnabled = false;
            bool isUnlinked = string.IsNullOrEmpty(Config.SystemLinkedTo(HTSSystem.SystemTag));
            Frame.Navigate(typeof(F16CEditHTSThreatsPage), new F16CEditHTSThreatsPageNavArgs(this, Config, isUnlinked),
                           new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        // --- add emitter --------------------------------------------------------------------------------------------

        // add button click: open the ui to prompt to select an emitter.
        //
        private async void MANBtnAdd_Click(object sender, RoutedEventArgs args)
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

                CopyEditToConfig(true);
            }
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        // text box lost focus: copy the local backing values to the configuration (note this is predicated on error
        // status) and rebuild the interface state.
        //
        // NOTE: though the text box has lost focus, the update may not yet have propagated into state. use the
        // NOTE: dispatch queue to give in-flight state updates time to complete.
        //
        private void MANTextBox_LostFocus(object sender, RoutedEventArgs args)
        {
            // CONSIDER: may be better here to handle this in a property changed handler rather than here?
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CopyEditToConfig(true);
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

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, HTSSystem.SystemTag,
                                           _configNameList, _configNameToUID);

            CopyConfigToEdit();

            NavArgs.BackButton.IsEnabled = true;
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
