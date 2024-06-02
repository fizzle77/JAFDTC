// ********************************************************************************************************************
//
// F16CEditMFDPage.xaml.cs : ui c# for viper misc setup editor page
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

using JAFDTC.Models;
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.Misc;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// system editor page to handle the ui for the miscellaneous systems in the viper.
    /// </summary>
    public sealed partial class F16CEditMiscPage : SystemEditorPageBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(MiscSystem.SystemTag, "Miscellaneous", "Miscellaneous", Glyphs.MISC, typeof(F16CEditMiscPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPage properties

        protected override SystemBase SystemConfig => ((F16CConfiguration)Config).Misc;

        protected override String SystemTag => MiscSystem.SystemTag;

        protected override string SystemName => "miscellaneous";

        protected override bool IsPageSateDefault => ((F16CConfiguration)Config).Misc.IsDefault;

        // ---- internal properties

        private MiscSystem EditMisc { get; set; }
        
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditMiscPage()
        {
            EditMisc = new MiscSystem();

            InitializeComponent();
            InitializeBase(EditMisc, uiBINGOValueBINGO, uiPageBtnTxtLink, uiPageTxtLink, uiPageBtnReset);

            MiscSystem miscSysDefault = MiscSystem.ExplicitDefaults;
            uiBINGOValueBINGO.PlaceholderText = miscSysDefault.Bingo;
            uiBULLValueSP.PlaceholderText = miscSysDefault.BullseyeWP;
            uiALOWValueCARAALOW.PlaceholderText = miscSysDefault.ALOWCARAALOW;
            uiALOWValueMSLFLOOR.PlaceholderText = miscSysDefault.ALOWMSLFloor;
            uiLASRValueTGP.PlaceholderText = "1688";
            uiLASRValueLST.PlaceholderText = "1688";
            uiLASRValueTime.PlaceholderText = miscSysDefault.LaserStartTime;
            uiTACANValueChan.PlaceholderText = miscSysDefault.TACANChannel;
            uiILSValueFreq.PlaceholderText = miscSysDefault.ILSFrequency;
            uiILSValueCourse.PlaceholderText = miscSysDefault.ILSCourse;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui events
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- jhmcs setup -------------------------------------------------------------------------------------------

        /// <summary>
        /// hmcs intensity slider changed: update the backing state and user interface.
        /// </summary>
        private void HMCSSliderIntensity_ValueChanged(object sender, RoutedEventArgs args)
        {
            Slider slider = (Slider)sender;
            EditMisc.HMCSIntensity = (slider.Value == 0.0) ? "" : $"{slider.Value / 100.0:F2}";
            SaveEditStateToConfig();
        }
    }
}
