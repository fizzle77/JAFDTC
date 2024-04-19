// ********************************************************************************************************************
//
// F16DeviceManager.cs -- f-16c airframe device manager
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

using JAFDTC.Utilities;
using JAFDTC.Models.DCS;
using System.Diagnostics;

namespace JAFDTC.Models.F16C
{
    /// <summary>
    /// manages the set of dcs airframe devices and associated commands/actions for the viper.
    /// </summary>
    internal class F16DeviceManager : AirframeDeviceManagerBase, IAirframeDeviceManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------
        
        public F16DeviceManager()
        {
            int delay = Settings.CommandDelaysMs[AirframeTypes.F16C];
            int delayMFDs = delay / 4;
            int delayList = delay / 4;
            int delayEntr = delay / 2;
            int delayDown = delay;
            int delayUp = delay;
            int delaySeq = delay;
            int delayRtn = delay;

            // ---- sms

            AirframeDevice sms = new(22, "SMS");
            sms.AddAction(3002, "LEFT_HDPT", 0, 1);
            sms.AddAction(3003, "RIGHT_HDPT", 0, 1);
            AddDevice(sms);

            // ---- ufc

            AirframeDevice ufc = new(17, "UFC");
            ufc.AddAction(3002, "0", 0, 1);
            ufc.AddAction(3003, "1", 0, 1);
            ufc.AddAction(3004, "2", 0, 1);
            ufc.AddAction(3005, "3", 0, 1);
            ufc.AddAction(3006, "4", 0, 1);
            ufc.AddAction(3007, "5", 0, 1);
            ufc.AddAction(3008, "6", 0, 1);
            ufc.AddAction(3009, "7", 0, 1);
            ufc.AddAction(3010, "8", 0, 1);
            ufc.AddAction(3011, "9", 0, 1);
            ufc.AddAction(3012, "COM1", 0, 1);
            ufc.AddAction(3013, "COM2", 0, 1);
            ufc.AddAction(3015, "LIST", delayList, 1);
            ufc.AddAction(3016, "ENTR", delayEntr, 1);
            ufc.AddAction(3017, "RCL", 0, 1);
            ufc.AddAction(3018, "AA", delay, 1);
            ufc.AddAction(3019, "AG", delay, 1);
            ufc.AddAction(3030, "INC", delay, 1);
            ufc.AddAction(3031, "DEC", delay, 1);
            ufc.AddAction(3032, "RTN", delayRtn, -1);
            ufc.AddAction(3033, "SEQ", delaySeq, 1);
            ufc.AddAction(3034, "UP", delayUp, 1);
            ufc.AddAction(3035, "DOWN", delayDown, -1);
            AddDevice(ufc);

            // ---- hotas buttons

            AirframeDevice hotas = new(16, "HOTAS");
            hotas.AddAction(3030, "DGFT", 0, 1, 1);
            hotas.AddAction(3030, "MSL", 0, -1, -1);
            hotas.AddAction(3030, "CENTER", 0, 0, 0);
            AddDevice(hotas);

            // ---- left mfd

            AirframeDevice leftMFD = new(24, "LMFD");
            leftMFD.AddAction(3012, "OSB-12-PG3", delayMFDs, 1);
            leftMFD.AddAction(3013, "OSB-13-PG2", delayMFDs, 1);
            leftMFD.AddAction(3014, "OSB-14-PG1", delayMFDs, 1);
            leftMFD.AddAction(3001, "OSB-01-BLANK", delayMFDs, 1);
            leftMFD.AddAction(3002, "OSB-02-HAD", delayMFDs, 1);
            leftMFD.AddAction(3003, "OSB-03", delayMFDs, 1);
            leftMFD.AddAction(3004, "OSB-04-RCCE", delayMFDs, 1);
            leftMFD.AddAction(3005, "OSB-05", delayMFDs, 1);
            leftMFD.AddAction(3006, "OSB-06-SMS", delayMFDs, 1);
            leftMFD.AddAction(3007, "OSB-07-HSD", delayMFDs, 1);
            leftMFD.AddAction(3008, "OSB-08-DTE", delayMFDs, 1);
            leftMFD.AddAction(3009, "OSB-09-TEST", delayMFDs, 1);
            leftMFD.AddAction(3010, "OSB-10-FLCS", delayMFDs, 1);
            leftMFD.AddAction(3016, "OSB-16-FLIR", delayMFDs, 1);
            leftMFD.AddAction(3017, "OSB-17-TFR", delayMFDs, 1);
            leftMFD.AddAction(3018, "OSB-18-WPN", delayMFDs, 1);
            leftMFD.AddAction(3019, "OSB-19-TGP", delayMFDs, 1);
            leftMFD.AddAction(3020, "OSB-20-FCR", delayMFDs, 1);
            AddDevice(leftMFD);

            // ---- right mfd

            AirframeDevice rightMFD = new(25, "RMFD");
            rightMFD.AddAction(3012, "OSB-12-PG3", delayMFDs, 1);
            rightMFD.AddAction(3013, "OSB-13-PG2", delayMFDs, 1);
            rightMFD.AddAction(3014, "OSB-14-PG1", delayMFDs, 1);
            rightMFD.AddAction(3001, "OSB-01-BLANK", delayMFDs, 1);
            rightMFD.AddAction(3002, "OSB-02-HAD", delayMFDs, 1);
            rightMFD.AddAction(3003, "OSB-03", delayMFDs, 1);
            rightMFD.AddAction(3004, "OSB-04-RCCE", delayMFDs, 1);
            rightMFD.AddAction(3005, "OSB-05", delayMFDs, 1);
            rightMFD.AddAction(3006, "OSB-06-SMS", delayMFDs, 1);
            rightMFD.AddAction(3007, "OSB-07-HSD", delayMFDs, 1);
            rightMFD.AddAction(3008, "OSB-08-DTE", delayMFDs, 1);
            rightMFD.AddAction(3009, "OSB-09-TEST", delayMFDs, 1);
            rightMFD.AddAction(3010, "OSB-10-FLCS", delayMFDs, 1);
            rightMFD.AddAction(3016, "OSB-16-FLIR", delayMFDs, 1);
            rightMFD.AddAction(3017, "OSB-17-TFR", delayMFDs, 1);
            rightMFD.AddAction(3018, "OSB-18-WPN", delayMFDs, 1);
            rightMFD.AddAction(3019, "OSB-19-TGP", delayMFDs, 1);
            rightMFD.AddAction(3020, "OSB-20-FCR", delayMFDs, 1);
            AddDevice(rightMFD);

            // ---- ehsi

            AirframeDevice ehsi = new(28, "EHSI");
            ehsi.AddAction(3001, "MODE", delay, 1);
            AddDevice(ehsi);

            // ---- hmcs panel

            AirframeDevice hmcsInt = new(30, "HMCS_INT");
            hmcsInt.AddAction(3001, "INT", 1, 0.0, 0.0);
            AddDevice(hmcsInt);

            // ---- interior lights

            AirframeDevice intl = new(12, "INTL");
            intl.AddAction(3002, "MAL_IND_LTS_TEST", delay, 1);
            AddDevice(intl);
        }
    }
}