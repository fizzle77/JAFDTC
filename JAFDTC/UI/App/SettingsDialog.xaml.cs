// ********************************************************************************************************************
//
// SettingsDialog.xaml.cs -- ui c# for settings dialog
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

using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// TODO: docuemnt
    /// </summary>
    public sealed partial class SettingsDialog : ContentDialog
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public string WingName { get; set; }

        public string Callsign { get; set; }

        public SettingsData.UploadFeedbackTypes UploadFeedback { get; set; }

        public bool IsNavPtImportIgnoreAirframe { get; set; }

        public bool IsAppOnTop { get; set; }

        public bool IsNewVersCheckDisabled { get; set; }

        public bool IsLuaInstallRequested { get; set; }
        
        public bool ISLuaUninstallRequested { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public SettingsDialog()
        {
            InitializeComponent();

            WingName = Settings.WingName;
            Callsign = Settings.Callsign;
            UploadFeedback = Settings.UploadFeedback;
            IsNavPtImportIgnoreAirframe = Settings.IsNavPtImportIgnoreAirframe;
            IsAppOnTop = Settings.IsAlwaysOnTop;
            IsNewVersCheckDisabled = Settings.IsNewVersCheckDisabled;
            IsLuaInstallRequested = false;
            ISLuaUninstallRequested = false;

            uiSetValueWingName.Text = WingName;
            uiSetValueCallsign.Text = Callsign;
            uiSetComboFeedback.SelectedIndex = (int)UploadFeedback;
            uiSetCkbxNPIgnoresAirframe.IsChecked = IsNavPtImportIgnoreAirframe;
            uiSetCkbxRemainOnTop.IsChecked = IsAppOnTop;
            uiSetCkbxVersionCheck.IsChecked = !IsNewVersCheckDisabled;

            if (DCSLuaManager.IsLuaInstalled())
            {
                uiSetBtnInstall.IsEnabled = false;
                uiSetBtnUninstall.IsEnabled = true;
            }
            else
            {
                uiSetBtnInstall.IsEnabled = true;
                uiSetBtnUninstall.IsEnabled = false;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        private void SetValueWingName_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tbox = (TextBox)sender;
            if (tbox != null)
            {
                WingName = tbox.Text;
            }
        }

        private void SetValueCallsign_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tbox = (TextBox)sender;
            if (tbox != null)
            {
                Callsign = tbox.Text;
            }
        }

        private void SetComboFeedback_SelectionChanged(object sender, RoutedEventArgs e)
        {
            ComboBox cbox = (ComboBox)sender;
            if (cbox != null)
            {
                UploadFeedback = (SettingsData.UploadFeedbackTypes)cbox.SelectedIndex;
            }
        }

        private void SetCkbxNPIgnoresAirframe_Click(object sender, RoutedEventArgs e)
        {
            CheckBox ckbox = (CheckBox)sender;
            if (ckbox != null)
            {
                IsNavPtImportIgnoreAirframe = (bool)ckbox.IsChecked;
            }
        }

        private void SetCkbxRemainOnTop_Click(object sender, RoutedEventArgs e)
        {
            CheckBox ckbox = (CheckBox)sender;
            if (ckbox != null)
            {
                IsAppOnTop = (bool)ckbox.IsChecked;
            }
        }

        private void SetCkbxVersionCheck_Click(object sender, RoutedEventArgs e)
        {
            CheckBox ckbox = (CheckBox)sender;
            if (ckbox != null)
            {
                IsNewVersCheckDisabled = !(bool)ckbox.IsChecked;
            }
        }

        private void SetBtnInstall_Click(object sender, RoutedEventArgs e)
        {
            IsLuaInstallRequested = true;
            Hide();
        }

        private void SetBtnUninstall_Click(object sender, RoutedEventArgs e)
        {
            ISLuaUninstallRequested = true;
            Hide();
        }
    }
}
