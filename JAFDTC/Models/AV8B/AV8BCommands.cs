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

            int delayMFDs = delay / 4;
            int delayList = delay / 4;
            int delayEntr = delay / 2;
            int delayDown = delay;
            int delayUp = delay;
            int delaySeq = delay;
            int delayRtn = delay;

            Device lmpcd = new(26, "LMPCD");
            lmpcd.AddCommand(new Command(3002, "LEFT_HDPT", 0, 1));
            // TODO: add lmpcd commands?
            AddDevice(lmpcd);

            Device ufc = new(23, "UFC");
            ufc.AddCommand(new Command(3002, "LEFT_HDPT", 0, 1));
            // TODO: add ufc commands?
            AddDevice(ufc);

            Device odu = new(24, "ODU");
            odu.AddCommand(new Command(3002, "LEFT_HDPT", 0, 1));
            // TODO: add odu commands?
            AddDevice(odu);
        }
    }
}
