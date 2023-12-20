// ********************************************************************************************************************
//
// AV8BCommands.cs -- av-8b commands
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

using JAFDTC.Models.DCS;
using JAFDTC.Utilities;
using System.Diagnostics;

namespace JAFDTC.Models.AV8B
{
    /// <summary>
	/// manages the set of dcs cockpit commands associated with devices in the harrier.
    /// </summary>
    internal class AV8BCommands : AirframeDeviceManagerBase, IAirframeDeviceManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public AV8BCommands()
        {
            int delay = Settings.CommandDelaysMs[AirframeTypes.AV8B];

            Device lmpcd = new(26, "LMPCD");
            lmpcd.AddCommand(new Command(3201, "MPCD_L_2", delay, 1));
            AddDevice(lmpcd);

            Device ufc = new(23, "UFC");
            ufc.AddCommand(new Command(3302, "1", delay, 1));
            ufc.AddCommand(new Command(3303, "2", delay, 1));
            ufc.AddCommand(new Command(3304, "3", delay, 1));
            ufc.AddCommand(new Command(3306, "4", delay, 1));
            ufc.AddCommand(new Command(3307, "5", delay, 1));
            ufc.AddCommand(new Command(3308, "6", delay, 1));
            ufc.AddCommand(new Command(3310, "7", delay, 1));
            ufc.AddCommand(new Command(3311, "8", delay, 1));
            ufc.AddCommand(new Command(3312, "9", delay, 1));
            ufc.AddCommand(new Command(3315, "0", delay, 1));
            ufc.AddCommand(new Command(3314, "UFC_ENTER", delay, 1));
            AddDevice(ufc);

            Device odu = new(24, "ODU");
            odu.AddCommand(new Command(3250, "ODU_OPT1", delay, 1));
            odu.AddCommand(new Command(3251, "ODU_OPT2", delay, 1));
            odu.AddCommand(new Command(3252, "ODU_OPT3", delay, 1));
            odu.AddCommand(new Command(3248, "ODU_OPT4", delay, 1));
            odu.AddCommand(new Command(3249, "ODU_OPT5", delay, 1));
            AddDevice(odu);
        }
    }
}
