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
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static JAFDTC.Models.F16C.SMS.SMSSystem;
using static System.Net.Mime.MediaTypeNames;

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

        protected override bool IsPageSateDefault => ((F16CConfiguration)Config).SMS.IsDefault;

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

        private static void CopyPropertyHonorDefault(BindableObject next, BindableObject edit, BindableObject dflt,
                                                     string propName)
        {
            if ((propName != null) && (next != null) && (edit != null) && (dflt != null))
            {
                PropertyInfo prop = next.GetType().GetProperty(propName);
                string editVal = (string)prop.GetValue(edit);
                string dfltVal = (string)prop.GetValue(dflt);
                prop.SetValue(next, (!string.IsNullOrEmpty(editVal) && (editVal != dfltVal)) ? editVal : "");
            }
        }

        private static void CopyPropertyHonorDefaultComboVal(BindableObject next, BindableObject edit,
                                                             BindableObject dflt, string propName)
        {
            if ((propName != null) && (next != null) && (edit != null) && (dflt != null))
            {
                PropertyInfo prop = next.GetType().GetProperty(propName);
                string editVal = (string)prop.GetValue(edit);
                string dfltVal = GetDefaultComboItemFromSpec((string)prop.GetValue(dflt));
                prop.SetValue(next, (!string.IsNullOrEmpty(editVal) && (editVal != dfltVal)) ? editVal : "");
            }
        }

        /// <summary>
        /// Copy data from the edit object the page interacts with to the system configuration object and persist the
        /// updated configuration to disk.
        /// </summary>
        protected override void SaveEditStateToConfig()
        {
            if ((EditState != null) && !EditState.HasErrors)
            {
                F16CConfiguration config = (F16CConfiguration)Config;

                // in the event we are enabling IsProfileSelected, ensure the munition has at most one profile with
                // IsProfileSelected set.
                //
                if (EditSetup.IsProfileSelected == "True")
                {
                    Dictionary<string, MunitionSettings> profiles = config.SMS.GetProfilesForMunition(EditMuniID);
                    foreach (MunitionSettings muni in profiles.Values)
                        if (muni.Profile != EditSetup.Profile)
                            muni.IsProfileSelected = "";
                }

                // remap the EditSetup properties based on defaults from _munitions. if the edit settings match the
                // default value, we will update to "". we do this here because it is easier at this point to enforce
                // setting a default field to "" than to determine later when a non-"" field is default.
                //
                MunitionSettings settings = config.SMS.GetSettingsForMunitionProfile(EditMuniID, EditProfileID);
                MunitionSettings defaults = _munitions[(int)EditMuniID].MunitionInfo;

                CopyPropertyHonorDefault(settings, EditState, defaults, "IsProfileSelected");
                CopyPropertyHonorDefault(settings, EditState, defaults, "RipplePulse");
                CopyPropertyHonorDefault(settings, EditState, defaults, "RippleSpacing");
                CopyPropertyHonorDefault(settings, EditState, defaults, "ArmDelay");
                CopyPropertyHonorDefault(settings, EditState, defaults, "ArmDelay2");
                CopyPropertyHonorDefault(settings, EditState, defaults, "BurstAlt");
                CopyPropertyHonorDefault(settings, EditState, defaults, "ReleaseAng");
                CopyPropertyHonorDefault(settings, EditState, defaults, "ImpactAng");
                CopyPropertyHonorDefault(settings, EditState, defaults, "ImpactAzi");
                CopyPropertyHonorDefault(settings, EditState, defaults, "ImpactVel");
                CopyPropertyHonorDefault(settings, EditState, defaults, "CueRange");
                CopyPropertyHonorDefault(settings, EditState, defaults, "AutoPwrSP");

                CopyPropertyHonorDefaultComboVal(settings, EditState, defaults, "EmplMode");
                CopyPropertyHonorDefaultComboVal(settings, EditState, defaults, "ReleaseMode");
                CopyPropertyHonorDefaultComboVal(settings, EditState, defaults, "RippleDelayMode");
                CopyPropertyHonorDefaultComboVal(settings, EditState, defaults, "FuzeMode");
                CopyPropertyHonorDefaultComboVal(settings, EditState, defaults, "ArmDelayMode");
                CopyPropertyHonorDefaultComboVal(settings, EditState, defaults, "Spin");
                CopyPropertyHonorDefaultComboVal(settings, EditState, defaults, "AutoPwrMode");

                config.SMS.CleanUp();
                config.Save(this, SystemTag);
            }
        }

        /// <summary>
        /// Find and return the property information and encapsulating object corresponding to the provided control
        /// in the persisted configuration objects. We need to remap onto the specific munition instance for the
        /// current munition and profile.
        /// </summary>
        protected override void GetControlConfigProperty(FrameworkElement ctrl,
                                                         out PropertyInfo prop, out BindableObject obj)
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            MunitionSettings settings = config.SMS.GetSettingsForMunitionProfile(EditMuniID, EditProfileID);
            prop = settings.GetType().GetProperty(ctrl.Tag.ToString());
            obj = settings;

            if (prop == null)
                throw new ApplicationException($"Unexpected {ctrl.GetType()}: Tag {ctrl.Tag}");
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the the default item in a ComboBox described by a spec of the form "{default_tag};{tags_csv}".
        /// </summary>
        private static string GetDefaultComboItemFromSpec(string spec)
        {
            List<string> fields = (!string.IsNullOrEmpty(spec)) ? spec.Split(';').ToList() : null;
            return (fields != null) ? fields[0] : null;
        }

        /// <summary>
        /// return a list of TextBlock instances to serve as the menu items for a ComboBox. the list is constructed
        /// from a specification of the form: "{default_tag};{tags_csv}" where {tags_csv} is a csv list of strings
        /// for item tags and {default_tag} is the tag of the default item. if textMap is given, it is indexed by
        /// each tag from {tags_csv} to get the text for the item; otherwise, the text and tag are the same.
        /// </summary>
        private static IList<TextBlock> BuildComboItemsFromSpec(string spec, string[] textMap)
        {
            List<TextBlock> comboItems = new();
            if (!string.IsNullOrEmpty(spec))
            {
                List<string> fields = spec.Split(';').ToList();
                List<string> items = fields[1].Split(',').ToList();
                for (int i = 0; i < items.Count; i++)
                    comboItems.Add(new TextBlock()
                    {
                        Text = (textMap != null) ? textMap[int.Parse(items[i])] : items[i],
                        Tag = (items[i] == fields[0]) ? $"+{items[i]}" : items[i]
                    });
            }
            return comboItems;
        }

        /// <summary>
        /// select the item from a ComboBox that has a tag matching the given value. selects the index of the default
        /// item (indicated by a tag with a "+" prefix) if no matching item is found.
        /// </summary>
        private static void SelectComboItemWIthTag(ComboBox combo, string tag)
        {
            int selIndex = 0;
            for (int i = 0; i < combo.Items.Count; i++)
            {
                FrameworkElement elem = (FrameworkElement)combo.Items[i];
                if ((elem != null) && (elem.Tag != null))
                {
                    string elemTag = elem.Tag.ToString();
                    if ((elemTag == tag) || (elemTag == $"+{tag}"))
                    {
                        selIndex = i;
                        break;
                    }
                    else if (elemTag[0] == '+')
                        selIndex = i;
                }
            }
            if (selIndex != combo.SelectedIndex)
                combo.SelectedIndex = selIndex;
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
        /// set the default/non-default icon in the munition list according to whether or not the munitions have a
        /// default configuration. a munition is non-default if it has any non-default profiles.
        /// </summary>
        private void UpdateNonDefaultMunitionIcons()
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            foreach (F16CMunition munition in uiListMunition.Items)
            {
                UIElement container = (UIElement)uiListMunition.ContainerFromItem(munition);
                FontIcon icon = Utilities.FindControl<FontIcon>(container, typeof(FontIcon), "DefaultBadgeIcon");
                if (icon != null)
                {
                    Dictionary<string, MunitionSettings> profiles = config.SMS.GetProfilesForMunition(munition.ID);
                    Visibility visibility = Visibility.Collapsed;
                    foreach (MunitionSettings settings in profiles.Values)
                        if (!settings.IsDefault)
                            visibility = Visibility.Visible;
                    icon.Visibility = visibility;
                }
            }
        }

        /// <summary>
        /// rebuild the core interface for a munition change. this involves setting up initial visibility on the
        /// controls relevant to the selected munition. this function sets up munition-specific state that is *not*
        /// dependent on specific settings (UpdateUICustom() handles state that depends on specific settings).
        /// </summary>
        private void UpdateUIForMunitionChange(F16CMunition newMunition)
        {
            StartUIRebuild();

            SMSSystem.Munitions muniID = newMunition.ID;
            bool isMav = ((muniID == Munitions.AGM_65D) || (muniID == Munitions.AGM_65G) ||
                          (muniID == Munitions.AGM_65H) || (muniID == Munitions.AGM_65K));

            // rebuild the combo items in the profile, employment, release, fuze, spin, and arm delay combos
            // according to the selected munition.
            //
            MunitionSettings info = newMunition.MunitionInfo;
            uiComboProfile.ItemsSource = BuildComboItemsFromSpec(info.Profile, null);
            uiComboEmploy.ItemsSource = BuildComboItemsFromSpec(info.EmplMode, _textForEmplMode);
            uiComboRelMode.ItemsSource = BuildComboItemsFromSpec(info.ReleaseMode, _textForRippleMode);
            uiComboFuzeMode.ItemsSource = BuildComboItemsFromSpec(info.FuzeMode, _textForFuzeMode);
            uiComboSpin.ItemsSource = BuildComboItemsFromSpec(info.Spin, null);
            uiComboArmDelayMode.ItemsSource = BuildComboItemsFromSpec(info.ArmDelayMode, null);

            // set baseline visibility based on the newly selected munition (eg, hide controls that are not
            // relevant and show those that are). this only handles visibility that is a function of the munition,
            // not visibility that is a function of the settings (UpdateUiCusomt() takes care of that).
            //
            SetVisibility(_elemsProfile, info.Profile);
            SetVisibility(_elemsRelease, info.ReleaseMode);
            SetVisibility(_elemsSpin, info.Spin);
            SetVisibility(_elemsFuze, info.FuzeMode);
            SetVisibility(_elemsArmDelay, info.ArmDelay);
            SetVisibility(_elemsArmDelay2, info.ArmDelay2);
            SetVisibility(_elemsArmDelayMode, info.ArmDelayMode);
            SetVisibility(_elemsBurstAlt, info  .BurstAlt);
            SetVisibility(_elemsReleaseAng, info.ReleaseAng);
            SetVisibility(_elemsImpactAng, info.ImpactAng);
            SetVisibility(_elemsImpactAzi, info.ImpactAzi);
            SetVisibility(_elemsImpactVel, info.ImpactVel);
            SetVisibility(_elemsCueRange, info.CueRange);
            SetVisibility(_elemsAutoPwr, info.AutoPwrMode);

            // change release mode label to line up with the way mavs handle ripples versus other munitions.
            //
            uiLabelRelMode.Text = (isMav) ? "Ripple Quantity" : "Release Mode";

            // TODO
            //
            if (!string.IsNullOrEmpty(info.ArmDelay))
            {
                uiValueArmDelay.PlaceholderText = info.ArmDelay;
            }
            if (!string.IsNullOrEmpty(info.BurstAlt))
            {
                uiValueBurstAlt.PlaceholderText = info.BurstAlt;
                uiValueBurstAlt.MaxLength = ((info.ID == Munitions.CBU_87) || (info.ID == Munitions.CBU_97)) ? 5 : 4;
            }
            if (!string.IsNullOrEmpty(info.ReleaseAng))
            {
                uiValueReleaseAng.PlaceholderText = info.ReleaseAng;
                uiValueReleaseAng.MaxLength = (info.ID == Munitions.GBU_24) ? 3 : 2;
            }
            if (!string.IsNullOrEmpty(info.RippleSpacing))
            {
                uiValueRippleFt.PlaceholderText = info.RippleSpacing;
                uiValueRippleFt.MaxLength = ((info.ID == Munitions.CBU_103) || (info.ID == Munitions.CBU_105)) ? 4 : 3;
            }

            FinishUIRebuild();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        protected override void UpdateUICustom(bool isEditable)
        {
            F16CConfiguration config = (F16CConfiguration)Config;

            UpdateNonDefaultMunitionIcons();

            F16CMunition muni = (F16CMunition)uiListMunition.SelectedItem;
            uiTextMuniDesc.Text = (muni != null) ? muni.DescrUI : "No Munition Selected";

            SelectComboItemWIthTag(uiComboProfile, EditProfileID);

            // set the per-munition reset button's enable state based on whether or not there are any changes to any
            // of the munition's profiles along with the editable state passed in..
            //
            Dictionary<string, MunitionSettings> profiles = config.SMS.GetProfilesForMunition(EditMuniID);
            bool isEnabled = false;
            foreach (MunitionSettings settings in profiles.Values)
                if (!settings.IsDefault)
                    isEnabled = isEditable;
            Utilities.SetEnableState(uiMuniBtnReset, isEnabled);

            // set up visibility of the ripple-related fields (quantity, spacing, and delay) based on the current
            // release mode selected in the settings.
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
            uiStackAutoPwr.Visibility = ((EditSetup.AutoPwrModeEnum != MunitionSettings.AutoPowerModes.Unknown) &&
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
                    CopyConfigToEditState();
                    UpdateUIForMunitionChange(newSelectedMunition);
                }
            }

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
