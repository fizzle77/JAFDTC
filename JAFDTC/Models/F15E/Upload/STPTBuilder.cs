// ********************************************************************************************************************
//
// STPTBuilder.cs -- f-15e steerpoint command builder
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

using JAFDTC.Models.DCS;
using JAFDTC.Models.F15E.STPT;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F15E.Upload
{
    /// <summary>
    /// command builder for the steerpoint system (including target and referenced points) in the mudhen. translates
    /// steerpoint setup in F15EConfiguration into commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class STPTBuilder : F15EBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public STPTBuilder(F15EConfiguration cfg, F15EDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// <summary>
        public override void Build()
        {
            ObservableCollection<SteerpointInfo> stpts = _cfg.STPT.Points;
            AirframeDevice ufc = _aircraft.GetDevice("UFC_PILOT");

            if (!_cfg.STPT.IsDefault)
            {
                // TODO: fix this...
                // AddIfBlock("GoToFrontCockpit", null, delegate() { });

                AddActions(ufc, new() { "CLR", "CLR", "MENU", "SHF", "3", "PB10", "PB10" });

                BuildSteerpoints(ufc, stpts);

                string number = _cfg.STPT.Points[0].Number.ToString();
                string route = RouteToPB(_cfg.STPT.Points[0].Route);

                AddActions(ufc, new() { "CLR", "CLR", "MENU", number, "SHF", route, "PB10" });
            }
        }

        /// <summary>
        /// TODO: document
        /// <summary>
        private void BuildSteerpoints(AirframeDevice ufc, ObservableCollection<SteerpointInfo> stpts)
        {
            foreach (SteerpointInfo stpt in stpts)
            {
                if (stpt.IsValid)
                {
                    string stptNum = stpt.Number.ToString();
                    string routePB = RouteToPB(stpt.Route);
                    AddActions(ufc, ActionsForString(stptNum), new() { "SHF", routePB, "PB1" });

                    // TODO: check this...
                    AddIfBlock("IsStrDifferent", new() { $"STR {stptNum}{stpt.Route}" }, delegate()
                    {
                        AddActions(ufc, new() { "CLR", "CLR" });
                        AddActions(ufc, ActionsForString(stptNum), new() { ".", "SHF", routePB, "PB1" });
                    });
                    // TODO: check this...
                    AddIfBlock("IsStrDifferent", new() { $"STR {stptNum}{stpt.Route}" }, delegate()
                    {
                        AddActions(ufc, ActionsForString(stptNum), new() { "SHF", routePB, "PB1" });
                    });

                    if (stpt.IsTarget)
                    {
                        AddActions(ufc, ActionsForString(stptNum), new() { ".", "SHF", routePB, "PB1" });
                    }

                    AddActions(ufc, ActionsForMudhen2864CoordinateString(stpt.LatUI), new() { "PB2" });
                    AddActions(ufc, ActionsForMudhen2864CoordinateString(stpt.LonUI), new() { "PB3" });
                    AddActions(ufc, ActionsForString(stpt.Alt), new() { "PB7" });

                    foreach (RefPointInfo rfpt in stpt.RefPoints)
                    {
                        if (rfpt.IsValid)
                        {
                            AddActions(ufc, ActionsForString(stptNum), new() { "." });
                            if (stpt.IsTarget)
                            {
                                AddActions(ufc, ActionsForString("0"));
                            }
                            AddActions(ufc, ActionsForString(rfpt.Number.ToString()), new() { "SHF", routePB, "PB1" });

                            AddActions(ufc, ActionsForMudhen2864CoordinateString(rfpt.LatUI), new() { "PB2" });
                            AddActions(ufc, ActionsForMudhen2864CoordinateString(rfpt.LonUI), new() { "PB3" });
                            AddActions(ufc, ActionsForString(rfpt.Alt), new() { "PB7" });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// return button names corresponding to the given route (a/b/c), these are preceeded by a "SHF".
        /// </summary>
        private static string RouteToPB(string route)
        {
            return route switch
            {
                "B" => "3",
                "C" => "9",
                _ => "1"
            };
        }

        /// <summary>
        /// build the list of actions necessary to enter a lat/lon coordinate into a navpoint system that uses
        /// the 2/8/6/4 buttons to enter N/S/E/W directions. coordinate is specified as a string. prior to processing,
        /// all separators are removed. the coordinate string should start with N/S/E/W followed by the digits
        /// and/or characters that should be typed in to the device. they device must have single-character actions
        /// that map to the non-separator characters that may appear in the coordinate string.
        /// <summary>
        private static List<string> ActionsForMudhen2864CoordinateString(string coord)
        {
            coord = AdjustNoSeparators(coord.Replace(" ", ""));

            List<string> actions = new();
            foreach (char c in coord.ToUpper().ToCharArray())
            {
                switch (c)
                {
                    case 'N': actions.Add("SHF"); actions.Add("2"); break;
                    case 'S': actions.Add("SHF"); actions.Add("8"); break;
                    case 'E': actions.Add("SHF"); actions.Add("6"); break;
                    case 'W': actions.Add("SHF"); actions.Add("4"); break;
                    default: actions.Add(c.ToString()); break;
                }
            }
            return actions;
        }
    }
}
