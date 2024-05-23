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

using JAFDTC.Models.A10C;
using JAFDTC.UI.App;
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
    public sealed partial class A10CEditDSMSProfileOrderPage : Page, IA10CDSMSContentFrame
    {
        private DSMSEditorNavArgs _dsmsEditorNavArgs;
        private A10CConfiguration _config;

        private ObservableCollection<A10CMunition> _munitions;

        private bool _suspendUIUpdates = false;

        public A10CEditDSMSProfileOrderPage()
        {
            _munitions = new ObservableCollection<A10CMunition>(A10CMunition.GetUniqueProfileMunitions());

            InitializeComponent();
        }

        private void SaveEditStateToConfig()
        {
            List<int> newOrder = new List<int>(_munitions.Count);
            foreach (A10CMunition m in _munitions)
                newOrder.Add(m.ID);
            _config.DSMS.ProfileOrder = newOrder;
            _config.Save(_dsmsEditorNavArgs.ParentPage, SystemTag);
        }

        public void CopyConfigToEditState()
        {
            if (_suspendUIUpdates)
                return;

            if (_config.DSMS.ProfileOrder == null)
            {
                _munitions = new ObservableCollection<A10CMunition>(A10CMunition.GetUniqueProfileMunitions());
                uiListProfiles.ItemsSource = _munitions;
            }
            else
            {
                for (int newIndex = 0; newIndex < _munitions.Count; newIndex++)
                {
                    int munitionID = _config.DSMS.ProfileOrder[newIndex];
                    A10CMunition m = A10CMunition.GetMunitionFromID(munitionID);
                    if (m != null)
                    {
                        int oldIndex = _munitions.IndexOf(m);
                        _munitions.Move(oldIndex, newIndex);
                    }
                }
            }

            bool isNotLinked = string.IsNullOrEmpty(_config.SystemLinkedTo(SystemTag));
            uiListProfiles.IsEnabled = isNotLinked;
        }

        private void uiListProfiles_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            _suspendUIUpdates = true;
            SaveEditStateToConfig();
            _suspendUIUpdates = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            _dsmsEditorNavArgs = (DSMSEditorNavArgs)args.Parameter;
            _config = (A10CConfiguration)_dsmsEditorNavArgs.NavArgs.Config;

            CopyConfigToEditState();

            base.OnNavigatedTo(args);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }
    }
}
