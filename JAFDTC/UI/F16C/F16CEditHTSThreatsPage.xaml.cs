// ********************************************************************************************************************
//
// F16CEditHTSThreatsPage.cs : ui c# for viper hts threat editor page
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

using JAFDTC.Models.DCS;
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.HTS;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// navigation argument for pages that navigate to the hts threat editor. this provides the configuration being
    /// edited along with parent editor.
    /// </summary>
    internal sealed class F16CEditHTSThreatsPageNavArgs
    {
        public F16CEditHTSPage ParentEditor { get; set; }

        public F16CConfiguration Config { get; set; }

        public bool IsUnlinked { get; set; }

        public F16CEditHTSThreatsPageNavArgs(F16CEditHTSPage parentEditor, F16CConfiguration config, bool isUnlinked)
            => (ParentEditor, Config, IsUnlinked) = (parentEditor, config, isUnlinked);
    }

    /// <summary>
    /// Emitter instance set up for appearence in the emitter list view. this element is bound with one item per hst
    /// table class.
    /// </summary>
    internal sealed class EmitterListItem : BindableObject
    {
        public bool IsEnabled { get; set; }

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        public string HTSTable { get; set; }

        // following properties will be multi-line for the n emitters that map to the hst table.

        public string ALICCode { get; set; }

        public string F16RWR { get; set; }

        public string Country { get; set; }

        public string Type { get; set; }

        public string Name { get; set; }                // combines Emitter.Name, Emitter.NATO

        public EmitterListItem(string htsTable = "", string name = "")
            => (HTSTable, ALICCode, F16RWR, Country, Type, Name, IsEnabled) = (htsTable, "", "", "", "", name, true);
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class F16CEditHTSThreatsPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private F16CEditHTSThreatsPageNavArgs NavArgs { get; set; }

        // NOTE: changes to the Config object may only occur through the marshall methods. bindings to and edits by
        // NOTE: the ui are always directed at the EditEmitterList property.
        //
        private F16CConfiguration Config { get; set; }

        ObservableCollection<EmitterListItem> EditEmitterList { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditHTSThreatsPage()
        {
            InitializeComponent();

            EditEmitterList = new ObservableCollection<EmitterListItem>();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        // marshall data between our local state and the hts configuration. note we only handle the EnabledThreats
        // table here.
        //
        private void CopyConfigToEdit()
        {
            EditEmitterList[^1].IsChecked = Config.HTS.EnabledThreats[0];       // man table
            for (int i = 1; i < EditEmitterList.Count; i++)                     // tables 1-11
            {
                EditEmitterList[i-1].IsChecked = Config.HTS.EnabledThreats[i];
            }
        }

        private void CopyEditToConfig(bool isPersist = false)
        {
            Config.HTS.EnabledThreats[0] = EditEmitterList[^1].IsChecked;       // man table
            for (int i = 1; i < EditEmitterList.Count; i++)                     // tables 1-11
            {
                Config.HTS.EnabledThreats[i] = EditEmitterList[i - 1].IsChecked;
            }

            if (isPersist)
            {
                Config.Save(NavArgs.ParentEditor, HTSSystem.SystemTag);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // utilities
        //
        // ------------------------------------------------------------------------------------------------------------

        private static string PrepareToAddEmitter(EmitterListItem item)
        {
            if (string.IsNullOrEmpty(item.ALICCode))
            {
                item.Name = "";
                return "";
            }
            return "\n";
        }

        // add emitter information to an existing emitter list item. we accumulate into the item with one emitter per
        // line of text.
        //
        private static void AddEmitterToEmitterListItem(bool isEnabled, EmitterListItem item, Emitter emitter)
        {
            item.IsEnabled = isEnabled;
            string sep = PrepareToAddEmitter(item);
            item.ALICCode += sep + emitter.ALICCode.ToString();
            item.F16RWR += sep + emitter.F16RWR.ToString();
            item.Country += sep + emitter.Country.ToString();
            item.Type += sep + emitter.Type.ToString();
            item.Name += sep + emitter.Name.ToString();
            if (!string.IsNullOrEmpty(emitter.NATO))
            {
                item.Name += $" ({emitter.NATO})";
            }
        }

        // build the content of the standard threat tables (1-11) and install it in the ui state.
        //
        private void BuildThreatTables()
        {
            List<Emitter> dbEmitters = EmitterDbase.Instance.Find();
            dbEmitters.Sort(delegate (Emitter a, Emitter b)
            {
                int result = string.Compare(a.Country, b.Country);
                if (result == 0)
                {
                    result = a.ALICCode - b.ALICCode;
                    if (result == 0)
                    {
                        result = string.Compare(a.Name, b.Name); ;
                    }
                }
                return result;
            });

            Dictionary<int, EmitterListItem> threatTableMap = new();
            for (int i = 1; i < Config.HTS.EnabledThreats.Length; i++)
            {
                threatTableMap.Add(i, new EmitterListItem(i.ToString(), "No Emitters Defined in this Threat Class"));
            }
            foreach (Emitter emitter in dbEmitters)
            {
                AddEmitterToEmitterListItem(NavArgs.IsUnlinked, threatTableMap[emitter.HTSTable], emitter);
            }

            EmitterListItem item = new("MAN", "No Emitters Defined in this Threat Class");
            for (int row = 0; row < Config.HTS.MANTable.Count; row++)
            {
                if (!string.IsNullOrEmpty(Config.HTS.MANTable[row].Code))
                {
                    dbEmitters = EmitterDbase.Instance.Find(int.Parse(Config.HTS.MANTable[row].Code));
                    foreach (Emitter emitter in dbEmitters)
                    {
                        AddEmitterToEmitterListItem(NavArgs.IsUnlinked, item, emitter);
                    }
                    if (dbEmitters.Count == 0)
                    {
                        string sep = PrepareToAddEmitter(item);
                        item.ALICCode += $"{sep}{Config.HTS.MANTable[row].Code}";
                        item.F16RWR += $"{sep}–";
                        item.Country += $"{sep}–";
                        item.Type += $"{sep}–";
                        item.Name += $"{sep} Unknown Emitter";
                    }
                }
            }

            EditEmitterList.Clear();
            for (int threatClass = 1; threatClass < Config.HTS.EnabledThreats.Length; threatClass++)
            {
                EditEmitterList.Add(threatTableMap[threatClass]);
            }
            EditEmitterList.Add(item);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- list buttons ------------------------------------------------------------------------------------------

        // on set threats click: enable all threat classes.
        //
        private void ListBtnSet_Click(object sender, RoutedEventArgs args)
        {
            foreach (EmitterListItem item in EditEmitterList)
            {
                item.IsChecked = true;
            }
        }

        // on clear threats click: disable all threat classes.
        //
        private void ListBtnClear_Click(object sender, RoutedEventArgs args)
        {
            foreach (EmitterListItem item in EditEmitterList)
            {
                item.IsChecked = false;
            }
        }

        // ---- page level buttons ------------------------------------------------------------------------------------

        // done button click: copy our state into the configuration and return to the previous page.
        //
        private void PageBtnDone_Click(object sender, RoutedEventArgs args)
        {
            CopyEditToConfig(true);
            Frame.GoBack();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        // on navigating to this page, set up and tear down our internal and ui state based on the configuration we
        // are editing.
        //
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            NavArgs = (F16CEditHTSThreatsPageNavArgs)args.Parameter;
            Config = NavArgs.Config;

            BuildThreatTables();
            CopyConfigToEdit();

            uiListBtnSet.IsEnabled = NavArgs.IsUnlinked;
            uiListBtnClear.IsEnabled = NavArgs.IsUnlinked;

            base.OnNavigatedTo(args);
        }
    }
}
