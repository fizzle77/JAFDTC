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
using System.Runtime.InteropServices;
using System.Text;

namespace JAFDTC.Models.A10C.Upload
{
    /// <summary>
    /// command builder for the DSMS systems in the warthog. translates cmds setup in A10CConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class DSMSBuilder : A10CBuilderBase, IBuilder
    { 
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------
        
        public DSMSBuilder(A10CConfiguration cfg, A10CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

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
            BuildDSMS_INV(cdu, lmfd);
        }

        private void BuildDSMS_INV(AirframeDevice cdu, AirframeDevice lmfd)
        {
            // get configs that require INV changes
            Dictionary<string, MunitionSettings> nonDefaultSettings = _cfg.DSMS.GetNonDefaultInvSettings();
            if (nonDefaultSettings == null || nonDefaultSettings.Count == 0)
                return;

            AddActions(lmfd, new() { "LMFD_14", "LMFD_05" }); // Go to DSMS INV

            Dictionary<int, bool> _setupStations = GetStationSetupMap();
            // attempt symmetric loads on the first 5 stations
            for (int station = 1; station <= 5; station++)
            {
                int symLoadedStation = BuildDSMS_INV_station(cdu, lmfd, nonDefaultSettings, station, true);
                _setupStations[station] = true;
                if (symLoadedStation > 0)
                    _setupStations[symLoadedStation] = true;
            }
            // station 6 has no symmetric station, don't attempt sym load
            BuildDSMS_INV_station(cdu, lmfd, nonDefaultSettings, 6, false);
            _setupStations[6] = true;
            // set up any stations that weren't symloaded with stations 1-5
            for (int station = 7; station <= 11; station++)
            {
                if (!_setupStations[station])
                {
                    BuildDSMS_INV_station(cdu, lmfd, nonDefaultSettings, station, false);
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
        private int BuildDSMS_INV_station(AirframeDevice cdu, AirframeDevice lmfd, 
            Dictionary<string, MunitionSettings> nonDefaultSettings, int station, bool attemptSymLoad)
        {
            string munitionAtStation = _cfg.DSMS.Loadout[station];
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
                    if (_cfg.DSMS.Loadout[symStation] == setting.Munition.Key)
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

        private static Dictionary<int, bool> GetStationSetupMap() => new Dictionary<int, bool>() 
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
