// ********************************************************************************************************************
//
// IFFCCBuilder.cs -- a-10c iffcc system builder
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

using JAFDTC.Models.A10C.IFFCC;
using JAFDTC.Models.DCS;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.A10C.Upload
{
    internal class IFFCCBuilder : A10CBuilderBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public IFFCCBuilder(A10CConfiguration cfg, A10CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.IFFCC.IsDefault)
                return;

            AddExecFunction("NOP", new() { "==== IFFCCBuilder:Build()" });

            AirframeDevice ahcp = _aircraft.GetDevice("AHCP");
            AirframeDevice ufc = _aircraft.GetDevice("UFC");

            AddAction(ahcp, "IFFCC_TEST", WAIT_BASE);

            BuildIFFCC(ufc);

            AddAction(ahcp, "IFFCC_ON");
        }

        private void BuildIFFCC(AirframeDevice ufc)
        {
            // CCIP Consent
            int clicks = GetNumClicksForWraparoundSetting<CCIPConsentOptions>(IFFCCSystem.ExplicitDefaults.CCIPConsentValue,
                                                                              _cfg.IFFCC.CCIPConsentValue);
            AddActions(ufc, "DATA_DN", clicks);

            BuildAAS(ufc);
            BuildDisplayModes(ufc);
        }

        private void BuildAAS(AirframeDevice ufc)
        {
            AddAction(ufc, "SEL_DN");
            AddAction(ufc, "SEL_DN");

            if (_cfg.IFFCC.IsAASDefault)
            {
                AddAction(ufc, "SEL_DN");
                return;
            }

            AddAction(ufc, "ENTER");

            int queuedDnClicks = 0;

            if (!_cfg.IFFCC.IsManFxdDefault)
            {
                AddAction(ufc, "ENTER");

                if (!_cfg.IFFCC.IsFxdWingspanDefault)
                    AddActions(ufc, "DATA_UP", _cfg.IFFCC.FxdWingspanValue - IFFCCSystem.ExplicitDefaults.FxdWingspanValue);
                AddAction(ufc, "SEL_DN");

                if (!_cfg.IFFCC.IsFxdLengthDefault)
                    AddActions(ufc, "DATA_UP", _cfg.IFFCC.FxdLengthValue - IFFCCSystem.ExplicitDefaults.FxdLengthValue);
                AddAction(ufc, "SEL_DN");

                if (!_cfg.IFFCC.IsFxdTgtSpeedDefault)
                    AddActions(ufc, "DATA_UP", _cfg.IFFCC.FxdTgtSpeedValue - IFFCCSystem.ExplicitDefaults.FxdTgtSpeedValue);
                AddAction(ufc, "SEL_DN");

                AddAction(ufc, "ENTER"); // enter exiting section moves the cursor down on AAS list
            }
            else
                queuedDnClicks++;

            if (!_cfg.IFFCC.IsManRtyDefault)
            {
                UnqueueDnClicks(ufc, ref queuedDnClicks);
                AddAction(ufc, "ENTER");

                if (!_cfg.IFFCC.IsRtyWingspanDefault)
                    AddActions(ufc, "DATA_UP", _cfg.IFFCC.RtyWingspanValue - IFFCCSystem.ExplicitDefaults.RtyWingspanValue);
                AddAction(ufc, "SEL_DN");

                if (!_cfg.IFFCC.IsRtyLengthDefault)
                    AddActions(ufc, "DATA_UP", _cfg.IFFCC.RtyLengthValue - IFFCCSystem.ExplicitDefaults.RtyLengthValue);
                AddAction(ufc, "SEL_DN");

                if (!_cfg.IFFCC.IsRtyTgtSpeedDefault)
                    AddActions(ufc, "DATA_UP", _cfg.IFFCC.RtyTgtSpeedValue - IFFCCSystem.ExplicitDefaults.RtyTgtSpeedValue);
                AddAction(ufc, "SEL_DN");

                AddAction(ufc, "ENTER"); // enter exiting section moves the cursor down on AAS list
            }
            else
                queuedDnClicks++;

            if (!_cfg.IFFCC.AreAircraftPresetsDefault)
            {
                queuedDnClicks++;

                if (!_cfg.IFFCC.IsA10EnabledDefault)
                {
                    UnqueueDnClicks(ufc, ref queuedDnClicks);
                    AddAction(ufc, "ENTER");
                }
                queuedDnClicks++;

                if (!_cfg.IFFCC.IsF15EnabledDefault)
                {
                    UnqueueDnClicks(ufc, ref queuedDnClicks);
                    AddAction(ufc, "ENTER");
                }
                queuedDnClicks++;

                if (!_cfg.IFFCC.IsF16EnabledDefault)
                {
                    UnqueueDnClicks(ufc, ref queuedDnClicks);
                    AddAction(ufc, "ENTER");
                }
                queuedDnClicks++;

                if (!_cfg.IFFCC.IsF18EnabledDefault)
                {
                    UnqueueDnClicks(ufc, ref queuedDnClicks);
                    AddAction(ufc, "ENTER");
                }
                queuedDnClicks++;

                if (!_cfg.IFFCC.IsMig29EnabledDefault)
                {
                    UnqueueDnClicks(ufc, ref queuedDnClicks);
                    AddAction(ufc, "ENTER");
                }
                queuedDnClicks++;

                if (!_cfg.IFFCC.IsSu27EnabledDefault)
                {
                    UnqueueDnClicks(ufc, ref queuedDnClicks);
                    AddAction(ufc, "ENTER");
                }
                queuedDnClicks++;

                if (!_cfg.IFFCC.IsSu25EnabledDefault)
                {
                    UnqueueDnClicks(ufc, ref queuedDnClicks);
                    AddAction(ufc, "ENTER");
                }
                queuedDnClicks++;

                if (!_cfg.IFFCC.IsAH64EnabledDefault)
                {
                    UnqueueDnClicks(ufc, ref queuedDnClicks);
                    AddAction(ufc, "ENTER");
                }
                queuedDnClicks++;


                if (!_cfg.IFFCC.IsUH60EnabledDefault)
                {
                    UnqueueDnClicks(ufc, ref queuedDnClicks);
                    AddAction(ufc, "ENTER");
                }
                queuedDnClicks++;

                if (!_cfg.IFFCC.IsMi8EnabledDefault)
                {
                    UnqueueDnClicks(ufc, ref queuedDnClicks);
                    AddAction(ufc, "ENTER");
                }
                queuedDnClicks++;

                queuedDnClicks += 2;
            }

            UnqueueDnClicks(ufc, ref queuedDnClicks);
            AddAction(ufc, "ENTER");
        }

        private void BuildDisplayModes(AirframeDevice ufc)
        {
            if (_cfg.IFFCC.IsDisplayModesDefault)
                return;

            AddAction(ufc, "SEL_DN");
            AddAction(ufc, "SEL_DN");
            AddAction(ufc, "ENTER");

            // Auto Data Display
            if (!_cfg.IFFCC.IsAutoDataDisplayDefault)
                AddAction(ufc, "DATA_DN");
            AddAction(ufc, "SEL_DN");

            // CCIP Gun Cross "Occult"
            if (!_cfg.IFFCC.IsCCIPGunCrossOccultDefault)
                AddAction(ufc, "DATA_DN");
            AddAction(ufc, "SEL_DN");

            // Tapes
            if (!_cfg.IFFCC.IsTapesDefault)
                AddAction(ufc, "DATA_DN");
            AddAction(ufc, "SEL_DN");

            // Metric
            if (!_cfg.IFFCC.IsMetricDefault)
                AddAction(ufc, "DATA_DN");
            AddAction(ufc, "SEL_DN");

            // Radar Alt Tape
            if (!_cfg.IFFCC.IsRdrAltTapeDefault)
                AddAction(ufc, "DATA_DN");
            AddAction(ufc, "SEL_DN");

            // Airspeed
            if (!_cfg.IFFCC.IsAirspeedDefault)
                AddActions(ufc, "DATA_DN", _cfg.IFFCC.AirspeedValue - IFFCCSystem.ExplicitDefaults.AirspeedValue);
            AddAction(ufc, "SEL_DN");

            // Vertical Velocity
            if (!_cfg.IFFCC.IsVertVelDefault)
                AddAction(ufc, "DATA_DN");
            AddAction(ufc, "SEL_DN");

            AddAction(ufc, "SEL_DN");
            AddAction(ufc, "ENTER");
        }

        private void UnqueueDnClicks(AirframeDevice ufc, ref int queuedDnClicks)
        {
            AddActions(ufc, "SEL_DN", queuedDnClicks);
            queuedDnClicks = 0;
        }
    }
}
