// ********************************************************************************************************************
//
// F15EEditUFCPage.xaml.cs : ui c# for mudhen ufc setup editor page
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
using JAFDTC.Models.F15E;
using JAFDTC.Models.F15E.UFC;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using System;
using System.Diagnostics;

namespace JAFDTC.UI.F15E
{
    /// <summary>
    /// system editor page to handle the ui for the mudhen ufc system configuration.
    /// </summary>
    public sealed partial class F15EEditUFCPage : SystemEditorPageBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(UFCSystem.SystemTag, "Up-Front Controls", "UFC", Glyphs.UFC, typeof(F15EEditUFCPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPage properties

        protected override SystemBase SystemConfig => ((F15EConfiguration)Config).UFC;

        protected override String SystemTag => UFCSystem.SystemTag;

        protected override string SystemName => "UFC";

        // ---- private properties

        private UFCSystem EditUFC { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F15EEditUFCPage()
        {
            EditUFC = new UFCSystem();

            InitializeComponent();
            InitializeBase(EditUFC, uiTACANValueChan, uiCtlLinkResetBtns);

            UFCSystem ufcSysDefault = UFCSystem.ExplicitDefaults;
            uiLaltValueWarn.PlaceholderText = ufcSysDefault.LowAltWarn;
            uiTACANValueChan.PlaceholderText = ufcSysDefault.TACANChannel;
            uiILSValueFreq.PlaceholderText = ufcSysDefault.ILSFrequency;
        }
    }
}
