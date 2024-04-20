// ********************************************************************************************************************
//
// EditRadioPage.cs : ui c# for general radio setup editor page
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
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using ABI.Windows.AI.MachineLearning;
using Microsoft.UI.Xaml.Markup;
using System.Xml.Linq;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// object representing the user interface view of the radio preset displayed in the preset list in the radio
    /// preset editor. this object presents a ui-centric generic version of the radio preset object for a particular
    /// airframe suitable to binding to ui widgets.
    /// </summary>
    public sealed class RadioPresetItem : BindableObject
    {
        public IEditRadioPageHelper NavHelper { get; }

        public object Tag { get; set; }

        public int Radio { get; set; }

        public string Modulation { get; set; }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value, null);
        }

        private string _preset;
        public string Preset
        {
            get => _preset;
            set
            {
                string error = (NavHelper.ValidatePreset(Radio, value, false)) ? null : "Invalid preset";
                SetProperty(ref _preset, value, error);
            }
        }

        private string _frequency;
        public string Frequency
        {
            get => _frequency;
            set
            {
                string error = "Invalid frequency";
                if (NavHelper.ValidateFrequency(Radio, value, false))
                {
                    value = NavHelper.FixupFrequency(Radio, value);
                    error = null;
                }
                SetProperty(ref _frequency, value, error);
            }
        }

        private int _modulationIndex;
        public int ModulationIndex
        {
            get => _modulationIndex;
            set => SetProperty(ref _modulationIndex, (value == -1) ? _modulationIndex : value, null);
        }

        private List<TextBlock> _modulationItems;
        public List<TextBlock> ModulationItems
        {
            get => _modulationItems;
            set => SetProperty(ref _modulationItems, value, null);
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value, null);
        }

        public Visibility ModulationVisibility
            => NavHelper.RadioCanProgramModulation(Radio) ? Visibility.Visible : Visibility.Collapsed;

        public RadioPresetItem(IEditRadioPageHelper helper, int tag, int radio)
            => (NavHelper, Tag, Radio, Preset, Frequency, Description, IsEnabled) = (helper, tag, radio, "", "", "", true);
    }

    // ================================================================================================================

    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed class RadioMiscItem : BindableObject
    {
        public IEditRadioPageHelper NavHelper { get; }

        public int Radio { get; set; }

        private string _defaultTuning;
        public string DefaultTuning
        {
            get => _defaultTuning;
            set
            {
                string error = "Invalid default tuning";
                string newValue = NavHelper.ValidateDefaultTuning(Radio, value);
                if (newValue != null)
                {
                    value = newValue;
                    error = null;
                }
                SetProperty(ref _defaultTuning, value, error);
            }
        }

        private bool _isAux1Enabled;
        public bool IsAux1Enabled
        {
            get => _isAux1Enabled;
            set => SetProperty(ref _isAux1Enabled, value, null);
        }

        private bool _isAux2Enabled;
        public bool IsAux2Enabled
        {
            get => _isAux2Enabled;
            set => SetProperty(ref _isAux2Enabled, value, null);
        }

        private bool _isAux3Enabled;
        public bool IsAux3Enabled
        {
            get => _isAux3Enabled;
            set => SetProperty(ref _isAux3Enabled, value, null);
        }

        private bool _isAux4Enabled;
        public bool IsAux4Enabled
        {
            get => _isAux4Enabled;
            set => SetProperty(ref _isAux4Enabled, value, null);
        }

        public RadioMiscItem(IEditRadioPageHelper helper, int radio)
            => (NavHelper, Radio, DefaultTuning, IsAux1Enabled, IsAux2Enabled, IsAux3Enabled, IsAux4Enabled)
                = (helper, radio, "", false, false, false, false);
    }

    // ================================================================================================================

    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class EditRadioPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private ConfigEditorPageNavArgs NavArgs { get; set; }

        private IEditRadioPageHelper NavHelper { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditPresets/EditRadio properties.
        //
        private IConfiguration Config { get; set; }

        private ObservableCollection<RadioPresetItem> EditPresets { get; set; }

        private int EditRadio { get; set; }

        private RadioMiscItem EditMisc { get; set; }

        private int EditItemTag { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsRebuildingUI { get; set; }

        // ---- read-only properties

        private readonly Dictionary<string, string> _configNameToUID;
        private readonly List<string> _configNameList;

        private readonly List<TextBlock> _radioSelComboText;
        private readonly List<FontIcon> _radioSelComboIcons;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public EditRadioPage()
        {
            InitializeComponent();

            EditPresets = new ObservableCollection<RadioPresetItem>();
            EditRadio = 0;
            EditItemTag = 1;

            _configNameToUID = new Dictionary<string, string>();
            _configNameList = new List<string>();

            _radioSelComboText = new List<TextBlock>()
            {
                uiRadSelectItem0Text, uiRadSelectItem1Text, uiRadSelectItem2Text, uiRadSelectItem3Text
            };
            _radioSelComboIcons = new List<FontIcon>()
            {
                uiRadSelectItem0Icon, uiRadSelectItem1Icon, uiRadSelectItem2Icon, uiRadSelectItem3Icon
            };
            _defaultBorderBrush = uiRadPrevBtn.BorderBrush;
            _defaultBkgndBrush = uiRadPrevBtn.Background;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshall data between the appropriate preset set in the radio configuration and our local radio state.
        /// </summary>
        private void CopyConfigToEdit(int radio)
        {
            foreach (RadioPresetItem item in EditPresets)
            {
                item.ErrorsChanged -= PreField_DataValidationError;
                item.PropertyChanged -= PreField_PropertyChanged;
            }
            NavHelper.CopyConfigToEdit(radio, Config, EditPresets, EditMisc);
            foreach (RadioPresetItem item in EditPresets)
            {
                item.ErrorsChanged += PreField_DataValidationError;
                item.PropertyChanged += PreField_PropertyChanged;
                item.Tag = EditItemTag++;
                item.ModulationItems = NavHelper.RadioModulationItems(radio, item.Frequency);
                if (item.ModulationItems != null)
                {
                    int modIndex = 0;
                    for (int i = 0; i < item.ModulationItems.Count; i++)
                    {
                        if (item.Modulation == (string)item.ModulationItems[i].Tag)
                        {
                            modIndex = i;
                            break;
                        }
                    }
                    item.ModulationIndex = modIndex;
                }
            }
        }

        /// <summary>
        /// marshall data between our local radio state and the appropriate preset set in the radio configuration.
        /// </summary>
        private void CopyEditToConfig(int radio, bool isPersist = false)
        {
            if (!CurStateHasErrors() && !EditMisc.HasErrors)
            {
                foreach (RadioPresetItem item in EditPresets)
                {
                    if (item.ModulationItems != null)
                    {
                        int index = item.ModulationIndex;
                        if ((index < 0) || (index >= item.ModulationItems.Count))
                        {
                            index = 0;
                        }
                        item.Modulation = (string)item.ModulationItems[index].Tag;
                    }
                }
                NavHelper.CopyEditToConfig(radio, EditPresets, EditMisc, Config);
                if (isPersist)
                {
                    Config.Save(this, NavHelper.SystemTag);
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

        private TextBox FindFieldForPresetItem(RadioPresetItem item, string name)
        {
            Grid gridRow = Utilities.FindControl<Grid>(uiPreListView, typeof(Grid), item.Tag);
            return (gridRow != null) ? Utilities.FindControl<TextBox>(gridRow, typeof(TextBox), name) : null;
        }

        private ComboBox FindComboForPresetItem(RadioPresetItem item)
        {
            Grid gridRow = Utilities.FindControl<Grid>(uiPreListView, typeof(Grid), item.Tag);
            return (gridRow != null) ? Utilities.FindControl<ComboBox>(gridRow, typeof(ComboBox), "Modulation") : null;
        }

        private void ValidateAllFields()
        {
            foreach (RadioPresetItem item in EditPresets)
            {
                TextBox fieldPreset = FindFieldForPresetItem(item, "Preset");
                if (fieldPreset != null)
                {
                    SetFieldValidState(fieldPreset, ((List<string>)item.GetErrors("Preset")).Count == 0);
                }
                TextBox fieldFreq = FindFieldForPresetItem(item, "Frequency");
                if (fieldFreq != null)
                {
                    SetFieldValidState(fieldFreq, ((List<string>)item.GetErrors("Frequency")).Count == 0);
                }
            }
            SetFieldValidState(uiMiscValueDefaultFreq, ((List<string>)EditMisc.GetErrors("DefaultTuning")).Count == 0);
        }

        /// <summary>
        /// validation error: update ui state for the various components that may have errors.
        /// </summary>
        private void PreField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            RadioPresetItem item = (RadioPresetItem)sender;
            Grid listRowGrid = Utilities.FindControl<Grid>(uiPreListView, typeof(Grid), item.Tag);
            if (args.PropertyName == null)
            {
                ValidateAllFields();
            }
            else if (listRowGrid != null)
            {
                List<string> errors = (List<string>)item.GetErrors(args.PropertyName);
                TextBox field = Utilities.FindControl<TextBox>(listRowGrid, typeof(TextBox), args.PropertyName);
                SetFieldValidState(field, (errors.Count == 0));
            }
            RebuildInterfaceState();
        }

        private void MiscField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            RadioMiscItem item = (RadioMiscItem)sender;
            if (args.PropertyName == null)
            {
                ValidateAllFields();
            }
            else if (args.PropertyName == "DefaultTuning")
            {
                List<string> errors = (List<string>)item.GetErrors(args.PropertyName);
                SetFieldValidState(uiMiscValueDefaultFreq, (errors.Count == 0));
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// property changed: rebuild interface state to account for configuration changes.
        /// </summary>
        private void PreField_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if ((args.PropertyName == "Preset") && (EditPresets.Count > 1))
            {
                // TODO: check for repeated preset numbers here also, or nah?
                for (int i = 1; i < EditPresets.Count; i++)
                {
                    if (int.Parse(EditPresets[i].Preset) < int.Parse(EditPresets[i - 1].Preset))
                    {
                        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                        {
                            SortPresets();
                        });
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// returns true if the current state has errors, false otherwise.
        /// </summary>
        private bool CurStateHasErrors()
        {
            foreach (RadioPresetItem item in EditPresets)
            {
                if (item.HasErrors)
                {
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// create a new radio preset and add it to the preset list. the new item is initialized from a preset in the
        /// configuration or natively here. the preset number will be set to +1 beyond max, if that would put it out
        /// of range, we will start to fill unassigned presets. frequency defaults to the lowest valid frequency.
        /// </summary>
        void AddNewPreset(int radio)
        {
            Int64 mask = 0;
            int newPreset = 1;
            int maxPreset = NavHelper.RadioMaxPresets(radio);
            int newIndex = EditPresets.Count;
            foreach (RadioPresetItem item in EditPresets)
            {
                int preset = int.Parse(item.Preset);
                mask |= ((Int64)1 << preset);
                newPreset = Math.Max(newPreset, preset + 1);
            }
            if (newPreset > maxPreset)
            {
                newIndex = 0;
                for (newPreset = 1; (newPreset < maxPreset) && ((mask & ((Int64)1 << newPreset)) != 0); newPreset++)
                    newIndex++;
            }

            RadioPresetItem newItem = new(NavHelper, EditItemTag++, radio);
            newItem.ErrorsChanged += PreField_DataValidationError;
            newItem.PropertyChanged += PreField_PropertyChanged;
            newItem.Preset = newPreset.ToString();
            newItem.Frequency = NavHelper.RadioDefaultFrequency(radio);
            newItem.ModulationItems = NavHelper.RadioModulationItems(radio, newItem.Frequency);
            EditPresets.Insert(newIndex, newItem);
        }

        /// <summary>
        /// sort the current list of presets by name in place in the presets list. this is done in place to avoid
        /// changing the EditPresets instance.
        /// </summary>
        private void SortPresets()
        {
            var sortableList = new List<RadioPresetItem>(EditPresets);
            sortableList.Sort((a, b) => int.Parse(a.Preset).CompareTo(int.Parse(b.Preset)));
            for (int i = 0; i < sortableList.Count; i++)
            {
                EditPresets.Move(EditPresets.IndexOf(sortableList[i]), i);
            }
        }

        /// <summary>
        /// change the selected cmds program and update various ui and model state.
        /// </summary>
        private void SelectRadio(int radio)
        {
            if (radio != EditRadio)
            {
                CopyEditToConfig(EditRadio, true);
                EditRadio = radio;
                CopyConfigToEdit(EditRadio);
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// returns the preset item with the given tag, null if no such item is found.
        /// </summary>
        private RadioPresetItem FindPresetItemByTag(object tag)
        {
            foreach (RadioPresetItem item in EditPresets)
            {
                if (item.Tag.Equals(tag))
                {
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildModulationForPreset(object tag)
        {
            RadioPresetItem item = FindPresetItemByTag(tag);
            if (item != null)
            {
                List<TextBlock> newItems = NavHelper.RadioModulationItems(EditRadio, item.Frequency);
                if ((newItems != null) && (newItems.Count > 0)) 
                {
                    int newIndex = 0;
                    for (int i = 0; i < newItems.Count; i++)
                    {
                        if (item.Modulation == (string)newItems[i].Tag)
                        {
                            newIndex = i;
                            break;
                        }
                    }

                    item.ModulationItems = newItems;
                    ComboBox combo = FindComboForPresetItem(item);
                    if (combo != null)
                    {
                        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                        {
                            combo.SelectedIndex = newIndex;
                        });
                    }
                }
            }
        }

        /// <summary>
        /// update the "blue dot" state on the radio select menu to show the blue dot when the setup of the
        /// corresponding radio differs from defaults.
        /// </summary>
        private void RebuildRadioSelectMenu()
        {
            for (int i = 0; i < NavHelper.RadioNames.Count; i++)
            {
                _radioSelComboIcons[i].Visibility = (NavHelper.RadioModuleIsDefault(Config, i)) ? Visibility.Collapsed
                                                                                                : Visibility.Visible;
            }
        }

        /// <summary>
        /// rebuild the link controls on the page based on where the configuration is linked to.
        /// </summary>
        private void RebuildLinkControls()
        {
            Utilities.RebuildLinkControls(Config, NavHelper.SystemTag, NavArgs.UIDtoConfigMap,
                                          uiPageBtnTxtLink, uiPageTxtLink);
        }

        /// <summary>
        /// rebuild the radio misc control by setting visibility or title text as necessary.
        /// </summary>
        private static void RebuildPerRadioMiscAuxControl(CheckBox checkbox, TextBlock titleBlock, string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                checkbox.Visibility = Visibility.Collapsed;
            }
            else
            {
                checkbox.Visibility = Visibility.Visible;
                titleBlock.Text = title;
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void RebuildPerRadioMiscControls()
        {
            RebuildPerRadioMiscAuxControl(uiMiscCkbxAux1, uiMiscTextAux1, NavHelper.RadioAux1Title(EditRadio));
            RebuildPerRadioMiscAuxControl(uiMiscCkbxAux2, uiMiscTextAux2, NavHelper.RadioAux2Title(EditRadio));
            RebuildPerRadioMiscAuxControl(uiMiscCkbxAux3, uiMiscTextAux3, NavHelper.RadioAux3Title(EditRadio));
            RebuildPerRadioMiscAuxControl(uiMiscCkbxAux4, uiMiscTextAux4, NavHelper.RadioAux4Title(EditRadio));

            if (string.IsNullOrEmpty(EditMisc.DefaultTuning) || EditMisc.HasErrors)
            {
                uiMiscTextDefaultLabel.Text = "";
            }
            else if (int.TryParse(EditMisc.DefaultTuning, out _))
            {
                uiMiscTextDefaultLabel.Text = "Preset";
            }
            else
            {
                uiMiscTextDefaultLabel.Text = "MHz";
            }
        }

        /// <summary>
        /// update the enable state on the ui elements based on the current settings. link controls must be set up via
        /// RebuildLinkControls() prior to calling this function.
        /// </summary>
        private void RebuildEnableState()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(NavHelper.SystemTag));

            if ((EditPresets.Count > 0) && (EditPresets[0].IsEnabled != isEditable))
            {
                foreach (RadioPresetItem item in EditPresets)
                {
                    item.IsEnabled = isEditable;
                }
            }

            Utilities.SetEnableState(uiBarAdd, isEditable && (EditPresets.Count < NavHelper.RadioMaxPresets(EditRadio)));
            Utilities.SetEnableState(uiBarImport, isEditable);
            Utilities.SetEnableState(uiBarExport, isEditable && (EditPresets.Count > 0));

            Utilities.SetEnableState(uiMiscValueDefaultFreq, isEditable);
            Utilities.SetEnableState(uiMiscCkbxAux1, isEditable);
            Utilities.SetEnableState(uiMiscCkbxAux2, isEditable);
            Utilities.SetEnableState(uiMiscCkbxAux3, isEditable);
            Utilities.SetEnableState(uiMiscCkbxAux4, isEditable);

            bool isDefault = NavHelper.RadioSysIsDefault(Config) && (EditPresets.Count == 0);
            Utilities.SetEnableState(uiPageBtnResetAll, !isDefault);

            Utilities.SetEnableState(uiPageBtnLink, _configNameList.Count > 0);

            bool isNoErrs = !CurStateHasErrors();
            Utilities.SetEnableState(uiRadSelectCombo, isNoErrs);
            Utilities.SetEnableState(uiRadNextBtn, (isNoErrs && (EditRadio < (NavHelper.RadioNames.Count - 1))));
            Utilities.SetEnableState(uiRadPrevBtn, (isNoErrs && (EditRadio > 0)));
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
                    RebuildRadioSelectMenu();
                    RebuildPerRadioMiscControls();
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
        /// reset all button click: reset all radio settings back to their defaults.
        /// </summary>
        private async void PageBtnResetAll_Click(object sender, RoutedEventArgs args)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configruation?",
                "Are you sure you want to reset the radio configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                Config.UnlinkSystem(NavHelper.SystemTag);
                NavHelper.RadioSysReset(Config);
                Config.Save(this, NavHelper.SystemTag);
                CopyConfigToEdit(EditRadio);
            }
        }

        /// <summary>
        /// system link click: manage the ui to link this configuration to another radio configuration.
        /// </summary>
        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, Config, NavHelper.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                Config.UnlinkSystem(NavHelper.SystemTag);
                Config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                Config.LinkSystemTo(NavHelper.SystemTag, NavArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                Config.Save(this);
                CopyConfigToEdit(EditRadio);
            }
        }

        // ---- commands ----------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void CmdAdd_Click(object sender, RoutedEventArgs args)
        {
            // TODO: scroll to visible after adding new preset?
            AddNewPreset(EditRadio);
            CopyEditToConfig(EditRadio, true);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void CmdImport_Click(object sender, RoutedEventArgs args)
        {
            // TODO: implement
            await Utilities.Message1BDialog(Content.XamlRoot, "Sad Trombone", "Not yet supported, you'll have to do it the old-fashioned way...");
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private async void CmdExport_Click(object sender, RoutedEventArgs args)
        {
            // TODO: implement
            await Utilities.Message1BDialog(Content.XamlRoot, "Sad Trombone", "Not yet supported, you'll have to do it the old-fashioned way...");
        }

        // ---- radio selection ---------------------------------------------------------------------------------------

        /// <summary>
        /// previous radio button click: advance to the previous radio.
        /// </summary>
        private void RadBtnPrev_Click(object sender, RoutedEventArgs args)
        {
            SelectRadio(EditRadio - 1);
            uiRadSelectCombo.SelectedIndex = EditRadio;
        }

        /// <summary>
        /// next radio button click: advance to the next radio.
        /// </summary>
        private void RadBtnNext_Click(object sender, RoutedEventArgs args)
        {
            SelectRadio(EditRadio + 1);
            uiRadSelectCombo.SelectedIndex = EditRadio;
        }

        /// <summary>
        /// radio select combo click: switch to the selected radio. the tag of the sender (a TextBlock) gives us the
        /// radio number to select.
        /// </summary>
        private void RadSelectCombo_SelectionChanged(object sender, RoutedEventArgs args)
        {
            Grid item = (Grid)((ComboBox)sender).SelectedItem;
            if (!IsRebuildingUI && (item != null) && (item.Tag != null))
            {
                // NOTE: assume tag == index here...
                SelectRadio(int.Parse((string)item.Tag));
            }
        }

        // ---- miscellaneous controls --------------------------------------------------------------------------------

        /// <summary>
        /// aux checkbox 1 click: copy the local backing values to the configuration.
        /// </summary>
        private void MiscCkbxAux1_Click(object sender, RoutedEventArgs args)
        {
            // HACK: x:Bind doesn't work with bools? seems that way? this is a hack.
            //
            CheckBox cbox = (CheckBox)sender;
            EditMisc.IsAux1Enabled = (bool)cbox.IsChecked;
            CopyEditToConfig(EditRadio, true);
        }

        /// <summary>
        /// aux checkbox 2 click: copy the local backing values to the configuration.
        /// </summary>
        private void MiscCkbxAux2_Click(object sender, RoutedEventArgs args)
        {
            // HACK: x:Bind doesn't work with bools? seems that way? this is a hack.
            //
            CheckBox cbox = (CheckBox)sender;
            EditMisc.IsAux2Enabled = (bool)cbox.IsChecked;
            CopyEditToConfig(EditRadio, true);
        }

        /// <summary>
        /// aux checkbox 3 click: copy the local backing values to the configuration.
        /// </summary>
        private void MiscCkbxAux3_Click(object sender, RoutedEventArgs args)
        {
            // HACK: x:Bind doesn't work with bools? seems that way? this is a hack.
            //
            CheckBox cbox = (CheckBox)sender;
            EditMisc.IsAux3Enabled = (bool)cbox.IsChecked;
            CopyEditToConfig(EditRadio, true);
        }

        /// <summary>
        /// aux checkbox 4 click: copy the local backing values to the configuration.
        /// </summary>
        private void MiscCkbxAux4_Click(object sender, RoutedEventArgs args)
        {
            // HACK: x:Bind doesn't work with bools? seems that way? this is a hack.
            //
            CheckBox cbox = (CheckBox)sender;
            EditMisc.IsAux4Enabled = (bool)cbox.IsChecked;
            CopyEditToConfig(EditRadio, true);
        }

        // ---- preset list -------------------------------------------------------------------------------------------

        /// <summary>
        /// preset delete button click: delete the preset associated with the row.
        /// </summary>
        private void PreBntDelete_Click(object sender, RoutedEventArgs args)
        {
            Button btn = (Button)sender;
            RadioPresetItem item = FindPresetItemByTag(btn.Tag);
            if (item != null)
            {
                EditPresets.Remove(item);
                CopyEditToConfig(EditRadio, true);
            }
        }

        // ---- modulation changes ------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void PageModCombo_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            // CONSIDER: may be better here to handle this in a property changed handler rather than here?
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CopyEditToConfig(EditRadio, true);
            });
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        /// <summary>
        /// text box lost focus: copy the local backing values to the configuration (note this is predicated on error
        /// status) and save (which will rebuild the interface state). if we're updating a preset frequency, also
        /// handle any modulation updates.
        ///
        /// NOTE: though the text box has lost focus, the update may not yet have propagated into state. use the
        /// NOTE: dispatch queue to give in-flight state updates time to complete.
        /// </summary>
        private void PageTextBox_LostFocus(object sender, RoutedEventArgs args)
        {
            // CONSIDER: may be better here to handle this in a property changed handler rather than here?
            TextBox txtBox = (TextBox)sender;
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                if ((txtBox.Tag != null) && txtBox.Tag.Equals("Frequency"))
                {
                    RebuildModulationForPreset(((Grid)txtBox.Parent).Tag);
                }
                CopyEditToConfig(EditRadio, true);
            });
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configuration saved: rebuild interface state to align with the latest save.
        /// </summary>
        private void ConfigurationSavedHandler(object sender, ConfigurationSavedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        /// we are editing.
        /// 
        /// we do not use page caching here as we're just tracking the configuration state.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;

            NavHelper = (IEditRadioPageHelper)Activator.CreateInstance(NavArgs.EditorHelperType);

            EditMisc = new RadioMiscItem(NavHelper, 0);
            EditMisc.ErrorsChanged += MiscField_DataValidationError;

            while (uiRadSelectCombo.Items.Count > NavHelper.RadioNames.Count)
            {
                uiRadSelectCombo.Items.RemoveAt(uiRadSelectCombo.Items.Count - 1);
            }
            for (int i = 0; i < NavHelper.RadioNames.Count; i++)
            {
                _radioSelComboText[i].Text = NavHelper.RadioNames[i];
            }

            Config = NavArgs.Config;
            Config.ConfigurationSaved += ConfigurationSavedHandler;
            CopyConfigToEdit(EditRadio);

            Utilities.BuildSystemLinkLists(NavArgs.UIDtoConfigMap, Config.UID, NavHelper.SystemTag,
                                           _configNameList, _configNameToUID);

            uiRadSelectCombo.SelectedIndex = EditRadio;
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
