// ********************************************************************************************************************
//
// F16CCommands.cs -- f-16c commands
//
// Copyright(C) 2021-2023 the-paid-actor & others
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
	/// manages the set of dcs cockpit commands associated with devices in the viper.
	/// </summary>
	internal class F16CCommands : AirframeDeviceManagerBase, IAirframeDeviceManager
	{
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------
        
		public F16CCommands()
		{
			int delay = Settings.CommandDelaysMs[AirframeTypes.F16C];

			int delayMFDs = delay / 4;
            int delayList = delay / 4;
            int delayEntr = delay / 2;
            int delayDown = delay;
            int delayUp = delay;
            int delaySeq = delay;
            int delayRtn = delay;

			Device sms = new(22, "SMS");
			sms.AddCommand(new Command(3002, "LEFT_HDPT", 0, 1));
			sms.AddCommand(new Command(3003, "RIGHT_HDPT", 0, 1));
			AddDevice(sms);

            Device ufc = new(17, "UFC");
			ufc.AddCommand(new Command(3002, "0", 0, 1));
			ufc.AddCommand(new Command(3003, "1", 0, 1));
			ufc.AddCommand(new Command(3004, "2", 0, 1));
			ufc.AddCommand(new Command(3005, "3", 0, 1));
			ufc.AddCommand(new Command(3006, "4", 0, 1));
			ufc.AddCommand(new Command(3007, "5", 0, 1));
			ufc.AddCommand(new Command(3008, "6", 0, 1));
			ufc.AddCommand(new Command(3009, "7", 0, 1));
			ufc.AddCommand(new Command(3010, "8", 0, 1));
			ufc.AddCommand(new Command(3011, "9", 0, 1));
			ufc.AddCommand(new Command(3012, "COM1", 0, 1));
			ufc.AddCommand(new Command(3013, "COM2", 0, 1));
			ufc.AddCommand(new Command(3015, "LIST", delayList, 1));
			ufc.AddCommand(new Command(3016, "ENTR", delayEntr, 1));
			ufc.AddCommand(new Command(3017, "RCL", 0, 1));
			ufc.AddCommand(new Command(3018, "AA", delay, 1));
			ufc.AddCommand(new Command(3019, "AG", delay, 1));
			ufc.AddCommand(new Command(3030, "INC", delay, 1));
			ufc.AddCommand(new Command(3031, "DEC", delay, 1));
			ufc.AddCommand(new Command(3032, "RTN", delayRtn, -1));
			ufc.AddCommand(new Command(3033, "SEQ", delaySeq, 1));
			ufc.AddCommand(new Command(3034, "UP", delayUp, 1));
			ufc.AddCommand(new Command(3035, "DOWN", delayDown, -1));
			AddDevice(ufc);

            Device hotas = new(16, "HOTAS");
			hotas.AddCommand(new Command(3030, "DGFT", -1, 1));
			hotas.AddCommand(new Command(3030, "MSL", -1, -1));
			hotas.AddCommand(new Command(3030, "CENTER", -1, 0));
			AddDevice(hotas);

            Device leftMFD = new(24, "LMFD");
			leftMFD.AddCommand(new Command(3012, "OSB-12-PG3", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3013, "OSB-13-PG2", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3014, "OSB-14-PG1", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3001, "OSB-01-BLANK", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3002, "OSB-02-HAD", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3003, "OSB-03", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3004, "OSB-04-RCCE", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3005, "OSB-05", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3006, "OSB-06-SMS", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3007, "OSB-07-HSD", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3008, "OSB-08-DTE", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3009, "OSB-09-TEST", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3010, "OSB-10-FLCS", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3016, "OSB-16-FLIR", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3017, "OSB-17-TFR", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3018, "OSB-18-WPN", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3019, "OSB-19-TGP", delayMFDs, 1));
			leftMFD.AddCommand(new Command(3020, "OSB-20-FCR", delayMFDs, 1));
			AddDevice(leftMFD);

            Device rightMFD = new(25, "RMFD");
			rightMFD.AddCommand(new Command(3012, "OSB-12-PG3", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3013, "OSB-13-PG2", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3014, "OSB-14-PG1", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3001, "OSB-01-BLANK", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3002, "OSB-02-HAD", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3003, "OSB-03", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3004, "OSB-04-RCCE", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3005, "OSB-05", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3006, "OSB-06-SMS", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3007, "OSB-07-HSD", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3008, "OSB-08-DTE", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3009, "OSB-09-TEST", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3010, "OSB-10-FLCS", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3016, "OSB-16-FLIR", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3017, "OSB-17-TFR", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3018, "OSB-18-WPN", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3019, "OSB-19-TGP", delayMFDs, 1));
			rightMFD.AddCommand(new Command(3020, "OSB-20-FCR", delayMFDs, 1));
			AddDevice(rightMFD);

			Device ehsi = new(28, "EHSI");
			ehsi.AddCommand(new Command(3001, "MODE", delay, 1));
			AddDevice(ehsi);

			// HACK A'HOY: generally, on the dcs side, a command has the semantics of "push, return to neurtal"
			// HACK A'HOY: (that is, we don't send separate "press" and "release" commands for a button, just a
			// HACK A'HOY: press command that implicitly has a release. this is broken for knob-like controls where
			// HACK A'HOY: you want to leave the control where it was set, not return to zero.
			//
			// HACK A'HOY: turns out, sending a negative delay will inhibit the implicit "release". maybe this is
			// HACK A'HOY: "by design", maybe not, but it serves our purpose here and saves us from having to write
			// HACK A'HOY: a bunch of code.
			//
			Device hmcsInt = new(30, "HMCS_INT");
            hmcsInt.AddCommand(new Command(3001, "INT", -1, 0.0));
            AddDevice(hmcsInt);
        }
    }
}