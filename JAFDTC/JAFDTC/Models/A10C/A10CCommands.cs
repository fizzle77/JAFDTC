// ********************************************************************************************************************
//
// A10CCommands.cs -- a-10c commands
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

namespace JAFDTC.Models.A10C
{
    /// <summary>
	/// manages the set of dcs cockpit commands associated with devices in the warthog.
    /// </summary>
    class A10CCommands : AirframeDeviceManagerBase, IAirframeDeviceManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public A10CCommands()
        {
            int delay = Settings.CommandDelaysMs[AirframeTypes.AV8B];

            int delayChar = delay / 2;

            Device cdu = new(9, "CDU");
            cdu.AddCommand(new Command(3015, "1", delayChar, 1));
            cdu.AddCommand(new Command(3016, "2", delayChar, 1));
            cdu.AddCommand(new Command(3017, "3", delayChar, 1));
            cdu.AddCommand(new Command(3018, "4", delayChar, 1));
            cdu.AddCommand(new Command(3019, "5", delayChar, 1));
            cdu.AddCommand(new Command(3020, "6", delayChar, 1));
            cdu.AddCommand(new Command(3021, "7", delayChar, 1));
            cdu.AddCommand(new Command(3022, "8", delayChar, 1));
            cdu.AddCommand(new Command(3023, "9", delayChar, 1));
            cdu.AddCommand(new Command(3024, "0", delayChar, 1));
            cdu.AddCommand(new Command(3027, "A", delayChar, 1));
            cdu.AddCommand(new Command(3028, "B", delayChar, 1));
            cdu.AddCommand(new Command(3029, "C", delayChar, 1));
            cdu.AddCommand(new Command(3030, "D", delayChar, 1));
            cdu.AddCommand(new Command(3031, "E", delayChar, 1));
            cdu.AddCommand(new Command(3032, "F", delayChar, 1));
            cdu.AddCommand(new Command(3033, "G", delayChar, 1));
            cdu.AddCommand(new Command(3034, "H", delayChar, 1));
            cdu.AddCommand(new Command(3035, "I", delayChar, 1));
            cdu.AddCommand(new Command(3036, "J", delayChar, 1));
            cdu.AddCommand(new Command(3037, "K", delayChar, 1));
            cdu.AddCommand(new Command(3038, "L", delayChar, 1));
            cdu.AddCommand(new Command(3039, "M", delayChar, 1));
            cdu.AddCommand(new Command(3040, "N", delayChar, 1));
            cdu.AddCommand(new Command(3041, "O", delayChar, 1));
            cdu.AddCommand(new Command(3042, "P", delayChar, 1));
            cdu.AddCommand(new Command(3043, "Q", delayChar, 1));
            cdu.AddCommand(new Command(3044, "R", delayChar, 1));
            cdu.AddCommand(new Command(3045, "S", delayChar, 1));
            cdu.AddCommand(new Command(3046, "T", delayChar, 1));
            cdu.AddCommand(new Command(3047, "U", delayChar, 1));
            cdu.AddCommand(new Command(3048, "V", delayChar, 1));
            cdu.AddCommand(new Command(3049, "W", delayChar, 1));
            cdu.AddCommand(new Command(3050, "X", delayChar, 1));
            cdu.AddCommand(new Command(3051, "Y", delayChar, 1));
            cdu.AddCommand(new Command(3052, "Z", delayChar, 1));
            cdu.AddCommand(new Command(3057, " ", delayChar, 1));
            cdu.AddCommand(new Command(3058, "CLR", delay, 1));
            cdu.AddCommand(new Command(3001, "LSK_3L", delay, 1));
            cdu.AddCommand(new Command(3002, "LSK_5L", delay, 1));
            cdu.AddCommand(new Command(3003, "LSK_7L", delay, 1));
            cdu.AddCommand(new Command(3004, "LSK_9L", delay, 1));
            cdu.AddCommand(new Command(3005, "LSK_3R", delay, 1));
            cdu.AddCommand(new Command(3006, "LSK_5R", delay, 1));
            cdu.AddCommand(new Command(3007, "LSK_7R", delay, 1));
            cdu.AddCommand(new Command(3008, "LSK_9R", delay, 1));
            cdu.AddCommand(new Command(3011, "WP", delay, 1));
            AddDevice(cdu);
        }
    }
}
