// ********************************************************************************************************************
//
// SystemEditorPageBase.cs : Base Page class for system editors
//
// Copyright(C) 2024 fizzle, ilominar/raven
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
using JAFDTC.UI.Controls;
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// Base class for a system editor page that provides basic functionality to marshall settings between the UI
    /// and an ISystem in a persisted IConfiguration class. Edits always take place in a UI-centric SystemBase
    /// object rather than a persisted object. The editor page is responsible for moving data between the UI-centric
    /// class and persisted object(s).
    /// 
    /// This class makes the following assumptions,
    /// 
    /// 1. The base class uses EditState and SystemConfig for the UI-centric and persisted configuration parameter
    ///    store, respectively. The base class handles a simple mapping where both EditState and SystemConfig have
    ///    the same layout and parameter names allowing direct copies between EditState and SystemConfig. Editors
    ///    that require more sophisticated mappings can override one or more of the GetControlEditStateProperty(),
    ///    GetControlConfigProperty(), CopyConfigToEditState(), SaveEditStateToConfig(), or CopyAllSettings() methods.
    ///
    /// 2. The base class supports settings specified by TextBoxes, ComboBoxes, or CheckBoxes. Derived classes are
    ///    responsible for handling other control types.
    /// 
    /// 3. To be managed by the base class, a TextBox, ComboBox, or CheckBox must have a non-null Tag. Controls with
    ///    a null Tag are ignored. The base implementation expects the Tag to contain the property name in the
    ///    persisted/UI objects that the control updates. Derived classes can implement other mappings by overriding
    ///    GetControlEditStateProperty() and/or GetControlConfigProperty().
    ///    
    ///    For example, a system editor checkbox that manages the "Latch" setting that corresponds to a MySystem.Latch
    ///    property in the persisted/UI objects would set Tag="Latch" in its XAML definition.
    ///    
    ///    NOTE: SystemEditorPageBase supports an optional tag whitelist to allow controls not managed by the base
    ///    NOTE: class to use tags. When the whitelist is specified, SystemEditorPageBase only manages TextBox,
    ///    NOTE: ComboBox, or CheckBox controls with non-null tags if the tag appears on the whitelist.
    /// 
    /// 4. The base class provides change handlers for TextBox, ComboBox, or CheckBox controls that specify
    ///    configuration parameters (TextBox_LostFocus, ComboBox_SelectionChanged, or CheckBox_Clicked,
    ///    respectively). Generally, controls that the base class manages should use these handlers, but derived
    ///    classes may use their change handlers as necessary. Derived classes must provide change handlers for any
    ///    other control types that specify configuration parameters.
    ///    
    /// 5. The underlying type for a setting managed by a TextBox must be a string.
    /// 
    /// 6. The underlying type for a setting managed by a ComboBox must be a string. The string may carry an arbitrary
    ///    string value or a serialized int (preferably an int-backed enum). A setting S is mapped onto a ComboBox
    ///    selection index I as follows,
    ///
    ///        (a) If S is non-null and matches the Tag of ComboBoxItem A, I is the index of A
    ///              => Assumes ComboBoxItem tags for a given ComboBox are unique strings
    ///        (b) If S is non-null and can be parsed as an integer, I is S converted to an integer
    ///              => Assumes S is a serialized integer on [0, N) where N is the number of items in the ComboBox
    ///        (c) Otherwise,
    ///            (i)  If the Tag of ComboBoxItem A begins with "+", I is the index of A
    ///            (ii) If there is no tag beginning with "+", I is 0
    ///
    ///    When setting the value of S due to a change in the selection, the value is set to Tag (as a string) if
    ///    the new selection has a non-empty tag (after removing any leading "+" character); otherwise, the value is
    ///    set to the index (serialized as a string) of the selection.
    ///   
    /// 7. The underlying type for a setting managed by a CheckBox must be a string containing a serialized bool
    ///    (i.e., "True" or "False").
    ///    
    /// 8. Custom UI update logic can be performed inside the UI Dispatcher delegate by overriding UpdateUICustom().
    ///    
    /// 9. Derived classes must call InitializeBase() after InitializeComponent().
    /// </summary>
    public abstract class SystemEditorPageBase : Page
    {
        /// <summary>
        /// an empty object to stand in for the EditState object the page operates on. this can be used as the
        /// editState parameter to InitializeBase() in the event the derived class does not need SystemEditorPage to
        /// manage the editor state as SystemEditorPageBase frequently assumes there is a valid EditState object.
        /// </summary>
        protected class PrivateEditState : BindableObject { }

        /// <summary>
        /// location of setting state for GetControlProperty(), CopyAllSettings(), and friends.
        /// </summary>
        protected enum SettingLocation
        {
            Edit,
            Config
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // Properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- protected abstract properties

        /// <summary>
        /// Derived classes must override this method to return the system configuration object that is persisted
        /// to disk for the system the page edits.
        /// </summary>
        protected abstract SystemBase SystemConfig { get; }

        /// <summary>
        /// Derived classes must override this method to return the system tag for the system the page edits.
        /// </summary>
        protected abstract string SystemTag { get; }

        /// <summary>
        /// Derived classes must override this method to return the system name for the system the page edits.
        /// </summary>
        protected abstract string SystemName { get; }

        // ---- protected properties

        /// <summary>
        /// Derived classes may override this method to return a bool indicating whether or not page state is default.
        /// </summary>
        protected virtual bool IsPageStateDefault => SystemConfig.IsDefault;

        protected ConfigEditorPageNavArgs NavArgs { get; private set; }

        // NOTE: changes to the Config object may only occur through the marshall methods CopyConfigToEditState() and
        // NOTE: SaveEditStateToConfig(). bindings to and edits by the ui are directed at the EditState property.

        protected IConfiguration Config { get; private set; }

        protected BindableObject EditState { get; set; }

        protected Brush DefaultBorderBrush { get; set; }

        protected Brush DefaultBkgndBrush { get; set; }

        protected bool IsUIUpdatePending { get; private set; }

        protected bool IsUIRebuilding => (_isUIRebuildingCounter > 0);

        // ---- private properties

        private int _isUIRebuildingCounter;

        private LinkResetBtnsControl _linkResetBtnsControl;

        private HashSet<string> _tagWhiteList;

        // ------------------------------------------------------------------------------------------------------------
        //
        // Construction
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Must be called from derived class constructor after InitializeComponent().
        /// </summary>
        protected void InitializeBase(BindableObject editState, TextBox setDefaultsFrom,
                                      LinkResetBtnsControl linkResetBtnsControl, List<string> tagWhitelist = null)
        {
            EditState = editState;

            if (setDefaultsFrom != null)
            {
                DefaultBorderBrush = setDefaultsFrom.BorderBrush;
                DefaultBkgndBrush = setDefaultsFrom.Background;
            }

            _isUIRebuildingCounter = 0;

            if (linkResetBtnsControl != null)
                _linkResetBtnsControl = linkResetBtnsControl;

            // TODO: need api to manage whitelist post-construction, maybe?
            if (tagWhitelist != null)
                _tagWhiteList = new HashSet<string>(tagWhitelist);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // control and property management
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Lazy load and return all TextBox controls on this page that have a non-null Tag. By convention, TextBoxen
        /// that specify configuration parameters have their tag set to the name of the corresponding property in the
        /// configuration objects. Keys in the returned dictionary are the property names.
        /// </summary>
        protected Dictionary<string, TextBox> PageTextBoxes
        {
            get
            {
                if (_pageTextBoxes == null || _pageTextBoxes.Count == 0)
                {
                    List<TextBox> allTextBoxes = new();
                    Utilities.FindDescendantControls(allTextBoxes, this);

                    _pageTextBoxes = new Dictionary<string, TextBox>(allTextBoxes.Count);
                    foreach (TextBox textBox in allTextBoxes)
                        if ((textBox.Tag != null) && ((_tagWhiteList == null) ||
                                                      (_tagWhiteList.Contains(textBox.Tag.ToString()))))
                        {
                            _pageTextBoxes[textBox.Tag.ToString()] = textBox;
                        }
                }
                return _pageTextBoxes;
            }
        }
        private Dictionary<string, TextBox> _pageTextBoxes;

        /// <summary>
        /// Lazy load and return all ComboBox controls on this page that have a non-null Tag. By convention, ComboBoxen
        /// that specify configuration parameters have their tag set to the name of the corresponding property in the
        /// configuration objects. Keys in the returned dictionary are the property names.
        /// </summary>
        protected Dictionary<string, ComboBox> PageComboBoxes
        {
            get
            {
                if (_pageComboBoxes == null || _pageComboBoxes.Count == 0)
                {
                    List<ComboBox> allComboBoxes = new();
                    Utilities.FindDescendantControls(allComboBoxes, this);

                    _pageComboBoxes = new Dictionary<string, ComboBox>(allComboBoxes.Count);
                    foreach (ComboBox comboBox in allComboBoxes)
                        if ((comboBox.Tag != null) && ((_tagWhiteList == null) ||
                                                       (_tagWhiteList.Contains(comboBox.Tag.ToString()))))
                        {
                            _pageComboBoxes[comboBox.Tag.ToString()] = comboBox;
                        }
                }
                return _pageComboBoxes;
            }
        }
        private Dictionary<string, ComboBox> _pageComboBoxes;

        /// <summary>
        /// Lazy load and return all CheckBox controls on this page that have a non-null Tag. By convention, CheckBoxen
        /// that specify configuration parameters have their tag set to the name of the corresponding property in the
        /// configuration objects. Keys in the returned dictionary are the property names.
        /// </summary>
        protected Dictionary<string, CheckBox> PageCheckBoxes
        {
            get
            {
                if (_pageCheckBoxes == null || _pageCheckBoxes.Count == 0)
                {
                    List<CheckBox> allCheckBoxes = new();
                    Utilities.FindDescendantControls(allCheckBoxes, this);

                    _pageCheckBoxes = new Dictionary<string, CheckBox>(allCheckBoxes.Count);
                    foreach (CheckBox checkBox in allCheckBoxes)
                        if ((checkBox.Tag != null) && ((_tagWhiteList == null) ||
                                                       (_tagWhiteList.Contains(checkBox.Tag.ToString()))))
                        {
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
        /// Find and return the property information and encapsulating object corresponding to the provided control
        /// in the EditState object. Base implementation returns data on the property named by the control's Tag in
        /// the EditState object. Derived classes may override this method if they require a more complex mapping
        /// between the tag and the returned (property, encapsulating object) tuple.
        /// </summary>
        protected virtual void GetControlEditStateProperty(FrameworkElement ctrl,
                                                           out PropertyInfo prop, out BindableObject obj)
        {
            prop = EditState.GetType().GetProperty(ctrl.Tag.ToString());
            obj = EditState;

            if (prop == null)
                throw new ApplicationException($"Unexpected {ctrl.GetType()}: Tag {ctrl.Tag}");
        }

        /// <summary>
        /// Find and return the property information and encapsulating object corresponding to the provided control
        /// in the persisted configuration object. Base implementation returns data on the property named by the
        /// control's Tag in the SystemConfig object. Derived classes may override this method if they require a more
        /// complex mapping between the tag and the returned (property, encapsulating object) tuple.
        /// </summary>
        protected virtual void GetControlConfigProperty(FrameworkElement ctrl,
                                                        out PropertyInfo prop, out BindableObject obj)
        {
            prop = SystemConfig.GetType().GetProperty(ctrl.Tag.ToString());
            obj = SystemConfig;

            if (prop == null)
                throw new ApplicationException($"Unexpected {ctrl.GetType()}: Tag {ctrl.Tag}");
        }

        /// <summary>
        /// Finds the corresponding edit or config state for the provided control. This method may return null for
        /// either of the out parameters, prop and obj. Callers must handle such cases.
        /// </summary>
        protected void GetControlProperty(SettingLocation locn, FrameworkElement ctrl,
                                          out PropertyInfo prop, out BindableObject obj)
        {
            if (locn == SettingLocation.Edit)
                GetControlEditStateProperty(ctrl, out prop, out obj);
            else
                GetControlConfigProperty(ctrl, out prop, out obj);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // state management: ui/edit/config copying and saving
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Copy data from the system configuration object to the edit object the page interacts with. Derived classes
        /// should override if not all relevant settings are included in the EditState and SystemConfig classes
        /// managed by this base class or if the EditState and SystemConfig objects are not symmetric.
        /// </summary>
        protected virtual void CopyConfigToEditState()
        {
            if (EditState != null)
            {
                EditState.ClearErrors();
                CopyAllSettings(SettingLocation.Config, SettingLocation.Edit);
                UpdateUIFromEditState();
            }
        }

        /// <summary>
        /// Copy data from the edit object the page interacts with to the system configuration object and persist the
        /// updated configuration to disk. Derived classes should override if not all relevant settings are included
        /// in the EditState and SystemConfig classes managed by this base class or if the EditState and SystemConfig
        /// objects are not symmetric.
        /// </summary>
        protected virtual void SaveEditStateToConfig()
        {
            if ((EditState != null) && !EditState.HasErrors && !IsUIRebuilding)
            {
                CopyAllSettings(SettingLocation.Edit, SettingLocation.Config, true);
                Config.Save(this, SystemTag);
                UpdateUIFromEditState();
            }
        }

        /// <summary>
        /// Iterate over all the settings controls via PageTextBoxes, PageComboBoxes, and PageCheckBoxes. For each
        /// control, get the corresponding property and copy its value from source to destination.
        /// 
        /// When checkSrcErr is true, an error in the source parameter will skip copying only that value.
        /// 
        /// Derived classes should override if not all controls can be mapped to a property on the EditState and 
        /// SystemConfig classes managed by this base class.
        /// </summary>
        protected void CopyAllSettings(SettingLocation srcLoc, SettingLocation destLoc, bool checkSrcErr = false)
        {
            Debug.Assert(srcLoc != destLoc);

            // null source/dest objects are possible below when pages aren't yet fully initialized. no need to
            // guard for null here as CopyProperty() will silently ignore null objects.

            foreach (TextBox textbox in PageTextBoxes.Values)
            {
                GetControlProperty(srcLoc, textbox, out PropertyInfo prop, out BindableObject src);
                GetControlProperty(destLoc, textbox, out PropertyInfo _, out BindableObject dest);
                CopyProperty(prop, src, dest, checkSrcErr);
            }
            foreach (ComboBox combobox in PageComboBoxes.Values)
            {
                GetControlProperty(srcLoc, combobox, out PropertyInfo prop, out BindableObject src);
                GetControlProperty(destLoc, combobox, out PropertyInfo _, out BindableObject dest);
                CopyProperty(prop, src, dest, checkSrcErr);
            }
            foreach (CheckBox checkbox in PageCheckBoxes.Values)
            {
                GetControlProperty(srcLoc, checkbox, out PropertyInfo prop, out BindableObject src);
                GetControlProperty(destLoc, checkbox, out PropertyInfo _, out BindableObject dest);
                CopyProperty(prop, src, dest, checkSrcErr);
            }
        }

        /// <summary>
        /// Find propertyName on source and copy its value to dest. Silently does nothing if the property is unknown,
        /// or either src or dest objects are null.
        /// </summary>
        protected static void CopyProperty(string propName, BindableObject src, BindableObject dest)
        {
            CopyProperty(src.GetType().GetProperty(propName), src, dest);
        }

        /// <summary>
        /// Find propertyName on source and if found, copy its value to destination.Silently does nothing if prop,
        /// src, or dest objects are null or if source error checking is enabled and there is an error.
        /// </summary>
        protected static void CopyProperty(PropertyInfo prop, BindableObject src, BindableObject dest,
                                           bool checkSrcErr = false)
        {
            if ((prop != null) && (src != null) && (dest != null) && !(checkSrcErr && src.PropertyHasErrors(prop.Name)))
                prop.SetValue(dest, prop.GetValue(src));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui configuration/state updates
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Mark the start of a UI rebuild. During UI rebuilds, events from the managed controls are ignored. Each
        /// call to StartUIRebuild() should be balanced by a call to FinishUIRebuild(). Calls can be nested.
        /// </summary>
        protected void StartUIRebuild() => Interlocked.Increment(ref _isUIRebuildingCounter);

        /// <summary>
        /// Mark the end of a UI rebuild. During UI rebuilds, events from the managed controls are ignored. Each
        /// call to FinishUIRebuild() should be balanced by a call to StartUIRebuild(). Calls can be nested.
        /// </summary>
        protected void FinishUIRebuild() => Interlocked.Decrement(ref _isUIRebuildingCounter);

        /// <summary>
        /// Iterate over all the settings controls via PageTextBoxes, PageComboBoxes, and PageCheckBoxes. Set the value
        /// for each control based on the corresponding property as GetControlEditStateProperty() identifies (the base
        /// implementation uses EditState).
        /// </summary>
        protected void UpdateUIFromEditState()
        {
            if (!IsUIUpdatePending)
            {
                IsUIUpdatePending = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    try
                    {
                        StartUIRebuild();
                        DoUIUpdate();
                    }
                    finally
                    {
                        FinishUIRebuild();
                        IsUIUpdatePending = false;
                    }
                });
            }
        }

        /// <summary>
        /// Core UI update method enqueued from UpdateUIFromEditState(). This method is called under IsUIUpdatePending
        /// as well as IsUIRebuilding.
        /// </summary>
        private void DoUIUpdate()
        {
            bool isEditable = string.IsNullOrEmpty(Config.SystemLinkedTo(SystemTag));

            // We want UIUpdateCustom() to be called even if EditState is null, so we only
            // prevent the base UI updates that rely on it here, rather than just returning.
            if (EditState != null)
            {
                foreach (KeyValuePair<string, TextBox> kv in PageTextBoxes)
                {
                    GetControlEditStateProperty(kv.Value, out PropertyInfo property, out BindableObject editState);
                    if ((property == null) || (editState == null) || editState.PropertyHasErrors(property.Name))
                        continue;

                    // Don't re-set a text box with errors, because that will set it back to its pre-error value.
                    Utilities.SetTextBoxEnabledAndText(kv.Value, isEditable, true, (string)property.GetValue(editState));
                }

                foreach (KeyValuePair<string, ComboBox> kv in PageComboBoxes)
                {
                    GetControlEditStateProperty(kv.Value, out PropertyInfo property, out BindableObject editState);
                    if (property == null)
                        continue;

                    string value = (string)property.GetValue(editState);
                    int selectedIndex = -1;
                    int defaultIndex = 0;
                    for (int i = 0; i < kv.Value.Items.Count; i++)
                    {
                        FrameworkElement elem = (FrameworkElement)kv.Value.Items[i];
                        if ((elem != null) && (elem.Tag != null))
                        {
                            string tag = elem.Tag.ToString();
                            if (tag[0] == '+')
                            {
                                defaultIndex = i;
                                tag = tag[1..];
                            }
                            if (tag.ToString() == value)
                            {
                                selectedIndex = i;
                                break;
                            }
                        }
                    }
                    if ((selectedIndex == -1) && !int.TryParse(value, out selectedIndex))
                        selectedIndex = defaultIndex;

                    Utilities.SetComboEnabledAndSelection(kv.Value, isEditable, true, selectedIndex);
                }

                foreach (KeyValuePair<string, CheckBox> kv in PageCheckBoxes)
                {
                    GetControlEditStateProperty(kv.Value, out PropertyInfo property, out BindableObject editState);
                    if (property == null)
                        continue;

                    if (!bool.TryParse((string)property.GetValue(editState), out bool isChecked))
                    {
                        FileManager.Log(string.Format("Unparseable bool ({0}) in {1}. {2} replaced with false.",
                                                      property.GetValue(editState), editState.GetType(), property.Name));
                        isChecked = false;
                    }

                    Utilities.SetCheckEnabledAndState(kv.Value, isEditable, isChecked);
                }
            }

            _linkResetBtnsControl?.SetResetButtonEnabled(!IsPageStateDefault);

            UpdateUICustom(isEditable);
        }

        /// <summary>
        /// Derived classes may override this method to perform custom UI update steps inside the queued dispatcher
        /// delegate UpdateUIFromEditState(). This method is called after the base code has set up the content and
        /// enable state (based on linked status) of all controls that are mapped to configuration parameters.
        /// 
        /// Derived classes must check EditState for null before accessing it.
        /// </summary>
        protected virtual void UpdateUICustom(bool isEditable) { }

        /// <summary>
        /// Derived classes may override this method if they require custom behavior to reset to defaults.
        /// </summary>
        protected virtual void ResetConfigToDefault()
        {
            SystemConfig.Reset();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- control event handlers --------------------------------------------------------------------------------

        /// <summary>
        /// Change handler for all of the ComboBoxes that manage a setting. Sets the corresponding edit state property
        /// value and updates the underlying config.
        /// </summary>
        protected void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs _)
        {
            ComboBox comboBox = (ComboBox)sender;
            GetControlEditStateProperty(comboBox, out PropertyInfo property, out BindableObject editState);

            if (!IsUIRebuilding && (property != null) && (editState != null))
            {
                FrameworkElement item = (FrameworkElement)comboBox.SelectedItem;
                if ((item != null) && (item.Tag != null))
                {
                    string tag = item.Tag.ToString();
                    property.SetValue(editState, (tag[0] == '+') ? tag[1..] : tag);
                }
                else if (item != null)
                    property.SetValue(editState, comboBox.SelectedIndex.ToString());

                SaveEditStateToConfig();
            }
        }

        /// <summary>
        /// Change handler for all of the CheckBoxes that manage a setting. Sets the corresponding edit state
        /// property value and updates the underlying config.
        /// </summary>
        protected void CheckBox_Clicked(object sender, RoutedEventArgs _)
        {
            CheckBox checkBox = (CheckBox)sender;
            GetControlEditStateProperty(checkBox, out PropertyInfo property, out BindableObject editState);

            if (!IsUIRebuilding && (property != null) && (editState != null))
            {
                property.SetValue(editState, (checkBox.IsChecked == true).ToString());
                SaveEditStateToConfig();
            }
        }

        /// <summary>
        /// Change handler for all of the TextBoxes that manage a setting. Sets the corresponding edit state
        /// property value and updates the underlying config.
        /// </summary>
        protected void TextBox_LostFocus(object sender, RoutedEventArgs _)
        {
            TextBox textBox = (TextBox)sender;
            GetControlEditStateProperty(textBox, out PropertyInfo property, out BindableObject editState);

            if (!IsUIRebuilding && (property != null) && (editState != null))
            {
                property.SetValue(editState, textBox.Text);
                SaveEditStateToConfig();
            }
        }

        /// <summary>
        /// A UX nicety: highlight the whole value in a TextBox when it gets focus such that the new value can
        /// immediately be entered.
        /// </summary>
        protected void TextBox_GotFocus(object sender, RoutedEventArgs _)
        {
            TextBox textBox = (TextBox)sender;
            textBox.SelectAll();
        }

        // ---- field validation --------------------------------------------------------------------------------------

        private void SetFieldValidVisualState(TextBox field, bool isValid)
        {
            field.BorderBrush = (isValid) ? DefaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
            field.Background = (isValid) ? DefaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBackgroundBrush"];
        }

        protected void ValidateEditState(BindableObject editState, string propertyName)
        {
            List<string> errors = (List<string>)editState.GetErrors(propertyName);
            if (propertyName == null)
            {
                foreach (TextBox widget in PageTextBoxes.Values)
                    SetFieldValidVisualState(widget, (errors.Count == 0));
            }
            else if (PageTextBoxes.ContainsKey(propertyName))
            {
                SetFieldValidVisualState(PageTextBoxes[propertyName], (errors.Count == 0));
            }
        }

        /// <summary>
        /// Override to perform custom logic when a property changes.
        /// 
        /// Editors having additional custom edit states should subscribe error change events to this handler.
        /// </summary>
        protected virtual void EditState_ErrorsChanged(object sender, DataErrorsChangedEventArgs args)
        {
            ValidateEditState(EditState, args.PropertyName);
        }

        /// <summary>
        /// Override to perform custom logic when a property changes.
        /// 
        /// Editors having additional custom edit states should subscribe property change events to this handler.
        /// </summary>
        protected virtual void EditState_PropertyChanged(object sender, PropertyChangedEventArgs e) { }

        // ---- page-level event handlers -----------------------------------------------------------------------------

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (ConfigEditorPageNavArgs)args.Parameter;
            Config = NavArgs.Config;

            if (EditState != null)
            {
                EditState.ErrorsChanged += EditState_ErrorsChanged;
                EditState.PropertyChanged += EditState_PropertyChanged;
            }

            if (_linkResetBtnsControl != null)
            {
                _linkResetBtnsControl.Initialize(SystemName, SystemTag, this, Config);
                _linkResetBtnsControl.DoReset += ResetConfigToDefault;
                _linkResetBtnsControl.AfterConfigLinkedOrReset += CopyConfigToEditState;
                _linkResetBtnsControl.NavigatedTo(NavArgs.UIDtoConfigMap);
            }
            CopyConfigToEditState();

            base.OnNavigatedTo(args);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (EditState != null)
            {
                EditState.ErrorsChanged -= EditState_ErrorsChanged;
                EditState.PropertyChanged -= EditState_PropertyChanged;
            }

            if (_linkResetBtnsControl != null)
            {
                _linkResetBtnsControl.DoReset -= ResetConfigToDefault;
                _linkResetBtnsControl.AfterConfigLinkedOrReset -= CopyConfigToEditState;
            }

            base.OnNavigatingFrom(e);
        }
    }
}
