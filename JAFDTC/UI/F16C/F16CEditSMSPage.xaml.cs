// ********************************************************************************************************************
//
// F16CEditSMSPage.xaml.cs : ui c# for viper sms editor page
//
// Copyright(C) 2024 ilominar/raven
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
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.SMS;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static JAFDTC.Models.F16C.SMS.SMSSystem;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class F16CEditSMSPage : SystemEditorPageBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(SMSSystem.SystemTag, "Munitions", "SMS", Glyphs.SMS, typeof(F16CEditSMSPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- TODO

        protected override SystemBase SystemConfig => ((F16CConfiguration)Config).SMS;

        protected override String SystemTag => SMSSystem.SystemTag;

        protected override string SystemName => "SMS munition setup";

        // ---- TODO

        private MunitionSettings EditSettings { get; set; }

        private SMSSystem.Munitions EditMuni { get; set; }

        private string EditProfile { get; set; }

        // ---- TODO

        private readonly List<F16CMunition> _munitions;
        private readonly List<FrameworkElement> _elemsProfile;
        private readonly List<FrameworkElement> _elemsFuze;
        private readonly List<FrameworkElement> _elemsArmDelay;
        private readonly List<FrameworkElement> _elemsArmDelay2;
        private readonly List<FrameworkElement> _elemsSpin;
        private readonly List<FrameworkElement> _elemsAutoPwr;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditSMSPage()
        {
            EditSettings = new MunitionSettings();
            EditMuni = SMSSystem.Munitions.CBU_87;
            EditProfile = "1";

            _munitions = FileManager.LoadF16CMunitions();

            InitializeComponent();
            InitializeBase(EditSettings, uiTextRippleQty, uiPageBtnTxtLink, uiPageTxtLink, uiPageBtnReset);

            _elemsProfile = new() { uiLabelProfile, uiComboProfile };
            _elemsFuze = new() { uiLabelFuze, uiComboFuze };
            _elemsArmDelay = new() { uiLabelArmDelay, uiValueArmDelay, uiLabelArmDelayUnits };
            _elemsArmDelay2 = new() { uiLabelArmDelay2, uiValueArmDelay2, uiLabelArmDelay2Units };
            _elemsSpin = new() { uiLabelSpin, uiComboSpin, uiLabelSpinUnits };
            _elemsAutoPwr = new() { uiLabelAutoPwr, uiComboAutoPwr, uiStackAutoPwr };
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Copy data from the system configuration object to the edit object the page interacts with.
        /// </summary>
        protected override void CopyConfigToEditState()
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            MunitionSettings settings = config.SMS.GetSettingsForMunitionProfile(EditMuni, EditProfile);
            EditSettings.EmplMode = settings.EmplMode;

            UpdateUIFromEditState();
        }

        /// <summary>
        /// Copy data from the edit object the page interacts with to the system configuration object and persist the
        /// updated configuration to disk.
        /// </summary>
        protected override void SaveEditStateToConfig()
        {
            if (!EditState.HasErrors)
            {
                F16CConfiguration config = (F16CConfiguration)Config;
                MunitionSettings settings = config.SMS.GetSettingsForMunitionProfile(EditMuni, EditProfile);
                settings.EmplMode = EditSettings.EmplMode;
                config.SMS.CleanUp();
                config.Save(this, SystemTag);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private static int GetDefaultComboItemFromSpec(string spec)
        {
            List<string> fields = (!string.IsNullOrEmpty(spec)) ? spec.Split(';').ToList() : null;
            return (!string.IsNullOrEmpty(spec)) ? int.Parse(fields[1]) : -1;
        }

        /// <summary>
        /// return a list of TextBlock instances to serve as the menu items for a ComboBox. the list is constructed
        /// from a specification of the form: "{items_csv};{default_index}" where {items_csv} is a csv list of strings
        /// for item names and {default_index} is the index of the default item.
        /// </summary>
        private static IList<TextBlock> BuildComboItemsFromSpec(string spec, out int dfltIndex)
        {
            List<TextBlock> comboItems = new List<TextBlock>();
            if (!string.IsNullOrEmpty(spec))
            {
                List<string> fields = spec.Split(';').ToList();
                List<string> items = fields[0].Split(',').ToList();
                for (int i = 0; i < items.Count; i++)
                    comboItems.Add(new TextBlock() { Text = items[i], Tag = items[i] });

                dfltIndex = int.Parse(fields[1]);
            }
            else
            {
                dfltIndex = -1;
            }
            return comboItems;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void SelectComboItemWIthTag(ComboBox combo, string tag, int dfltIndex)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                FrameworkElement elem = (FrameworkElement)combo.Items[i];
                if ((elem != null) && (elem.Tag != null) && (elem.Tag.ToString() == tag))
                {
                    if (i != combo.SelectedIndex)
                        combo.SelectedIndex = i;
                    return;
                }
            }
            combo.SelectedIndex = dfltIndex;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private Visibility SetVisibility(List<FrameworkElement> elems, string spec)
        {
            Visibility visible = (!string.IsNullOrEmpty(spec)) ? Visibility.Visible : Visibility.Collapsed;
            foreach (FrameworkElement elem in elems)
                elem.Visibility = visible;
            return visible;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void UpdateUIForMunitionChange()
        {
            MunitionSettings muni = ((F16CMunition)uiListMunition.SelectedItem).MunitionInfo;
            uiComboProfile.ItemsSource = BuildComboItemsFromSpec(muni.Profile, out _);
            uiComboEmploy.ItemsSource = BuildComboItemsFromSpec(muni.EmplMode, out _);
            uiComboFuze.ItemsSource = BuildComboItemsFromSpec(muni.Fuze, out _);

            Visibility visible;
            SetVisibility(_elemsProfile, muni.Profile);
            SetVisibility(_elemsFuze, muni.Fuze);
            SetVisibility(_elemsArmDelay, muni.ArmDelay);
            SetVisibility(_elemsArmDelay2, muni.ArmDelay2);
            SetVisibility(_elemsSpin, muni.Spin);
            visible = SetVisibility(_elemsAutoPwr, muni.AutoPwrMode);
            // TODO: hide autopwr sp if supported but disabled...
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        protected override void UpdateUICustom()
        {
            F16CMunition muni = (F16CMunition)uiListMunition.SelectedItem;
            uiTextMuniDesc.Text = (muni != null) ? muni.DescrUI : "No Munition Selected";

            SelectComboItemWIthTag(uiComboProfile, EditProfile, GetDefaultComboItemFromSpec(muni.MunitionInfo.Profile));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui events
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- munition list -----------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void ListMunition_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if ((args.RemovedItems.Count > 0) && ((F16CMunition)args.RemovedItems[0] != null))
                    SaveEditStateToConfig();

            if (args.AddedItems.Count > 0)
            {
                F16CMunition newSelectedMunition = (F16CMunition)args.AddedItems[0];
                if (newSelectedMunition != null)
                {
                    EditMuni = (SMSSystem.Munitions)newSelectedMunition.ID;
                    EditProfile = "1";
                }
            }

            UpdateUIForMunitionChange();
            UpdateUIFromEditState();
        }

        // ---- munition parameters -----------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private void ComboProfile_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            // TODO: save to current profile, move to new profile
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void MuniBtnReset_Click(object sender, RoutedEventArgs args)
        {
            // TODO: reset munition
        }

        // ---- page-level event handlers -----------------------------------------------------------------------------

        /// <summary>
        /// on navigation to the page, select the first muinition from the munition list to get something set up.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            base.OnNavigatedTo(args);

            // TODO: consider preserving selected munition across visits?            
            uiListMunition.SelectedIndex = 0;
        }
    }
}
