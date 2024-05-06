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

            _munitions = FileManager.LoadA10Munitions();
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
                // Laser
                Utilities.SetTextBoxEnabledAndText(uiTextLaserCode, isNotLinked, selectedMunition.Laser, _editState.LaserCode, _editState.LaserCode);
                Utilities.SetCheckEnabledAndState(uiCheckAutoLase, isNotLinked && selectedMunition.AutoLase, _editState.GetAutoLaseValue(selectedMunition));
                Utilities.SetTextBoxEnabledAndText(uiTextLaseTime, isNotLinked, selectedMunition.AutoLase, _editState.GetLaseSeconds(selectedMunition));

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
                    Utilities.SetComboEnabledAndSelection(uiComboDeliveryMode, isNotLinked, true, (int)_editState.GetDeliveryModeValue(selectedMunition));

                // Escape Maneuver
                Utilities.SetComboEnabledAndSelection(uiComboEscMnvr, isNotLinked, selectedMunition.EscMnvr, (int)_editState.GetEscapeManeuverValue(selectedMunition));

                // Release Mode (SGL, PRS, RIP SGL, RIP PRS)
                if (selectedMunition.SingleReleaseOnly)
                {
                    // For munitions allowing only SGL release, select it and disable.
                    uiComboReleaseMode.SelectedIndex = 0;
                    uiComboReleaseMode.IsEnabled = false;
                }
                else
                    Utilities.SetComboEnabledAndSelection(uiComboReleaseMode, isNotLinked, true, (int)_editState.GetReleaseModeValue(selectedMunition));

                // Ripple Qty and Distance
                bool enableRippleOptions = selectedMunition.Ripple && uiComboReleaseMode.SelectedIndex > 1; // disabled when SGL or PRS release is selected
                Utilities.SetTextBoxEnabledAndText(uiTextRippleQty, isNotLinked, enableRippleOptions, _editState.GetRippleQty(selectedMunition), null);
                Utilities.SetTextBoxEnabledAndText(uiTextRippleFt, isNotLinked, enableRippleOptions && selectedMunition.RipFt, 
                    _editState.GetRippleFt(selectedMunition));

                // HOF & RPM
                Utilities.SetComboEnabledAndSelection(uiComboHOF, isNotLinked, selectedMunition.HOF, (int)_editState.GetHOFOptionValue(selectedMunition));
                Utilities.SetComboEnabledAndSelection(uiComboRPM, isNotLinked, selectedMunition.HOF, (int)_editState.GetRPMOptionValue(selectedMunition));

                // Fuze
                Utilities.SetComboEnabledAndSelection(uiComboFuze, isNotLinked, selectedMunition.Fuze, (int)_editState.GetFuzeOptionValue(selectedMunition));

                MunitionSettings newSettings = _editState.GetMunitionSettings(selectedMunition);
                newSettings.ErrorsChanged += BaseField_DataValidationError;
                newSettings.PropertyChanged += BaseField_PropertyChanged;

                _uiUpdatePending = false;
            });
        }

        private void SaveEditStateToConfig(A10CMunition selectedMunition = null)
        {
            if (_editState.HasErrors)
                return;

            _config.DSMS.LaserCode = _editState.LaserCode;
            if (selectedMunition == null)
                return;

            MunitionSettings settings = _editState.GetMunitionSettings(selectedMunition);
            if (settings.HasErrors)
                return;
            _config.DSMS.SetAutoLase(selectedMunition, _editState.GetAutoLase(selectedMunition));
            _config.DSMS.SetLaseSeconds(selectedMunition, _editState.GetLaseSeconds(selectedMunition));
            _config.DSMS.SetDeliveryMode(selectedMunition, _editState.GetDeliveryMode(selectedMunition));
            _config.DSMS.SetEscapeManeuver(selectedMunition, _editState.GetEscapeManeuver(selectedMunition));
            _config.DSMS.SetReleaseMode(selectedMunition, _editState.GetReleaseMode(selectedMunition));
            _config.DSMS.SetHOFOption(selectedMunition, _editState.GetHOFOption(selectedMunition));
            _config.DSMS.SetRPMOption(selectedMunition, _editState.GetRPMOption(selectedMunition));
            _config.DSMS.SetRippleQty(selectedMunition, _editState.GetRippleQty(selectedMunition));
            _config.DSMS.SetRippleFt(selectedMunition, _editState.GetRippleFt(selectedMunition));
            _config.DSMS.SetFuzeOption(selectedMunition, _editState.GetFuzeOption(selectedMunition));
            _config.Save(this, SystemTag);
        }

        public void CopyConfigToEditState()
        {
            if (IsMunitionSelectionValid(out A10CMunition munition))
            {
                _uiUpdatePending = true;
                CopyConfigToEditState(munition);

                _uiUpdatePending = false;
                UpdateUIFromEditState();
            }
        }
        
        private void CopyConfigToEditState(A10CMunition selectedMunition)
        {
            _editState.LaserCode = _config.DSMS.LaserCode;
            _editState.SetAutoLase(selectedMunition, _config.DSMS.GetAutoLase(selectedMunition));
            _editState.SetLaseSeconds(selectedMunition, _config.DSMS.GetLaseSeconds(selectedMunition));
            _editState.SetDeliveryMode(selectedMunition, _config.DSMS.GetDeliveryMode(selectedMunition));
            _editState.SetEscapeManeuver(selectedMunition, _config.DSMS.GetEscapeManeuver(selectedMunition));
            _editState.SetReleaseMode(selectedMunition, _config.DSMS.GetReleaseMode(selectedMunition));
            _editState.SetRippleQty(selectedMunition, _config.DSMS.GetRippleQty(selectedMunition));
            _editState.SetRippleFt(selectedMunition, _config.DSMS.GetRippleFt(selectedMunition));
            _editState.SetHOFOption(selectedMunition, _config.DSMS.GetHOFOption(selectedMunition));
            _editState.SetRPMOption(selectedMunition, _config.DSMS.GetRPMOption(selectedMunition));
            _editState.SetFuzeOption(selectedMunition, _config.DSMS.GetFuzeOption(selectedMunition));
        }

        private bool IsMunitionSelectionValid(out A10CMunition selectedMunition)
        {
            selectedMunition = (A10CMunition)uiComboMunition.SelectedItem;
            return selectedMunition != null;
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
                    MunitionSettings oldSettings = _editState.GetMunitionSettings(oldSelectedMunition);
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
            if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
            {
                _editState.SetAutoLase(selectedMunition, uiCheckAutoLase.IsChecked.ToString());
                SaveEditStateToConfig(selectedMunition);
            }
        }

        private void TextLaseTime_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
            {
                _editState.SetLaseSeconds(selectedMunition, uiTextLaseTime.Text);
                SaveEditStateToConfig(selectedMunition);
            }
        }

        private void ComboDeliveryMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
            {
                _editState.SetDeliveryMode(selectedMunition, uiComboDeliveryMode.SelectedIndex.ToString());
                SaveEditStateToConfig(selectedMunition);
            }
        }

        private void ComboEscMnvr_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
            {
                _editState.SetEscapeManeuver(selectedMunition, uiComboEscMnvr.SelectedIndex.ToString());
                SaveEditStateToConfig(selectedMunition);
            }
        }

        private void ComboReleaseMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
            {
                _editState.SetReleaseMode(selectedMunition, uiComboReleaseMode.SelectedIndex.ToString());
                SaveEditStateToConfig(selectedMunition);
            }
        }

        private void TextRippleQty_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
            {
                _editState.SetRippleQty(selectedMunition, uiTextRippleQty.Text);
                SaveEditStateToConfig(selectedMunition);
            }
        }

        private void TextRippleFt_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
            {
                _editState.SetRippleFt(selectedMunition, uiTextRippleFt.Text);
                SaveEditStateToConfig(selectedMunition);
            }
        }

        private void ComboHOF_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
            {
                _editState.SetHOFOption(selectedMunition, uiComboHOF.SelectedIndex.ToString());
                SaveEditStateToConfig(selectedMunition);
            }
        }

        private void ComboRPM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
            {
                _editState.SetRPMOption(selectedMunition, uiComboRPM.SelectedIndex.ToString());
                SaveEditStateToConfig(selectedMunition);
            }
        }

        private void ComboFuze_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
            {
                _editState.SetFuzeOption(selectedMunition, uiComboFuze.SelectedIndex.ToString());
                SaveEditStateToConfig(selectedMunition);
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
                    ValidateAllFields(_munitionTextFieldPropertyMap, _editState.GetMunitionSettings(selectedMunition).GetErrors(null));
            }
            else
            {
                List<string> errors = (List<string>)_editState.GetErrors(args.PropertyName);
                if (_dsmsTextFieldPropertyMap.ContainsKey(args.PropertyName))
                    SetFieldValidVisualState(_dsmsTextFieldPropertyMap[args.PropertyName], (errors.Count == 0));

                if (selectedMunition != null)
                {
                    errors = (List<string>)_editState.GetMunitionSettings(selectedMunition).GetErrors(args.PropertyName);
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
