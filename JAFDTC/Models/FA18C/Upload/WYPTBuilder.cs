// ********************************************************************************************************************
//
// WYPTBuilder.cs -- fa-18c waypoint command builder
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

using JAFDTC.Models.FA18C.WYPT;
using JAFDTC.Models.DCS;
using System.Text;
using JAFDTC.Models.Base;
using JAFDTC.Utilities;

namespace JAFDTC.Models.FA18C.Upload
{
    /// <summary>
    /// TODO: document
    /// </summary>
    internal class WYPTBuilder : FA18CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public WYPTBuilder(FA18CConfiguration cfg, FA18CCommands dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure radio system (com1/com2 uhf/vhf radios) via the icp/ded according to the non-default programming
        /// settings (this function is safe to call with a configuration with default settings: defaults are skipped as
        /// necessary).
        /// <summary>
        public override void Build()
        {
            Device ufc = _aircraft.GetDevice("UFC");
            Device rmfd = _aircraft.GetDevice("RMFD");

            if (!_cfg.WYPT.IsDefault)
            {
                AppendCommand(rmfd.GetCommand("OSB-18"));    // MENU
                AppendCommand(rmfd.GetCommand("OSB-18"));    // MENU
                AppendCommand(rmfd.GetCommand("OSB-02"));    // HSI

                AppendCommand(rmfd.GetCommand("OSB-10"));    // DATA
                AppendCommand(rmfd.GetCommand("OSB-07"));    // WYPT
                AppendCommand(rmfd.GetCommand("OSB-05"));    // UFC

                SelectWp0(rmfd, 0);
                for (int i = 0; i < _cfg.WYPT.Points[0].Number; i++)
                {
                    AppendCommand(rmfd.GetCommand("OSB-12"));
                }

                for (int i = 0; i < _cfg.WYPT.Points.Count; i++)
                {
                    WaypointInfo wypt = _cfg.WYPT.Points[i];

                    if (wypt.IsValid)
                    {
                        AppendCommand(ufc.GetCommand("Opt1"));
                        AppendCommand(Wait());
                        AppendCommand(Build2864Coordinate(ufc, Coord.RemoveLLDegZeroFill(wypt.LatUI)));    // DDM
                        AppendCommand(ufc.GetCommand("ENT"));
                        AppendCommand(WaitLong());

                        AppendCommand(Build2864Coordinate(ufc, Coord.RemoveLLDegZeroFill(wypt.LonUI)));    // DDM
                        AppendCommand(ufc.GetCommand("ENT"));
                        AppendCommand(WaitLong());

                        AppendCommand(ufc.GetCommand("Opt3"));
                        AppendCommand(ufc.GetCommand("Opt1"));
                        AppendCommand(BuildDigits(ufc, wypt.Alt));
                        AppendCommand(ufc.GetCommand("ENT"));
                        AppendCommand(Wait());
                    }
                    AppendCommand(rmfd.GetCommand("OSB-12"));   // Next Waypoint
                }

                for (var i = 0; i < _cfg.WYPT.Points.Count; i++)
                {
                    AppendCommand(rmfd.GetCommand("OSB-13"));   // Prev Waypoint
                }

                AppendCommand(rmfd.GetCommand("OSB-18"));
                AppendCommand(rmfd.GetCommand("OSB-18"));
                AppendCommand(rmfd.GetCommand("OSB-15"));
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void SelectWp0(Device rmfd, int i)
        {
            if (i < 140) // It might not notice on the first pass, so we go around once more
            {
                AppendCommand(StartCondition("NotAtWp0"));
                AppendCommand(rmfd.GetCommand("OSB-13"));
                AppendCommand(EndCondition("NotAtWp0"));
                SelectWp0(rmfd, i + 1);
            }
        }

#if NOPE

        private void selectWp0(Device rmfd, int i)
        {
            if (i < 140) // It might not notice on the first pass, so we go around once more
            {
                AppendCommand(StartCondition("NotAtWp0"));
                AppendCommand(rmfd.GetCommand("OSB-13"));
                AppendCommand(EndCondition("NotAtWp0"));
                selectWp0(rmfd, i + 1);
            }
        }
        public override void Build()
        {
            var wpts = _cfg.Waypoints.Waypoints;
            var wptStart = _cfg.Waypoints.SteerpointStart;
            var wptEnd = wptStart + wpts.Count;

            if (wpts.Count == 0)
            {
                return;
            }

            var wptDiff = wptEnd - wptStart;

            var ufc = _aircraft.GetDevice("UFC");
            var rmfd = _aircraft.GetDevice("RMFD");
            AppendCommand(rmfd.GetCommand("OSB-18")); // MENU
            AppendCommand(rmfd.GetCommand("OSB-18")); // MENU
            AppendCommand(rmfd.GetCommand("OSB-02")); // HSI

            AppendCommand(rmfd.GetCommand("OSB-10")); // DATA
            AppendCommand(rmfd.GetCommand("OSB-07")); // WYPT
            AppendCommand(rmfd.GetCommand("OSB-05")); // UFC

            selectWp0(rmfd, 0);
            for (var i = 0; i < wptStart; i++)
            {
                AppendCommand(rmfd.GetCommand("OSB-12"));
            }

            for (var i = 0; i < wptDiff; i++)
            {
                Waypoint wpt;
                wpt = wpts[i];

                if (wpt.Blank)
                {
                    continue;
                }

                AppendCommand(ufc.GetCommand("Opt1"));
                AppendCommand(Wait());
                AppendCommand(BuildCoordinate(ufc, wpt.Latitude));
                AppendCommand(ufc.GetCommand("ENT"));
                AppendCommand(WaitLong());

                AppendCommand(BuildCoordinate(ufc, wpt.Longitude));
                AppendCommand(ufc.GetCommand("ENT"));
                AppendCommand(WaitLong());

                AppendCommand(ufc.GetCommand("Opt3"));
                AppendCommand(ufc.GetCommand("Opt1"));
                AppendCommand(BuildDigits(ufc, wpt.Elevation.ToString()));
                AppendCommand(ufc.GetCommand("ENT"));
                AppendCommand(Wait());

                AppendCommand(rmfd.GetCommand("OSB-12")); // Next Waypoint
            }
            for (var i = 0; i < wptDiff; i++)
            {
                AppendCommand(rmfd.GetCommand("OSB-13")); // Prev Waypoint
            }

            AppendCommand(rmfd.GetCommand("OSB-18"));
            AppendCommand(rmfd.GetCommand("OSB-18"));
            AppendCommand(rmfd.GetCommand("OSB-15"));
        }

        private string BuildCoordinatxxxe(Device ufc, string coord)
        {
            var sb = new StringBuilder();

            var latStr = RemoveSeparators(coord.Replace(" ", ""));
            var i = 0;
            var lon = false;
            var longLon = false;

            foreach (var c in latStr.ToCharArray())
            {
                if (c == 'N')
                {
                    sb.Append(ufc.GetCommand("2"));
                    i = 0;
                }
                else if (c == 'S')
                {
                    sb.Append(ufc.GetCommand("8"));
                    i = 0;
                }
                else if (c == 'E')
                {
                    sb.Append(ufc.GetCommand("6"));
                    i = 0;
                    lon = true;
                }
                else if (c == 'W')
                {
                    sb.Append(ufc.GetCommand("4"));
                    i = 0;
                    lon = true;
                }
                else
                {
                    if (i <= 5 || (i <= 6 && longLon))
                    {
                        if (!(i == 0 && c == '0' && lon))
                        {
                            if (i == 0 && c == '1' && lon) longLon = true;

                            sb.Append(ufc.GetCommand(c.ToString()));
                            i++;
                            lon = false;
                        }

                    }
                }
            }

            return sb.ToString();
        }
#endif
    }
}
