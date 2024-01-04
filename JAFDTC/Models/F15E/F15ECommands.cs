// ********************************************************************************************************************
//
// F15ECommands.cs -- f-15e commands
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

namespace JAFDTC.Models.F15E
{
    /// <summary>
    /// manages and defines the set of dcs cockpit commands associated with devices in the mudhen.
    /// </summary>
    internal class F15ECommands : AirframeDeviceManagerBase, IAirframeDeviceManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F15ECommands()
        {
            var delay = Settings.CommandDelaysMs[AirframeTypes.F15E];

            Device ufc = new(56, "UFC_PILOT");
            ufc.AddCommand(new Command(3001, "PB1", delay, 1));
            ufc.AddCommand(new Command(3002, "PB2", delay, 1));
            ufc.AddCommand(new Command(3003, "PB3", delay, 1));
            ufc.AddCommand(new Command(3004, "PB4", delay, 1));
            ufc.AddCommand(new Command(3005, "PB5", delay, 1));
            ufc.AddCommand(new Command(3006, "PB6", delay, 1));
            ufc.AddCommand(new Command(3007, "PB7", delay, 1));
            ufc.AddCommand(new Command(3008, "PB8", delay, 1));
            ufc.AddCommand(new Command(3009, "PB9", delay, 1));
            ufc.AddCommand(new Command(3010, "PB10", delay, 1));
            ufc.AddCommand(new Command(3019, "GCML", delay, 1));
            ufc.AddCommand(new Command(3020, "1", delay, 1));
            ufc.AddCommand(new Command(3021, "2", delay, 1));
            ufc.AddCommand(new Command(3022, "3", delay, 1));
            ufc.AddCommand(new Command(3023, "GCMR", delay, 1));
            ufc.AddCommand(new Command(3025, "4", delay, 1));
            ufc.AddCommand(new Command(3026, "5", delay, 1));
            ufc.AddCommand(new Command(3027, "6", delay, 1));
            ufc.AddCommand(new Command(3030, "7", delay, 1));
            ufc.AddCommand(new Command(3031, "8", delay, 1));
            ufc.AddCommand(new Command(3032, "9", delay, 1));
            ufc.AddCommand(new Command(3036, "0", delay, 1));
            ufc.AddCommand(new Command(3029, ".", delay, 1));
            ufc.AddCommand(new Command(3033, "SHF", delay, 1));
            ufc.AddCommand(new Command(3038, "MENU", delay, 1));
            ufc.AddCommand(new Command(3035, "CLR", delay, 1));

            ufc.AddCommand(new Command(3011, "PRESLCCW", delay, -1));
            ufc.AddCommand(new Command(3012, "PRESRCCW", delay, -1));
            ufc.AddCommand(new Command(3055, "PRESL", delay, 1));
            ufc.AddCommand(new Command(3056, "PRESR", delay, 1));
            AddDevice(ufc);

            Command[] mpdCommands = new Command[] {
                new(3061, "PB01", delay, 1),
                new(3062, "PB02", delay, 1),
                new(3063, "PB03", delay, 1),
                new(3064, "PB04", delay, 1),
                new(3065, "PB05", delay, 1),
                new(3066, "PB06", delay, 1),
                new(3067, "PB07", delay, 1),
                new(3068, "PB08", delay, 1),
                new(3069, "PB09", delay, 1),
                new(3070, "PB10", delay, 1),
                new(3071, "PB11", delay, 1),
                new(3072, "PB12", delay, 1),
                new(3073, "PB13", delay, 1),
                new(3074, "PB14", delay, 1),
                new(3075, "PB15", delay, 1),
                new(3076, "PB16", delay, 1),
                new(3077, "PB17", delay, 1),
                new(3078, "PB18", delay, 1),
                new(3079, "PB19", delay, 1),
                new(3080, "PB20", delay, 1)
            };

            Device frontLeftMPD = new(34, "FLMPD");
            AddDevice(frontLeftMPD);
            foreach (var cmd in mpdCommands)
            {
                frontLeftMPD.AddCommand(cmd);
            }

            Device frontMPCD = new(35, "FMPCD");
            AddDevice(frontMPCD);
            foreach (var cmd in mpdCommands)
            {
                frontMPCD.AddCommand(cmd);
            }

            Device frontRightMPD = new(36, "FRMPD");
            AddDevice(frontRightMPD);
            foreach (var cmd in mpdCommands)
            {
                frontRightMPD.AddCommand(cmd);
            }

            Device rearLeftMPCD = new(37, "RLMPCD");
            AddDevice(rearLeftMPCD);
            foreach (var cmd in mpdCommands)
            {
                rearLeftMPCD.AddCommand(cmd);
            }

            Device rearLeftMPD = new(38, "RLMPD");
            AddDevice(rearLeftMPD);
            foreach (var cmd in mpdCommands)
            {
                rearLeftMPD.AddCommand(cmd);
            }

            Device rearRightMPD = new(39, "RRMPD");
            AddDevice(rearRightMPD);
            foreach (var cmd in mpdCommands)
            {
                rearRightMPD.AddCommand(cmd);
            }

            Device rearRightMPCD = new(40, "RRMPCD");
            AddDevice(rearRightMPCD);
            foreach (var cmd in mpdCommands)
            {
                rearRightMPCD.AddCommand(cmd);
            }

            Device fltInst = new(17, "FLTINST");
            fltInst.AddCommand(new Command(3385, "BingoIncrease", 0, 0.1));
            fltInst.AddCommand(new Command(3385, "BingoDecrease", 0, -0.1));
            AddDevice(fltInst);
        }
    }
}
