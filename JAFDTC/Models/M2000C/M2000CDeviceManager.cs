// ********************************************************************************************************************
//
// M2000CDeviceManager.cs -- m-2000c commands
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

namespace JAFDTC.Models.M2000C
{
    /// <summary>
    /// manages the set of dcs airframe devices and associated commands/actions for the mirage.
    /// </summary>
    internal class M2000CDeviceManager : AirframeDeviceManagerBase, IAirframeDeviceManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public M2000CDeviceManager()
        {
            int delay = Settings.CommandDelaysMs[AirframeTypes.M2000C];

            int delayChar = delay / 2;

            AirframeDevice pcn = new(9, "PCN");
            pcn.AddAction(3570, "INS_PREP_SW", delayChar, 1);
            pcn.AddAction(3572, "INS_DEST_SW", delayChar, 1);
            pcn.AddAction(3584, "1", delayChar, 1);
            pcn.AddAction(3585, "2", delayChar, 1);
            pcn.AddAction(3586, "3", delayChar, 1);
            pcn.AddAction(3587, "4", delayChar, 1);
            pcn.AddAction(3588, "5", delayChar, 1);
            pcn.AddAction(3589, "6", delayChar, 1);
            pcn.AddAction(3590, "7", delayChar, 1);
            pcn.AddAction(3591, "8", delayChar, 1);
            pcn.AddAction(3592, "9", delayChar, 1);
            pcn.AddAction(3593, "0", delayChar, 1);
            pcn.AddAction(3594, "INS_CLR_BTN", delayChar, 1);
            pcn.AddAction(3595, "INS_ENTER_BTN", delayChar, 1);
            AddDevice(pcn);
        }
    }
}
