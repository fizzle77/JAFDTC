// ********************************************************************************************************************
//
// STPTBuilder.cs -- f-16c steerpoint command builder
//
// Copyright(C) 2021-2023 the-paid-actor & others
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
using JAFDTC.Models.F16C.STPT;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using Windows.ApplicationModel.Activation;

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

        public STPTBuilder(F16CConfiguration cfg, F16CCommands dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

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
            Device ufc = _aircraft.GetDevice("UFC");

            if (stpts.Count > 0)
            {
                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("RTN"));

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
        private void BuildWaypoints(Device ufc, Dictionary<string, SteerpointInfo> jetStpts, int dZ)
        {
            AppendCommand(ufc.GetCommand("LIST"));
            AppendCommand(ufc.GetCommand("1"));
            AppendCommand(ufc.GetCommand("SEQ"));

            foreach (KeyValuePair<string, SteerpointInfo> kv in jetStpts)
            {
                string stptId = kv.Key;
                SteerpointInfo stpt = kv.Value;

                if (stpt.IsValid)
                {
                    PredAppendDigitsWithEnter(ufc, stptId);
                    AppendCommand(ufc.GetCommand("DOWN"));

                    AppendCommand(Build2864Coordinate(ufc, stpt.LatUI));
                    AppendCommand(ufc.GetCommand("ENTR"));
                    AppendCommand(ufc.GetCommand("DOWN"));

                    AppendCommand(Build2864Coordinate(ufc, stpt.LonUI));
                    AppendCommand(ufc.GetCommand("ENTR"));
                    AppendCommand(ufc.GetCommand("DOWN"));

                    PredAppendDigitsWithEnter(ufc, stpt.Alt, true);
                    AppendCommand(ufc.GetCommand("DOWN"));

                    PredAppendDigitsNoSepWithEnter(ufc, AdjustHMSTOSForZulu(stpt.TOS, dZ));
                    AppendCommand(ufc.GetCommand("DOWN"));

                    if ((stpt.OAP[0].Type == RefPointTypes.OAP) || (stpt.OAP[1].Type == RefPointTypes.OAP))
                    {
                        AppendCommand(ufc.GetCommand("SEQ"));
                        if (stpt.OAP[0].Type == RefPointTypes.OAP)
                        {
                            BuildOA(ufc, stptId, stpt.OAP[0].Range, stpt.OAP[0].Brng, stpt.OAP[0].Elev);
                        }
                        AppendCommand(ufc.GetCommand("SEQ"));
                        if (stpt.OAP[1].Type == RefPointTypes.OAP)
                        {
                            BuildOA(ufc, stptId, stpt.OAP[1].Range, stpt.OAP[1].Brng, stpt.OAP[1].Elev);
                        }
                        AppendCommand(ufc.GetCommand("SEQ"));
                        AppendCommand(ufc.GetCommand("SEQ"));
                    }
                }
            }

            AppendCommand(ufc.GetCommand("1"));
            AppendCommand(ufc.GetCommand("ENTR"));
            AppendCommand(ufc.GetCommand("RTN"));
        }

        /// <summary>
        /// build the set of commands necessary to enter a single oap into the steerpoint system.
        /// <summary>
        private void BuildOA(Device ufc, string stptNum, string range, string brng, string elev)
        {
            PredAppendDigitsWithEnter(ufc, stptNum);
            AppendCommand(ufc.GetCommand("DOWN"));

            PredAppendDigitsNoSepWithEnter(ufc, range);
            AppendCommand(ufc.GetCommand("DOWN"));

            PredAppendDigitsNoSepWithEnter(ufc, brng);
            AppendCommand(ufc.GetCommand("DOWN"));

            PredAppendDigitsNoSepWithEnter(ufc, elev, true);
            AppendCommand(ufc.GetCommand("DOWN"));
        }

        /// <summary>
        /// build the set of commands necessary to enter an vip into the steerpoint system.
        /// <summary>
        private void BuildVIP(Device ufc, Dictionary<string, SteerpointInfo> jetStpts)
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
                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("LIST"));
                AppendCommand(ufc.GetCommand("3"));

                AppendCommand(Wait());

                AppendCommand(StartCondition("VIP_TO_TGT_NotSelected"));
                AppendCommand(ufc.GetCommand("SEQ"));
                AppendCommand(EndCondition("VIP_TO_TGT_NotSelected"));

                AppendCommand(StartCondition("VIP_TO_TGT_NotHighlighted"));
                AppendCommand(ufc.GetCommand("0"));
                AppendCommand(EndCondition("VIP_TO_TGT_NotHighlighted"));

                BuildVIPDetail(ufc, stptNum, stpt.VxP[0].Range, stpt.VxP[0].Brng, stpt.VxP[0].Elev);
                AppendCommand(ufc.GetCommand("SEQ"));

                AppendCommand(StartCondition("VIP_TO_PUP_NotHighlighted"));
                AppendCommand(ufc.GetCommand("0"));
                AppendCommand(EndCondition("VIP_TO_PUP_NotHighlighted"));

                BuildVIPDetail(ufc, stptNum, stpt.VxP[1].Range, stpt.VxP[1].Brng, stpt.VxP[1].Elev);

                // TODO: not needed?
                // AppendCommand(ufc.GetCommand("SEQ"));
                AppendCommand(ufc.GetCommand("RTN"));
            }
        }

        /// <summary>
        /// build the set of commands necessary to enter a single relative point (range, bearing, elev) for a vip into
        /// the steerpoint system.
        /// <summary>
        private void BuildVIPDetail(Device ufc, string stptNum, string range, string brng, string elev)
        {
            AppendCommand(ufc.GetCommand("DOWN"));
            PredAppendDigitsWithEnter(ufc, stptNum);
            AppendCommand(ufc.GetCommand("DOWN"));

            PredAppendDigitsNoSepWithEnter(ufc, brng);
            AppendCommand(ufc.GetCommand("DOWN"));

            PredAppendDigitsNoSepWithEnter(ufc, range);
            AppendCommand(ufc.GetCommand("DOWN"));

            PredAppendDigitsNoSepWithEnter(ufc, elev, true);
            AppendCommand(ufc.GetCommand("DOWN"));
        }

        /// <summary>
        /// build the set of commands necessary to enter an vrp into the steerpoint system.
        /// <summary>
        private void BuildVRP(Device ufc, Dictionary<string, SteerpointInfo> jetStpts)
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
                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("LIST"));
                AppendCommand(ufc.GetCommand("9"));

                AppendCommand(Wait());

                AppendCommand(StartCondition("TGT_TO_VRP_NotSelected"));
                AppendCommand(ufc.GetCommand("SEQ"));
                AppendCommand(EndCondition("TGT_TO_VRP_NotSelected"));

                AppendCommand(StartCondition("TGT_TO_VRP_NotHighlighted"));
                AppendCommand(ufc.GetCommand("0"));
                AppendCommand(EndCondition("TGT_TO_VRP_NotHighlighted"));

                BuildVRPDetail(ufc, stptNum, stpt.VxP[0].Range, stpt.VxP[0].Brng, stpt.VxP[0].Elev);
                AppendCommand(ufc.GetCommand("SEQ"));

                AppendCommand(StartCondition("TGT_TO_PUP_NotHighlighted"));
                AppendCommand(ufc.GetCommand("0"));
                AppendCommand(EndCondition("TGT_TO_PUP_NotHighlighted"));

                BuildVRPDetail(ufc, stptNum, stpt.VxP[1].Range, stpt.VxP[1].Brng, stpt.VxP[1].Elev);
                AppendCommand(ufc.GetCommand("SEQ"));
                AppendCommand(ufc.GetCommand("RTN"));
            }
        }

        /// <summary>
        /// build the set of commands necessary to enter a single relative point (range, bearing, elev) for a vrp into
        /// the steerpoint system.
        /// <summary>
        private void BuildVRPDetail(Device ufc, string stptNum, string range, string brng, string elev)
        {
            AppendCommand(ufc.GetCommand("DOWN"));
            AppendCommand(BuildDigits(ufc, stptNum));
            AppendCommand(ufc.GetCommand("ENTR"));
            AppendCommand(ufc.GetCommand("DOWN"));

            PredAppendDigitsNoSepWithEnter(ufc, brng);
            AppendCommand(ufc.GetCommand("DOWN"));

            PredAppendDigitsNoSepWithEnter(ufc, range);
            AppendCommand(ufc.GetCommand("DOWN"));

            PredAppendDigitsNoSepWithEnter(ufc, elev, true);
            AppendCommand(ufc.GetCommand("DOWN"));
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
