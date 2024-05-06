// ********************************************************************************************************************
//
// FA18CDeviceManager.cs -- fa-18c commands
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

namespace JAFDTC.Models.FA18C
{
    /// <summary>
    /// manages the set of dcs airframe devices and associated commands/actions for the hornet.
    /// </summary>
    internal class FA18CDeviceManager : AirframeDeviceManagerBase, IAirframeDeviceManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public FA18CDeviceManager()
        {
            var delay = Settings.CommandDelaysMs[AirframeTypes.FA18C];

            var delayMFDs = delay;
            var delayUFC = delay / 2;
            var delayUFCOpt = delay;
            var delayUFCOnOff = delay;
            var delayUFCEnt = delay * 2;
            var delayIFEI = delay;
            var delayRot = delay / 20;

            // ---- ufc

            AirframeDevice ufc = new(25, "UFC");
            ufc.AddAction(3001, "AP", delayUFC, 1);
            ufc.AddAction(3002, "IFF", delayUFC, 1);
            ufc.AddAction(3003, "TCN", delayUFC, 1);
            ufc.AddAction(3004, "ILS", delayUFC, 1);
            ufc.AddAction(3005, "DL", delayUFC, 1);
            ufc.AddAction(3006, "BCN", delayUFC, 1);
            ufc.AddAction(3007, "OnOff", delayUFCOnOff, 1);
            ufc.AddAction(3008, "COM1", delayUFCOnOff, 1);
            ufc.AddAction(3009, "COM2", delayUFCOnOff, 1);
            ufc.AddAction(3010, "Opt1", delayUFCOpt, 1);
            ufc.AddAction(3011, "Opt2", delayUFCOpt, 1);
            ufc.AddAction(3012, "Opt3", delayUFCOpt, 1);
            ufc.AddAction(3013, "Opt4", delayUFCOpt, 1);
            ufc.AddAction(3014, "Opt5", delayUFCOpt, 1);
            ufc.AddAction(3018, "0", delayUFC, 1);
            ufc.AddAction(3019, "1", delayUFC, 1);
            ufc.AddAction(3020, "2", delayUFC, 1);
            ufc.AddAction(3021, "3", delayUFC, 1);
            ufc.AddAction(3022, "4", delayUFC, 1);
            ufc.AddAction(3023, "5", delayUFC, 1);
            ufc.AddAction(3024, "6", delayUFC, 1);
            ufc.AddAction(3025, "7", delayUFC, 1);
            ufc.AddAction(3026, "8", delayUFC, 1);
            ufc.AddAction(3027, "9", delayUFC, 1);
            ufc.AddAction(3028, "CLR", delayUFC, 1);
            ufc.AddAction(3029, "ENT", delayUFCEnt, 1);

            ufc.AddAction(3033, "COM1ChDec", 0, -1, -1);
            ufc.AddAction(3033, "COM1ChInc", 0, 1, 1);
            ufc.AddAction(3034, "COM2ChDec", 0, -1, -1);
            ufc.AddAction(3034, "COM2ChInc", 0, 1, 1);
            AddDevice(ufc);

            // ---- ifei

            AirframeDevice ifei = new(33, "IFEI");
            ifei.AddAction(3003, "UP", delayIFEI, 1);
            ifei.AddAction(3004, "DOWN", delayIFEI, 1);
            AddDevice(ifei);

            // ---- left mfd

            AirframeDevice leftMFD = new(35, "LMFD");
            leftMFD.AddAction(3011, "OSB-01", delayMFDs, 1);
            leftMFD.AddAction(3012, "OSB-02", delayMFDs, 1);
            leftMFD.AddAction(3013, "OSB-03", delayMFDs, 1);
            leftMFD.AddAction(3014, "OSB-04", delayMFDs, 1);
            leftMFD.AddAction(3015, "OSB-05", delayMFDs, 1);
            leftMFD.AddAction(3016, "OSB-06", delayMFDs, 1);
            leftMFD.AddAction(3017, "OSB-07", delayMFDs, 1);
            leftMFD.AddAction(3018, "OSB-08", delayMFDs, 1);
            leftMFD.AddAction(3019, "OSB-09", delayMFDs, 1);
            leftMFD.AddAction(3020, "OSB-10", delayMFDs, 1);
            leftMFD.AddAction(3021, "OSB-11", delayMFDs, 1);
            leftMFD.AddAction(3022, "OSB-12", delayMFDs, 1);
            leftMFD.AddAction(3023, "OSB-13", delayMFDs, 1);
            leftMFD.AddAction(3024, "OSB-14", delayMFDs, 1);
            leftMFD.AddAction(3025, "OSB-15", delayMFDs, 1);
            leftMFD.AddAction(3026, "OSB-16", delayMFDs, 1);
            leftMFD.AddAction(3027, "OSB-17", delayMFDs, 1);
            leftMFD.AddAction(3028, "OSB-18", delayMFDs, 1);
            leftMFD.AddAction(3029, "OSB-19", delayMFDs, 1);
            leftMFD.AddAction(3030, "OSB-20", delayMFDs, 1);
            AddDevice(leftMFD);

            // ---- right mfd

            AirframeDevice rightMFD = new(36, "RMFD");
            rightMFD.AddAction(3011, "OSB-01", delayMFDs, 1);
            rightMFD.AddAction(3012, "OSB-02", delayMFDs, 1);
            rightMFD.AddAction(3013, "OSB-03", delayMFDs, 1);
            rightMFD.AddAction(3014, "OSB-04", delayMFDs, 1);
            rightMFD.AddAction(3015, "OSB-05", delayMFDs, 1);
            rightMFD.AddAction(3016, "OSB-06", delayMFDs, 1);
            rightMFD.AddAction(3017, "OSB-07", delayMFDs, 1);
            rightMFD.AddAction(3018, "OSB-08", delayMFDs, 1);
            rightMFD.AddAction(3019, "OSB-09", delayMFDs, 1);
            rightMFD.AddAction(3020, "OSB-10", delayMFDs, 1);
            rightMFD.AddAction(3021, "OSB-11", delayMFDs, 1);
            rightMFD.AddAction(3022, "OSB-12", delayMFDs, 1);
            rightMFD.AddAction(3023, "OSB-13", delayMFDs, 1);
            rightMFD.AddAction(3024, "OSB-14", delayMFDs, 1);
            rightMFD.AddAction(3025, "OSB-15", delayMFDs, 1);
            rightMFD.AddAction(3026, "OSB-16", delayMFDs, 1);
            rightMFD.AddAction(3027, "OSB-17", delayMFDs, 1);
            rightMFD.AddAction(3028, "OSB-18", delayMFDs, 1);
            rightMFD.AddAction(3029, "OSB-19", delayMFDs, 1);
            rightMFD.AddAction(3030, "OSB-20", delayMFDs, 1);
            AddDevice(rightMFD);

            // ---- radar altimeter

            AirframeDevice radarAltimeter = new(30, "RadAlt");
            radarAltimeter.AddAction(3002, "Decrease", delayRot, -8);
            radarAltimeter.AddAction(3002, "Increase", delayRot, 0.015);
            radarAltimeter.AddAction(3001, "Test", delay, 1);
            AddDevice(radarAltimeter);

            // ---- cmds

            AirframeDevice cmds = new(54, "CMDS");
            cmds.AddAction(3001, "ON", -1, 0.1);
            cmds.AddAction(3001, "OFF", -1, -1);
            cmds.AddAction(3001, "BYPASS", -1, 1);
            AddDevice(cmds);

            // ---- interior lights

            AirframeDevice intl = new(9, "INTL");
            intl.AddAction(3007, "LIGHTS_TEST_SW", delay, 1);
            AddDevice(intl);
        }
    }
}
