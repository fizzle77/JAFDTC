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
            AirframeDevice lmfd = _aircraft.GetDevice("LMFD");

            if (_cfg.DSMS.IsDefault)
                return;

            AddIfBlock("IsDSMSInDefaultMFDPosition", true, null, delegate () { BuildDSMS(lmfd); });
        }

        private void BuildDSMS(AirframeDevice lmfd)
        {
            BuildDSMS_INV(lmfd);
        }

        private void BuildDSMS_INV(AirframeDevice lmfd)
        {
            // get configs that require INV changes
            List<MunitionSettings> nonDefaultSettings = _cfg.DSMS.GetNonDefaultInvSettings();
            if (nonDefaultSettings == null || nonDefaultSettings.Count == 0)
                return;

            AddActions(lmfd, new() { "LMFD_14", "LMFD_05" }); // Go to DSMS INV

            // iterate over all stations
            // iterate over non-default configs
            // if station matches config's munition, execute
            // if symmetric station matches config's munition
            // load sym
            // mark symmetric station done

            for (int station = 1; station <= 11; station++)
            {
                BuildDSMS_INV_station(lmfd, nonDefaultSettings, station);
            }
        }

        private bool BuildDSMS_INV_station(AirframeDevice lmfd, List<MunitionSettings> nonDefaultSettings, int station)
        {
            foreach (MunitionSettings setting in nonDefaultSettings)
            {
                if (_cfg.DSMS.Loadout[station] == setting.Munition.Key)
                {
                    // station, inv stat
                    AddActions(lmfd, new() { buttonForStation(station), "LMFD_03" }, null, WAIT_BASE);

                    // Laser Code
                    
                    // HOF
                    if (setting.Munition.HOF && !setting.IsHOFOptionDefault)
                    {
                        int hofVal = (int)_cfg.DSMS.GetHOFOptionValue(setting.Munition);
                        int numPresses = hofVal >= 6 ? hofVal - 6 : hofVal + 3;
                        List<string> actions = new List<string>(numPresses);
                        for (int i = 0; i < numPresses; i++)
                            actions.Add("LMFD_18");
                        AddActions(lmfd, actions);
                    }

                    // RPM

                    // Load
                }
            }

            // return whether we did SYM LOAD on opposite station
            if (station == 6)
                return false;

            return true;
        }

        private static string buttonForStation(int station) => _stationButtonMap[station];
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
    }
}
