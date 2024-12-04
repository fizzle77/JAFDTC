// ********************************************************************************************************************
//
// WYPTBuilder.cs -- av-8b waypoint command builder
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

using JAFDTC.Models.AV8B.WYPT;
using JAFDTC.Models.DCS;
using System.Collections.Generic;
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

        public WYPTBuilder(AV8BConfiguration cfg, AV8BDeviceManager _dcsCmds, StringBuilder sb) : base(cfg, _dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure waypoint system via the cdu according to the non-default programming settings (this function is
        /// safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// </summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            ObservableCollection<WaypointInfo> wypts = _cfg.WYPT.Points;

            if (wypts.Count == 0)
                return;

            AddExecFunction("NOP", new() { "==== WYPTBuilder:Build()" });

            AirframeDevice lmpcd = _aircraft.GetDevice("LMPCD");
            AirframeDevice ufc = _aircraft.GetDevice("UFC");
            AirframeDevice odu = _aircraft.GetDevice("ODU");

            AddAction(lmpcd, "MPCD_L_2");
            for (int i = 0; i < wypts.Count; i++)
            {
                if (wypts[i].IsValid)
                {
                    if (_cfg.WYPT.IsAppendMode)
                    {
                        AddActions(ufc, new() { $"{wypts[i].Number - 1}", $"{wypts[i].Number}" });
                    }
                    else
                    {
                        AddActions(ufc, new() { "7", "7" });
                    }
                    AddAction(ufc, "UFC_ENTER");
                    AddAction(odu, "ODU_OPT2");

                    AddActions(ufc, ActionsFor2864CoordinateString(wypts[i].LatUI), new() { "UFC_ENTER" });
                    AddActions(ufc, ActionsFor2864CoordinateString(wypts[i].LonUI), new() { "UFC_ENTER" });

                    AddAction(odu, "ODU_OPT1");
                }
            }
            AddAction(lmpcd, "MPCD_L_2");
        }
    }
}
