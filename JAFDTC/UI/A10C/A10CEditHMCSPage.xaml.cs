// ********************************************************************************************************************
//
// A10CEditHMCSPage.xaml.cs : ui c# for warthog hmcs page
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
using JAFDTC.Models.A10C.HMCS;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Reflection;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// Code-behind class for the A10 HMCS editor.
    /// </summary>
    public sealed partial class A10CEditHMCSPage : A10CPageBase
    {
        private const string SYSTEM_NAME = "HMCS";

        private HMCSSystem EditState => (HMCSSystem)_editState;
        public override A10CSystemBase SystemConfig => _config.HMCS;

        public static ConfigEditorPageInfo PageInfo
            => new(HMCSSystem.SystemTag, SYSTEM_NAME, SYSTEM_NAME, Glyphs.HMCS, typeof(A10CEditHMCSPage));

        public A10CEditHMCSPage() : base(SYSTEM_NAME, HMCSSystem.SystemTag)
        {
            InitializeComponent();
            InitializeBase(new HMCSSystem(), uiTextFlightMembers, uiCtlLinkResetBtns);
        }

        // ---- UI helpers  -------------------------------------------------------------------------------------------

        protected override void SaveEditStateToConfig()
        {
            if (IsProfileSelectionValid(out HMCSProfileSettings profileEditState))
                SaveEditStateToConfig(profileEditState);
        }

        private void SaveEditStateToConfig(HMCSProfileSettings profileEditState)
        {
            if (EditState.HasErrors || profileEditState.HasErrors)
                return;

            CopyAllSettings(profileEditState, SettingLocation.Edit, SettingLocation.Config);
            _config.Save(this, HMCSSystem.SystemTag);
        }

        protected override void CopyConfigToEditState()
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
        /// Iterate over all the settings controls via PageComboBoxes, PageTextBoxes, and PageCheckBoxes.
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
        /// If there is a valid selection for the current profile to edit, set selectedProfileEditState and return true.
        /// </summary>
        private bool IsProfileSelectionValid(out HMCSProfileSettings selectedProfileEditState)
        {
            Grid g = (Grid)uiComboEditProfile.SelectedItem;
            if (g == null)
                selectedProfileEditState = null;
            else
                selectedProfileEditState = EditState.GetProfileSettings((string)g.Tag);
            return selectedProfileEditState != null;
        }

        protected override void UpdateUICustom()
        {
            uiProfile1SelectIcon.Visibility = Utilities.HiddenIfDefault(_config.HMCS.GetProfileSettings(Profiles.PRO1));
            uiProfile2SelectIcon.Visibility = Utilities.HiddenIfDefault(_config.HMCS.GetProfileSettings(Profiles.PRO2));
            uiProfile3SelectIcon.Visibility = Utilities.HiddenIfDefault(_config.HMCS.GetProfileSettings(Profiles.PRO3));
        }

        protected override void GetControlPropertyHelper(
            SettingLocation settingLocation, 
            FrameworkElement control, 
            out PropertyInfo property,
            out BindableObject configOrEdit)
        {
            base.GetControlPropertyHelper(settingLocation, control, out property, out configOrEdit);
            if (property == null)
            {
                string propName = control.Tag.ToString();
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
                HMCSProfileSettings oldSettings = EditState.GetProfileSettings((string)((Grid)e.RemovedItems[0]).Tag);
                if (oldSettings != null)
                {
                    oldSettings.ErrorsChanged -= BaseField_DataValidationError;
                    
                    // BindableObject remembers errors but not the value that caused them. So changing this
                    // back to a profile with an error, you get a red box on a good value. Clearing the error
                    // state is far from ideal, but slightly less mysterious user behavior.
                    oldSettings.ClearErrors();
                }
            }

            if (e.AddedItems.Count > 0)
            {
                HMCSProfileSettings newSettings = EditState.GetProfileSettings((string)((Grid)e.AddedItems[0]).Tag);
                if (newSettings != null)
                {
                    newSettings.ErrorsChanged += BaseField_DataValidationError;

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

        // ---- field validation -------------------------------------------------------------------------------------------

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

        protected override void BaseField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            base.BaseField_DataValidationError(sender, args);
            if (IsProfileSelectionValid(out HMCSProfileSettings selectedProfileEditState))
                ValidateEditState(selectedProfileEditState, args.PropertyName);
        }

        // ---- page events -------------------------------------------------------------------------------------------

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
    }
}
