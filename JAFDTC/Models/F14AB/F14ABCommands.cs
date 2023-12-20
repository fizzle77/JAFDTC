// ********************************************************************************************************************
//
// F14ABCommands.cs -- f-14a/b commands
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

using JAFDTC.Utilities;
using JAFDTC.Models.DCS;
using System.Diagnostics;

namespace JAFDTC.Models.F14AB
{
    /// <summary>
	/// manages the set of dcs cockpit commands associated with devices in the tomcat.
    /// </summary>
    internal class F14ABCommands : AirframeDeviceManagerBase, IAirframeDeviceManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F14ABCommands()
        {
            int delay = Settings.CommandDelaysMs[AirframeTypes.F14AB];

            int delayChar = delay / 2;

            Device cap = new(24, "CAP");
            cap.AddCommand(new Command(3531, "RIO_CAP_CLEAR", delayChar, 1));
            cap.AddCommand(new Command(3518, "RIO_CAP_BTN_1", delayChar, 1));
            cap.AddCommand(new Command(3519, "RIO_CAP_BTN_2", delayChar, 1));
            cap.AddCommand(new Command(3520, "RIO_CAP_BTN_3", delayChar, 1));
            cap.AddCommand(new Command(3533, "RIO_CAP_NE", delayChar, 1));
            cap.AddCommand(new Command(3532, "RIO_CAP_SW", delayChar, 1));
            cap.AddCommand(new Command(3536, "RIO_CAP_LAT_1", delayChar, 1));
            cap.AddCommand(new Command(3541, "RIO_CAP_LONG_6", delayChar, 1));
            cap.AddCommand(new Command(3535, "0", delayChar, 1));
            cap.AddCommand(new Command(3536, "1", delayChar, 1));
            cap.AddCommand(new Command(3537, "2", delayChar, 1));
            cap.AddCommand(new Command(3538, "3", delayChar, 1));
            cap.AddCommand(new Command(3539, "4", delayChar, 1));
            cap.AddCommand(new Command(3540, "5", delayChar, 1));
            cap.AddCommand(new Command(3541, "6", delayChar, 1));
            cap.AddCommand(new Command(3542, "7", delayChar, 1));
            cap.AddCommand(new Command(3543, "8", delayChar, 1));
            cap.AddCommand(new Command(3544, "9", delayChar, 1));

            // HACK A'HOY: generally, on the dcs side, a command has the semantics of "push, return to neurtal"
            // HACK A'HOY: (that is, we don't send separate "press" and "release" commands for a button, just a
            // HACK A'HOY: press command that implicitly has a release. this is broken for knob-like controls where
            // HACK A'HOY: you want to leave the control where it was set, not return to zero.
            //
            // HACK A'HOY: turns out, sending a negative delay will inhibit the implicit "release". maybe this is
            // HACK A'HOY: "by design", maybe not, but it serves our purpose here and saves us from having to write
            // HACK A'HOY: a bunch of code.
            //
            cap.AddCommand(new Command(3530, "RIO_CAP_CATEGORY_TAC", -delay, 3));
            AddDevice(cap);
        }
    }
}
