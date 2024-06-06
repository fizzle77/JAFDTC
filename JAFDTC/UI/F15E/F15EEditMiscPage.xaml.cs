// ********************************************************************************************************************
//
// F15EEditMFDPage.xaml.cs : ui c# for mudhen misc setup editor page
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
using JAFDTC.Models.F15E.Misc;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;

namespace JAFDTC.UI.F15E
{
    /// <summary>
    /// system editor page to handle the ui for the mudhen miscellaneous system configuration.
    /// </summary>
    public sealed partial class F15EEditMiscPage : SystemEditorPageBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(MiscSystem.SystemTag, "Miscellaneous", "Miscellaneous", Glyphs.MISC, typeof(F15EEditMiscPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPage properties

        public override SystemBase SystemConfig => ((F15EConfiguration)Config).Misc;

        protected override String SystemTag => MiscSystem.SystemTag;

        protected override string SystemName => "miscellaneous";

        // ---- internal properties

        private MiscSystem EditMisc { get; set; }

        private F15EConfiguration.CrewPositions EditCrewMember { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F15EEditMiscPage()
        {
            EditMisc = new MiscSystem();

            InitializeComponent();
            InitializeBase(EditMisc, uiBINGOValueBINGO, uiCtlLinkResetBtns);

            uiBINGOValueBINGO.PlaceholderText = MiscSystem.ExplicitDefaults.Bingo;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Copy data from the system configuration object to the edit object the page interacts with.
        /// </summary>
        public override void CopyConfigToEditState()
        {
            if (EditState != null)
            {
                EditState.ClearErrors();
                CopyAllSettings(SettingLocation.Config, SettingLocation.Edit);

                F15EConfiguration config = (F15EConfiguration)Config;
                EditCrewMember = config.CrewMember;

                UpdateUIFromEditState();
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// change the selected crew member and update various ui and model state.
        /// </summary>
        private void SelectCrewMember(F15EConfiguration.CrewPositions member)
        {
            if (member == (int)F15EConfiguration.CrewPositions.PILOT)
            {
                uiGridPilotRow.Visibility = Visibility.Visible;
                uiGridWizzoRow.Visibility = Visibility.Collapsed;
            }
            else
            {
                uiGridPilotRow.Visibility = Visibility.Collapsed;
                uiGridWizzoRow.Visibility = Visibility.Visible;
            }
            EditCrewMember = member;
            SaveEditStateToConfig();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on aux command invoked, update the state of the editor based on the command.
        /// </summary>
        private void AuxCommandInvokedHandler(object sender, ConfigAuxCommandInfo args)
        {
            SaveEditStateToConfig();

            F15EConfiguration config = (F15EConfiguration)Config;
            SelectCrewMember(config.CrewMember);
        }

        // ---- page-level event handlers -----------------------------------------------------------------------------

        /// <summary>
        /// on navigation to the page, select the crewmember set up in the config.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            base.OnNavigatedTo(args);

            NavArgs.ConfigPage.AuxCommandInvoked += AuxCommandInvokedHandler;

            SelectCrewMember(EditCrewMember);
        }

        /// <summary>
        /// on navigating from this page, tear down our internal and ui state.
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            NavArgs.ConfigPage.AuxCommandInvoked -= AuxCommandInvokedHandler;

            base.OnNavigatedFrom(args);
        }
    }
}
