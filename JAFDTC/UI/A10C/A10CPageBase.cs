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

using JAFDTC.Models;
using JAFDTC.Models.A10C;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
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
    /// 5. EditState is the in-memory storage for settings, and property values are copied in and out of SystemConfig,
    ///    the config store. For editors that have more sophisticated needs, relevant methods can be overriden:
    ///    GetControlPropertyHelper, CopyConfigToEditState, SaveEditStateToConfig, and CopyAllSettings.
    ///    
    /// 6. Custom UI update logic can be performed inside the UI Dispatcher delegate by overriding UpdateUI().
    ///    
    /// 7. Derived classes must call the base contructor. They must also call InitializeBase() after
    ///    InitializeComponent().
    /// </summary>
    public abstract class A10CPageBase : SystemEditorPageBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // control and property management
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Find and return the property on the edit state object corresponding to the provided control.
        /// </summary>
        protected override void GetControlEditStateProperty(FrameworkElement control, out PropertyInfo property, out BindableObject editState)
        {
            GetControlPropertyHelper(SettingLocation.Edit, control, out property, out editState);
        }

        /// <summary>
        /// Find and return the property on the persisted configuration object corresponding to the provided control.
        /// </summary>
        protected override void GetControlConfigProperty(FrameworkElement control, out PropertyInfo property, out BindableObject config)
        {
            GetControlPropertyHelper(SettingLocation.Config, control, out property, out config);
        }

        /// <summary>
        /// Finds the corresponding edit-state or config class and property for the provided control.
        /// 
        /// Derived classes should override this method if not every tagged control on the page is backed by the page's common 
        /// EditState and SystemConfig.
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

            property = EditState.GetType().GetProperty(propName);
            if (settingLocation == SettingLocation.Edit)
                configOrEdit = EditState;
            else
                configOrEdit = SystemConfig;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // state management: ui/edit/config copying and saving
        //
        // ------------------------------------------------------------------------------------------------------------

        /// Iterate over all the settings controls via PageTextBoxes, PageComboBoxes, and PageCheckBoxes.
        /// For each control, get the corresponding property and copy its value from source to destination.
        /// 
        /// When checkErrorsInSource is true, an error in the source parameter will skip copying only that value.
        /// 
        /// Derived classes should override if not all controls can be mapped to a property on the EditState and 
        /// SystemConfig classes managed by this base class.
        /// </summary>
        protected override void CopyAllSettings(SettingLocation srcLoc, SettingLocation destLoc, bool checkErrorsInSource = false)
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

        // ---- control event handlers --------------------------------------------------------------------------------

        // use this everywhere?
        protected void TextBox_LosingFocus(UIElement sender, LosingFocusEventArgs _)
        {
            TextBox textBox = (TextBox)sender;
            GetControlEditStateProperty(textBox, out PropertyInfo property, out BindableObject editState);

            if (property != null && editState != null)
                property.SetValue(editState, textBox.Text);

            SaveEditStateToConfig();
        }
    }
}
