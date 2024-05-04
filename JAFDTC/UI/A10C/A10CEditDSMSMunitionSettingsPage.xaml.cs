using JAFDTC.Models;
using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.DSMS;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using static JAFDTC.Models.A10C.DSMS.DSMSSystem;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// UI Code for the A-10 DSMS A10CMunition Settings Frame
    /// </summary>
    public sealed partial class A10CEditDSMSMunitionSettingsPage : Page
    {
        private ConfigEditorPageNavArgs _navArgs;
        private A10CConfiguration _config;
        private readonly DSMSSystem _editState;

        private bool _uiUpdatePending;

        private readonly Dictionary<string, TextBox> _dsmsTextFieldPropertyMap;
        private readonly Dictionary<string, TextBox> _munitionTextFieldPropertyMap;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        private readonly List<A10CMunition> _munitions;

        public A10CEditDSMSMunitionSettingsPage()
        {
            this.InitializeComponent();

            _munitions = FileManager.LoadMunitions();
            _editState = new DSMSSystem();

            _editState.ErrorsChanged += BaseField_DataValidationError;
            _editState.PropertyChanged += BaseField_PropertyChanged;

            // For mapping property errors to corresponding text field.
            _dsmsTextFieldPropertyMap = new Dictionary<string, TextBox>()
            {
                ["LaserCode"] = uiTextLaserCode
            };
            _munitionTextFieldPropertyMap = new Dictionary<string, TextBox>()
            {
                ["LaseSeconds"] = uiTextLaseTime,
                ["RippleFt"] = uiTextRippleFt,
                ["RippleQty"] = uiTextRippleQty
            };
            _defaultBorderBrush = uiTextLaserCode.BorderBrush;
            _defaultBkgndBrush = uiTextLaserCode.Background;
        }

        private void UpdateUIFromEditState()
        {
            if (_uiUpdatePending)
                return;
            _uiUpdatePending = true;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                bool isNotLinked = string.IsNullOrEmpty(_config.SystemLinkedTo(SystemTag));

                A10CMunition selectedMunition = (A10CMunition)uiComboMunition.SelectedItem;
                string key = selectedMunition.Key;

                // Laser
                Utilities.SetTextBoxEnabledAndText(uiTextLaserCode, isNotLinked, selectedMunition.Laser, _editState.LaserCode, _editState.LaserCode);
                Utilities.SetCheckEnabledAndState(uiCheckAutoLase, isNotLinked && selectedMunition.AutoLase, _editState.GetAutoLaseValue(key));
                Utilities.SetTextBoxEnabledAndText(uiTextLaseTime, isNotLinked, selectedMunition.AutoLase, _editState.GetLaseSeconds(key));

                // Delivery Mode (CCIP/CCRP)
                if (selectedMunition.CCIP ^ selectedMunition.CCRP)
                {
                    if (selectedMunition.CCIP)
                        uiComboDeliveryMode.SelectedIndex = 0;
                    else
                        uiComboDeliveryMode.SelectedIndex = 1;
                    uiComboDeliveryMode.IsEnabled = false;
                }
                else
                    Utilities.SetComboEnabledAndSelection(uiComboDeliveryMode, isNotLinked, true, (int)_editState.GetDeliveryModeValue(key));

                // Escape Maneuver
                Utilities.SetComboEnabledAndSelection(uiComboEscMnvr, isNotLinked, selectedMunition.EscMnvr, (int)_editState.GetEscapeManeuverValue(key));

                // Release Mode (SGL, PRS, RIP SGL, RIP PRS)
                if (selectedMunition.SingleReleaseOnly)
                {
                    // For munitions allowing only SGL release, select it and disable.
                    uiComboReleaseMode.SelectedIndex = 0;
                    uiComboReleaseMode.IsEnabled = false;
                }
                else
                    Utilities.SetComboEnabledAndSelection(uiComboReleaseMode, isNotLinked, true, (int)_editState.GetReleaseModeValue(key));

                // Ripple Qty and Distance
                bool enableRippleOptions = selectedMunition.Ripple && uiComboReleaseMode.SelectedIndex > 1; // disabled when SGL or PRS release is selected
                Utilities.SetTextBoxEnabledAndText(uiTextRippleQty, isNotLinked, enableRippleOptions, _editState.GetRippleQty(key), null);
                Utilities.SetTextBoxEnabledAndText(uiTextRippleFt, isNotLinked, enableRippleOptions && selectedMunition.RipFt, 
                    _editState.GetRippleFt(key));

                // HOF & RPM
                Utilities.SetComboEnabledAndSelection(uiComboHOF, isNotLinked, selectedMunition.HOF, (int)_editState.GetHOFOptionValue(key));
                Utilities.SetComboEnabledAndSelection(uiComboRPM, isNotLinked, selectedMunition.HOF, (int)_editState.GetRPMOptionValue(key));

                // Fuze
                Utilities.SetComboEnabledAndSelection(uiComboFuze, isNotLinked, selectedMunition.Fuze, (int)_editState.GetFuzeOptionValue(key));

                MunitionSettings newSettings = _editState.GetMunitionSettings(key);
                newSettings.ErrorsChanged += BaseField_DataValidationError;
                newSettings.PropertyChanged += BaseField_PropertyChanged;

                _uiUpdatePending = false;
            });
        }

        private void SaveEditStateToConfig(string selectedMunitionKey = null)
        {
            if (_editState.HasErrors)
                return;

            _config.DSMS.LaserCode = _editState.LaserCode;
            if (selectedMunitionKey == null)
                return;

            MunitionSettings settings = _editState.GetMunitionSettings(selectedMunitionKey);
            if (settings.HasErrors)
                return;
            _config.DSMS.SetAutoLase(selectedMunitionKey, _editState.GetAutoLase(selectedMunitionKey));
            _config.DSMS.SetLaseSeconds(selectedMunitionKey, _editState.GetLaseSeconds(selectedMunitionKey));
            _config.DSMS.SetDeliveryMode(selectedMunitionKey, _editState.GetDeliveryMode(selectedMunitionKey));
            _config.DSMS.SetEscapeManeuver(selectedMunitionKey, _editState.GetEscapeManeuver(selectedMunitionKey));
            _config.DSMS.SetReleaseMode(selectedMunitionKey, _editState.GetReleaseMode(selectedMunitionKey));
            _config.DSMS.SetHOFOption(selectedMunitionKey, _editState.GetHOFOption(selectedMunitionKey));
            _config.DSMS.SetRPMOption(selectedMunitionKey, _editState.GetRPMOption(selectedMunitionKey));
            _config.DSMS.SetRippleQty(selectedMunitionKey, _editState.GetRippleQty(selectedMunitionKey));
            _config.DSMS.SetRippleFt(selectedMunitionKey, _editState.GetRippleFt(selectedMunitionKey));
            _config.DSMS.SetFuzeOption(selectedMunitionKey, _editState.GetFuzeOption(selectedMunitionKey));
            _config.Save(this, SystemTag);
        }

        public void CopyConfigToEditState()
        {
            if (IsMunitionSelectionValid(out string key))
            {
                _uiUpdatePending = true;
                CopyConfigToEditState(key);

                _uiUpdatePending = false;
                UpdateUIFromEditState();
            }
        }
        
        private void CopyConfigToEditState(A10CMunition munition)
        {
            CopyConfigToEditState(munition.Key);
        }

        private void CopyConfigToEditState(string key)
        {
            _editState.LaserCode = _config.DSMS.LaserCode;
            _editState.SetAutoLase(key, _config.DSMS.GetAutoLase(key));
            _editState.SetLaseSeconds(key, _config.DSMS.GetLaseSeconds(key));
            _editState.SetDeliveryMode(key, _config.DSMS.GetDeliveryMode(key));
            _editState.SetEscapeManeuver(key, _config.DSMS.GetEscapeManeuver(key));
            _editState.SetReleaseMode(key, _config.DSMS.GetReleaseMode(key));
            _editState.SetRippleQty(key, _config.DSMS.GetRippleQty(key));
            _editState.SetRippleFt(key, _config.DSMS.GetRippleFt(key));
            _editState.SetHOFOption(key, _config.DSMS.GetHOFOption(key));
            _editState.SetRPMOption(key, _config.DSMS.GetRPMOption(key));
            _editState.SetFuzeOption(key, _config.DSMS.GetFuzeOption(key));
        }

        private bool IsMunitionSelectionValid(out string selectedMunitionKey)
        {
            selectedMunitionKey = null;
            A10CMunition selectedMunition = (A10CMunition)uiComboMunition.SelectedItem;
            if (selectedMunition == null)
                return false;

            selectedMunitionKey = selectedMunition.Key;
            return true;
        }

        // ---- event handlers -------------------------------------------------------------------------------------------

        // It's important that these TextBoxes use the LosingFocus focus event, not LostFocus. LosingFocus
        // fires synchronoulsy before uiComboMunition_SelectionChanged, ensuring an altered text value is
        // correctly saved before switching munitions.

        private void ComboMunition_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                A10CMunition oldSelectedMunition = (A10CMunition)e.RemovedItems[0];
                if (oldSelectedMunition != null)
                {
                    MunitionSettings oldSettings = _editState.GetMunitionSettings(oldSelectedMunition.Key);
                    oldSettings.ErrorsChanged -= BaseField_DataValidationError;
                    oldSettings.PropertyChanged -= BaseField_PropertyChanged;
                }
            }

            if (e.AddedItems.Count > 0)
            {
                A10CMunition newSelectedMunition = (A10CMunition)e.AddedItems[0];
                if (newSelectedMunition != null)
                {
                    CopyConfigToEditState(newSelectedMunition);
                    UpdateUIFromEditState();
                }
            }
        }

        private void TextLaserCode_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            _editState.LaserCode = uiTextLaserCode.Text;
            SaveEditStateToConfig();
        }

        private void CheckAutoLase_Changed(object sender, RoutedEventArgs e)
        {
            if (IsMunitionSelectionValid(out string selectedMunitionKey))
            {
                _editState.SetAutoLase(selectedMunitionKey, uiCheckAutoLase.IsChecked.ToString());
                SaveEditStateToConfig(selectedMunitionKey);
            }
        }

        private void TextLaseTime_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (IsMunitionSelectionValid(out string selectedMunitionKey))
            {
                _editState.SetLaseSeconds(selectedMunitionKey, uiTextLaseTime.Text);
                SaveEditStateToConfig(selectedMunitionKey);
            }
        }

        private void ComboDeliveryMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsMunitionSelectionValid(out string selectedMunitionKey))
            {
                _editState.SetDeliveryMode(selectedMunitionKey, uiComboDeliveryMode.SelectedIndex.ToString());
                SaveEditStateToConfig(selectedMunitionKey);
            }
        }

        private void ComboEscMnvr_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsMunitionSelectionValid(out string selectedMunitionKey))
            {
                _editState.SetEscapeManeuver(selectedMunitionKey, uiComboEscMnvr.SelectedIndex.ToString());
                SaveEditStateToConfig(selectedMunitionKey);
            }
        }

        private void ComboReleaseMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsMunitionSelectionValid(out string selectedMunitionKey))
            {
                _editState.SetReleaseMode(selectedMunitionKey, uiComboReleaseMode.SelectedIndex.ToString());
                SaveEditStateToConfig(selectedMunitionKey);
            }
        }

        private void TextRippleQty_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (IsMunitionSelectionValid(out string selectedMunitionKey))
            {
                _editState.SetRippleQty(selectedMunitionKey, uiTextRippleQty.Text);
                SaveEditStateToConfig(selectedMunitionKey);
            }
        }

        private void TextRippleFt_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (IsMunitionSelectionValid(out string selectedMunitionKey))
            {
                _editState.SetRippleFt(selectedMunitionKey, uiTextRippleFt.Text);
                SaveEditStateToConfig(selectedMunitionKey);
            }
        }

        private void ComboHOF_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsMunitionSelectionValid(out string selectedMunitionKey))
            {
                _editState.SetHOFOption(selectedMunitionKey, uiComboHOF.SelectedIndex.ToString());
                SaveEditStateToConfig(selectedMunitionKey);
            }
        }

        private void ComboRPM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsMunitionSelectionValid(out string selectedMunitionKey))
            {
                _editState.SetRPMOption(selectedMunitionKey, uiComboRPM.SelectedIndex.ToString());
                SaveEditStateToConfig(selectedMunitionKey);
            }
        }

        private void ComboFuze_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsMunitionSelectionValid(out string selectedMunitionKey))
            {
                _editState.SetFuzeOption(selectedMunitionKey, uiComboFuze.SelectedIndex.ToString());
                SaveEditStateToConfig(selectedMunitionKey);
            }
        }

        private void ConfigurationSavedHandler(object sender, ConfigurationSavedEventArgs e)
        {
            UpdateUIFromEditState();
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            _navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            _config = (A10CConfiguration)_navArgs.Config;

            _config.ConfigurationSaved += ConfigurationSavedHandler;

            if (_munitions.Count > 0)
            {
                CopyConfigToEditState(_munitions[0]);
                uiComboMunition.SelectedIndex = 0;
                UpdateUIFromEditState();
            }

            base.OnNavigatedTo(args);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _config.ConfigurationSaved -= ConfigurationSavedHandler;
            base.OnNavigatedFrom(e);
        }

        // ---- field validation -------------------------------------------------------------------------------------------

        private void SetFieldValidVisualState(TextBox field, bool isValid)
        {
            field.BorderBrush = (isValid) ? _defaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
            field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBackgroundBrush"];
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
                SetFieldValidVisualState(kvp.Value, !map.ContainsKey(kvp.Key));
            }
        }

        // validation error: update ui state for the various components that may have errors.
        //
        private void BaseField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            A10CMunition selectedMunition = (A10CMunition)uiComboMunition.SelectedItem;
            if (args.PropertyName == null)
            {
                ValidateAllFields(_dsmsTextFieldPropertyMap, _editState.GetErrors(null));
                if (selectedMunition != null)
                    ValidateAllFields(_munitionTextFieldPropertyMap, _editState.GetMunitionSettings(selectedMunition.Key).GetErrors(null));
            }
            else
            {
                List<string> errors = (List<string>)_editState.GetErrors(args.PropertyName);
                if (_dsmsTextFieldPropertyMap.ContainsKey(args.PropertyName))
                    SetFieldValidVisualState(_dsmsTextFieldPropertyMap[args.PropertyName], (errors.Count == 0));

                if (selectedMunition != null)
                {
                    errors = (List<string>)_editState.GetMunitionSettings(selectedMunition.Key).GetErrors(args.PropertyName);
                    if (_munitionTextFieldPropertyMap.ContainsKey(args.PropertyName))
                        SetFieldValidVisualState(_munitionTextFieldPropertyMap[args.PropertyName], (errors.Count == 0));
                }
            }
            UpdateUIFromEditState();
        }

        // property changed: rebuild interface state to account for configuration changes.
        //
        private void BaseField_PropertyChanged(object sender, EventArgs args)
        {
            UpdateUIFromEditState();
        }
    }
}
