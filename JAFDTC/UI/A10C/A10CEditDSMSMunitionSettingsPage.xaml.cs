using JAFDTC.Models.DCS;
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


namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// UI Content for the A-10 DSMS Munition Settings
    /// </summary>
    public sealed partial class A10CEditDSMSMunitionSettingsPage : Page
    {
        public List<Munition> Munitions;

        public A10CEditDSMSMunitionSettingsPage()
        {
            this.InitializeComponent();
            Munitions = FileManager.LoadMunitions();
        }

        private void UpdateMunitionSettings(Munition selectedMunition)
        {
            uiTextLaserCode.IsEnabled = selectedMunition.Laser;
            uiCheckAutoLase.IsEnabled = selectedMunition.Laser;
            uiTextLaseTime.IsEnabled = selectedMunition.Laser;
        }

        private void ComboMunition_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMunitionSettings((Munition)e.AddedItems[0]);
        }
    }
}
