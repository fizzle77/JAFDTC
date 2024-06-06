// ********************************************************************************************************************
//
// A10CEditDSMSProfileOrderPage.cs :  ui c# for warthog dsms munitions profile page
//
// Copyright(C) 2024 fizzle
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
using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.DSMS;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using static JAFDTC.Models.A10C.DSMS.DSMSSystem;
using static JAFDTC.UI.A10C.A10CEditDSMSPage;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// Content pane for setting A-10 default weapon profile order.
    /// </summary>
    public sealed partial class A10CEditDSMSProfileOrderPage : A10CPageBase
    {
        public override SystemBase SystemConfig => ((A10CConfiguration)Config).DSMS;
        protected override string SystemTag => DSMSSystem.SystemTag;
        protected override string SystemName => "DSMS";

        private DSMSSystem DSMSEditState => (DSMSSystem)EditState;
        private DSMSSystem DSMSConfig => (DSMSSystem)SystemConfig;

        private DSMSEditorNavArgs _dsmsEditorNavArgs;
        private ObservableCollection<A10CMunition> _munitions;

        public A10CEditDSMSProfileOrderPage()
        {
            _munitions = new ObservableCollection<A10CMunition>(A10CMunition.GetUniqueProfileMunitions());
            EditState = new DSMSSystem();

            InitializeComponent();
            InitializeBase(EditState, null, null);
        }

        protected override void SaveEditStateToConfig()
        {
            List<int> newOrder = new List<int>(_munitions.Count);
            foreach (A10CMunition m in _munitions)
                newOrder.Add(m.ID);
            DSMSConfig.ProfileOrder = newOrder;
            Config.Save(_dsmsEditorNavArgs.ParentPage, SystemTag);
        }

        public override void CopyConfigToEditState()
        {
            if (DSMSConfig.ProfileOrder == null)
            {
                _munitions = new ObservableCollection<A10CMunition>(A10CMunition.GetUniqueProfileMunitions());
                uiListProfiles.ItemsSource = _munitions;
            }
            else
            {
                for (int newIndex = 0; newIndex < _munitions.Count; newIndex++)
                {
                    int munitionID = DSMSConfig.ProfileOrder[newIndex];
                    A10CMunition m = A10CMunition.GetMunitionFromID(munitionID);
                    if (m != null)
                    {
                        int oldIndex = _munitions.IndexOf(m);
                        _munitions.Move(oldIndex, newIndex);
                    }
                }
            }

            uiListProfiles.IsEnabled = !Config.IsLinked(SystemTag);
        }

        private void uiListProfiles_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            SaveEditStateToConfig();
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            _dsmsEditorNavArgs = (DSMSEditorNavArgs)args.Parameter;
            base.OnNavigatedTo(_dsmsEditorNavArgs.BaseArgs);
        }
    }
}
