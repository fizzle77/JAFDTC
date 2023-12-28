// ********************************************************************************************************************
//
// GetListDialog.xaml.cs -- ui c# for dialog to grab an item from a list of options
//
// Copyright(C) 2023 ilominar/raven
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
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// dialog to request an item from a list of options.
    /// </summary>
    public sealed partial class GetListDialog : ContentDialog
    {
        public List<string> Items { get; private set; }

        public string SelectedItem { get => (Items != null) ? (string)uiComboItems.SelectedItem : null; }

        public GetListDialog(List<string> items = null, string prompt = null, int comboWidth = 0, int selIndex = 0)
        {
            InitializeComponent();

            Items = items;
            if (prompt != null)
            {
                uiTextPrompt.Text = prompt;
            }
            if ((Items != null) && (Items.Count > 0))
            {
                uiComboItems.SelectedIndex = selIndex;
            }
            if (comboWidth > 0)
            {
                uiComboItems.Width = comboWidth;
            }
            IsPrimaryButtonEnabled = ((Items != null) && (Items.Count > 0) && (uiComboItems.SelectedIndex >= 0));
        }

        private void ComboItems_SelectionChanged(object sender, RoutedEventArgs args)
        {
            IsPrimaryButtonEnabled = ((Items != null) && (Items.Count > 0) && (uiComboItems.SelectedIndex >= 0));
        }
    }
}
