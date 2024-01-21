// ********************************************************************************************************************
//
// F14ABDeviceManager.cs -- f-14a/b airframe device manager
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

using JAFDTC.Utilities;
using JAFDTC.Models.DCS;
using System.Diagnostics;

namespace JAFDTC.Models.F14AB
{
    /// <summary>
    /// manages the set of dcs airframe devices and associated commands/actions for the tomcat.
    /// </summary>
    internal class F14ABDeviceManager : AirframeDeviceManagerBase, IAirframeDeviceManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F14ABDeviceManager()
        {
            int delay = Settings.CommandDelaysMs[AirframeTypes.F14AB];
            int delayChar = delay / 2;

            AirframeDevice cap = new(24, "CAP");
            cap.AddAction(3531, "RIO_CAP_CLEAR", delayChar, 1);
            cap.AddAction(3518, "RIO_CAP_BTN_1", delayChar, 1);
            cap.AddAction(3519, "RIO_CAP_BTN_2", delayChar, 1);
            cap.AddAction(3520, "RIO_CAP_BTN_3", delayChar, 1);
            cap.AddAction(3533, "RIO_CAP_NE", delayChar, 1);
            cap.AddAction(3532, "RIO_CAP_SW", delayChar, 1);
            cap.AddAction(3536, "RIO_CAP_LAT_1", delayChar, 1);
            cap.AddAction(3541, "RIO_CAP_LONG_6", delayChar, 1);
            cap.AddAction(3535, "0", delayChar, 1);
            cap.AddAction(3536, "1", delayChar, 1);
            cap.AddAction(3537, "2", delayChar, 1);
            cap.AddAction(3538, "3", delayChar, 1);
            cap.AddAction(3539, "4", delayChar, 1);
            cap.AddAction(3540, "5", delayChar, 1);
            cap.AddAction(3541, "6", delayChar, 1);
            cap.AddAction(3542, "7", delayChar, 1);
            cap.AddAction(3543, "8", delayChar, 1);
            cap.AddAction(3544, "9", delayChar, 1);
            cap.AddAction(3530, "RIO_CAP_CATEGORY_TAC", -delay, 3, 3);
            AddDevice(cap);
        }
    }
}
