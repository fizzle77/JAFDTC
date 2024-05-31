// ********************************************************************************************************************
//
// A10CPageBase.cs : Base Page class for A-10 system editors
//
// Copyright(C) 2024 fizzle
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

using JAFDTC.Models.A10C;
using JAFDTC.UI.App;
using JAFDTC.UI.Controls;
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// There are some conventions followed in A-10 editor pages that make this class work:
    /// 
    /// 1. All settings are managed by TextBoxes, ComboBoxes, or CheckBoxes. There is no handling of other control 
    ///    types. The change handlers for those control types provided by this base class (TextBox_LostFocus,
    ///    ComboBox_SelectionChanged, and CheckBox_Clicked) should be used for all controls managing a system property.
    ///    You can use other control types, but you must handle their state yourself.
    /// 
    /// 2. The underlying type for a setting managed by a ComboBox must be an int (or preferably an int-backed enum). 
    ///    Values displayed in the ComboBox must correspond exactly, and be in the same order as, the corresponding 
    ///    enum of options.
    ///    
    /// 3. The underlying type for a setting managed by a CheckBox must be a bool: "True" or "False" in the serialized
    ///    config.
    /// 
    /// 4. Every control that manages a system setting (whether TextBox, ComboBox, or CheckBox) must have the property's
    ///    name as its Tag. For example, the TGP editor's checkbox that manages the Latch setting corresponds to the
    ///    TGPSystem.Latch property, so it has Tag="Latch" in its XAML definition. Only controls with a tag are handled
    ///    by this base class.
    ///    
    /// 5. _editState is the in-memory storage for settings, and property values are copied in and out of SystemConfig,
    ///    the config store. For editors that have more sophisticated needs, relevant methods can be overriden:
    ///    GetControlPropertyHelper, CopyConfigToEditState, SaveEditStateToConfig, and CopyAllSettings.
    ///    
    /// 6. Custom UI update logic can be performed inside the UI Dispatcher delegate by overriding UpdateUI().
    ///    
    /// 7. Derived classes must call the base contructor. They must also call InitializeBase() after
    ///    InitializeComponent().
    /// </summary>
    public abstract class A10CPageBase : Page
    {
        protected enum SettingLocation
        {
            Edit,
            Config
        }

        private readonly string _systemName;
        private readonly string _systemTag;
        private LinkResetBtnsControl _linkResetBtnsControl;

        protected ConfigEditorPageNavArgs _navArgs;
        protected A10CConfiguration _config;
        protected A10CSystemBase _editState;

        protected Brush _defaultBorderBrush;
        protected Brush _defaultBkgndBrush;

        private bool _isUIUpdatePending = false;
        private bool _isUIUpdateInProgress = false;

        public abstract A10CSystemBase SystemConfig { get; }


        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Must be called from derived class constructor.
        /// </summary>
        public A10CPageBase(string systemName, string systemTag) 
        {
            if (systemName == null) throw new ArgumentException("systemName can not be null");
            if (systemTag == null) throw new ArgumentException("systemTag can not be null");
            _systemName = systemName;
            _systemTag = systemTag;
        }

        /// <summary>
        /// Must be called from derived class constructor after InitializeComponent().
        /// </summary>
        protected void InitializeBase(
            A10CSystemBase editState,
            TextBox setDefaultsFrom = null,
            LinkResetBtnsControl linkResetBtnsControl = null)
        {
            //if (editState == null) throw new ArgumentException("editState can not be null");
            //if (setDefaultsFrom == null) throw new ArgumentException("setDefaultsFrom can not be null");

            if (editState != null)
                _editState = editState;

            if (setDefaultsFrom != null)
            {
                _defaultBorderBrush = setDefaultsFrom.BorderBrush;
                _defaultBkgndBrush = setDefaultsFrom.Background;
            }

            _linkResetBtnsControl = linkResetBtnsControl; // can be null
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // control and property management
        //
        // ------------------------------------------------------------------------------------------------------------

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
                    Utilities.FindDescendantControls(allTextBoxes, this);

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
                    Utilities.FindDescendantControls(allComboBoxes, this);

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
                    Utilities.FindDescendantControls(allCheckBoxes, this);

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
        /// OnNavigatedTo populates all the controls, which performs the lazy load for these maps.
        /// On most editors, that is the right thing to do. On some (noteably the A-10 HMCS editor)
        /// it needs to be done later, in Page_Loaded. This allows those control maps to be rebuilt
        /// and is much easier than trying to carefully control when the initial UI build happens.
        /// Possibly a better understanding of the event model down the line will allow this to be
        /// removed.
        /// </summary>
        protected void ResetControlMaps()
        {
            _pageTextBoxes = null;
            _pageComboBoxes = null;
            _pageCheckBoxes = null;
        }

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
        /// Finds the corresponding edit-state or config class and property for the provided control.
        /// 
        /// Derived classes should override this method if not every tagged control on the page is backed by the page's common 
        /// _editState and SystemConfig.
        /// 
        /// Both out parameters, property and configOrEdit, may be null and callers must handle the possibility.
        /// </summary>
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

        // ------------------------------------------------------------------------------------------------------------
        //
        // state management: ui/edit/config copying and saving
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Derived classes should override if not all relevant settings are included in the _editState and SystemConfig 
        /// classes managed by this base class.
        /// </summary>
        public virtual void CopyConfigToEditState()
        {
            if (_editState != null)
            {
                _editState.ClearErrors();
                CopyAllSettings(SettingLocation.Config, SettingLocation.Edit, false);
                UpdateUIFromEditState();
            }
        }

        /// <summary>
        /// Derived classes should override if not all relevant settings are included in the _editState and SystemConfig 
        /// classes managed by this base class.
        /// </summary>
        protected virtual void SaveEditStateToConfig()
        {
            if (_editState != null && !_isUIUpdateInProgress)
            {
                CopyAllSettings(SettingLocation.Edit, SettingLocation.Config, true);
                _config.Save(this, _systemTag);
                UpdateUIFromEditState();
            }
        }

        /// Iterate over all the settings controls via PageTextBoxes, PageComboBoxes, and PageCheckBoxes.
        /// For each control, get the corresponding property and copy its value from source to destination.
        /// 
        /// When checkErrorsInSource is true, an error in the source parameter will skip copying only that value.
        /// 
        /// Derived classes should override if not all controls can be mapped to a property on the _editState and 
        /// SystemConfig classes managed by this base class.
        /// </summary>
        protected virtual void CopyAllSettings(SettingLocation srcLoc, SettingLocation destLoc, bool checkErrorsInSource = false)
        {
            if (srcLoc == destLoc)
                throw new ArgumentException("source and destination cannot be equal");

            foreach (TextBox textbox in PageTextBoxes.Values)
            {
                GetControlPropertyHelper(srcLoc, textbox, out PropertyInfo property, out BindableObject source);
                if (property == null) throw new ApplicationException("Unexpected TextBox: " + textbox.Tag);
                GetControlPropertyHelper(destLoc, textbox, out PropertyInfo _, out BindableObject destination);

                // null source/destination are possible, when pages aren't yet fully initialized.
                // We pass those along to CopyProperty, which will silently ignore them.
                CopyProperty(property, source, destination, checkErrorsInSource); 
            }


            foreach (ComboBox combobox in PageComboBoxes.Values)
            {
                GetControlPropertyHelper(srcLoc, combobox, out PropertyInfo property, out BindableObject source);
                if (property == null) throw new ApplicationException("Unexpected ComboBox: " + combobox.Tag);
                GetControlPropertyHelper(destLoc, combobox, out PropertyInfo _, out BindableObject destination);

                // null source/destination are possible, when pages aren't yet fully initialized.
                // We pass those along to CopyProperty, which will silently ignore them.
                CopyProperty(property, source, destination, checkErrorsInSource);
            }

            foreach (CheckBox checkbox in PageCheckBoxes.Values)
            {
                GetControlPropertyHelper(srcLoc, checkbox, out PropertyInfo property, out BindableObject source);
                if (property == null) throw new ApplicationException("Unexpected CheckBox: " + checkbox.Tag);
                GetControlPropertyHelper(destLoc, checkbox, out PropertyInfo _, out BindableObject destination);

                // null source/destination are possible, when pages aren't yet fully initialized.
                // We pass those along to CopyProperty, which will silently ignore them.
                CopyProperty(property, source, destination, checkErrorsInSource);
            }
        }

        /// <summary>
        /// Find propertyName on source and if found, copy its value to destination.
        /// 
        /// Silently does nothing if any of the provided arguments are null.
        /// </summary>
        protected void CopyProperty(string propertyName, BindableObject source, BindableObject destination)
        {
            PropertyInfo propInfo = source.GetType().GetProperty(propertyName);
            if (propInfo != null && source != null && destination != null)
                CopyProperty(propInfo, source, destination);
        }

        /// <summary>
        /// Find propertyName on source and if found, copy its value to destination.
        /// 
        /// When checkErrorsInSource is true, an error in the source parameter will skip copying.
        /// 
        /// Silently does nothing if any of the provided arguments are null.
        /// </summary>
        protected void CopyProperty(PropertyInfo property, BindableObject source, BindableObject destination, bool checkErrorsInSource = false)
        {
            if (source != null && destination != null && !(checkErrorsInSource && source.PropertyHasErrors(property.Name)))
                property.SetValue(destination, property.GetValue(source));
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
                try
                {
                    _isUIUpdateInProgress = true;
                    DoUIUpdate();
                }
                finally
                {
                    _isUIUpdateInProgress = false;
                    _isUIUpdatePending = false;
                }
            });
        }

        private void DoUIUpdate()
        {
            bool isNotLinked = string.IsNullOrEmpty(_config.SystemLinkedTo(_systemTag));

            foreach (KeyValuePair<string, TextBox> kv in PageTextBoxes)
            {
                GetControlEditStateProperty(kv.Value, out PropertyInfo property, out BindableObject editState);
                if (property == null) throw new ApplicationException("Unexpected TextBox: " + kv.Key);

                if (editState != null)
                {
                    // Don't re-set a text box with errors, because that will set it back to its pre-error value.
                    if (!editState.PropertyHasErrors(property.Name))
                    {
                        Utilities.SetTextBoxEnabledAndText(kv.Value, isNotLinked, true,
                            (string)property.GetValue(editState));
                    }
                }
            }

            foreach (KeyValuePair<string, ComboBox> kv in PageComboBoxes)
            {
                GetControlEditStateProperty(kv.Value, out PropertyInfo property, out BindableObject editState);
                if (property == null) throw new ApplicationException("Unexpected ComboBox: " + kv.Key);

                if (editState != null)
                {
                    if (!int.TryParse((string)property.GetValue(editState), out int selectedIndex))
                    {
                        FileManager.Log(string.Format("Unparseable int ({0}) encountered in {1}.{2}. Replacing with 0.",
                            property.GetValue(editState), editState.GetType(), property.Name));
                        selectedIndex = 0;
                    }
                    Utilities.SetComboEnabledAndSelection(kv.Value, isNotLinked, true, selectedIndex);
                }
            }

            foreach (KeyValuePair<string, CheckBox> kv in PageCheckBoxes)
            {
                GetControlEditStateProperty(kv.Value, out PropertyInfo property, out BindableObject editState);
                if (property == null) throw new ApplicationException("Unexpected CheckBox: " + kv.Key);

                if (editState != null)
                {
                    if (!bool.TryParse((string)property.GetValue(editState), out bool isChecked))
                    {
                        FileManager.Log(string.Format("Unparseable bool ({0}) encountered in {1}.{2}. Replacing with false.",
                            property.GetValue(editState), editState.GetType(), property.Name));
                        isChecked = false;
                    }
                    Utilities.SetCheckEnabledAndState(kv.Value, isNotLinked, isChecked);
                }
            }

            _linkResetBtnsControl?.SetResetButtonEnabled(!_editState.IsDefault);

            UpdateUI();
        }

        /// <summary>
        /// Override to perform custom UI update steps inside the queued dispatcher delegate.
        /// </summary>
        protected virtual void UpdateUI() { }

        /// <summary>
        /// Override to perform custom logic when a property changes.
        /// 
        /// Editors having additional custom edit states should subscribe property change events to this handler.
        /// </summary>
        protected virtual void EditState_PropertyChanged(object sender, PropertyChangedEventArgs e) { }

        /// <summary>
        /// Override to perform custom logic when a property changes.
        /// 
        /// Editors having additional custom edit states should subscribe error change events to this handler.
        /// </summary>
        protected virtual void EditState_ErrorsChanged(object sender, DataErrorsChangedEventArgs args)
        {
            ValidateEditState(_editState, args.PropertyName);
        }

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

            _linkResetBtnsControl?.SetResetButtonEnabled(!_editState.IsDefault);

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

            _linkResetBtnsControl?.SetResetButtonEnabled(!_editState.IsDefault);

            SaveEditStateToConfig();
        }

        // use this everywhere?
        protected void TextBox_LosingFocus(UIElement sender, LosingFocusEventArgs _)
        {
            TextBox textBox = (TextBox)sender;
            GetControlEditStateProperty(textBox, out PropertyInfo property, out BindableObject editState);

            if (property != null && editState != null)
                property.SetValue(editState, textBox.Text);

            _linkResetBtnsControl?.SetResetButtonEnabled(!_editState.IsDefault);

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

        // ---- field validation -------------------------------------------------------------------------------------------

        protected void SetFieldValidVisualState(TextBox field, bool isValid)
        {
            field.BorderBrush = (isValid) ? _defaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
            field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBackgroundBrush"];
        }

        protected void ValidateEditState(BindableObject editState, string propertyName)
        {
            if (propertyName == null)
            {
                foreach (KeyValuePair<string, TextBox> kv in PageTextBoxes)
                    SetFieldValidVisualState(kv.Value, !editState.PropertyHasErrors(kv.Key));
            }
            else if (PageTextBoxes.ContainsKey(propertyName))
                SetFieldValidVisualState(PageTextBoxes[propertyName], !editState.PropertyHasErrors(propertyName));
        }

        // ---- page-level event handlers -------------------------------------------------------------------------------------------

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            _navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            _config = (A10CConfiguration)_navArgs.Config;

            if (_editState != null)
            {
                _editState.ErrorsChanged += EditState_ErrorsChanged;
                _editState.PropertyChanged += EditState_PropertyChanged;
            }

            if (_linkResetBtnsControl != null)
            {
                _linkResetBtnsControl.Initialize(_systemName, _systemTag, this, _config);
                _linkResetBtnsControl.ConfigLinkedOrReset += CopyConfigToEditState;
                _linkResetBtnsControl.NavigatedTo(_navArgs.UIDtoConfigMap);
            }
            CopyConfigToEditState();

            base.OnNavigatedTo(args);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (_editState != null)
            {
                _editState.ErrorsChanged -= EditState_ErrorsChanged;
                _editState.PropertyChanged -= EditState_PropertyChanged;
            }

            if (_linkResetBtnsControl != null)
                _linkResetBtnsControl.ConfigLinkedOrReset -= CopyConfigToEditState;

            base.OnNavigatingFrom(e);
        }
    }
}
