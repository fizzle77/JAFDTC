// ********************************************************************************************************************
//
// DSMSBuilder.cs -- a-10c dsms system builder
//
// Copyright(C) 2024 fizzle, JAFDTC contributors
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

using JAFDTC.Models.A10C.DSMS;
using JAFDTC.Models.DCS;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.A10C.Upload
{
    /// <summary>
    /// command builder for the DSMS systems in the warthog. translates cmds setup in A10CConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal partial class DSMSBuilder : A10CBuilderBase, IBuilder
    {
        LoadoutQueryBuilder _loadoutQuery;
        DSMSProfileQueryBuilder _profileQuery;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public DSMSBuilder(A10CConfiguration cfg, A10CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) 
        {
            StringBuilder sbLoadoutQuery = new StringBuilder();
            _loadoutQuery = new LoadoutQueryBuilder(dcsCmds, sbLoadoutQuery, "QueryLoadout", null);

            StringBuilder sbProfileQuery = new StringBuilder();
            _profileQuery = new DSMSProfileQueryBuilder(dcsCmds, sbProfileQuery, "QueryDSMSProfiles", null);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void Build()
        {
            if (_cfg.DSMS.IsDefault)
                return;

            AirframeDevice lmfd = _aircraft.GetDevice("LMFD");
            AirframeDevice cdu = _aircraft.GetDevice("CDU");

            AddIfBlock("IsDSMSInDefaultMFDPosition", true, null, delegate () { BuildDSMS(cdu, lmfd); });
            // TODO handle non-default DSMS
        }

        private void BuildDSMS(AirframeDevice cdu, AirframeDevice lmfd)
        {
            Build_INV(cdu, lmfd);
            Build_DefaultProfiles(cdu, lmfd);
            Build_DefaultProfileOrder(lmfd);
        }

        private void Build_INV(AirframeDevice cdu, AirframeDevice lmfd)
        {
            // get configs that require INV changes
            Dictionary<string, MunitionSettings> nonDefaultSettings = _cfg.DSMS.GetNonDefaultInvSettings();
            if (nonDefaultSettings == null || nonDefaultSettings.Count == 0)
                return;

            AddActions(lmfd, new() { "LMFD_14", "LMFD_05" }, null, WAIT_BASE); // Go to DSMS INV

            Dictionary<int, bool> _setupStations = GetStationIsSetupMap();
            // attempt symmetric loads on the first 5 stations
            for (int station = 1; station <= 5; station++)
            {
                int symLoadedStation = Build_INV_station(cdu, lmfd, nonDefaultSettings, station, true);
                _setupStations[station] = true;
                if (symLoadedStation > 0)
                    _setupStations[symLoadedStation] = true;
            }
            // station 6 has no symmetric station, don't attempt sym load
            Build_INV_station(cdu, lmfd, nonDefaultSettings, 6, false);
            _setupStations[6] = true;
            // set up any stations that weren't symloaded with stations 1-5
            for (int station = 7; station <= 11; station++)
            {
                if (!_setupStations[station])
                {
                    Build_INV_station(cdu, lmfd, nonDefaultSettings, station, false);
                }
            }
        }


        /// <summary>
        /// Sets the INV page settings for one station.
        /// </summary>
        /// <param name="cdu"></param>
        /// <param name="lmfd"></param>
        /// <param name="nonDefaultSettings">All the non-default INV settings</param>
        /// <param name="station">The A-10 pylon to setup</param>
        /// <param name="attemptSymLoad">Do symmetric load of the opposite pylon if it has the same munition loaded</param>
        /// <returns>Station number of a symloaded station if there was one, otherwise -1.</returns>
        private int Build_INV_station(AirframeDevice cdu, AirframeDevice lmfd, 
            Dictionary<string, MunitionSettings> nonDefaultSettings, int station, bool attemptSymLoad)
        {
            string munitionAtStation = _loadoutQuery.StationMunitionMap[station];
            if (munitionAtStation == null)
                return -1; // pylon is empty
            
            if (nonDefaultSettings.TryGetValue(munitionAtStation, out MunitionSettings setting))
            {
                // station, inv stat
                AddActions(lmfd, new() { GetButtonForStation(station), "LMFD_03" }, null, WAIT_BASE);

                // Laser Code
                // There's a single laser code setting for the whole DSMS system that applies to any Laser-capable weapon.
                if (setting.Munition.Laser && !_cfg.DSMS.IsLaserCodeDefault)
                {
                    foreach (char c in _cfg.DSMS.LaserCode)
                        AddAction(cdu, c.ToString());
                    // The button for setting laser code moves depending on the munition. Grab it from the data.
                    AddAction(lmfd, "LMFD_" + setting.Munition.LaserButton);
                }

                // HOF
                if (setting.Munition.HOF && !setting.IsHOFOptionDefault)
                {
                    int hofVal = (int)_cfg.DSMS.GetHOFOptionValue(setting.Munition);
                    int numPresses = hofVal >= 6 ? hofVal - 6 : hofVal + 4;
                    for (int i = 0; i < numPresses; i++)
                        AddAction(lmfd, "LMFD_18");
                }

                // RPM
                if (setting.Munition.RPM && !setting.IsRPMOptionDefault)
                {
                    int rpmVal = (int)_cfg.DSMS.GetRPMOptionValue(setting.Munition);
                    int numPresses = rpmVal >= 3 ? rpmVal - 3 : rpmVal + 3;
                    for (int i = 0; i < numPresses; i++)
                        AddAction(lmfd, "LMFD_17");
                }

                // Load
                if (attemptSymLoad)
                {
                    int symStation = GetSymmetricStation(station);
                    if (setting.Munition.INV_Keys.Contains(_loadoutQuery.StationMunitionMap[symStation]))
                    {
                        // load sym
                        AddAction(lmfd, "LMFD_10");
                        return symStation;
                    }
                }

                // load
                AddAction(lmfd, "LMFD_09");
                return -1;
            }

            return -1;
        }

        // Modify default weapon profiles having non-default configured settings.
        private void Build_DefaultProfiles(AirframeDevice cdu, AirframeDevice lmfd)
        {
            // get configs that require profile changes
            Dictionary<int, MunitionSettings> nonDefaultSettings = _cfg.DSMS.GetNonDefaultProfileSettings();
            if (nonDefaultSettings == null || nonDefaultSettings.Count == 0)
                return;

            AddActions(lmfd, new() { "LMFD_14", "LMFD_01" }, null, WAIT_BASE); // Go to DSMS Profiles
            int selectedProfileIndex = 0;

            foreach (KeyValuePair<int, int> kv in _profileQuery.MunitionProfileIndexMap)
            {
                if (nonDefaultSettings.TryGetValue(kv.Key, out MunitionSettings settings))
                {
                    selectedProfileIndex = SelectProfile(lmfd, selectedProfileIndex, kv.Value);
                    Build_CurrentProfile(cdu, lmfd, settings);
                }
            }

            // Back to DSMS Main
            AddAction(lmfd, "LMFD_01"); // STAT
        }

        private void Build_CurrentProfile(AirframeDevice cdu, AirframeDevice lmfd, MunitionSettings settings)
        {
            AddAction(lmfd, "LMFD_03"); // VIEW PRO

            // "front page" stuff first

            // Release Mode: SGL, PRS, RIP SGL, RIP PRS
            if ((settings.Munition.Pairs || settings.Munition.Ripple) && !settings.IsReleaseModeDefault)
            {
                for (int i = 0; i < int.Parse(settings.ReleaseMode); i++)
                    AddAction(lmfd, "LMFD_06");
            }

            // Fuze
            if (settings.Munition.Fuze && !settings.IsFuzeOptionDefault) 
            {
                for (int i = 0; i < int.Parse(settings.FuzeOption); i++)
                    AddAction(lmfd, "LMFD_07");
            }

            // Ripple Qty
            if (settings.Munition.Ripple && !settings.IsRippleQtyDefault)
            {
                AddAction(cdu, "CLR");
                foreach (char c in settings.RippleQty)
                    AddAction(cdu, c.ToString());
                AddAction(lmfd, "LMFD_08");
            }

            // Ripple Distance
            if (settings.Munition.RipFt && !settings.IsRippleFtDefault)
            {
                AddAction(cdu, "CLR");
                foreach (char c in settings.RippleFt)
                    AddAction(cdu, c.ToString());
                AddAction(lmfd, "LMFD_09");
            }

            // Delivery Mode: CCIP/CCRP
            if (!settings.IsDeliveryModeDefault)
            {
                for (int i = 0; i < int.Parse(settings.DeliveryMode); i++)
                    AddAction(lmfd, "LMFD_10");
            }

            // Settings inside the "CHG SET" page
            if (!settings.IsProfileCHGSETDefault)
            {
                AddAction(lmfd, "LMFD_16"); // CHG SET

                // Escape Maneuver
                if (settings.Munition.EscMnvr && !settings.IsEscapeManeuverDefault)
                {
                    int escVal = (int)_cfg.DSMS.GetEscapeManeuverValue(settings.Munition);
                    int numPresses = escVal >= 1 ? escVal - 1 : escVal + 3;
                    for (int i = 0; i < numPresses; i++)
                        AddAction(lmfd, "LMFD_20");
                }

                // Auto Lase
                if (settings.Munition.Laser && !settings.IsAutoLaseDefault)
                    AddAction(lmfd, "LMFD_06");

                // Lase Seconds
                if (settings.Munition.Laser && !settings.IsLaseSecondsDefault)
                {
                    AddAction(cdu, "CLR");
                    foreach (char c in settings.LaseSeconds)
                        AddAction(cdu, c.ToString());
                    AddAction(lmfd, "LMFD_17");
                }
            }

            // Save
            AddWait(WAIT_BASE);
            AddAction(lmfd, "LMFD_03");
        }

        /// <summary>
        /// Modify the order of the default weapon profiles.
        /// Important to do this last because it will invalidate the content of _profileQuery.MunitionProfileIndexMap
        /// </summary>
        /// <param name="lmfd"></param>
        private void Build_DefaultProfileOrder(AirframeDevice lmfd)
        {
            if (_cfg.DSMS.IsProfileOrderDefault)
                return;

            AddActions(lmfd, new() { "LMFD_14", "LMFD_01" }, null, WAIT_BASE); // Go to DSMS Profiles

            AddAction(lmfd, "LMFD_19"); // arrow down to first profile past WPNS OFF
            int selectedProfileIndex = 1;

            // simple bubble sort of the profiles according to configured order
            // TODO there's probably some .NET sorting base class that would be nicer here.
            List<string> profiles = _profileQuery.Profiles;
            for (int i = 1; i < profiles.Count - 1; i++)
            {
                bool changeMade = false;
                for (int j = 1; j < profiles.Count - i; j++)
                {
                    if (_cfg.DSMS.GetOrderedProfilePosition(profiles[j]) > _cfg.DSMS.GetOrderedProfilePosition(profiles[j + 1]))
                    {
                        selectedProfileIndex = bubbleSortSwap(lmfd, selectedProfileIndex, profiles, j);
                        changeMade = true;
                    }
                }

                if (changeMade == false)
                    break;
            }

            // Back to DSMS Main
            AddAction(lmfd, "LMFD_01"); // STAT
        }

        private int bubbleSortSwap(AirframeDevice lmfd, int selectedProfileIndex, List<string> profiles, int firstToSwapIndex)
        {
            // swap on the jet
            SelectProfile(lmfd, selectedProfileIndex, firstToSwapIndex);
            AddAction(lmfd, "LMFD_07"); // move down

            // swap in-memory
            string first = profiles[firstToSwapIndex];
            string second = profiles[firstToSwapIndex + 1];
            profiles[firstToSwapIndex] = second;
            profiles[firstToSwapIndex + 1] = first;

            return firstToSwapIndex + 1;
        }

        private class ProfileOrderComparer : IComparer<string>
        {
            private readonly Dictionary<string, int> _targetOrder;

            public ProfileOrderComparer(Dictionary<string, int> targetOrder)
            {
                _targetOrder = targetOrder;
            }

            public int Compare(string x, string y)
            {
                return _targetOrder[x] - _targetOrder[y];
            }
        }
        
        /// <summary>
        /// Moves the selection indicator on the DSMS profile's page to the profile at toSelectIndex.
        /// Assumes we are already on the DSMS profile page.
        /// </summary>
        /// <param name="lmfd"></param>
        /// <param name="currentSelectionIndex"></param>
        /// <param name="toSelectIndex"></param>
        /// <returns>The new, current selected index.</returns>
        private int SelectProfile(AirframeDevice lmfd, int currentSelectionIndex, int toSelectIndex)
        {
            if (toSelectIndex > currentSelectionIndex)
            {
                for (int i = 0; i < toSelectIndex - currentSelectionIndex; i++)
                    AddAction(lmfd, "LMFD_19"); // arrow down
            }
            else if (toSelectIndex < currentSelectionIndex)
            {
                for (int i = 0; i < currentSelectionIndex - toSelectIndex; i++)
                    AddAction(lmfd, "LMFD_20"); // arrow up
            }

            return toSelectIndex;
        }

        private static string GetButtonForStation(int station) => _stationButtonMap[station];
        private static readonly Dictionary<int, string> _stationButtonMap = new Dictionary<int, string>
        {
            {  1, "LMFD_16" },
            {  2, "LMFD_17" },
            {  3, "LMFD_18" },
            {  4, "LMFD_19" },
            {  5, "LMFD_20" },
            {  6, "LMFD_03" },
            {  7, "LMFD_06" },
            {  8, "LMFD_07" },
            {  9, "LMFD_08" },
            { 10, "LMFD_09" },
            { 11, "LMFD_10" },
        };

        private static int GetSymmetricStation(int station) => _symStationMap[station];
        private static readonly Dictionary<int, int> _symStationMap = new Dictionary<int, int>
        {
            {  1, 11 },
            {  2, 10 },
            {  3,  9 },
            {  4,  8 },
            {  5,  7 },
            {  6, -1 },
            {  7,  5 },
            {  8,  4 },
            {  9,  3 },
            { 10,  2 },
            { 11,  1 },
        };

        private static Dictionary<int, bool> GetStationIsSetupMap() => new Dictionary<int, bool>() 
        {
            {  1, false },
            {  2, false },
            {  3, false },
            {  4, false },
            {  5, false },
            {  6, false },
            {  7, false },
            {  8, false },
            {  9, false },
            { 10, false },
            { 11, false }
        };
    }
}
