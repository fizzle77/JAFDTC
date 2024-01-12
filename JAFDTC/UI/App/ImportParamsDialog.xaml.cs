// ********************************************************************************************************************
//
// ImportParamsDialog.xaml.cs -- ui c# for dialog to grab import parameters
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
using System.Collections.Generic;
using System.Diagnostics;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// dialog to set import parameters including flight (via combobox) and two optional boolean parameters.
    /// </summary>
    public sealed partial class ImportParamsDialog : ContentDialog
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public List<string> Items { get; private set; }

        // ---- public properties, computed

        public string SelectedItem { get => (Items != null) ? (string)uiComboItems.SelectedItem : null; }

        public Dictionary<string, object> Options
        {
            get => new() { ["A"] = uiCkbxValueA.IsChecked, ["B"] = uiCkbxValueB.IsChecked };
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ImportParamsDialog(List<string> items = null, Dictionary<string, string> optTitles = null,
                                  Dictionary<string, object> optDefaults = null)
        {
            InitializeComponent();

            uiCkbxValueA.Visibility = Visibility.Collapsed;
            uiCkbxValueB.Visibility = Visibility.Collapsed;

            Items = items;
            if ((optTitles != null) && optTitles.ContainsKey("A") && (optTitles["A"] != null))
            {
                uiCkbxValueA.Visibility = Visibility.Visible;
                uiCkbxTitleA.Text = optTitles["A"];
                uiCkbxValueA.IsChecked = (bool?)(((optDefaults != null) &&
                                                 optDefaults.ContainsKey("A")) ? optDefaults["A"] : false);
            }
            if ((optTitles != null) && optTitles.ContainsKey("B") && (optTitles["B"] != null))
            {
                uiCkbxValueB.Visibility = Visibility.Visible;
                uiCkbxTitleB.Text = optTitles["B"];
                uiCkbxValueB.IsChecked = (bool?)(((optDefaults != null) &&
                                                 optDefaults.ContainsKey("B")) ? optDefaults["B"] : false);

            }
            uiComboItems.SelectedIndex = (Items.Count > 0) ? 0 : -1;
            IsPrimaryButtonEnabled = ((Items != null) && (Items.Count > 0) && (uiComboItems.SelectedIndex >= 0));
            IsSecondaryButtonEnabled = ((Items != null) && (Items.Count > 0) && (uiComboItems.SelectedIndex >= 0));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interaction
        //
        // ------------------------------------------------------------------------------------------------------------

        private void ComboItems_SelectionChanged(object sender, RoutedEventArgs args)
        {
            IsPrimaryButtonEnabled = ((Items != null) && (Items.Count > 0) && (uiComboItems.SelectedIndex >= 0));
            IsSecondaryButtonEnabled = ((Items != null) && (Items.Count > 0) && (uiComboItems.SelectedIndex >= 0));
        }
    }
}
