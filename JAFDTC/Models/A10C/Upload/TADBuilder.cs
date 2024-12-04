// ********************************************************************************************************************
//
// TADBuilder.cs -- a-10c tad system builder
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

using JAFDTC.Models.A10C.TAD;
using JAFDTC.Models.DCS;
using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.A10C.Upload
{
    internal class TADBuilder : A10CBuilderBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public TADBuilder(A10CConfiguration cfg, A10CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.TAD.IsDefault)
                return;

            AddExecFunction("NOP", new() { "==== TADBuilder:Build()" });

            AirframeDevice hotas = _aircraft.GetDevice("HOTAS");
            AirframeDevice cdu = _aircraft.GetDevice("CDU");
            AirframeDevice lmfd = _aircraft.GetDevice("LMFD");

            // Ensure TGP is on default MFD button and warmed up.
            AddIfBlock("IsTADInDefaultMFDPosition", true, null, delegate () { BuildTAD(hotas, cdu, lmfd); });
        }

        private void BuildTAD(AirframeDevice hotas, AirframeDevice cdu, AirframeDevice lmfd)
        {
            AddActions(lmfd, new() { "LMFD_15", "LMFD_15" } ); // Go to TGP page, make it SOI

            // Coordinate Display
            if (!_cfg.TAD.IsCoordDisplayDefault)
            {
                int clicks = GetNumClicksForWraparoundSetting<CoordDisplayOptions>(TADSystem.ExplicitDefaults.CoordDisplayValue,
                    _cfg.TAD.CoordDisplayValue);
                for (int i = 0; i < clicks; i++)
                    AddAction(lmfd, "LMFD_09");
            }

            // Datalink Options
            if (!_cfg.TAD.IsDatalinkDefault)
            {
                AddAction(lmfd, "LMFD_10"); // NET

                // Group ID
                if (!_cfg.TAD.IsGrpIDDefault)
                {
                    AddActions(cdu, ActionsForCleanNum(_cfg.TAD.GrpID));
                    AddAction(lmfd, "LMFD_08");
                }

                // Own ID
                if (!_cfg.TAD.IsOwnIDDefault)
                {
                    AddActions(cdu, ActionsForCleanNum(_cfg.TAD.OwnID));
                    AddAction(lmfd, "LMFD_07");
                }

                // Callsign
                if (!_cfg.TAD.IsCallsignDefault)
                {
                    AddActions(cdu, ActionsForString(_cfg.TAD.Callsign));
                    AddAction(lmfd, "LMFD_17");
                }

                AddAction(lmfd, "LMFD_01"); // TAD
            }

            // Hook Option
            if (!_cfg.TAD.IsHookOptionDefault)
            {
                AddActions(hotas, new() { "TMS_DN", "TMS_OFF" });
                AddActions(hotas, new() { "CHINA_HAT_AFT", "CHINA_HAT_OFF" });

                int clicks = GetNumClicksForWraparoundSetting<HookModeOptions>(TADSystem.ExplicitDefaults.HookOptionValue,
                    _cfg.TAD.HookOptionValue);
                for (int i = 0; i < clicks; i++)
                    AddAction(lmfd, "LMFD_18");
            }

            // Hook Ownship
            if (_cfg.TAD.HookOwnshipValue)
            {
                AddActions(hotas, new() { "TMS_DN", "TMS_OFF" });
                AddActions(hotas, new() { "CHINA_HAT_AFT", "CHINA_HAT_OFF" });
                AddActions(hotas, new() { "TMS_UP", "TMS_OFF" });
            }

            // Map Range
            if (!_cfg.TAD.IsMapRangeDefault)
            {
                int clicks = _cfg.TAD.MapRangeValue - TADSystem.ExplicitDefaults.MapRangeValue;
                for (int i = 0; i < clicks; i++)
                    AddActions(hotas, new() { "DMS_DN", "DMS_OFF" });
            }

            // Ownship Position: Center/Depress
            if (!_cfg.TAD.IsCenterDepressedDefault)
                AddActions(hotas, new() { "DMS_RIGHT", "DMS_OFF" });

            // Map Option
            if (!_cfg.TAD.IsMapOptionDefault)
            {
                int clicks = GetNumClicksForWraparoundSetting<MapOptions>(
                    TADSystem.ExplicitDefaults.MapOptionValue, _cfg.TAD.MapOptionValue);
                for (int i = 0; i < clicks; i++)
                    AddAction(lmfd, "LMFD_20");
            }

            // Profile Display Options
            if (!_cfg.TAD.IsProfileDefault)
            {
                AddAction(lmfd, "LMFD_01"); // CNTL
                AddAction(lmfd, "LMFD_17"); // CHG SET

                int queuedDownClicks = 0;

                // Bullseye
                if (!_cfg.TAD.IsProfileBullseyeDefault)
                    AddAction(lmfd, "LMFD_18");
                queuedDownClicks++;

                // Range Rings
                if (!_cfg.TAD.IsProfileRangeRingsDefault)
                {
                    queuedDownClicks = UnqueueDownClicks(lmfd, queuedDownClicks);
                    AddAction(lmfd, "LMFD_18");
                }
                queuedDownClicks++;

                // Hook Info
                if (!_cfg.TAD.IsProfileHookInfoDefault)
                {
                    queuedDownClicks = UnqueueDownClicks(lmfd, queuedDownClicks);
                    int clicks = GetNumClicksForWraparoundSetting<HookInfoVisibilityOptions>(
                        TADSystem.ExplicitDefaults.ProfileHookInfoValue, _cfg.TAD.ProfileHookInfoValue);
                    for (int i = 0; i < clicks; i++)
                        AddAction(lmfd, "LMFD_18");
                }
                queuedDownClicks++;

                // Waypoint Lines
                if (!_cfg.TAD.IsProfileWaypointLinesDefault)
                {
                    queuedDownClicks = UnqueueDownClicks(lmfd, queuedDownClicks);
                    AddAction(lmfd, "LMFD_18");
                }
                queuedDownClicks++;

                // Waypoint Label
                if (!_cfg.TAD.IsProfileWaypointLabelDefault)
                {
                    queuedDownClicks = UnqueueDownClicks(lmfd, queuedDownClicks);
                    AddAction(lmfd, "LMFD_18");
                }
                queuedDownClicks++;

                // Waypoints
                if (!_cfg.TAD.IsProfileWaypointsDefault)
                {
                    queuedDownClicks = UnqueueDownClicks(lmfd, queuedDownClicks);
                    AddAction(lmfd, "LMFD_18");
                }
                queuedDownClicks++;

                // SPI Display
                if (!_cfg.TAD.IsProfileSPIDisplayDefault)
                {
                    UnqueueDownClicks(lmfd, queuedDownClicks);
                    AddAction(lmfd, "LMFD_18");
                }

                AddAction(lmfd, "LMFD_03"); // SAVE
                AddAction(lmfd, "LMFD_01"); // TAD
            }
        }

        private int UnqueueDownClicks(AirframeDevice lmfd, int clicks)
        {
            for (int i = 0; i < clicks; i++)
                AddAction(lmfd, "LMFD_19");
            return 0;
        }
    }
}
