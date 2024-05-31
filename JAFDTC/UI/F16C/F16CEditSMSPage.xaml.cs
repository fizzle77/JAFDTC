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
using Windows.ApplicationModel.Background;
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

        private MunitionSettings EditSetup { get; set; }

        private SMSSystem.Munitions EditMuniID { get; set; }

        private string EditProfileID { get; set; }

        // ---- TODO

        private readonly List<F16CMunition> _munitions;
        private readonly string[] _textForEmplMode;
        private readonly string[] _textForRippleMode;
        private readonly string[] _textForFuzeMode;
        
        private readonly List<FrameworkElement> _elemsProfile;
        private readonly List<FrameworkElement> _elemsRelease;
        private readonly List<FrameworkElement> _elemsSpin;
        private readonly List<FrameworkElement> _elemsFuze;
        private readonly List<FrameworkElement> _elemsArmDelay;
        private readonly List<FrameworkElement> _elemsArmDelay2;
        private readonly List<FrameworkElement> _elemsArmDelayMode;
        private readonly List<FrameworkElement> _elemsBurstAlt;
        private readonly List<FrameworkElement> _elemsReleaseAng;
        private readonly List<FrameworkElement> _elemsImpactAng;
        private readonly List<FrameworkElement> _elemsImpactAzi;
        private readonly List<FrameworkElement> _elemsImpactVel;
        private readonly List<FrameworkElement> _elemsCueRange;
        private readonly List<FrameworkElement> _elemsAutoPwr;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditSMSPage()
        {
            EditSetup = new MunitionSettings();
            EditMuniID = SMSSystem.Munitions.CBU_87;
            EditProfileID = "1";

            _munitions = FileManager.LoadF16CMunitions();

            _textForEmplMode = new[] { "CCIP", "CCRP", "DTOS", "LADD", "MAN", "PRE", "VIS", "BORE" };
            _textForRippleMode = new[] { "SGL", "PAIR", "Single", "Front/Back", "Left/Right", "1", "2",
                                         "SGL", "RP 1", "RP 2", "RP 3", "RP 4" };
            _textForFuzeMode = new[] { "NSTL", "NOSE", "TAIL", "NSTL (HI)", "NOSE (LO)", "TAIL (HI)" };

            InitializeComponent();
            InitializeBase(EditSetup, uiValueRippleQty, uiPageBtnTxtLink, uiPageTxtLink, uiPageBtnReset);

            _elemsProfile = new() { uiLabelProfile, uiComboProfile, uiCkboxProfileEnb };
            _elemsRelease = new() { uiLabelRelMode, uiComboRelMode, null, uiStackRelMode };
            _elemsSpin = new() { uiLabelSpin, uiComboSpin, uiLabelSpinUnits };
            _elemsFuze = new() { uiLabelFuzeMode, uiComboFuzeMode };
            _elemsArmDelay = new() { uiLabelArmDelay, uiValueArmDelay, uiLabelArmDelayUnits };
            _elemsArmDelay2 = new() { uiLabelArmDelay2, uiValueArmDelay2, uiLabelArmDelay2Units };
            _elemsArmDelayMode = new() { uiLabelArmDelayMode, uiComboArmDelayMode, uiLabelArmDelayModeUnits };
            _elemsBurstAlt = new() { uiLabelBurstAlt, uiValueBurstAlt, uiLabelBurstAltUnits };
            _elemsReleaseAng = new() { uiLabelReleaseAng, uiValueReleaseAng, uiLabelReleaseAngUnits };
            _elemsImpactAng = new() { uiLabelImpactAng, uiValueImpactAng, uiLabelImpactAngUnits };
            _elemsImpactAzi = new() { uiLabelImpactAzi, uiValueImpactAzi, uiLabelImpactAziUnits };
            _elemsImpactVel = new() { uiLabelImpactVel, uiValueImpactVel, uiLabelImpactVelUnits };
            _elemsCueRange = new() { uiLabelCueRng, uiValueCueRng, uiLabelCueRngUnits };
            _elemsAutoPwr = new() { uiLabelAutoPwr, uiComboAutoPwr, null, uiStackAutoPwr };
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
            MunitionSettings settings = config.SMS.GetSettingsForMunitionProfile(EditMuniID, EditProfileID);
            EditSetup.Profile = new(settings.Profile);
            EditSetup.IsProfileSelected = new(settings.IsProfileSelected);
            EditSetup.EmplMode = new(settings.EmplMode);
            EditSetup.ReleaseMode = new(settings.ReleaseMode);
            EditSetup.AutoPwrMode = new(settings.AutoPwrMode);
            EditSetup.AutoPwrSP = new(settings.AutoPwrSP);

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
                MunitionSettings settings = config.SMS.GetSettingsForMunitionProfile(EditMuniID, EditProfileID);
                settings.IsProfileSelected = new(EditSetup.IsProfileSelected);
                // TODO: need to make sure this is mutex...
                settings.EmplMode = new(EditSetup.EmplMode);
                settings.ReleaseMode = new(EditSetup.ReleaseMode);
                settings.AutoPwrMode = new(EditSetup.AutoPwrMode);
                settings.AutoPwrSP = new(EditSetup.AutoPwrSP);

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
            return (!string.IsNullOrEmpty(spec)) ? int.Parse(fields[0]) : -1;
        }

        /// <summary>
        /// return a list of TextBlock instances to serve as the menu items for a ComboBox. the list is constructed
        /// from a specification of the form: "{default_index};{tags_csv}" where {tags_csv} is a csv list of strings
        /// for item tags and {default_index} is the index of the default item. if textMap is given, it is indexed
        /// by each tag from {tags_csv} to get the text for the item; otherwise, the text and tag are the same.
        /// </summary>
        private static IList<TextBlock> BuildComboItemsFromSpec(string spec, string[] textMap)
        {
            List<TextBlock> comboItems = new();
            if (!string.IsNullOrEmpty(spec))
            {
                List<string> fields = spec.Split(';').ToList();
                List<string> items = fields[1].Split(',').ToList();
                for (int i = 0; i < items.Count; i++)
                    comboItems.Add(new TextBlock() { Text = (textMap != null) ? textMap[int.Parse(items[i])] : items[i],
                                                     Tag = items[i] });
            }
            return comboItems;
        }

        /// <summary>
        /// select the item from a ComboBox that has a tag matching the given value. selects the default index if no
        /// such item is found.
        /// </summary>
        private static void SelectComboItemWIthTag(ComboBox combo, string tag, int dfltIndex)
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
        /// sets the visibility of FrameworkElements in a list according to the value of a spec string from munition
        /// information. elements are hidden if the spec string is null/empty or they follow a null item in the list,
        /// visible otherwise.
        /// </summary>
        private static Visibility SetVisibility(List<FrameworkElement> elems, string spec)
        {
            Visibility visible = (!string.IsNullOrEmpty(spec)) ? Visibility.Visible : Visibility.Collapsed;
            foreach (FrameworkElement elem in elems)
            {
                if (elem != null)
                    elem.Visibility = visible;
                else
                    visible = Visibility.Collapsed;
            }
            return visible;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void UpdateUIForMunitionChange()
        {
            StartUIRebuild();

            SMSSystem.Munitions muniID = ((F16CMunition)uiListMunition.SelectedItem).ID;
            bool isMav = ((muniID == Munitions.AGM_65D) || (muniID == Munitions.AGM_65G) ||
                          (muniID == Munitions.AGM_65H) || (muniID == Munitions.AGM_65K));

            MunitionSettings muni = ((F16CMunition)uiListMunition.SelectedItem).MunitionInfo;
            uiComboProfile.ItemsSource = BuildComboItemsFromSpec(muni.Profile, null);
            uiComboEmploy.ItemsSource = BuildComboItemsFromSpec(muni.EmplMode, _textForEmplMode);
            uiComboRelMode.ItemsSource = BuildComboItemsFromSpec(muni.ReleaseMode, _textForRippleMode);
            uiComboFuzeMode.ItemsSource = BuildComboItemsFromSpec(muni.FuzeMode, _textForFuzeMode);
            uiComboSpin.ItemsSource = BuildComboItemsFromSpec(muni.Spin, null);
            uiComboArmDelayMode.ItemsSource = BuildComboItemsFromSpec(muni.ArmDelayMode, null);

            // set baseline visibility based on the munition configuration. note that UpdateUICustom() will take care
            // of additional visibility changes based on current settings of the configuration.
            //
            SetVisibility(_elemsProfile, muni.Profile);
            SetVisibility(_elemsRelease, muni.ReleaseMode);
            SetVisibility(_elemsSpin, muni.Spin);
            SetVisibility(_elemsFuze, muni.FuzeMode);
            SetVisibility(_elemsArmDelay, muni.ArmDelay);
            SetVisibility(_elemsArmDelay2, muni.ArmDelay2);
            SetVisibility(_elemsArmDelayMode, muni.ArmDelayMode);
            SetVisibility(_elemsBurstAlt, muni.BurstAlt);
            SetVisibility(_elemsReleaseAng, muni.ReleaseAng);
            SetVisibility(_elemsImpactAng, muni.ImpactAng);
            SetVisibility(_elemsImpactAzi, muni.ImpactAzi);
            SetVisibility(_elemsImpactVel, muni.ImpactVel);
            SetVisibility(_elemsCueRange, muni.CueRange);
            SetVisibility(_elemsAutoPwr, muni.AutoPwrMode);

            if (isMav)
            {
                uiLabelRelMode.Text = "Ripple Quantity";
                uiStackRelMode.Visibility = Visibility.Collapsed;
            }
            else
            {
                uiLabelRelMode.Text = "Release Mode";
            }

            // TODO: this needs to be fixed, ripple spacing for cbus is inconsistent with other ripples
            if (!string.IsNullOrEmpty(muni.RippleSpacing))
                uiValueRippleFt.PlaceholderText = muni.RippleSpacing;

            FinishUIRebuild();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        protected override void UpdateUICustom()
        {
            SMSSystem.Munitions muniID = ((F16CMunition)uiListMunition.SelectedItem).ID;
            bool isMav = ((muniID == Munitions.AGM_65D) || (muniID == Munitions.AGM_65G) ||
                          (muniID == Munitions.AGM_65H) || (muniID == Munitions.AGM_65K));

            F16CMunition muni = (F16CMunition)uiListMunition.SelectedItem;
            uiTextMuniDesc.Text = (muni != null) ? muni.DescrUI : "No Munition Selected";

            SelectComboItemWIthTag(uiComboProfile, EditProfileID, GetDefaultComboItemFromSpec(muni.MunitionInfo.Profile));

            // set up the ripple fields (quantity, spacing, and delay) based on the current release mode.
            //
            switch (EditSetup.ReleaseModeEnum)
            {
                case MunitionSettings.ReleaseModes.PAIR:
                    uiStackRelMode.Visibility = Visibility.Visible;
                    uiLabelRippleQty.Visibility = Visibility.Visible;
                    uiValueRippleQty.Visibility = Visibility.Visible;
                    uiValueRippleFt.Visibility = Visibility.Visible;
                    uiLabelRippleFtUnits.Visibility = Visibility.Visible;
                    uiComboRippleDt.Visibility = Visibility.Collapsed;
                    uiLabelRippleDtUnits.Visibility = Visibility.Collapsed;
                    break;
                case MunitionSettings.ReleaseModes.TRI_PAIR_F2B:
                case MunitionSettings.ReleaseModes.TRI_PAIR_L2R:
                    uiStackRelMode.Visibility = Visibility.Visible;
                    uiLabelRippleQty.Visibility = Visibility.Collapsed;
                    uiValueRippleQty.Visibility = Visibility.Collapsed;
                    uiValueRippleFt.Visibility = Visibility.Visible;
                    uiLabelRippleFtUnits.Visibility = Visibility.Visible;
                    uiComboRippleDt.Visibility = Visibility.Collapsed;
                    uiLabelRippleDtUnits.Visibility = Visibility.Collapsed;
                    break;
                case MunitionSettings.ReleaseModes.GBU24_RP1:
                case MunitionSettings.ReleaseModes.GBU24_RP2:
                case MunitionSettings.ReleaseModes.GBU24_RP3:
                case MunitionSettings.ReleaseModes.GBU24_RP4:
                    uiStackRelMode.Visibility = Visibility.Visible;
                    uiLabelRippleQty.Visibility = Visibility.Collapsed;
                    uiValueRippleQty.Visibility = Visibility.Collapsed;
                    uiValueRippleFt.Visibility = Visibility.Collapsed;
                    uiLabelRippleFtUnits.Visibility = Visibility.Collapsed;
                    uiComboRippleDt.Visibility = Visibility.Visible;
                    uiLabelRippleDtUnits.Visibility = Visibility.Visible;
                    break;
                default:
                    uiStackRelMode.Visibility = Visibility.Collapsed;
                    break;
            }

            // auto power steerpoint fields are only visible if auto power mode is not off.
            //
            uiStackAutoPwr.Visibility = ((EditSetup.AutoPwrModeEnum != MunitionSettings.AutoPowerModes.UNKNOWN) &&
                                         (EditSetup.AutoPwrModeEnum != MunitionSettings.AutoPowerModes.OFF))
                                        ? Visibility.Visible : Visibility.Collapsed;
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
                    EditMuniID = (SMSSystem.Munitions)newSelectedMunition.ID;
                    EditProfileID = "1";
                    //
                    // ClearErrors() here as the assignments in CopyConfigToEditState() won't get the job done as the
                    // fields are in their last valid state and errors are only cleared in BindableObjects when the
                    // values are changing.
                    //
                    EditSetup.ClearErrors();
                    CopyConfigToEditState();
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
        /// reset munition click: reset the current munition/profile and persist the configuration.
        /// </summary>
        private void MuniBtnReset_Click(object sender, RoutedEventArgs args)
        {
            EditSetup.Reset();
            SaveEditStateToConfig();
            UpdateUIFromEditState();
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
