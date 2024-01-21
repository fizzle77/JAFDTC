// ********************************************************************************************************************
//
// WYPTBuilder.cs -- f-14a/b waypoint command builder
//
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
using JAFDTC.Models.F14AB.WYPT;
using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F14AB.Upload
{
    /// <summary>
    /// command builder for the waypoint system in the tomcat. translates cmds setup in F14ABConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class WYPTBuilder : F14ABBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public WYPTBuilder(F14ABConfiguration cfg, F14ABDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

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
            AirframeDevice cap = _aircraft.GetDevice("PCN");

            // TODO: support for other navigation point types?

            if (wypts.Count > 0)
            {
                // NOTE: should be at most 3 steerpoints, 1-3.

                AddAction(cap, "RIO_CAP_CATEGORY_TAC");
                for (int i = 0; i < wypts.Count; i++)
                {
                    if (wypts[i].IsValid)
                    {
                        // NOTE: coords are zero-filled in the ui, back that out here.

                        AddActions(cap, new() { $"RIO_CAP_BTN_{i + 1}", "RIO_CAP_CLEAR" });

                        AddAction(cap, "RIO_CAP_LAT_1");
                        AddActions(cap, ActionsForCoordinateString(Coord.RemoveLLDegZeroFill(wypts[i].LatUI)));
                        AddAction(cap, "RIO_CAP_ENTER");

                        AddAction(cap, "RIO_CAP_LONG_6");
                        AddActions(cap, ActionsForCoordinateString(Coord.RemoveLLDegZeroFill(wypts[i].LonUI)));
                        AddAction(cap, "RIO_CAP_ENTER");

                        AddAction(cap, "RIO_CAP_CLEAR");
                    }
                }
            }
        }

        /// <summary>
        /// build the set of commands necessary to enter a lat/lon coordinate into the waypoint system. once
        /// separators and spaces are removed, the coordinate string should start with N/S/E/W followed by the
        /// digits and/or characters that should be typed in to the cap (so, zero-filled ddm format).
        /// <summary>
        private static List<string> ActionsForCoordinateString(string coord)
        {
            coord = AdjustNoSeparators(coord.Replace(" ", ""));

            List<string> actions = new();
            foreach (char c in coord.ToUpper().ToCharArray())
            {
                switch (c)
                {
                    case 'N': actions.Add("RIO_CAP_NE"); break;
                    case 'S': actions.Add("RIO_CAP_SW"); break;
                    case 'E': actions.Add("RIO_CAP_NE"); break;
                    case 'W': actions.Add("RIO_CAP_SW"); break;
                    default: actions.Add(c.ToString()); break;
                }
            }
            return actions;
        }
    }
}
