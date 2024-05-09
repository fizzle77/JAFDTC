// ********************************************************************************************************************
//
// STPTBuilder.cs -- f-16c steerpoint command builder
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
using JAFDTC.Models.F16C.STPT;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// command builder for the steerpoint system in the viper. translates cmds setup in F16CConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class STPTBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public STPTBuilder(F16CConfiguration cfg, F16CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure steerpoint system via the icp/ded according to the non-default programming settings (this
        /// function is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            ObservableCollection<SteerpointInfo> stpts = _cfg.STPT.Points;
            AirframeDevice ufc = _aircraft.GetDevice("UFC");

            if (stpts.Count > 0)
            {
                AddActions(ufc, new() { "RTN", "RTN" });

                int dZ = -GetZuluDelta(stpts[0]);       // negate to get offset from local to zulu

                Dictionary<string, SteerpointInfo> jetStpts = new();
                for (var i = 0; i < stpts.Count; i++)
                {
                    SteerpointInfo stpt = stpts[i];
                    jetStpts.Add(stpt.Number.ToString(), stpt);
                }

                BuildWaypoints(ufc, jetStpts, dZ);
                BuildVIP(ufc, jetStpts);
                BuildVRP(ufc, jetStpts);
            }
        }

        /// <summary>
        /// add the set of waypoints (given by a dictionary that maps steerpoint_number:steerpoint) to the current
        /// set of navigation points using the ufc. this will enter both the steerpoint as well as any oap's tied
        /// to the steerpoint.
        /// <summary>
        private void BuildWaypoints(AirframeDevice ufc, Dictionary<string, SteerpointInfo> jetStpts, int dZ)
        {
            AddActions(ufc, new() { "LIST", "1", "SEQ" });

            foreach (KeyValuePair<string, SteerpointInfo> kv in jetStpts)
            {
                string stptId = kv.Key;
                SteerpointInfo stpt = kv.Value;

                if (stpt.IsValid)
                {
                    string tos = AdjustHMSForZulu(stpt.TOS, dZ);

                    AddActions(ufc, PredActionsForNumAndEnter(stptId), new() { "DOWN" });
                    AddActions(ufc, ActionsFor2864CoordinateString(stpt.LatUI), new() { "ENTR", "DOWN" });
                    AddActions(ufc, ActionsFor2864CoordinateString(stpt.LonUI), new() { "ENTR", "DOWN" });
                    AddActions(ufc, PredActionsForNumAndEnter(stpt.Alt), new() { "DOWN" });
                    AddActions(ufc, PredActionsForNumAndEnter(tos, false, true), new() { "DOWN" });

                    if ((stpt.OAP[0].Type == RefPointTypes.OAP) || (stpt.OAP[1].Type == RefPointTypes.OAP))
                    {
                        AddAction(ufc, "SEQ");
                        if (stpt.OAP[0].Type == RefPointTypes.OAP)
                        {
                            BuildOA(ufc, stptId, stpt.OAP[0].Range, stpt.OAP[0].Brng, stpt.OAP[0].Elev);
                        }
                        AddAction(ufc, "SEQ");
                        if (stpt.OAP[1].Type == RefPointTypes.OAP)
                        {
                            BuildOA(ufc, stptId, stpt.OAP[1].Range, stpt.OAP[1].Brng, stpt.OAP[1].Elev);
                        }
                        AddActions(ufc, new() { "SEQ", "SEQ" });
                    }
                }
            }
            AddActions(ufc, new() { "1", "ENTR", "RTN" });
        }

        /// <summary>
        /// build the set of commands necessary to enter a single oap into the steerpoint system.
        /// <summary>
        private void BuildOA(AirframeDevice ufc, string stptNum, string range, string brng, string elev)
        {
            AddActions(ufc, PredActionsForNumAndEnter(stptNum), new() { "DOWN" });
            AddActions(ufc, PredActionsForNumAndEnter(range, false, true), new() { "DOWN" });
            AddActions(ufc, PredActionsForNumAndEnter(brng, false, true), new() { "DOWN" });
            AddActions(ufc, PredActionsForNumAndEnter(elev, false, true), new() { "DOWN" });
        }

        /// <summary>
        /// build the set of commands necessary to enter an vip into the steerpoint system.
        /// <summary>
        private void BuildVIP(AirframeDevice ufc, Dictionary<string, SteerpointInfo> jetStpts)
        {
            string stptNum = null;
            SteerpointInfo stpt = null;

            foreach (KeyValuePair<string, SteerpointInfo> kvp in jetStpts)
            {
                if (kvp.Value.VxP[0].Type == RefPointTypes.VIP)
                {
                    stptNum = kvp.Key;
                    stpt = kvp.Value;
                    break;
                }
            }
            if (stptNum != null)
            {
                AddActions(ufc, new() { "RTN", "RTN", "LIST", "3" }, null, WAIT_BASE);

                AddIfBlock("IsI2TNotSelected", true, null, delegate () { AddAction(ufc, "SEQ"); });
                AddIfBlock("IsI2TNotHighlighted", true, null, delegate () { AddAction(ufc, "0"); });
                BuildVIPDetail(ufc, stptNum, stpt.VxP[0].Range, stpt.VxP[0].Brng, stpt.VxP[0].Elev);
                AddAction(ufc, "SEQ");

                AddIfBlock("IsI2PNotHighlighted", true, null, delegate () { AddAction(ufc, "0"); });
                BuildVIPDetail(ufc, stptNum, stpt.VxP[1].Range, stpt.VxP[1].Brng, stpt.VxP[1].Elev);

                // TODO: not needed?
                // AddAction(ufc, "SEQ");
                AddAction(ufc, "RTN");
            }
        }

        /// <summary>
        /// build the set of commands necessary to enter a single relative point (range, bearing, elev) for a vip into
        /// the steerpoint system.
        /// <summary>
        private void BuildVIPDetail(AirframeDevice ufc, string stptNum, string range, string brng, string elev)
        {
            AddAction(ufc, "DOWN");
            AddActions(ufc, PredActionsForNumAndEnter(stptNum), new() { "DOWN" });
            AddActions(ufc, PredActionsForNumAndEnter(brng, false, true), new() { "DOWN" });
            AddActions(ufc, PredActionsForNumAndEnter(range, false, true), new() { "DOWN" });
            AddActions(ufc, PredActionsForNumAndEnter(elev, false, true), new() { "DOWN" });
        }

        /// <summary>
        /// build the set of commands necessary to enter an vrp into the steerpoint system.
        /// <summary>
        private void BuildVRP(AirframeDevice ufc, Dictionary<string, SteerpointInfo> jetStpts)
        {
            string stptNum = null;
            SteerpointInfo stpt = null;

            foreach (KeyValuePair<string, SteerpointInfo> kvp in jetStpts)
            {
                if (kvp.Value.VxP[0].Type == RefPointTypes.VRP)
                {
                    stptNum = kvp.Key;
                    stpt = kvp.Value;
                }
            }
            if (stptNum != null)
            {
                AddActions(ufc, new() { "RTN", "RTN", "LIST", "9" }, null, WAIT_BASE);

                AddIfBlock("IsT2RNotSelected", true, null, delegate () { AddAction(ufc, "SEQ"); });
                AddIfBlock("IsT2RNotHighlighted", true, null, delegate () { AddAction(ufc, "0"); });

                BuildVRPDetail(ufc, stptNum, stpt.VxP[0].Range, stpt.VxP[0].Brng, stpt.VxP[0].Elev);
                AddAction(ufc, "SEQ");

                AddIfBlock("IsT2PNotHighlighted", true, null, delegate () { AddAction(ufc, "0"); });

                BuildVRPDetail(ufc, stptNum, stpt.VxP[1].Range, stpt.VxP[1].Brng, stpt.VxP[1].Elev);
                AddActions(ufc, new() { "SEQ", "RTN" });
            }
        }

        /// <summary>
        /// build the set of commands necessary to enter a single relative point (range, bearing, elev) for a vrp into
        /// the steerpoint system.
        /// <summary>
        private void BuildVRPDetail(AirframeDevice ufc, string stptNum, string range, string brng, string elev)
        {
            AddAction(ufc, "DOWN");
            AddActions(ufc, PredActionsForNumAndEnter(stptNum), new() { "DOWN" });
            AddActions(ufc, PredActionsForNumAndEnter(brng, false, true), new() { "DOWN" });
            AddActions(ufc, PredActionsForNumAndEnter(range, false, true), new() { "DOWN" });
            AddActions(ufc, PredActionsForNumAndEnter(elev, true, true), new() { "DOWN" });
        }

        /// <summary>
        /// return the zulu offset for the steerpoint, 0 if location is unknown. uses the poi database to determine
        /// the theater and from there the proper offset. local_time = zulu + GetZuluDelta().
        /// </summary>
        private static int GetZuluDelta(SteerpointInfo stpt)
        {
            if (double.TryParse(stpt.Lat, out double lat) && double.TryParse(stpt.Lon, out double lon))
            {
                return PointOfInterestDbase.TheaterForCoords(lat, lon) switch
                {
                    "Marianas"          => 10,  // UTC +10
                    "Caucasus"          => 4,   // UTC +4
                    "Persian Gulf"      => 4,   // UTC +4
                    "Sinai"             => 3,   // UTC +3
                    "Syria"             => 3,   // UTC +3
                    "South Atlantic"    => -3,  // UTC -3
                    "Nevada"            => -8,  // UTC -8
                    _                   => 0
                };
            }
            return 0;
        }
    }
}
