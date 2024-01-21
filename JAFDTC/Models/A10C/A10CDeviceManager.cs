// ********************************************************************************************************************
//
// A10CDeviceManager.cs -- a-10c airframe device manager
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

namespace JAFDTC.Models.A10C
{
    /// <summary>
    /// manages the set of dcs airframe devices and associated commands/actions for the warthog.
    /// </summary>
    class A10CDeviceManager : AirframeDeviceManagerBase, IAirframeDeviceManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public A10CDeviceManager()
        {
            int delay = Settings.CommandDelaysMs[AirframeTypes.A10C];
            int delayChar = delay / 2;

            AirframeDevice cdu = new(9, "CDU");
            cdu.AddAction(3015, "1", delayChar, 1);
            cdu.AddAction(3016, "2", delayChar, 1);
            cdu.AddAction(3017, "3", delayChar, 1);
            cdu.AddAction(3018, "4", delayChar, 1);
            cdu.AddAction(3019, "5", delayChar, 1);
            cdu.AddAction(3020, "6", delayChar, 1);
            cdu.AddAction(3021, "7", delayChar, 1);
            cdu.AddAction(3022, "8", delayChar, 1);
            cdu.AddAction(3023, "9", delayChar, 1);
            cdu.AddAction(3024, "0", delayChar, 1);
            cdu.AddAction(3027, "A", delayChar, 1);
            cdu.AddAction(3028, "B", delayChar, 1);
            cdu.AddAction(3029, "C", delayChar, 1);
            cdu.AddAction(3030, "D", delayChar, 1);
            cdu.AddAction(3031, "E", delayChar, 1);
            cdu.AddAction(3032, "F", delayChar, 1);
            cdu.AddAction(3033, "G", delayChar, 1);
            cdu.AddAction(3034, "H", delayChar, 1);
            cdu.AddAction(3035, "I", delayChar, 1);
            cdu.AddAction(3036, "J", delayChar, 1);
            cdu.AddAction(3037, "K", delayChar, 1);
            cdu.AddAction(3038, "L", delayChar, 1);
            cdu.AddAction(3039, "M", delayChar, 1);
            cdu.AddAction(3040, "N", delayChar, 1);
            cdu.AddAction(3041, "O", delayChar, 1);
            cdu.AddAction(3042, "P", delayChar, 1);
            cdu.AddAction(3043, "Q", delayChar, 1);
            cdu.AddAction(3044, "R", delayChar, 1);
            cdu.AddAction(3045, "S", delayChar, 1);
            cdu.AddAction(3046, "T", delayChar, 1);
            cdu.AddAction(3047, "U", delayChar, 1);
            cdu.AddAction(3048, "V", delayChar, 1);
            cdu.AddAction(3049, "W", delayChar, 1);
            cdu.AddAction(3050, "X", delayChar, 1);
            cdu.AddAction(3051, "Y", delayChar, 1);
            cdu.AddAction(3052, "Z", delayChar, 1);
            cdu.AddAction(3057, " ", delayChar, 1);
            cdu.AddAction(3058, "CLR", delay, 1);
            cdu.AddAction(3001, "LSK_3L", delay, 1);
            cdu.AddAction(3002, "LSK_5L", delay, 1);
            cdu.AddAction(3003, "LSK_7L", delay, 1);
            cdu.AddAction(3004, "LSK_9L", delay, 1);
            cdu.AddAction(3005, "LSK_3R", delay, 1);
            cdu.AddAction(3006, "LSK_5R", delay, 1);
            cdu.AddAction(3007, "LSK_7R", delay, 1);
            cdu.AddAction(3008, "LSK_9R", delay, 1);
            cdu.AddAction(3011, "WP", delay, 1);
            AddDevice(cdu);
        }
    }
}
