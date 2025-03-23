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
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using System.Threading.Tasks;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// object representing the user interface view of the radio preset displayed in the preset list in the radio
    /// preset editor. this object presents a ui-centric generic version of the radio preset object for a particular
    /// airframe suitable to binding to ui widgets.
    /// </summary>
    public sealed class RadioPresetItem : BindableObject
    {
        public IEditRadioPageHelper PageHelper { get; }

        public object Tag { get; set; }

        public int Radio { get; set; }

        public string Modulation { get; set; }

        public List<TextBlock> ModulationItems { get; set; }

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
                string error = (PageHelper.ValidatePreset(Radio, value, false)) ? null : "Invalid preset";
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
                if (PageHelper.ValidateFrequency(Radio, value, false))
                {
                    value = PageHelper.FixupFrequency(Radio, value);
                    error = null;
                }
                SetProperty(ref _frequency, value, error);
            }
        }

        private int _modulationIndex;
        public int ModulationIndex
        {
            get => _modulationIndex;
            set => SetProperty(ref _modulationIndex, value, null);
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value, null);
        }

        public Visibility ModulationVisibility
            => PageHelper.RadioCanProgramModulation(Radio) ? Visibility.Visible : Visibility.Collapsed;

        public RadioPresetItem(IEditRadioPageHelper helper, int tag, int radio)
            => (PageHelper, Tag, Radio, Preset, Frequency, Description, IsEnabled) = (helper, tag, radio, "", "", "", true);
    }

    // ================================================================================================================

    /// <summary>
    /// object representing the user interface view of the miscellaneous controls in the radio preset editor. this
    /// object presents a ui-centric generic version of the miscellaneous settings for a particular airframe suitable
    /// to binding to ui widgets.
    /// </summary>
    public sealed class RadioMiscItem : BindableObject
    {
        public IEditRadioPageHelper PageHelper { get; }

        public int Radio { get; set; }

        private string _defaultTuning;
        public string DefaultTuning
        {
            get => _defaultTuning;
            set
            {
                string error = "Invalid default tuning";
                string newValue = PageHelper.ValidateDefaultTuning(Radio, value);
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

        public RadioMiscItem(IEditRadioPageHelper helper, int radio)
            => (PageHelper, Radio, DefaultTuning, IsAux1Enabled, IsAux2Enabled, IsAux3Enabled)
                = (helper, radio, "", false, false, false);
    }

    // ================================================================================================================

    /// <summary>
    /// page to edit radio presets. this is a general-purpose class that is instatiated in combination with an
    /// IEditRadioPageHelper class to provide airframe-specific specialization.
    /// 
    /// using IEditRadioPageHelper, this class can support other radio systems that go beyond the basic functionality
    /// </summary>
    public sealed partial class EditRadioPage : SystemEditorPageBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPage properties

        protected override SystemBase SystemConfig => null;

        protected override String SystemTag => PageHelper.SystemTag;

        protected override string SystemName => "radio";

        protected override bool IsPageStateDefault => (EditPresets.Count == 0);

        // ---- internal properties

        private IEditRadioPageHelper PageHelper { get; set; }

        private ObservableCollection<RadioPresetItem> EditPresets { get; set; }

        private int EditRadio { get; set; }

        private RadioMiscItem EditMisc { get; set; }

        private int EditItemTag { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public EditRadioPage()
        {
            EditPresets = new ObservableCollection<RadioPresetItem>();
            EditRadio = 0;
            EditItemTag = 1;

            InitializeComponent();
            InitializeBase(null, uiMiscValueDefaultFreq, uiCtlLinkResetBtns, new List<string>() { });
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshall data between the appropriate preset set in the radio configuration and our local radio state.
        /// </summary>
        protected override void CopyConfigToEditState()
        {
            foreach (RadioPresetItem item in EditPresets)
            {
                item.ErrorsChanged -= PreField_DataValidationError;
                item.PropertyChanged -= PreField_PropertyChanged;
            }

            PageHelper.CopyConfigToEdit(EditRadio, Config, EditPresets, EditMisc);
            SortEditPresets();
            
            foreach (RadioPresetItem item in EditPresets)
            {
                item.Tag = EditItemTag++;
                item.ModulationItems = PageHelper.RadioModulationItems(EditRadio, item.Frequency);
                item.ModulationIndex = ModulationIndexForModulation(item);

                item.ErrorsChanged += PreField_DataValidationError;
                item.PropertyChanged += PreField_PropertyChanged;
            }

            UpdateUIFromEditState();
        }

        /// <summary>
        /// marshall data between our local radio state and the appropriate preset set in the radio configuration.
        /// </summary>
        protected override void SaveEditStateToConfig()
        {
            if (!CurStateHasErrors() && !EditMisc.HasErrors)
            {
                foreach (RadioPresetItem item in EditPresets)
                    if (item.ModulationItems != null)
                    {
                        int index = item.ModulationIndex;
                        if ((index < 0) || (index >= item.ModulationItems.Count))
                            index = 0;
                        item.Modulation = (string)item.ModulationItems[index].Tag;
                    }

                PageHelper.CopyEditToConfig(EditRadio, EditPresets, EditMisc, Config);
                Config.Save(this, PageHelper.SystemTag);
                UpdateUIFromEditState();
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // field validation
        //
        // ------------------------------------------------------------------------------------------------------------

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
                    SetFieldValidVisualState(fieldPreset, ((List<string>)item.GetErrors("Preset")).Count == 0);
                TextBox fieldFreq = FindFieldForPresetItem(item, "Frequency");
                if (fieldFreq != null)
                    SetFieldValidVisualState(fieldFreq, ((List<string>)item.GetErrors("Frequency")).Count == 0);
            }
            SetFieldValidVisualState(uiMiscValueDefaultFreq,
                                     ((List<string>)EditMisc.GetErrors("DefaultTuning")).Count == 0);
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
                SetFieldValidVisualState(field, (errors.Count == 0));
            }
            UpdateUIFromEditState();
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
                SetFieldValidVisualState(uiMiscValueDefaultFreq, (errors.Count == 0));
            }
            UpdateUIFromEditState();
        }

        /// <summary>
        /// property changed: rebuild interface state to account for configuration changes.
        /// </summary>
        private void PreField_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "Preset")
            {
                // TODO: check for repeated preset numbers here also, or nah?
                SortEditPresets();
            }
            else if (args.PropertyName == "Frequency")
            {
                RadioPresetItem item = (RadioPresetItem)sender;
                item.ModulationItems = PageHelper.RadioModulationItems(EditRadio, item.Frequency);
                item.ModulationIndex = ModulationIndexForModulation(item);
            }
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                SaveEditStateToConfig();
            });
        }

        /// <summary>
        /// returns true if the current state has errors, false otherwise.
        /// </summary>
        private bool CurStateHasErrors()
        {
            foreach (RadioPresetItem item in EditPresets)
                if (item.HasErrors)
                    return true;
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
        private void AddNewPreset(int radio)
        {
            Int64 mask = 0;
            int newPreset = 1;
            int maxPreset = PageHelper.RadioMaxPresets(radio);
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

            RadioPresetItem newItem = new(PageHelper, EditItemTag++, radio);
            newItem.ErrorsChanged += PreField_DataValidationError;
            newItem.PropertyChanged += PreField_PropertyChanged;
            newItem.Preset = newPreset.ToString();
            newItem.Frequency = PageHelper.RadioDefaultFrequency(radio);
            newItem.ModulationItems = PageHelper.RadioModulationItems(radio, newItem.Frequency);
            EditPresets.Insert(newIndex, newItem);
        }

        /// <summary>
        /// sort the current list of presets by name in place in the presets list.
        /// </summary>
        private void SortEditPresets()
        {
            List<RadioPresetItem> sortableList = new(EditPresets);
            sortableList.Sort((a, b) => int.Parse(a.Preset).CompareTo(int.Parse(b.Preset)));
            EditPresets = new ObservableCollection<RadioPresetItem>();
            for (int i = 0; i < sortableList.Count; i++)
                EditPresets.Add(sortableList[i]);
        }

        /// <summary>
        /// change the selected radio and update various ui and model state.
        /// </summary>
        private void SelectRadio(int radio)
        {
            if (radio != EditRadio)
            {
                SaveEditStateToConfig();
                EditRadio = radio;
                CopyConfigToEditState();
            }
        }

        /// <summary>
        /// returns the preset item with the given tag, null if no such item is found.
        /// </summary>
        private RadioPresetItem FindPresetItemByTag(object tag)
        {
            foreach (RadioPresetItem item in EditPresets)
                if (item.Tag.Equals(tag))
                    return item;
            return null;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private static int ModulationIndexForModulation(RadioPresetItem item)
        {
            for (int i = 0; (item.ModulationItems != null) && (i < item.ModulationItems.Count); i++)
                if (item.Modulation == (string)item.ModulationItems[i].Tag)
                    return i;
            return 0;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private static bool IsModulationItemsEqual(ComboBox combo, RadioPresetItem item)
        {
            if ((combo.ItemsSource != null) &&
                (item.ModulationItems != null) &&
                (combo.Items.Count == item.ModulationItems.Count))
            {
                for (int i = 0; i < combo.Items.Count; i++)
                    if (((TextBlock)combo.Items[i]).Text != item.ModulationItems[i].Text)
                        return false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// update the "blue dot" state on the radio select menu to show the blue dot when the setup of the
        /// corresponding radio differs from defaults.
        /// </summary>
        private void RebuildRadioSelectMenu()
        {
            Utilities.SetBulletsInBulletComboBox(uiRadSelectCombo,
                                                 (int i) => !PageHelper.RadioModuleIsDefault(Config, i));
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
        /// rebuild the radio misc controls based on current setup.
        /// </summary>
        private void RebuildPerRadioMiscControls()
        {
            RebuildPerRadioMiscAuxControl(uiMiscCkbxAux1, uiMiscTextAux1, PageHelper.RadioAux1Title(EditRadio));
            RebuildPerRadioMiscAuxControl(uiMiscCkbxAux2, uiMiscTextAux2, PageHelper.RadioAux2Title(EditRadio));
            RebuildPerRadioMiscAuxControl(uiMiscCkbxAux3, uiMiscTextAux3, PageHelper.RadioAux3Title(EditRadio));

            if (string.IsNullOrEmpty(EditMisc.DefaultTuning) || EditMisc.HasErrors)
                uiMiscTextDefaultLabel.Text = "";
            else if (int.TryParse(EditMisc.DefaultTuning, out _))
                uiMiscTextDefaultLabel.Text = "Preset";
            else
                uiMiscTextDefaultLabel.Text = "MHz";
        }

        /// <summary>
        /// update the per-preset modulation comboboxen. this method does nothing if the current radio does not
        /// support per-preset modulation programming. depending on the state of the visual hierarchy, this
        /// may schedule work for down the road.
        /// </summary>
        private void RebuildPerPresetModulationSelections()
        {
            if (!PageHelper.RadioCanProgramModulation(EditRadio))
                return;

            // TODO: revisit this code, it's proving to be fragile and the hack is maybe no longer a great idea.

            // HACK: this shite seems to be necessary due to a gnarly interaction between Move and bindings.
            // HACK: were life fair, we'd manage the modulation combo items and selections through bindings.
            // HACK: tried that, it crashed.
            //
            foreach (RadioPresetItem item in EditPresets)
            {
                ComboBox combo = FindComboForPresetItem(item);
                if ((combo != null) && (item.ModulationItems == null))
                {
                    combo.ItemsSource = null;
                }
                else if (combo != null)
                {
                    if (!IsModulationItemsEqual(combo, item))
                    {
                        combo.ItemsSource = item.ModulationItems;
                        combo.SelectionChanged -= PreListModCombo_SelectionChanged;
                        combo.SelectedIndex = item.ModulationIndex;
                        combo.SelectionChanged += PreListModCombo_SelectionChanged;
                    }
                    else if (combo.SelectedIndex != item.ModulationIndex)
                    {
                        combo.SelectedIndex = item.ModulationIndex;
                    }
                }
                else if (combo == null)
                {
                    // list view's view hierarchy hasn't been built out yet, revisit this setup later once the
                    // hierarchy is built (which it will be, eventually). we'll wait a jiffy before trying again to
                    // avoid pestering the framework too much: "ARE WE THERE YET?!?".
                    //
                    //DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
                    //{
                    //    await Task.Delay(250);
                    //    RebuildPerPresetModulationSelections();
                    //});
                    //break;
                }
            }
        }

        /// <summary>
        /// update the enable state on the ui elements based on the current settings. link controls must be set up via
        /// RebuildLinkControls() prior to calling this function.
        /// </summary>
        protected override void UpdateUICustom(bool isEditable)
        {
            uiPreListView.ItemsSource = EditPresets;

            RebuildRadioSelectMenu();
            RebuildPerRadioMiscControls();
            RebuildPerPresetModulationSelections();

            if ((EditPresets.Count > 0) && (EditPresets[0].IsEnabled != isEditable))
                foreach (RadioPresetItem item in EditPresets)
                    item.IsEnabled = isEditable;

            Utilities.SetEnableState(uiBarAdd, isEditable && (EditPresets.Count < PageHelper.RadioMaxPresets(EditRadio)));
            Utilities.SetEnableState(uiBarImport, isEditable);
            Utilities.SetEnableState(uiBarExport, isEditable && (EditPresets.Count > 0));

            Utilities.SetEnableState(uiMiscValueDefaultFreq, isEditable);
            Utilities.SetEnableState(uiMiscCkbxAux1, isEditable);
            Utilities.SetEnableState(uiMiscCkbxAux2, isEditable);
            Utilities.SetEnableState(uiMiscCkbxAux3, isEditable);

            bool isNoErrs = !CurStateHasErrors();
            Utilities.SetEnableState(uiRadSelectCombo, isNoErrs);
            Utilities.SetEnableState(uiRadNextBtn, (isNoErrs && (EditRadio < (PageHelper.RadioNames.Count - 1))));
            Utilities.SetEnableState(uiRadPrevBtn, (isNoErrs && (EditRadio > 0)));
        }

        protected override void ResetConfigToDefault()
        {
            PageHelper.RadioSysReset(Config);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- commands ----------------------------------------------------------------------------------------------

        /// <summary>
        /// add preset click: add a new preset to the current radio and update the configuration.
        /// </summary>
        private void CmdAdd_Click(object sender, RoutedEventArgs args)
        {
            // TODO: scroll to visible after adding new preset?
            AddNewPreset(EditRadio);
            SaveEditStateToConfig();
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

            uiRadSelectCombo.SelectionChanged -= RadSelectCombo_SelectionChanged;
            uiRadSelectCombo.SelectedIndex = EditRadio;
            uiRadSelectCombo.SelectionChanged += RadSelectCombo_SelectionChanged;

            uiPreListView.ItemsSource = EditPresets;
        }

        /// <summary>
        /// next radio button click: advance to the next radio.
        /// </summary>
        private void RadBtnNext_Click(object sender, RoutedEventArgs args)
        {
            SelectRadio(EditRadio + 1);

            uiRadSelectCombo.SelectionChanged -= RadSelectCombo_SelectionChanged;
            uiRadSelectCombo.SelectedIndex = EditRadio;
            uiRadSelectCombo.SelectionChanged += RadSelectCombo_SelectionChanged;

            uiPreListView.ItemsSource = EditPresets;
        }

        /// <summary>
        /// radio select combo click: switch to the selected radio. the tag of the sender (a TextBlock) gives us the
        /// radio number to select.
        /// </summary>
        private void RadSelectCombo_SelectionChanged(object sender, RoutedEventArgs args)
        {
            Grid item = (Grid)((ComboBox)sender).SelectedItem;
            if ((item != null) && (item.Tag != null))
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
            SaveEditStateToConfig();
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
            SaveEditStateToConfig();
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
            SaveEditStateToConfig();
        }

        // ---- preset list -------------------------------------------------------------------------------------------

        /// <summary>
        /// preset delete button click: delete the preset associated with the row and update the configuration.
        /// </summary>
        private void PreListBtnDelete_Click(object sender, RoutedEventArgs args)
        {
            Button btn = (Button)sender;
            RadioPresetItem item = FindPresetItemByTag(btn.Tag);
            if (item != null)
            {
                EditPresets.Remove(item);
                SaveEditStateToConfig();
            }
        }

        /// <summary>
        /// preset modulation combo selection changed: update the edit state and update the configuration.
        /// </summary>
        private void PreListModCombo_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            ComboBox cbox = (ComboBox)sender;
            RadioPresetItem item = FindPresetItemByTag(((Grid)cbox.Parent).Tag);
            if ((item != null) && (cbox.SelectedIndex != -1))
            {
                item.ModulationIndex = cbox.SelectedIndex;
                SaveEditStateToConfig();
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
        /// 
        /// we do not use page caching here as we're just tracking the configuration state.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            // HACK: fixes circular dependency where base.OnNavigatedTo needs PageHelper, but PageHelper needs
            // NavArgs which are built inside base.OnNavigatedTo.
            ConfigEditorPageNavArgs navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            PageHelper = (IEditRadioPageHelper)Activator.CreateInstance(navArgs.EditorHelperType);

            EditMisc = new RadioMiscItem(PageHelper, 0);
            EditMisc.ErrorsChanged += MiscField_DataValidationError;

            base.OnNavigatedTo(args);

            List<FrameworkElement> items = new();
            for (int i = 0; i < PageHelper.RadioNames.Count; i++)
                    items.Add(Utilities.BulletComboBoxItem(PageHelper.RadioNames[i], i.ToString()));
            uiRadSelectCombo.ItemsSource = items;

            CopyConfigToEditState();

            uiRadSelectCombo.SelectedIndex = EditRadio;
        }
    }
}
