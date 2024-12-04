// ********************************************************************************************************************
//
// TGPBuilder.cs -- a-10c tgp system builder
//
// Copyright(C) 2024 fizzle, JAFDTC contributors
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

using JAFDTC.Models.A10C.TGP;
using JAFDTC.Models.DCS;
using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.A10C.Upload
{
    internal class TGPBuilder : A10CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public TGPBuilder(A10CConfiguration cfg, A10CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.TGP.IsDefault)
                return;

            AddExecFunction("NOP", new() { "==== TGPBuilder:Build()" });

            AirframeDevice hotas = _aircraft.GetDevice("HOTAS");
            AirframeDevice cdu = _aircraft.GetDevice("CDU");
            AirframeDevice rmfd = _aircraft.GetDevice("RMFD");

            // Ensure TGP is on default MFD button and warmed up.
            AddIfBlock("IsTGPInDefaultMFDPosition", true, null, delegate ()
            {
                AddAction(rmfd, "RMFD_15", WAIT_BASE); // Go to TGP page
                AddIfBlock("IsTGPReady", true, null, delegate () { BuildTGP(hotas, cdu, rmfd); });
            });
        }

        private void BuildTGP(AirframeDevice hotas, AirframeDevice cdu, AirframeDevice rmfd)
        {
            AddAction(rmfd, "RMFD_02", 1200); // A-G, wait to go active

            // Video Mode
            if (!_cfg.TGP.VideoModeIsDefault)
            {
                AddAction(rmfd, "RMFD_15", WAIT_BASE); // TGP SOI

                string action = (VideoModeOptions)_cfg.TGP.VideoModeValue switch
                {
                    VideoModeOptions.BHOT => "BOAT_SWITCH_FWD",
                    VideoModeOptions.WHOT => "BOAT_SWITCH_AFT",
                    VideoModeOptions.CCD => "BOAT_SWITCH_CENTER",
                    _ => throw new ApplicationException("Unexpected video mode: " + _cfg.TGP.VideoModeValue)
                };
                AddAction(hotas, action);
            }

            AddAction(rmfd, "RMFD_01", WAIT_BASE); // CNTL

            // Coordinate Display
            int clicks = GetNumClicksForWraparoundSetting<CoordDisplayOptions>(TGPSystem.ExplicitDefaults.CoordDisplayValue, _cfg.TGP.CoordDisplayValue);
            for (int i = 0; i < clicks; i++)
                AddAction(rmfd, "RMFD_07");

            // Laser Code
            if (!_cfg.TGP.LaserCodeIsDefault)
            {
                AddActions(cdu, ActionsForCleanNum(_cfg.TGP.LaserCode));
                AddAction(rmfd, "RMFD_18");
            }

            // LSS
            if (!_cfg.TGP.LSSIsDefault)
            {
                AddActions(cdu, ActionsForCleanNum(_cfg.TGP.LSS));
                AddAction(rmfd, "RMFD_17");
            }

            // Latch
            if (!_cfg.TGP.LatchIsDefault)
                AddAction(rmfd, "RMFD_08");

            // TAAF
            if (!_cfg.TGP.TAAFIsDefault)
            {
                AddActions(cdu, ActionsForCleanNum(_cfg.TGP.TAAF));
                AddAction(rmfd, "RMFD_20");
            }

            // FRND
            if (!_cfg.TGP.FrndIsDefault)
                AddAction(rmfd, "RMFD_19");

            // Yardstick
            clicks = GetNumClicksForWraparoundSetting<YardstickOptions>(TGPSystem.ExplicitDefaults.YardstickValue, _cfg.TGP.YardstickValue);
            for (int i = 0; i < clicks; i++)
                AddAction(rmfd, "RMFD_09");

            AddAction(rmfd, "RMFD_01"); // RTN
            AddAction(rmfd, "RMFD_03"); // STBY
        }
    }
}
