// ********************************************************************************************************************
//
// A10CEditDSMSMunitionSettingsPage.cs : ui c# for warthog dsms munitions editor page
//
// Copyright(C) 2024 fizzle, JAFDTC contributors
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
using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.DSMS;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.Generic;
using static JAFDTC.Models.A10C.DSMS.DSMSSystem;
using static JAFDTC.UI.A10C.A10CEditDSMSPage;
using System.Reflection;
using System.ComponentModel;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// UI Code for the A-10 DSMS A10CMunition Settings Frame
    /// </summary>
    public sealed partial class A10CEditDSMSMunitionSettingsPage : A10CPageBase
    {
        private const string SYSTEM_NAME = "DSMS";

        private DSMSSystem EditState => (DSMSSystem)_editState;
        public override A10CSystemBase SystemConfig => _config.DSMS;

        private DSMSEditorNavArgs _dsmsEditorNavArgs;
        private readonly List<A10CMunition> _munitions;

        public A10CEditDSMSMunitionSettingsPage() : base(SYSTEM_NAME, DSMSSystem.SystemTag)
        {
            InitializeComponent();
            InitializeBase(new DSMSSystem(), uiTextLaserCode, null);

            _munitions = FileManager.LoadA10Munitions();
        }

        // ---- UI helpers  -------------------------------------------------------------------------------------------

        protected override void GetControlPropertyHelper(
            SettingLocation settingLocation,
            FrameworkElement control,
            out PropertyInfo property,
            out BindableObject configOrEdit)
        {
            // base will check the "normal" DSMSSystem class properties first.
            base.GetControlPropertyHelper(settingLocation, control, out property, out configOrEdit);

            // if it comes up empty, use the MunitionSettings class for the selected munition.
            if (property == null)
            {
                string propName = control.Tag.ToString();
                property = typeof(MunitionSettings).GetProperty(propName);
                if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
                {
                    if (settingLocation == SettingLocation.Edit)
                        configOrEdit = EditState.GetMunitionSettings(selectedMunition);
                    else
                        configOrEdit = _config.DSMS.GetMunitionSettings(selectedMunition);
                }
                else
                    configOrEdit = null;
            }
        }

        /// <summary>
        /// The base UI update will correctly set values and link-related enablement, but this editor has 
        /// unique visibility and enablement behavior we need to perform here.
        /// </summary>
        protected override void UpdateUI()
        {
            if (!IsMunitionSelectionValid(out A10CMunition selectedMunition))
               return;

            SetLabelColorMatchingControlEnabledState(uiLabelLaserCode, uiTextLaserCode);

            Visibility autoLaseVisible = (selectedMunition.AutoLase) ? Visibility.Visible : Visibility.Collapsed;
            Visibility autoLaseFieldVisible = (selectedMunition.AutoLase && EditState.GetAutoLaseValue(selectedMunition)) 
                ? Visibility.Visible : Visibility.Collapsed;
            uiLabelAutoLase.Visibility = autoLaseVisible;
            uiStackAutoLase.Visibility = autoLaseVisible;
            uiTextLaseTime.Visibility = autoLaseFieldVisible;
            uiLabelLaseTimeUnits.Visibility = autoLaseFieldVisible;
            if (selectedMunition.AutoLase) SetLabelColorMatchingControlEnabledState(new TextBlock[] { uiLabelAutoLase, uiLabelLaseTimeUnits } , uiTextLaseTime);

            // Delivery Mode (CCIP/CCRP)
            if (selectedMunition.CCIP ^ selectedMunition.CCRP)
            {
                if (selectedMunition.CCIP)
                    uiComboDeliveryMode.SelectedIndex = 0;
                else
                    uiComboDeliveryMode.SelectedIndex = 1;
                uiComboDeliveryMode.IsEnabled = false;
                uiLabelDeliveryMode.Foreground = uiTextLaserCode.PlaceholderForeground;
            }
            SetLabelColorMatchingControlEnabledState(uiLabelDeliveryMode, uiComboDeliveryMode);

            // Escape Maneuver
            Visibility escVisible = (selectedMunition.EscMnvr) ? Visibility.Visible : Visibility.Collapsed;
            uiLabelEscMnvr.Visibility = escVisible;
            uiComboEscMnvr.Visibility = escVisible;

            // Release Mode (SGL, PRS, RIP SGL, RIP PRS)
            if (selectedMunition.SingleReleaseOnly)
            {
                // For munitions allowing only SGL release, select it and disable.
                uiComboReleaseMode.SelectedIndex = 0;
                uiComboReleaseMode.IsEnabled = false;
                uiStackRipple.Visibility = Visibility.Collapsed;
            }
            else
            {
                uiStackRipple.Visibility = (uiComboReleaseMode.SelectedIndex < 2) ? Visibility.Collapsed : Visibility.Visible;
            }
            SetLabelColorMatchingControlEnabledState(uiLabelReleaseMode, uiComboReleaseMode);

            // Ripple Qty and Distance
            bool enableRippleOptions = selectedMunition.Ripple && uiComboReleaseMode.SelectedIndex > 1; // disabled when SGL or PRS release is selected
            uiTextRippleQty.IsEnabled = enableRippleOptions;
            uiTextRippleFt.IsEnabled = enableRippleOptions && selectedMunition.RipFt;
            SetLabelColorMatchingControlEnabledState(new TextBlock[] { uiLabelRipple, uiLabelRippleAt, uiLabelRippleUnits }, uiComboReleaseMode);

            // HOF
            Visibility hofVisible = (selectedMunition.HOF) ? Visibility.Visible : Visibility.Collapsed;
            uiLabelHOF.Visibility = hofVisible;
            uiComboHOF.Visibility = hofVisible;
            if (selectedMunition.HOF) SetLabelColorMatchingControlEnabledState(uiLabelHOF, uiComboHOF);

            // RPM
            uiStackRPM.Visibility = selectedMunition.RPM ? Visibility.Visible : Visibility.Collapsed;
            if (selectedMunition.RPM) SetLabelColorMatchingControlEnabledState(uiLabelRPM, uiComboRPM);

            // Fuze
            Visibility fuzeVisible = (selectedMunition.Fuze) ? Visibility.Visible : Visibility.Collapsed;
            uiLabelFuse.Visibility = fuzeVisible;
            uiComboFuze.Visibility = fuzeVisible;
            if (selectedMunition.Fuze) SetLabelColorMatchingControlEnabledState(uiLabelFuse, uiComboFuze);

            UpdateNonDefaultMunitionIcons();

            MunitionSettings newSettings = EditState.GetMunitionSettings(selectedMunition);
            bool isNotLinked = !_config.IsLinked(SystemTag);
            Utilities.SetEnableState(uiMuniBtnReset, isNotLinked && !newSettings.IsDefault);
        }

        private void SetLabelColorMatchingControlEnabledState(TextBlock[] labels, Control control)
        {
            foreach (TextBlock label in labels)
                SetLabelColorMatchingControlEnabledState(label, control);
        }

        private void SetLabelColorMatchingControlEnabledState(TextBlock label, Control control)
        {
            label.Foreground = control.IsEnabled ? uiTextLaserCode.Foreground : uiTextLaserCode.PlaceholderForeground;
        }

        private bool IsMunitionSelectionValid(out A10CMunition selectedMunition)
        {
            selectedMunition = (A10CMunition)uiComboMunition.SelectedItem;
            return selectedMunition != null;
        }

        private void UpdateNonDefaultMunitionIcons()
        {
            foreach (A10CMunition munition in uiComboMunition.Items)
            {
                UIElement container = (UIElement)uiComboMunition.ContainerFromItem(munition);
                FontIcon icon = Utilities.FindControl<FontIcon>(container, typeof(FontIcon), "icon");
                if (icon != null)
                {
                    ISystem system = _config.DSMS.GetMunitionSettings(munition);
                    icon.Visibility = Utilities.HiddenIfDefault(system);
                }
            }
        }

        // ---- event handlers -------------------------------------------------------------------------------------------

        private void ComboMunition_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                A10CMunition oldSelectedMunition = (A10CMunition)e.RemovedItems[0];
                if (oldSelectedMunition != null)
                {
                    MunitionSettings oldSettings = EditState.GetMunitionSettings(oldSelectedMunition);
                    // Unnecessary because munition settings have no text boxes so no possible errors.
                    // oldSettings.ErrorsChanged -= EditState_ErrorsChanged;
                    oldSettings.PropertyChanged -= EditState_PropertyChanged;
                }
            }

            if (e.AddedItems.Count > 0)
            {
                A10CMunition newSelectedMunition = (A10CMunition)e.AddedItems[0];
                if (newSelectedMunition != null)
                {
                    CopyConfigToEditState();

                    MunitionSettings newSettings = EditState.GetMunitionSettings(newSelectedMunition);
                    // Unnecessary because munition settings have no text boxes so no possible errors.
                    // newSettings.ErrorsChanged += EditState_ErrorsChanged;
                    newSettings.PropertyChanged += EditState_PropertyChanged;
                }
            }
        }

        private void MuniBtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
            {
                EditState.GetMunitionSettings(selectedMunition).Reset();
                SaveEditStateToConfig();
                UpdateUIFromEditState();
            }
        }

        protected override void EditState_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateUIFromEditState();
            base.EditState_PropertyChanged(sender, e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            _dsmsEditorNavArgs = (DSMSEditorNavArgs)args.Parameter;

            if (_munitions.Count > 0)
                uiComboMunition.SelectedIndex = 0;

            base.OnNavigatedTo(_dsmsEditorNavArgs.BaseArgs);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
            {
                MunitionSettings settings = EditState.GetMunitionSettings(selectedMunition);
                settings.ErrorsChanged -= EditState_ErrorsChanged;
                settings.PropertyChanged -= EditState_PropertyChanged;
            }

            base.OnNavigatedFrom(e);
        }
    }
}
