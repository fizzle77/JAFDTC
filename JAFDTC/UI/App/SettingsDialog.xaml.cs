// ********************************************************************************************************************
//
// SettingsDialog.xaml.cs -- ui c# for settings dialog
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

using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// TODO: docuemnt
    /// </summary>
    public sealed partial class SettingsDialog : ContentDialog
    {
        public string WingName { get; set; }

        public string Callsign { get; set; }

        public bool IsAppOnTop { get; set; }

        public bool IsLuaInstallRequested { get; set; }
        
        public bool ISLuaUninstallRequested { get; set; }

        public SettingsDialog()
        {
            InitializeComponent();

            WingName = Settings.WingName;
            Callsign = Settings.Callsign;
            IsAppOnTop = Settings.IsAlwaysOnTop;
            IsLuaInstallRequested = false;
            ISLuaUninstallRequested = false;

            uiSetValueWingName.Text = WingName;
            uiSetValueCallsign.Text = Callsign;
            uiSetCkbxRemainOnTop.IsChecked = IsAppOnTop;

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

        private void SetCkbxRemainOnTop_Click(object sender, RoutedEventArgs e)
        {
            CheckBox ckbox = (CheckBox)sender;
            if (ckbox != null)
            {
                IsAppOnTop = (bool)ckbox.IsChecked;
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
