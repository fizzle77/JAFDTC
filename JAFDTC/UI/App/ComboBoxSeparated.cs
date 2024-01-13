// ********************************************************************************************************************
//
// ComboBoxSeperated.cs -- custom ComboBox class for the combo boxes with disabled separators.
//
// Copyright(C) 2023-2024 ilominar/raven
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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// custom ComboBox class for combo box lists with disabled separators. to include a separator, add a
    /// ComboBoxViewItemSeparator item to the combo box's items. this item will be set disabled when the control
    /// content is being built.
    /// 
    /// add <Style TargetType="ui:ComboBoxSeperated" BasedOn="{StaticResource DefaultComboBoxStyle}" /> to the
    /// page resources to get the control style to match defaults.
    /// </summary>
    public class ComboBoxSeparated : ComboBox
    {
        /// <summary>
        /// prepare the item container for an item by updating the IsEnabled property of the element based on the type
        /// of item we are building for. ComboBoxViewItemSeparator instances are always disabled, all others are
        /// always enabled.
        /// </summary>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            ((ComboBoxItem)element).IsEnabled = (item.GetType() == typeof(ComboBoxViewItemSeparator)) ? false : true;
        }
    }
}
