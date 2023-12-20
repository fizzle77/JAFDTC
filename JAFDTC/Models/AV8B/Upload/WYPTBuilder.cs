// ********************************************************************************************************************
//
// WYPTBuilder.cs -- av-8b waypoint command builder
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

using JAFDTC.Models.AV8B.WYPT;
using JAFDTC.Models.DCS;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.AV8B.Upload
{
    /// <summary>
    /// command builder for the waypoint system in the harrier. translates cmds setup in AV8BConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class WYPTBuilder : AV8BBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public WYPTBuilder(AV8BConfiguration cfg, AV8BCommands _dcsCmds, StringBuilder sb) : base(cfg, _dcsCmds, sb) { }

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
            Device lmpcd = _aircraft.GetDevice("LMPCD");
            Device ufc = _aircraft.GetDevice("UFC");
            Device odu = _aircraft.GetDevice("ODU");

            if (wypts.Count > 0)
            {
                AppendCommand(lmpcd.GetCommand("MPCD_L_2"));
                for (int i = 0; i < wypts.Count; i++)
                {
                    if (wypts[i].IsValid)
                    {
                        AppendCommand(ufc.GetCommand("7"));
                        AppendCommand(ufc.GetCommand("7"));
                        AppendCommand(ufc.GetCommand("UFC_ENTER"));
                        AppendCommand(odu.GetCommand("ODU_OPT2"));

                        AppendCommand(Build2864Coordinate(ufc, wypts[i].LatUI));
                        AppendCommand(ufc.GetCommand("UFC_ENTER"));

                        AppendCommand(Build2864Coordinate(ufc, wypts[i].LonUI));
                        AppendCommand(ufc.GetCommand("UFC_ENTER"));

                        AppendCommand(odu.GetCommand("ODU_OPT1"));
                    }
                }
                AppendCommand(lmpcd.GetCommand("MPCD_L_2"));
            }
        }
    }
}
