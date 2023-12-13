// ********************************************************************************************************************
//
// GetNumberDialogResult.xaml.cs -- ui c# for dialog to grab an integer value
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

using JAFDTC.UI;
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
    /// dialog to request a positive integer number within an optional range.
    /// </summary>
    public sealed partial class GetNumberDialog : ContentDialog
    {
        public int Value { get => int.Parse(uiNumberText.Text); }

        private int Min {  get; set; }
        private int Max { get; set; }

        public GetNumberDialog(string header = null, string initial = null, int min = 0, int max = 0)
        {
            InitializeComponent();

            if (header != null)
            {
                uiNumberText.Header = header;
            }
            if (initial != null)
            {
                uiNumberText.Text = initial;
            }
            Min = min;
            Max = max;
            IsPrimaryButtonEnabled = (uiNumberText.Text.Length > 0);
        }

        private void GetNumberDialog_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            Utilities.TextBoxIntValue_TextChanging(sender, args);
            
            IsPrimaryButtonEnabled = ((uiNumberText.Text.Length > 0) &&
                                      ((Min == Max) || ((Min <= Value)) && ((Value <= Max))));
        }
    }
}
