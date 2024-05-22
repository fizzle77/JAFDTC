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
using System.Linq.Expressions;
using System.Reflection;

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

        private void CopyAllSettings(HMCSProfileSettings editStateProfileSettings, SettingLocation source, SettingLocation destination)
        {
            if (source == destination)
                throw new ArgumentException("source and destination cannot be equal");
            
            // ComboBox settings may be in the top-level HMCS config or the individual
            // profiles settings, so we check for each one.
            foreach (var kv in GetPageComboBoxes())
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
            foreach (string propertyName in GetPageTextBoxes().Keys)
                CopyProperty(propertyName, profileSrc, profileDest);
        }

        private void CopyProperty(string propertyName, object source, object dest)
        {
            PropertyInfo propInfo;
            if (source is HMCSSystem)
                propInfo = typeof(HMCSSystem).GetProperty(propertyName);
            else
                propInfo = typeof(HMCSProfileSettings).GetProperty(propertyName);
            propInfo.SetValue(dest, propInfo.GetValue(source));
        }

        private bool IsProfileSelectionValid(out HMCSProfileSettings selectedProfileEditState)
        {
            Grid g = (Grid)uiComboProfile.SelectedItem;
            if (g == null)
                selectedProfileEditState = null;
            else
                selectedProfileEditState = _editState.GetProfileSettings((string)g.Tag);
            return selectedProfileEditState != null;
        }

        /// <summary>
        /// Returns all the TextBox controls on this page, indexed by corresponding property name.
        /// </summary>
        private Dictionary<string, TextBox> GetPageTextBoxes()
        {
            if (_pageTextBoxes == null || _pageTextBoxes.Count == 0)
            {
                var allTextBoxes = new List<TextBox>();
                Utilities.FindChildren(allTextBoxes, this);

                _pageTextBoxes = new Dictionary<string, TextBox>(allTextBoxes.Count);
                foreach ( var textBox in allTextBoxes )
                {
                    if (textBox.Tag != null)
                    _pageTextBoxes[textBox.Tag.ToString()] = textBox;
                }
            }
            return _pageTextBoxes;
        }
        private Dictionary<string, TextBox> _pageTextBoxes;

        /// <summary>
        /// Returns all the ComboBox controls on this page, indexed by corresponding property name.
        /// </summary>
        private Dictionary<string, ComboBox> GetPageComboBoxes()
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
        private Dictionary<string, ComboBox> _pageComboBoxes;

        private void UpdateUIFromEditState()
        {
            if (_isUIUpdatePending)
                return;
            _isUIUpdatePending = true;
            
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                bool isNotLinked = string.IsNullOrEmpty(_config.SystemLinkedTo(HMCSSystem.SystemTag));

                foreach (var comboBox in GetPageComboBoxes().Values)
                {
                    GetControlEditStateProperty(comboBox, out PropertyInfo property, out BindableObject editState);
                    if (editState != null)
                        Utilities.SetComboEnabledAndSelection(comboBox, isNotLinked, true, int.Parse(property.GetValue(editState).ToString()));
                }

                foreach (var textBox in GetPageTextBoxes().Values)
                {
                    GetControlEditStateProperty(textBox, out PropertyInfo property, out BindableObject editState);
                    if (editState != null)
                        Utilities.SetTextBoxEnabledAndText(textBox, isNotLinked, true, property.GetValue(editState).ToString());
                }

                uiProfile1SelectIcon.Visibility = Utilities.HiddenIfDefault(_config.HMCS.GetProfileSettings(Profiles.PRO1));
                uiProfile2SelectIcon.Visibility = Utilities.HiddenIfDefault(_config.HMCS.GetProfileSettings(Profiles.PRO2));
                uiProfile3SelectIcon.Visibility = Utilities.HiddenIfDefault(_config.HMCS.GetProfileSettings(Profiles.PRO3));

                UpdateLinkControls();

                _isUIUpdatePending = false;
            });
        }

        private void GetControlEditStateProperty(FrameworkElement control, out PropertyInfo property, out BindableObject editState)
        {
            string propName = control.Tag.ToString();

            property = typeof(HMCSSystem).GetProperty(propName);
            editState = _editState;
            if (property == null)
            {
                property = typeof(HMCSProfileSettings).GetProperty(propName);
                if (IsProfileSelectionValid(out HMCSProfileSettings profileSettingsEditState))
                    editState = profileSettingsEditState;
                else
                    editState = null;
            }
        }

        private void GetControlConfigProperty(FrameworkElement control, out PropertyInfo property, out BindableObject config)
        {
            string propName = control.Tag.ToString();

            property = typeof(HMCSSystem).GetProperty(propName);
            config = _config.HMCS;
            if (property == null)
            {
                property = typeof(HMCSProfileSettings).GetProperty(propName);
                if (IsProfileSelectionValid(out HMCSProfileSettings profileSettings))
                    config = _config.HMCS.GetProfileSettings(profileSettings.Profile);
                else
                    config = null;
            }
        }

        // ---- control event handlers --------------------------------------------------------------------------------

        private void uiComboProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                HMCSProfileSettings oldSettings = _editState.GetProfileSettings((string)((Grid)e.RemovedItems[0]).Tag);
                if (oldSettings != null)
                {
                    oldSettings.ErrorsChanged -= BaseField_DataValidationError;
                    oldSettings.PropertyChanged -= BaseField_PropertyChanged;
                    
                    // BindableObject remembers errors but not the value that caused them. So changing the profile
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

            switch (uiComboProfile.SelectedIndex)
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
            uiComboProfile.SelectedIndex++;
        }

        private void uiButtonPrev_Click(object sender, RoutedEventArgs e)
        {
            uiComboProfile.SelectedIndex--;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            GetControlEditStateProperty(comboBox, out PropertyInfo property, out BindableObject editState);

            if (property != null && editState != null)
                property.SetValue(editState, comboBox.SelectedIndex.ToString());

            SaveEditStateToConfig();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            GetControlEditStateProperty(textBox, out PropertyInfo property, out BindableObject editState);

            if (property != null && editState != null)
                property.SetValue(editState, textBox.Text);

            SaveEditStateToConfig();
        }

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
            if (GetPageTextBoxes().ContainsKey(propertyName))
                SetFieldValidVisualState(GetPageTextBoxes()[propertyName], (errors.Count == 0));
        }

        private void ValidateAllFields()
        {
            foreach (var kv in GetPageTextBoxes())
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

        // ---- page settings -----------------------------------------------------------------------------------------

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
            if (uiComboProfile.SelectedIndex < 0)
                uiComboProfile.SelectedIndex = 0;
        }

        private void UpdateLinkControls()
        {
            Utilities.RebuildLinkControls(_config, HMCSSystem.SystemTag, _navArgs.UIDtoConfigMap, uiPageBtnTxtLink, uiPageTxtLink);
        }
    }
}
