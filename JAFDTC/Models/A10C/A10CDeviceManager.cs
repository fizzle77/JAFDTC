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

            // ---- cdu

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

            // ---- left mfd

            AirframeDevice lmfd = new(2, "LMFD");
            lmfd.AddAction(3001, "LMFD_01", delay, 1);
            lmfd.AddAction(3002, "LMFD_02", delay, 1);
            lmfd.AddAction(3003, "LMFD_03", delay, 1);
            lmfd.AddAction(3004, "LMFD_04", delay, 1);
            lmfd.AddAction(3005, "LMFD_05", delay, 1);
            lmfd.AddAction(3006, "LMFD_06", delay, 1);
            lmfd.AddAction(3007, "LMFD_07", delay, 1);
            lmfd.AddAction(3008, "LMFD_08", delay, 1);
            lmfd.AddAction(3009, "LMFD_09", delay, 1);
            lmfd.AddAction(3010, "LMFD_10", delay, 1);
            lmfd.AddAction(3011, "LMFD_11", delay, 1);
            lmfd.AddAction(3012, "LMFD_12", delay, 1);
            lmfd.AddAction(3013, "LMFD_13", delay, 1);
            lmfd.AddAction(3014, "LMFD_14", delay, 1);
            lmfd.AddAction(3015, "LMFD_15", delay, 1);
            lmfd.AddAction(3016, "LMFD_16", delay, 1);
            lmfd.AddAction(3017, "LMFD_17", delay, 1);
            lmfd.AddAction(3018, "LMFD_18", delay, 1);
            lmfd.AddAction(3019, "LMFD_19", delay, 1);
            lmfd.AddAction(3020, "LMFD_20", delay, 1);
            lmfd.AddAction(3012, "LMFD_12_LONG", 2500, 1);
            AddDevice(lmfd);

            // ---- right mfd

            AirframeDevice rmfd = new(3, "RMFD");
            rmfd.AddAction(3001, "RMFD_01", delay, 1);
            rmfd.AddAction(3002, "RMFD_02", delay, 1);
            rmfd.AddAction(3003, "RMFD_03", delay, 1);
            rmfd.AddAction(3004, "RMFD_04", delay, 1);
            rmfd.AddAction(3005, "RMFD_05", delay, 1);
            rmfd.AddAction(3006, "RMFD_06", delay, 1);
            rmfd.AddAction(3007, "RMFD_07", delay, 1);
            rmfd.AddAction(3008, "RMFD_08", delay, 1);
            rmfd.AddAction(3009, "RMFD_09", delay, 1);
            rmfd.AddAction(3010, "RMFD_10", delay, 1);
            rmfd.AddAction(3011, "RMFD_11", delay, 1);
            rmfd.AddAction(3012, "RMFD_12", delay, 1);
            rmfd.AddAction(3013, "RMFD_13", delay, 1);
            rmfd.AddAction(3014, "RMFD_14", delay, 1);
            rmfd.AddAction(3015, "RMFD_15", delay, 1);
            rmfd.AddAction(3016, "RMFD_16", delay, 1);
            rmfd.AddAction(3017, "RMFD_17", delay, 1);
            rmfd.AddAction(3018, "RMFD_18", delay, 1);
            rmfd.AddAction(3019, "RMFD_19", delay, 1);
            rmfd.AddAction(3020, "RMFD_20", delay, 1);
            rmfd.AddAction(3012, "RMFD_12_LONG", 2500, 1);
            AddDevice(rmfd);

            // ---- up-front controls

            AirframeDevice ufc = new(8, "UFC");
            ufc.AddAction(3030, "UFC_COM1", delay, 1);
            ufc.AddAction(3030, "UFC_COM1_LONG", 1500, 1);
            ufc.AddAction(3033, "UFC_COM2", delay, 1);
            ufc.AddAction(3033, "UFC_COM2_LONG", 1500, 1);
            ufc.AddAction(3001, "1", delay, 1);
            ufc.AddAction(3002, "2", delay, 1);
            ufc.AddAction(3003, "3", delay, 1);
            ufc.AddAction(3004, "4", delay, 1);
            ufc.AddAction(3005, "5", delay, 1);
            ufc.AddAction(3006, "6", delay, 1);
            ufc.AddAction(3007, "7", delay, 1);
            ufc.AddAction(3008, "8", delay, 1);
            ufc.AddAction(3009, "9", delay, 1);
            ufc.AddAction(3010, "0", delay, 1);
            AddDevice(ufc);

            // ---- an/arc-210 uhf radio

            AirframeDevice arc210 = new(55, "UHF_ARC210");
            arc210.AddAction(3043, "ARC210_MASTER_TR_G", delay, 0.1, 0.1);
            arc210.AddAction(3043, "ARC210_MASTER_TR", delay, 0.2, 0.2);
            arc210.AddAction(3044, "ARC210_SEC_SW_PRST", delay, 0.2, 0.2);
            arc210.AddAction(3044, "ARC210_SEC_SW_MAN", delay, 0.3, 0.3);
            //
            // following actions are for thumbwheels, add via:
            //
            //     AddCommand(device.CustomizedDCSActionCommand(key, posn, posn))
            //
            arc210.AddAction(3025, "ARC210_100MHZ_SEL", delay, 0.0, 0.0);   // 0.0-0.3, 0/100/200/300 MHz
            arc210.AddAction(3023, "ARC210_10MHZ_SEL", delay, 0.0, 0.0);    // 0.0-0.9, 0/10/20/.../80/90 MHz
            arc210.AddAction(3021, "ARC210_1MHZ_SEL", delay, 0.0, 0.0);     // 0.0-0.9, 0/1/2/.../8/9 MHz
            arc210.AddAction(3019, "ARC210_100KHZ_SEL", delay, 0.0, 0.0);   // 0.0-0.9, 0.0/0.1/0.2/.../0.8/0.9 MHz
            arc210.AddAction(3017, "ARC210_25KHZ_SEL", delay, 0.0, 0.0);    // 0.0-0.3, 0/25/50/75 KHz
            AddDevice(arc210);

            // ---- an/arc-164 uhf radio

            AirframeDevice arc164 = new(54, "UHF_ARC164");
            arc164.AddAction(3008, "UHF_FUNCTION_MAIN", delay, 0.1, 0.1);
            arc164.AddAction(3008, "UHF_FUNCTION_BOTH", delay, 0.2, 0.2);
            arc164.AddAction(3007, "UHF_MODE_MNL", delay, 0.0, 0.0);
            arc164.AddAction(3007, "UHF_MODE_PRESET", delay, 0.1, 0.1);
            arc164.AddAction(3014, "UHF_COVER_OPEN", delay, 1, 1);
            arc164.AddAction(3014, "UHF_COVER_CLOSED", delay, 0, 0);
            arc164.AddAction(3015, "UHF_LOAD", delay, 1);
            //
            // following actions are for thumbwheels, add via:
            //
            //     AddCommand(device.CustomizedDCSActionCommand(key, posn, posn))
            //
            arc164.AddAction(3001, "UHF_PRESET_SEL", delay, 0.0, 0.0);          // 0.0-1.0, 1/2/3/../18/19/20
            arc164.AddAction(3002, "UHF_100MHZ_SEL", delay, 0.0, 0.0);          // 0.0-0.2, 2/3/"A" MHz
            arc164.AddAction(3003, "UHF_10MHZ_SEL", delay, 0.0, 0.0);           // 0.0-0.9, 0/10/20/.../80/90 MHz
            arc164.AddAction(3004, "UHF_1MHZ_SEL", delay, 0.0, 0.0);            // 0.0-0.9, 0/1/2/.../8/9 MHz
            arc164.AddAction(3005, "UHF_POINT1MHZ_SEL", delay, 0.0, 0.0);       // 0.0-0.9, 0.0/0.1/0.2/.../0.8/0.9 MHz
            arc164.AddAction(3006, "UHF_POINT025_SEL", delay, 0.0, 0.0);        // 0.0-0.3, 0/25/50/75 KHz
            AddDevice(arc164);

            // ---- an/arc-186 vhf radio

            AirframeDevice arc186 = new(56, "VHF_ARC186");
            arc186.AddAction(3004, "VHFFM_MODE_TR", delay, 0.2, 0.2);
            arc186.AddAction(3004, "VHFFM_FREQEMER_MAN", delay, 0.2, 0.3);
            arc186.AddAction(3004, "VHFFM_FREQEMER_PRE", delay, 0.3, 0.3);
            arc186.AddAction(3006, "VHFFM_LOAD", delay, 1);
            arc186.AddAction(3001, "VHFFM_PRESET_UP", delay, 0.01);
            arc186.AddAction(3009, "VHFFM_FREQ1_UP", delay, 0.05);
            arc186.AddAction(3011, "VHFFM_FREQ2_UP", delay, 0.1);
            arc186.AddAction(3013, "VHFFM_FREQ3_UP", delay, 0.1);
            arc186.AddAction(3015, "VHFFM_FREQ4_UP", delay, 0.25);
            arc186.AddAction(3001, "VHFFM_PRESET_DN", delay, -0.01);
            arc186.AddAction(3009, "VHFFM_FREQ1_DN", delay, -0.05);
            arc186.AddAction(3011, "VHFFM_FREQ2_DN", delay, -0.1);
            arc186.AddAction(3013, "VHFFM_FREQ3_DN", delay, -0.1);
            arc186.AddAction(3015, "VHFFM_FREQ4_DN", delay, -0.25);
            AddDevice(arc186);
        }
    }
}
