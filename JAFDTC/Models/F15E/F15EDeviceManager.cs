// ********************************************************************************************************************
//
// F15EDeviceManager.cs -- f-15e commands
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
    /// manages the set of dcs airframe devices and associated commands/actions for the mudhen.
    /// </summary>
    internal class F15EDeviceManager : AirframeDeviceManagerBase, IAirframeDeviceManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F15EDeviceManager()
        {
            var delay = Settings.CommandDelaysMs[AirframeTypes.F15E];

            // ---- front, ufc

            AirframeDevice ufcPilot = new(56, "UFC_PILOT");
            ufcPilot.AddAction(3001, "PB1", delay, 1);
            ufcPilot.AddAction(3002, "PB2", delay, 1);
            ufcPilot.AddAction(3003, "PB3", delay, 1);
            ufcPilot.AddAction(3004, "PB4", delay, 1);
            ufcPilot.AddAction(3005, "PB5", delay, 1);
            ufcPilot.AddAction(3006, "PB6", delay, 1);
            ufcPilot.AddAction(3007, "PB7", delay, 1);
            ufcPilot.AddAction(3008, "PB8", delay, 1);
            ufcPilot.AddAction(3009, "PB9", delay, 1);
            ufcPilot.AddAction(3010, "PB10", delay, 1);
            ufcPilot.AddAction(3019, "GCML", delay, 1);
            ufcPilot.AddAction(3020, "1", delay, 1);
            ufcPilot.AddAction(3021, "2", delay, 1);
            ufcPilot.AddAction(3022, "3", delay, 1);
            ufcPilot.AddAction(3023, "GCMR", delay, 1);
            ufcPilot.AddAction(3025, "4", delay, 1);
            ufcPilot.AddAction(3026, "5", delay, 1);
            ufcPilot.AddAction(3027, "6", delay, 1);
            ufcPilot.AddAction(3030, "7", delay, 1);
            ufcPilot.AddAction(3031, "8", delay, 1);
            ufcPilot.AddAction(3032, "9", delay, 1);
            ufcPilot.AddAction(3036, "0", delay, 1);
            ufcPilot.AddAction(3029, ".", delay, 1);
            ufcPilot.AddAction(3033, "SHF", delay, 1);
            ufcPilot.AddAction(3038, "MENU", delay, 1);
            ufcPilot.AddAction(3035, "CLR", delay, 1);
            ufcPilot.AddAction(3011, "PRESLCCW", delay, -1);
            ufcPilot.AddAction(3012, "PRESRCCW", delay, -1);
            ufcPilot.AddAction(3055, "PRESL", delay, 1);
            ufcPilot.AddAction(3056, "PRESR", delay, 1);
            AddDevice(ufcPilot);

            // ---- rear, ufc

            AirframeDevice ufcWizzo = new(57, "UFC_WSO");
            ufcWizzo.AddAction(3001, "PB1", delay, 1);
            ufcWizzo.AddAction(3002, "PB2", delay, 1);
            ufcWizzo.AddAction(3003, "PB3", delay, 1);
            ufcWizzo.AddAction(3004, "PB4", delay, 1);
            ufcWizzo.AddAction(3005, "PB5", delay, 1);
            ufcWizzo.AddAction(3006, "PB6", delay, 1);
            ufcWizzo.AddAction(3007, "PB7", delay, 1);
            ufcWizzo.AddAction(3008, "PB8", delay, 1);
            ufcWizzo.AddAction(3009, "PB9", delay, 1);
            ufcWizzo.AddAction(3010, "PB10", delay, 1);
            ufcWizzo.AddAction(3019, "GCML", delay, 1);
            ufcWizzo.AddAction(3020, "1", delay, 1);
            ufcWizzo.AddAction(3021, "2", delay, 1);
            ufcWizzo.AddAction(3022, "3", delay, 1);
            ufcWizzo.AddAction(3023, "GCMR", delay, 1);
            ufcWizzo.AddAction(3025, "4", delay, 1);
            ufcWizzo.AddAction(3026, "5", delay, 1);
            ufcWizzo.AddAction(3027, "6", delay, 1);
            ufcWizzo.AddAction(3030, "7", delay, 1);
            ufcWizzo.AddAction(3031, "8", delay, 1);
            ufcWizzo.AddAction(3032, "9", delay, 1);
            ufcWizzo.AddAction(3036, "0", delay, 1);
            ufcWizzo.AddAction(3029, ".", delay, 1);
            ufcWizzo.AddAction(3033, "SHF", delay, 1);
            ufcWizzo.AddAction(3038, "MENU", delay, 1);
            ufcWizzo.AddAction(3035, "CLR", delay, 1);
            ufcWizzo.AddAction(3011, "PRESLCCW", delay, -1);
            ufcWizzo.AddAction(3012, "PRESRCCW", delay, -1);
            ufcWizzo.AddAction(3055, "PRESL", delay, 1);
            ufcWizzo.AddAction(3056, "PRESR", delay, 1);
            AddDevice(ufcWizzo);

            // ---- front, rear mpd/mpcd

            AirframeDevice frontMPCD = new(35, "FMPCD");
            AirframeDevice frontLeftMPD = new(34, "FLMPD");
            AirframeDevice frontRightMPD = new(36, "FRMPD");
            AirframeDevice rearLeftMPCD = new(37, "RLMPCD");
            AirframeDevice rearLeftMPD = new(38, "RLMPD");
            AirframeDevice rearRightMPD = new(39, "RRMPD");
            AirframeDevice rearRightMPCD = new(40, "RRMPCD");
            Action[] mpdActions = new Action[] {
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
            foreach (var actn in mpdActions)
            {
                frontMPCD.AddAction(actn.ID, actn.Name, actn.Delay, actn.ValueDn);
                frontLeftMPD.AddAction(actn.ID, actn.Name, actn.Delay, actn.ValueDn);
                frontRightMPD.AddAction(actn.ID, actn.Name, actn.Delay, actn.ValueDn);
                rearLeftMPCD.AddAction(actn.ID, actn.Name, actn.Delay, actn.ValueDn);
                rearLeftMPD.AddAction(actn.ID, actn.Name, actn.Delay, actn.ValueDn);
                rearRightMPD.AddAction(actn.ID, actn.Name, actn.Delay, actn.ValueDn);
                rearRightMPCD.AddAction(actn.ID, actn.Name, actn.Delay, actn.ValueDn);
            }
            AddDevice(frontMPCD);
            AddDevice(frontLeftMPD);
            AddDevice(frontRightMPD);
            AddDevice(rearLeftMPCD);
            AddDevice(rearLeftMPD);
            AddDevice(rearRightMPD);
            AddDevice(rearRightMPCD);

            // ---- front, flight instruments

            AirframeDevice fltInst = new(17, "FLTINST");
            fltInst.AddAction(3385, "BingoIncrease", 0, 0.1);
            fltInst.AddAction(3385, "BingoDecrease", 0, -0.1);
            AddDevice(fltInst);

            // ---- front, interior lights

            AirframeDevice intlPilot = new(23, "INTL_PILOT");
            intlPilot.AddAction(3569, "F_INTL_WARN_TEST", delay, 1);
            AddDevice(intlPilot);

            // ---- rear, interior lights

            AirframeDevice intlWizzo = new(23, "INTL_WSO");
            intlWizzo.AddAction(3459, "R_INTL_WARN_TEST", delay, 1);
            AddDevice(intlWizzo);
        }
    }
}
