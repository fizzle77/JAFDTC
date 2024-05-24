using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.HMCS;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Windows.Networking.NetworkOperators;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// Code-behind class for the A10 HMCS editor.
    /// </summary>
    public sealed partial class A10CEditHMCSPage : Page
    {
        private enum SettingLocation
        {
            Edit,
            Config
        }

        private ConfigEditorPageNavArgs _navArgs;
        private A10CConfiguration _config;

        // For configuration linking UI.
        private readonly Dictionary<string, string> _configNameToUID= new Dictionary<string, string>();
        private readonly List<string> _configNameList = new List<string>();

        private bool _isUIUpdatePending = false;
        private HMCSSystem _editState;

        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        public static ConfigEditorPageInfo PageInfo
            => new(HMCSSystem.SystemTag, "HMCS", "HMCS", Glyphs.HMCS, typeof(A10CEditHMCSPage));

        public A10CEditHMCSPage()
        {
            InitializeComponent();

            _editState = new HMCSSystem();
            _editState.ErrorsChanged += BaseField_DataValidationError;
            _editState.PropertyChanged += BaseField_PropertyChanged;

            _defaultBorderBrush = uiTextFlightMembers.BorderBrush;
            _defaultBkgndBrush = uiTextFlightMembers.Background;
        }

        // ---- UI helpers  -------------------------------------------------------------------------------------------

        private void SaveEditStateToConfig()
        {
            if (IsProfileSelectionValid(out HMCSProfileSettings profileEditState))
                SaveEditStateToConfig(profileEditState);
        }

        private void SaveEditStateToConfig(HMCSProfileSettings profileEditState)
        {
            if (_editState.HasErrors || profileEditState.HasErrors)
                return;

            CopyAllSettings(profileEditState, SettingLocation.Edit, SettingLocation.Config);
            _config.Save(this, HMCSSystem.SystemTag);
        }

        private void CopyConfigToEditState()
        {
            if (IsProfileSelectionValid(out HMCSProfileSettings selectedProfileSettings))
            {
                _isUIUpdatePending = true; // prevent events from causing spurious UI updates
                CopyConfigToEditState(selectedProfileSettings);
                _isUIUpdatePending = false;

                UpdateUIFromEditState();
            }
        }

        private void CopyConfigToEditState(HMCSProfileSettings editStateProfileSettings)
        {
            CopyAllSettings(editStateProfileSettings, SettingLocation.Config, SettingLocation.Edit);
        }


        /// <summary>
        /// Iterate over all the settings controls via PageComboBoxes and PageTextBoxes.
        /// For each control, get the corresponding property and copy its value from source to destination.
        /// </summary>
        /// <param name="editStateProfileSettings">The currently select profile's edit state.</param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <exception cref="ArgumentException"></exception>
        private void CopyAllSettings(HMCSProfileSettings editStateProfileSettings, SettingLocation source, SettingLocation destination)
        {
            if (source == destination)
                throw new ArgumentException("source and destination cannot be equal");
            
            // ComboBox settings may be in the top-level HMCS config or the individual
            // profiles settings, so we check for each one.
            foreach (var kv in PageComboBoxes)
            {
                string propName = kv.Key;
                ComboBox comboBox = kv.Value;

                GetControlEditStateProperty(comboBox, out PropertyInfo _, out BindableObject editState);
                GetControlConfigProperty(comboBox, out PropertyInfo _, out BindableObject config);
                if (editState != null && config != null)
                {
                    if (source == SettingLocation.Edit)
                        CopyProperty(propName, editState, config);
                    else
                        CopyProperty(propName, config, editState);
                }
            }

            // TextBox settings are all in the profile settings, so we need not lookup each one.

            BindableObject profileSrc;  // The profile setting source being copied.
            BindableObject profileDest; // The profile setting destination for the copy.

            if (source == SettingLocation.Edit)
            {
                profileSrc = editStateProfileSettings;
                profileDest = _config.HMCS.GetProfileSettings(editStateProfileSettings.Profile);
            }
            else
            {
                profileSrc = _config.HMCS.GetProfileSettings(editStateProfileSettings.Profile);
                profileDest = editStateProfileSettings;
            }
            foreach (string propertyName in PageTextBoxes.Keys)
                CopyProperty(propertyName, profileSrc, profileDest);
        }

        /// <summary>
        /// Find propertyName on source and copy its value to dest.
        /// </summary>
        private void CopyProperty(string propertyName, BindableObject source, BindableObject dest)
        {
            PropertyInfo propInfo;
            if (source is HMCSSystem)
                propInfo = typeof(HMCSSystem).GetProperty(propertyName);
            else
                propInfo = typeof(HMCSProfileSettings).GetProperty(propertyName);
            propInfo.SetValue(dest, propInfo.GetValue(source));
        }

        /// <summary>
        /// If there is a valid selection for the current profile to edit, set selectedProfileEditState and return true.
        /// </summary>
        private bool IsProfileSelectionValid(out HMCSProfileSettings selectedProfileEditState)
        {
            Grid g = (Grid)uiComboEditProfile.SelectedItem;
            if (g == null)
                selectedProfileEditState = null;
            else
                selectedProfileEditState = _editState.GetProfileSettings((string)g.Tag);
            return selectedProfileEditState != null;
        }

        /// <summary>
        /// Lazy load and return all the TextBox controls on this page having a Tag set.
        /// By convention, the tags are all property names. Keys in the returned dictionary
        /// are property names.
        /// </summary>
        private Dictionary<string, TextBox> PageTextBoxes
        {
            get
            {
                if (_pageTextBoxes == null || _pageTextBoxes.Count == 0)
                {
                    var allTextBoxes = new List<TextBox>();
                    Utilities.FindChildren(allTextBoxes, this);

                    _pageTextBoxes = new Dictionary<string, TextBox>(allTextBoxes.Count);
                    foreach (var textBox in allTextBoxes)
                    {
                        if (textBox.Tag != null)
                            _pageTextBoxes[textBox.Tag.ToString()] = textBox;
                    }
                }
                return _pageTextBoxes;
            }
        }
        private Dictionary<string, TextBox> _pageTextBoxes;

        /// <summary>
        /// Lazy load and return all the ComboBox controls on this page having a Tag set.
        /// By convention, the tags are all property names. Keys in the returned dictionary
        /// are property names.
        /// </summary>
        private Dictionary<string, ComboBox> PageComboBoxes
        {
            get
            {
                if (_pageComboBoxes == null || _pageComboBoxes.Count == 0)
                {
                    var allComboBoxes = new List<ComboBox>();
                    Utilities.FindChildren(allComboBoxes, this);

                    _pageComboBoxes = new Dictionary<string, ComboBox>(allComboBoxes.Count);
                    foreach (var comboBox in allComboBoxes)
                    {
                        if (comboBox.Tag != null)
                            _pageComboBoxes[comboBox.Tag.ToString()] = comboBox;
                    }
                }
                return _pageComboBoxes;
            }
        }
        private Dictionary<string, ComboBox> _pageComboBoxes;

        private void UpdateUIFromEditState()
        {
            if (_isUIUpdatePending)
                return;
            _isUIUpdatePending = true;
            
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                bool isNotLinked = string.IsNullOrEmpty(_config.SystemLinkedTo(HMCSSystem.SystemTag));

                foreach (var comboBox in PageComboBoxes.Values)
                {
                    GetControlEditStateProperty(comboBox, out PropertyInfo property, out BindableObject editState);
                    if (editState != null)
                        Utilities.SetComboEnabledAndSelection(comboBox, isNotLinked, true, int.Parse(property.GetValue(editState).ToString()));
                }

                foreach (var textBox in PageTextBoxes.Values)
                {
                    GetControlEditStateProperty(textBox, out PropertyInfo property, out BindableObject editState);
                    if (editState != null)
                        Utilities.SetTextBoxEnabledAndText(textBox, isNotLinked, true, property.GetValue(editState).ToString());
                }

                uiProfile1SelectIcon.Visibility = Utilities.HiddenIfDefault(_config.HMCS.GetProfileSettings(Profiles.PRO1));
                uiProfile2SelectIcon.Visibility = Utilities.HiddenIfDefault(_config.HMCS.GetProfileSettings(Profiles.PRO2));
                uiProfile3SelectIcon.Visibility = Utilities.HiddenIfDefault(_config.HMCS.GetProfileSettings(Profiles.PRO3));

                uiPageBtnReset.IsEnabled = !_editState.IsDefault;
                
                UpdateLinkControls();

                _isUIUpdatePending = false;
            });
        }

        /// <summary>
        /// Find and return the property on the edit state object corresponding to the provided control.
        /// </summary>
        private void GetControlEditStateProperty(FrameworkElement control, out PropertyInfo property, out BindableObject editState)
        {
            GetControlPropertyHelper(SettingLocation.Edit, control, out property, out editState);
        }

        /// <summary>
        /// Find and return the property on the persisted configuration object corresponding to the provided control.
        /// </summary>
        private void GetControlConfigProperty(FrameworkElement control, out PropertyInfo property, out BindableObject config)
        {
            GetControlPropertyHelper(SettingLocation.Config, control, out property, out config);
        }

        private void GetControlPropertyHelper(SettingLocation settingLocation, FrameworkElement control, 
            out PropertyInfo property, out BindableObject configOrEdit)
        {
            string propName = control.Tag.ToString();

            property = typeof(HMCSSystem).GetProperty(propName);
            if (settingLocation == SettingLocation.Edit)
                configOrEdit = _editState;
            else
                configOrEdit = _config.HMCS;
            if (property == null)
            {
                property = typeof(HMCSProfileSettings).GetProperty(propName);
                if (IsProfileSelectionValid(out HMCSProfileSettings profileSettingsEditState))
                {
                    if (settingLocation == SettingLocation.Edit)
                        configOrEdit = profileSettingsEditState;
                    else
                        configOrEdit = _config.HMCS.GetProfileSettings(profileSettingsEditState.Profile);
                }
                else
                    configOrEdit = null;
            }
        }

        // ---- control event handlers --------------------------------------------------------------------------------

        private void uiComboEditProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                HMCSProfileSettings oldSettings = _editState.GetProfileSettings((string)((Grid)e.RemovedItems[0]).Tag);
                if (oldSettings != null)
                {
                    oldSettings.ErrorsChanged -= BaseField_DataValidationError;
                    oldSettings.PropertyChanged -= BaseField_PropertyChanged;
                    
                    // BindableObject remembers errors but not the value that caused them. So changing this
                    // back to a profile with an error, you get a red box on a good value. Clearing the error
                    // state is far from ideal, but slightly less mysterious user behavior.
                    oldSettings.ClearErrors();
                }
            }

            if (e.AddedItems.Count > 0)
            {
                HMCSProfileSettings newSettings = _editState.GetProfileSettings((string)((Grid)e.AddedItems[0]).Tag);
                if (newSettings != null)
                {
                    newSettings.ErrorsChanged += BaseField_DataValidationError;
                    newSettings.PropertyChanged += BaseField_PropertyChanged;

                    CopyConfigToEditState(newSettings);
                    UpdateUIFromEditState();
                    ValidateAllFields();
                }
            }

            switch (uiComboEditProfile.SelectedIndex)
            {
                case 0:
                    uiButtonPrev.IsEnabled = false;
                    uiButtonNext.IsEnabled = true;
                    break;
                default:
                    uiButtonPrev.IsEnabled = true;
                    uiButtonNext.IsEnabled = true;
                    break;
                case 2:
                    uiButtonPrev.IsEnabled = true;
                    uiButtonNext.IsEnabled = false;
                    break;
            }
        }

        private void uiButtonNext_Click(object sender, RoutedEventArgs e)
        {
            uiComboEditProfile.SelectedIndex++;
        }

        private void uiButtonPrev_Click(object sender, RoutedEventArgs e)
        {
            uiComboEditProfile.SelectedIndex--;
        }

        /// <summary>
        /// Change handler for all of the ComboBoxes that manage a setting.
        /// Sets the corresponding edit state property value and updates the underlying config.
        /// </summary>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            GetControlEditStateProperty(comboBox, out PropertyInfo property, out BindableObject editState);

            if (property != null && editState != null)
                property.SetValue(editState, comboBox.SelectedIndex.ToString());

            SaveEditStateToConfig();
        }

        /// <summary>
        /// Change handler for all of the TextBoxes that manage a setting.
        /// Sets the corresponding edit state property value and updates the underlying config.
        /// </summary>
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            GetControlEditStateProperty(textBox, out PropertyInfo property, out BindableObject editState);

            if (property != null && editState != null)
                property.SetValue(editState, textBox.Text);

            SaveEditStateToConfig();
        }

        /// <summary>
        /// A UX nicety: highlight the whole value in a TextBox when it
        /// gets focus such that the new value can immediately be entered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            textBox.SelectAll();
        }

        // property changed: rebuild interface state to account for configuration changes.
        //
        private void BaseField_PropertyChanged(object sender, EventArgs args)
        {
            UpdateUIFromEditState();
        }

        // ---- field validation -------------------------------------------------------------------------------------------

        private void SetFieldValidVisualState(TextBox field, bool isValid)
        {
            field.BorderBrush = (isValid) ? _defaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
            field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBackgroundBrush"];
        }

        private void ValidateEditState(BindableObject editState, string propertyName)
        {
            List<string> errors = (List<string>)editState.GetErrors(propertyName);
            if (PageTextBoxes.ContainsKey(propertyName))
                SetFieldValidVisualState(PageTextBoxes[propertyName], (errors.Count == 0));
        }

        private void ValidateAllFields()
        {
            foreach (var kv in PageTextBoxes)
            {
                string propName = kv.Key;
                TextBox textBox = kv.Value;

                GetControlEditStateProperty(textBox, out PropertyInfo _, out BindableObject editState);
                if (editState != null)
                    ValidateEditState(editState, propName);
            }
        }

        private void BaseField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            ValidateEditState(_editState, args.PropertyName);
            if (IsProfileSelectionValid(out HMCSProfileSettings selectedProfileEditState))
                ValidateEditState(selectedProfileEditState, args.PropertyName);
        }

        // ---- page events -------------------------------------------------------------------------------------------

        // on clicks of the reset all button, reset all settings back to default.
        //
        private async void PageBtnReset_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configuration?",
                "Are you sure you want to reset the HMCS configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                _config.UnlinkSystem(HMCSSystem.SystemTag);
                UpdateLinkControls();
                _config.HMCS.Reset();
                _config.Save(this, HMCSSystem.SystemTag);
                CopyConfigToEditState();
            }
        }

        private async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, _config, HMCSSystem.SystemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                _config.UnlinkSystem(HMCSSystem.SystemTag);
                _config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                _config.LinkSystemTo(HMCSSystem.SystemTag, _navArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                _config.Save(this);
            }

            CopyConfigToEditState();
            UpdateLinkControls();
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            _navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            _config = (A10CConfiguration)_navArgs.Config;
            
            Utilities.BuildSystemLinkLists(_navArgs.UIDtoConfigMap, _config.UID, HMCSSystem.SystemTag, _configNameList, _configNameToUID);
            UpdateLinkControls();

            base.OnNavigatedTo(args);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // This needs to be done here, not OnNavigatedTo, so that the visual tree is fully built
            // such that PageComboBoxes and PageTextBoxes finds things.
            if (uiComboEditProfile.SelectedIndex < 0)
                uiComboEditProfile.SelectedIndex = 0;

            // Leaving this here as a breadcrumb...
            //
            // The default ComboBox width behavior is so bad. Setting width to Auto has them resize based on the current selection, 
            // resulting in "wiggle" of surrounding elements. Setting a fixed size resolves this, but if people have increased their
            // default font size, or if you're running on Win 10 vs. 11, there isn't a single fixed size that's appropriate.
            // This seems like it should work, but generally gives a width that's ~20% too big and text inside the ComboBox gets
            // aligned to the far left and looks bad. I wonder if it needs to be done at a different time during load or layout?
            // Unsure and I've spent way too much time on this. *SHRUG*

            //foreach (ComboBox comboBox in GetPageComboBoxes().Values)
            //{
            //    double width = 0;
            //    foreach (ComboBoxItem item in comboBox.Items)
            //    {
            //        item.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            //        if (item.DesiredSize.Width > width)
            //            width = item.DesiredSize.Width;
            //    }
            //    comboBox.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            //    comboBox.Width = comboBox.DesiredSize.Width + width;
            //}
        }

        private void UpdateLinkControls()
        {
            Utilities.RebuildLinkControls(_config, HMCSSystem.SystemTag, _navArgs.UIDtoConfigMap, uiPageBtnTxtLink, uiPageTxtLink);
        }
    }
}
