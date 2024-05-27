using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.HMCS;
using JAFDTC.Models.A10C.TGP;
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
    public abstract class A10CPageBase : Page
    {
        protected enum SettingLocation
        {
            Edit,
            Config
        }

        protected ConfigEditorPageNavArgs _navArgs;
        protected A10CConfiguration _config;
        protected A10CSystemBase _editState;

        protected Brush _defaultBorderBrush;
        protected Brush _defaultBkgndBrush;

        protected bool _isUIUpdatePending = false;

        private TextBlock _uiPageBtnTxtLink;
        private TextBlock _uiPageTxtLink;
        private Button _uiPageBtnReset;

        // For configuration linking UI.
        private readonly Dictionary<string, string> _configNameToUID = new Dictionary<string, string>();
        private readonly List<string> _configNameList = new List<string>();
        private readonly string _systemName;

        private readonly string _systemTag;

        protected abstract A10CSystemBase SystemConfig { get; }


        public A10CPageBase(string systemName, string systemTag) 
        {
            _systemName = systemName;
            _systemTag = systemTag;
        }

        protected void InitializeBase(
            A10CSystemBase editState,
            TextBox setDefaultsFrom, 
            TextBlock uiPageBtnTxtLink, 
            TextBlock uiPageTxtLink,
            Button uiPageBtnReset)
        {
            _editState = editState;
            _editState.ErrorsChanged += BaseField_DataValidationError;
            _editState.PropertyChanged += BaseField_PropertyChanged;

            _defaultBorderBrush = setDefaultsFrom.BorderBrush;
            _defaultBkgndBrush = setDefaultsFrom.Background;

            _uiPageBtnTxtLink = uiPageBtnTxtLink;
            _uiPageTxtLink = uiPageTxtLink;
            _uiPageBtnReset = uiPageBtnReset;
        }

        /// <summary>
        /// Lazy load and return all the TextBox controls on this page having a Tag set.
        /// By convention, the tags are all property names. Keys in the returned dictionary
        /// are property names.
        /// </summary>
        protected Dictionary<string, TextBox> PageTextBoxes
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
        protected Dictionary<string, ComboBox> PageComboBoxes
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

        /// <summary>
        /// Lazy load and return all the CheckBox controls on this page having a Tag set.
        /// By convention, the tags are all property names. Keys in the returned dictionary
        /// are property names.
        /// </summary>
        protected Dictionary<string, CheckBox> PageCheckBoxes
        {
            get
            {
                if (_pageCheckBoxes == null || _pageCheckBoxes.Count == 0)
                {
                    var allCheckBoxes = new List<CheckBox>();
                    Utilities.FindChildren(allCheckBoxes, this);

                    _pageCheckBoxes = new Dictionary<string, CheckBox>(allCheckBoxes.Count);
                    foreach (var checkBox in allCheckBoxes)
                    {
                        if (checkBox.Tag != null)
                            _pageCheckBoxes[checkBox.Tag.ToString()] = checkBox;
                    }
                }
                return _pageCheckBoxes;
            }
        }
        private Dictionary<string, CheckBox> _pageCheckBoxes;

        /// <summary>
        /// Find and return the property on the edit state object corresponding to the provided control.
        /// </summary>
        protected void GetControlEditStateProperty(FrameworkElement control, out PropertyInfo property, out BindableObject editState)
        {
            GetControlPropertyHelper(SettingLocation.Edit, control, out property, out editState);
        }

        /// <summary>
        /// Find and return the property on the persisted configuration object corresponding to the provided control.
        /// </summary>
        protected void GetControlConfigProperty(FrameworkElement control, out PropertyInfo property, out BindableObject config)
        {
            GetControlPropertyHelper(SettingLocation.Config, control, out property, out config);
        }

        /// <summary>
        /// Override if not every tagged control on the page is backed by the page's common _editState.
        /// </summary>
        /// <param name="settingLocation"></param>
        /// <param name="control"></param>
        /// <param name="property"></param>
        /// <param name="configOrEdit"></param>
        protected virtual void GetControlPropertyHelper(
            SettingLocation settingLocation, 
            FrameworkElement control,
            out PropertyInfo property, 
            out BindableObject configOrEdit)
        {
            string propName = control.Tag.ToString();

            property = _editState.GetType().GetProperty(propName);
            if (settingLocation == SettingLocation.Edit)
                configOrEdit = _editState;
            else
                configOrEdit = SystemConfig;
        }

        protected virtual void CopyConfigToEditState()
        {
            CopyAllSettings(SystemConfig, _editState);
            UpdateUIFromEditState();
        }

        protected virtual void SaveEditStateToConfig()
        {
            if (_editState.HasErrors)
                return;

            CopyAllSettings(_editState, SystemConfig);
            _config.Save(this, _systemTag);
        }

        /// <summary>
        /// Iterate over all the settings controls via PageTextBoxes, PageComboBoxes, and PageCheckBoxes.
        /// For each control, get the corresponding property and copy its value from source to destination.
        /// </summary>
        protected virtual void CopyAllSettings(BindableObject source, BindableObject destination)
        {
            if (source == destination)
                throw new ArgumentException("source and destination cannot be equal");

            foreach (string propertyName in PageTextBoxes.Keys)
                CopyProperty(propertyName, source, destination);
            foreach (string propertyName in PageComboBoxes.Keys)
                CopyProperty(propertyName, source, destination);
            foreach (string propertyName in PageCheckBoxes.Keys)
                CopyProperty(propertyName, source, destination);
        }

        /// <summary>
        /// Find propertyName on source and copy its value to dest.
        /// </summary>
        protected void CopyProperty(string propertyName, BindableObject source, BindableObject dest)
        {
            PropertyInfo propInfo = source.GetType().GetProperty(propertyName);
            propInfo.SetValue(dest, propInfo.GetValue(source));
        }

        /// <summary>
        /// Iterate over all the settings controls via PageTextBoxes, PageComboBoxes, and PageCheckBoxes.
        /// Set the value for each control based on the corresponding _editState property.
        /// </summary>
        protected void UpdateUIFromEditState()
        {
            if (_isUIUpdatePending)
                return;
            _isUIUpdatePending = true;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                bool isNotLinked = string.IsNullOrEmpty(_config.SystemLinkedTo(_systemTag));

                foreach (KeyValuePair<string, TextBox> kv in PageTextBoxes)
                {
                    GetControlEditStateProperty(kv.Value, out PropertyInfo property, out BindableObject editState);
                    if (property == null) throw new ApplicationException("Unexpected TextBox: " + kv.Key);
                    Utilities.SetTextBoxEnabledAndText(kv.Value, isNotLinked, true, 
                        (string)property.GetValue(editState));
                }

                foreach (KeyValuePair<string, ComboBox> kv in PageComboBoxes)
                {
                    GetControlEditStateProperty(kv.Value, out PropertyInfo property, out BindableObject editState);
                    if (property == null) throw new ApplicationException("Unexpected ComboBox: " + kv.Key);
                    Utilities.SetComboEnabledAndSelection(kv.Value, isNotLinked, true, 
                        int.Parse((string)property.GetValue(editState)));
                }

                foreach (KeyValuePair<string, CheckBox> kv in PageCheckBoxes)
                {
                    GetControlEditStateProperty(kv.Value, out PropertyInfo property, out BindableObject editState);
                    if (property == null) throw new ApplicationException("Unexpected ComboBox: " + kv.Key);
                    Utilities.SetCheckEnabledAndState(kv.Value, isNotLinked,
                        bool.Parse((string)property.GetValue(editState)));
                }

                _uiPageBtnReset.IsEnabled = !_editState.IsDefault;

                UpdateUICustom();

                _isUIUpdatePending = false;
            });
        }

        /// <summary>
        /// Override to perform custom UI update steps inside the queued dispatcher delegate.
        /// </summary>
        protected virtual void UpdateUICustom() { }

        // ---- control event handlers --------------------------------------------------------------------------------

        /// <summary>
        /// Change handler for all of the ComboBoxes that manage a setting.
        /// Sets the corresponding edit state property value and updates the underlying config.
        /// </summary>
        protected void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs _)
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
        protected void TextBox_LostFocus(object sender, RoutedEventArgs _)
        {
            TextBox textBox = (TextBox)sender;
            GetControlEditStateProperty(textBox, out PropertyInfo property, out BindableObject editState);

            if (property != null && editState != null)
                property.SetValue(editState, textBox.Text);

            SaveEditStateToConfig();
        }

        /// <summary>
        /// Change handler for all of the CheckBoxes that manage a setting.
        /// Sets the corresponding edit state property value and updates the underlying config.
        /// </summary>
        protected void CheckBox_Clicked(object sender, RoutedEventArgs _)
        {
            CheckBox checkBox = (CheckBox)sender;
            GetControlEditStateProperty(checkBox, out PropertyInfo property, out BindableObject editState);

            if (property != null && editState != null)
                property.SetValue(editState, (checkBox.IsChecked == true).ToString());

            SaveEditStateToConfig();
        }

        /// <summary>
        /// A UX nicety: highlight the whole value in a TextBox when it
        /// gets focus such that the new value can immediately be entered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void TextBox_GotFocus(object sender, RoutedEventArgs _)
        {
            TextBox textBox = (TextBox)sender;
            textBox.SelectAll();
        }

        // ---- field validation -------------------------------------------------------------------------------------------

        protected void SetFieldValidVisualState(TextBox field, bool isValid)
        {
            field.BorderBrush = (isValid) ? _defaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
            field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBackgroundBrush"];
        }

        protected void ValidateEditState(BindableObject editState, string propertyName)
        {
            List<string> errors = (List<string>)editState.GetErrors(propertyName);
            if (PageTextBoxes.ContainsKey(propertyName))
                SetFieldValidVisualState(PageTextBoxes[propertyName], (errors.Count == 0));
        }

        protected virtual void BaseField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            ValidateEditState(_editState, args.PropertyName);
        }

        // property changed: rebuild interface state to account for configuration changes.
        //
        protected virtual void BaseField_PropertyChanged(object sender, EventArgs args)
        {
            UpdateUIFromEditState();
        }

        protected async void PageBtnReset_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await Utilities.Message2BDialog(
                Content.XamlRoot,
                "Reset Configuration?",
                "Are you sure you want to reset the " + _systemName + " configurations to avionics defaults? This action cannot be undone.",
                "Reset"
            );
            if (result == ContentDialogResult.Primary)
            {
                _config.UnlinkSystem(_systemTag);
                UpdateLinkControls();
                SystemConfig.Reset();
                _config.Save(this, _systemTag);
                CopyConfigToEditState();
            }
        }

        protected async void PageBtnLink_Click(object sender, RoutedEventArgs args)
        {
            string selectedItem = await Utilities.PageBtnLink_Click(Content.XamlRoot, _config, _systemTag,
                                                                    _configNameList);
            if (selectedItem == null)
            {
                _config.UnlinkSystem(_systemTag);
                _config.Save(this);
            }
            else if (selectedItem.Length > 0)
            {
                _config.LinkSystemTo(_systemTag, _navArgs.UIDtoConfigMap[_configNameToUID[selectedItem]]);
                _config.Save(this);
            }

            CopyConfigToEditState();
            UpdateLinkControls();
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            _navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            _config = (A10CConfiguration)_navArgs.Config;

            Utilities.BuildSystemLinkLists(_navArgs.UIDtoConfigMap, _config.UID, _systemTag, _configNameList, _configNameToUID);
            UpdateLinkControls();

            CopyConfigToEditState();

            base.OnNavigatedTo(args);
        }

        protected void UpdateLinkControls()
        {
            Utilities.RebuildLinkControls(_config, _systemTag, _navArgs.UIDtoConfigMap, _uiPageBtnTxtLink, _uiPageTxtLink);
        }
    }
}
