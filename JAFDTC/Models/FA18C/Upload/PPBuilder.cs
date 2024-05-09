// ********************************************************************************************************************
//
// PPBuilder.cs -- fa-18c pre-planned command builder
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
using JAFDTC.Models.FA18C.PP;
using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.FA18C.Upload
{
    /// <summary>
    /// command builder for the pre-planned system in the hornet. translates pre-planned programs in FA18CConfiguration
    /// into commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class PPBuilder : FA18CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly Dictionary<int, string> _mapPPNumToOSB;
        private readonly Dictionary<int, string> _mapSTPNumToUFC;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public PPBuilder(FA18CConfiguration cfg, FA18CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb)
        {
            _mapPPNumToOSB = new()
            {
                [1] = "OSB-06",
                [2] = "OSB-07",
                [3] = "OSB-08",
                [4] = "OSB-09",
                [5] = "OSB-10",
                [6] = "OSB-11"
            };
            _mapSTPNumToUFC = new()
            {
                [1] = "Opt1",
                [2] = "Opt2",
                [3] = "Opt3",
                [4] = "Opt4",
                [5] = "Opt5",
            };
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure pre-planned system via the lmfd/ufc according to the non-default programming settings (this
        /// function is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            AirframeDevice lmfd = _aircraft.GetDevice("LMFD");
            AirframeDevice ufc = _aircraft.GetDevice("UFC");

            if (!_cfg.PP.IsDefault)
            {
                Dictionary<Weapons, List<PPStation>> stationGroups = GroupStationsByPayloadType();

                // check if stores in configuration match stores in jet, we'll abort from dcs side if not rather
                // than continue to send button mashes.
                //
                AddWhileBlock("IsLMFDTAC", false, null, delegate () { AddAction(lmfd, "OSB-18"); });  // MENU (TAC)
                AddAction(lmfd, "OSB-05");                                                              // STORES
                foreach (KeyValuePair<Weapons, List<PPStation>> group in stationGroups)
                {
                    string wpnCode = GetDCSWeaponCode(group.Key);
                    foreach (var station in group.Value)
                    {
                        AddIfBlock("IsStationCarriesStore", false, new() { $"{station.Number}", wpnCode }, delegate ()
                        {
                            AddAbort($"ERROR: Station {station.Number} doesn't match configuration");
                        });
                    }
                }
                AddAction(lmfd, "OSB-18");                                                              // MENU

                foreach (KeyValuePair<Weapons, List<PPStation>> group in stationGroups)
                {
                    AddWhileBlock("IsLMFDTAC", false, null, delegate () { AddAction(lmfd, "OSB-18"); });    // MENU (TAC)
                    AddAction(lmfd, "OSB-05");                                                              // STORES
                    AddExecFunction("SelectStore", new() { GetDCSWeaponCode(group.Key) }, WAIT_LONG);

                    // add steerpoints to stations that carry weapons that support steerpoints.
                    //
                    if (group.Key == Weapons.SLAM_ER)
                    {
                        foreach (var station in group.Value)
                        {
                            if (!station.IsDefault && (station.STP.Count > 0))
                            {
                                AddWhileBlock("IsStationSelected", false, new() { $"{station.Number}" }, delegate ()
                                {
                                    AddAction(lmfd, "OSB-13", WAIT_LONG);                               // STEP
                                });
                                BuildSteerpoints(ufc, lmfd, station);
                            }
                        }
                    }

                    // select display osb needed to bring up pre-planned mission page for weapon and ensure that
                    // we're in pp mode, not too.
                    //
                    string dispKey = ((group.Key == Weapons.GBU_38) ||
                                      (group.Key == Weapons.GBU_32) ||
                                      (group.Key == Weapons.GBU_31_V12) ||
                                      (group.Key == Weapons.GBU_31_V34)) ? "OSB-11" : "OSB-12";
                    AddActions(lmfd, new() { dispKey, "OSB-04" }, null, WAIT_BASE);                     // DSPLY, MSN

                    // walk stations adding the target point for each valid pre-planned mission to the station.
                    //
                    foreach (PPStation station in group.Value)
                    {
                        AddWhileBlock("IsInPPStation", false, new() { $"{station.Number}" }, delegate ()
                        {
                            AddAction(lmfd, "OSB-13", WAIT_LONG);                                       // STEP
                        });
                        int lastValidPP = 0;
                        for (int i = 0; i < station.PP.Length; i++)
                        {
                            PPCoordinateInfo pp = station.PP[i];
                            if (pp.IsValid)
                            {
                                AddIfBlock("IsPPSelected", false, new() { $"{i + 1}" }, delegate ()
                                {
                                    AddAction(lmfd, _mapPPNumToOSB[i + 1], WAIT_LONG);
                                });
                                AddIfBlock("IsTargetOfOpportunity", true, null, delegate ()
                                {
                                    AddAction(lmfd, "OSB-05", WAIT_BASE);                               // MODE
                                });
                                AddActions(lmfd, new() { "OSB-14" });                                   // UFC

                                AddAction(ufc, "Opt3", WAIT_BASE);                                      // POSN
                                AddAction(ufc, "Opt1", WAIT_BASE);                                      // LAT
                                BuildCoordinate(ufc, Coord.RemoveLLDegZeroFill(pp.LatUI));

                                AddAction(ufc, "Opt3", WAIT_BASE);                                      // LON
                                BuildCoordinate(ufc, Coord.RemoveLLDegZeroFill(pp.LonUI));

                                AddActions(lmfd, new() { "OSB-14", "OSB-14" }, null, WAIT_BASE);        // UFC, UFC

                                AddAction(ufc, "Opt4", WAIT_BASE);                                      // ELEV
                                AddAction(ufc, "Opt3", WAIT_BASE);                                      // FEET
                                AddActions(ufc, ActionsForString(pp.Alt), new() { "ENT" }, WAIT_BASE);

                                AddActions(lmfd, new() { "OSB-14" });                                   // UFC

                                lastValidPP = i + 1;
                            }
                        }
                        if ((station.BoxedPP != 0) && (station.BoxedPP != lastValidPP))
                        {
                            AddAction(lmfd, _mapPPNumToOSB[station.BoxedPP], WAIT_LONG);
                        }
                    }
                    AddActions(lmfd, new() { "OSB-19" });                                               // RETURN
                }

                AddWhileBlock("IsLMFDTAC", false, null, delegate () { AddAction(lmfd, "OSB-18"); });    // MENU (TAC)
                AddAction(lmfd, "OSB-03");                                                              // HUD
            }
        }

        /// <summary>
        /// add steerpoints for a station carrying a weapon that supports steerpoints. existing steerpoints are
        /// deleted prior to adding.
        /// </summary>
        private void BuildSteerpoints(AirframeDevice ufc, AirframeDevice lmfd, PPStation station)
        {
            AddAction(lmfd, "OSB-11");                                                                  // STP
            for (int i = 1; i <= 5; i++)
            {
                AddAction(ufc, "Opt1", WAIT_BASE);                                                      // STPn
                AddAction(ufc, "Opt5", WAIT_BASE);                                                      // DEL
            }

            int stpProgNumber = 1;
            foreach (PPCoordinateInfo coord in station.STP)
            {
                if (coord.IsValid)
                {
                    AddActions(lmfd, new() { "OSB-11", "OSB-11" }, null, WAIT_BASE);                    // STP, STP
                    AddAction(ufc, _mapSTPNumToUFC[stpProgNumber], WAIT_BASE);

                    if (coord.WaypointNumber == 0)
                    {
                        AddAction(ufc, "Opt3", WAIT_BASE);                                              // POSN
                        AddAction(ufc, "Opt1", WAIT_BASE);                                              // LAT
                        BuildCoordinate(ufc, Coord.RemoveLLDegZeroFill(coord.LatUI));

                        AddAction(ufc, "Opt3", WAIT_BASE);                                              // POSN
                        AddAction(ufc, "Opt3", WAIT_BASE);                                              // LON
                        BuildCoordinate(ufc, Coord.RemoveLLDegZeroFill(coord.LonUI));

                        AddAction(ufc, "Opt4", WAIT_BASE);                                              // ALT
                        AddAction(ufc, "Opt3", WAIT_BASE);                                              // FEET
                        AddActions(ufc, ActionsForString(coord.Alt), new() { "ENT" }, WAIT_BASE);
                    }
                    else
                    {
                        AddAction(ufc, "Opt2");                                                         // WYPT
                        AddActions(ufc, ActionsForString(coord.WaypointNumber.ToString()), new() { "ENT" }, WAIT_BASE);
                    }

                    stpProgNumber++;
                }
            }

            AddAction(lmfd, "OSB-11");                                                                  // STP
        }

        /// <summary>
        /// build command stream to enter a precision coordinate for a target or steerpoint.
        /// </summary>
        private void BuildCoordinate(AirframeDevice ufc, string coord)
        {
            foreach (string key in ActionsFor2864PrecisionCoordString(coord))
            {
                AddAction(ufc, key, (key == "ENT") ? WAIT_BASE : WAIT_NONE);
            }
        }

        /// <summary>
        /// build the list of actions necessary to enter a lat/lon coordinate into the pre-planned system using
        /// precise coodinates. these coordinates use the 2/8/6/4 buttons to enter N/S/E/W directions. coordinate
        /// is specified as a string. prior to processing, all separators are removed. the coordinate string
        /// should start with N/S/E/W followed by the digits and/or characters that should be typed in to the
        /// device. the device must have single-character actions that map to the non-separator characters that
        /// may appear in the coordinate string.
        /// <summary>
        protected static List<string> ActionsFor2864PrecisionCoordString(string coord)
        {
            coord = AdjustNoSeparatorsFloatSafe(coord.Replace(" ", ""));

            List<string> actions = new();
            foreach (char c in coord.ToUpper().ToCharArray())
            {
                switch (c)
                {
                    case 'N': actions.Add("2"); break;
                    case 'S': actions.Add("8"); break;
                    case 'E': actions.Add("6"); break;
                    case 'W': actions.Add("4"); break;
                    case '.': actions.Add("ENT"); break;
                    default: actions.Add(c.ToString()); break;
                }
            }
            actions.Add("ENT");
            return actions;
        }

        /// <summary>
        /// return the weapon code dcs uses for the specified weapon.
        /// </summary>
        private static string GetDCSWeaponCode(Weapons weapon)
            => weapon switch
            {
                Weapons.GBU_38 => "J-82",
                Weapons.GBU_32 => "J-83",
                Weapons.GBU_31_V12 => "J-84",
                Weapons.GBU_31_V34 => "J-109",
                Weapons.JSOW_A => "JSA",
                Weapons.JSOW_C => "JSC",
                Weapons.SLAM => "SLAM",
                Weapons.SLAM_ER => "SLMR",
                _ => null
            };

        /// <summary>
        /// return a dictionary that maps weapons onto a list of stations that carry the weapons.
        /// </summary>
        private Dictionary<Weapons, List<PPStation>> GroupStationsByPayloadType()
        {
            Dictionary<Weapons, List<PPStation>> result = new ();
            foreach (PPStation station in _cfg.PP.Stations.Values)
            {
                if ((station.Weapon != Weapons.NONE) && true /*station.HasAnyPPEnabled()*/)
                {
                    result.TryGetValue(station.Weapon, out var list);
                    if (list == null)
                    {
                        list = new List<PPStation>();
                        result.Add(station.Weapon, list);
                    }
                    list.Add(station);
                }
            }
            return result;
        }
    }
}
