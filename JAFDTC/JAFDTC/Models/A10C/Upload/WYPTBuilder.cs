// ********************************************************************************************************************
//
// WYPTBuilder.cs -- a-10c waypoint command builder
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

using JAFDTC.Models.A10C.WYPT;
using JAFDTC.Models.DCS;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.A10C.Upload
{
    /// <summary>
    /// command builder for the waypoint system in the warthog. translates cmds setup in F16CConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class WYPTBuilder : A10CBuilderBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public WYPTBuilder(A10CConfiguration cfg, A10CCommands a10c, StringBuilder sb) : base(cfg, a10c, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure waypoint system via the cdu according to the non-default programming settings (this function is
        /// safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// </summary>
        public override void Build()
        {
            ObservableCollection<WaypointInfo> wypts = _cfg.WYPT.Points;
            Device cdu = _aircraft.GetDevice("CDU");

            if (wypts.Count > 0)
            {
                AppendCommand(cdu.GetCommand("WP"));

                AppendCommand(cdu.GetCommand("LSK_3L"));
                AppendCommand(Wait());

                AppendCommand(cdu.GetCommand("CLR"));
                AppendCommand(cdu.GetCommand("CLR"));
                AppendCommand(Wait());

                BuildWaypoints(cdu, wypts);

#if TODO_IMPLEMENT
                if (wypts.Count > 0)
                {
                    AppendCommand(BuildDigits(cdu, wypts[0].Number.ToString()));

                    AppendCommand(cdu.GetCommand("LSK_3L"));
                    AppendCommand(cdu.GetCommand("CLR"));
                    AppendCommand(cdu.GetCommand("CLR"));
                }

                // TODO: sequence to set a waypoint as current
                AppendCommand(cdu.GetCommand("CLR"));
                AppendCommand(cdu.GetCommand("CLR"));
                AppendCommand(cdu.GetCommand("CLR"));
                // enter wypt number
                AppendCommand(cdu.GetCommand("LSK_3L"));
#endif
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildWaypoints(Device cdu, ObservableCollection<WaypointInfo> jetWypts)
        {
            for (var i = 0; i < jetWypts.Count; i++)
            {
                string wyptID = jetWypts[i].Number.ToString();
                WaypointInfo wypt = jetWypts[i];

#if NOPE
                AppendCommand(BuildDigits(cdu, wyptID));

                AppendCommand(cdu.GetCommand("LSK_3L"));
                AppendCommand(cdu.GetCommand("CLR"));
                AppendCommand(cdu.GetCommand("CLR"));
#endif

                if (wypt.IsValid)
                {
                    AppendCommand(cdu.GetCommand("LSK_7R"));
                    AppendCommand(Wait());

                    AppendCommand(cdu.GetCommand("CLR"));
                    AppendCommand(cdu.GetCommand("CLR"));

                    BuildWaypointName(cdu, wyptID, wypt.Name);

                    BuildWaypointCoords(cdu, wypt);

                    int intAlt = Math.Max(int.Parse(wypt.Alt), 0);
                    AppendCommand(BuildDigits(cdu, intAlt.ToString()));

                    AppendCommand(cdu.GetCommand("LSK_5L"));
                    AppendCommand(Wait());

                    AppendCommand(cdu.GetCommand("CLR"));
                    AppendCommand(cdu.GetCommand("CLR"));
                }
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildWaypointName(Device cdu, string wyptID, string waypointName)
        {
            waypointName = Regex.Replace(waypointName.ToUpper(), "^[^A-Z]+", "");
            waypointName = Regex.Replace(waypointName, "[^A-Z0-9 ]", "");
            if (string.IsNullOrEmpty(waypointName))
            {
                waypointName = $"WP{wyptID}";
            }
            else if (waypointName.Length > 12)
            {
                waypointName = waypointName[..12];
            }

            AppendCommand(BuildAlphaNumString(cdu, waypointName));

            AppendCommand(cdu.GetCommand("LSK_3R"));
            AppendCommand(Wait());

            AppendCommand(cdu.GetCommand("CLR"));
            AppendCommand(cdu.GetCommand("CLR"));
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildWaypointCoords(Device cdu, WaypointInfo waypoint)
        {
            AppendCommand(BuildAlphaNumString(cdu, RemoveSeparators(waypoint.LatUI.Replace(" ", ""))));     // DDM format

            AppendCommand(cdu.GetCommand("LSK_7L"));
            AppendCommand(cdu.GetCommand("CLR"));
            AppendCommand(cdu.GetCommand("CLR"));

            AppendCommand(BuildAlphaNumString(cdu, RemoveSeparators(waypoint.LonUI.Replace(" ", ""))));     // DDM format

            AppendCommand(cdu.GetCommand("LSK_9L"));
            AppendCommand(cdu.GetCommand("CLR"));
            AppendCommand(cdu.GetCommand("CLR"));
        }
    }
}
