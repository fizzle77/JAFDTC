// ********************************************************************************************************************
//
// AV8BDeviceManager.cs -- av-8b airframe device manager
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

using JAFDTC.Models.DCS;
using JAFDTC.Utilities;
using System.Diagnostics;

namespace JAFDTC.Models.AV8B
{
    /// <summary>
    /// manages the set of dcs airframe devices and associated commands/actions for the harrier.
    /// </summary>
    internal class AV8BDeviceManager : AirframeDeviceManagerBase, IAirframeDeviceManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public AV8BDeviceManager()
        {
            int delay = Settings.CommandDelaysMs[AirframeTypes.AV8B];

            AirframeDevice lmpcd = new(26, "LMPCD");
            lmpcd.AddAction(3201, "MPCD_L_2", delay, 1);
            AddDevice(lmpcd);

            AirframeDevice ufc = new(23, "UFC");
            ufc.AddAction(3302, "1", delay, 1);
            ufc.AddAction(3303, "2", delay, 1);
            ufc.AddAction(3304, "3", delay, 1);
            ufc.AddAction(3306, "4", delay, 1);
            ufc.AddAction(3307, "5", delay, 1);
            ufc.AddAction(3308, "6", delay, 1);
            ufc.AddAction(3310, "7", delay, 1);
            ufc.AddAction(3311, "8", delay, 1);
            ufc.AddAction(3312, "9", delay, 1);
            ufc.AddAction(3315, "0", delay, 1);
            ufc.AddAction(3314, "UFC_ENTER", delay, 1);
            AddDevice(ufc);

            AirframeDevice odu = new(24, "ODU");
            odu.AddAction(3250, "ODU_OPT1", delay, 1);
            odu.AddAction(3251, "ODU_OPT2", delay, 1);
            odu.AddAction(3252, "ODU_OPT3", delay, 1);
            odu.AddAction(3248, "ODU_OPT4", delay, 1);
            odu.AddAction(3249, "ODU_OPT5", delay, 1);
            AddDevice(odu);
        }
    }
}
