// ********************************************************************************************************************
//
// F16CUploadAgent.cs -- f-16c upload agent
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023-2025 ilominar/raven
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
using JAFDTC.Models.F16C.MFD;
using JAFDTC.Models.F16C.Upload;
using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.F16C
{
    /// <summary>
    /// upload agent responsible for building a stream of commands for use by dcs to set up viper avionics according
    /// to a configuration.
    /// </summary>
    public class F16CUploadAgent : UploadAgentBase, IUploadAgent
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // private classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// generates the command sequence for setup in the viper. this includes making sure the left and right
        /// hardpoints have power applied.
        /// </summary>
        private class F16CSetupBuilder(F16CDeviceManager dcsCmds, StringBuilder sb)
                      : CoreSetupBuilder(dcsCmds, sb), IBuilder
        {
            public override void Build(Dictionary<string, object> state = null)
            {
                AddExecFunction("NOP", [ "==== F16CSetupBuilder:Build()" ]);

                base.Build();

                AirframeDevice sms = _aircraft.GetDevice("SMS");
                AirframeDevice ufc = _aircraft.GetDevice("UFC");
                AirframeDevice hotas = _aircraft.GetDevice("HOTAS");

                AddIfBlock("IsLeftHdptOn", false, null, delegate () { AddAction(sms, "LEFT_HDPT"); });
                AddIfBlock("IsRightHdptOn", false, null, delegate () { AddAction(sms, "RIGHT_HDPT"); });

                // TODO: ensure cmds is on?

                AddIfBlock("IsInNAVMode", false, null, delegate ()
                {
                    AddAction(hotas, "CENTER");
                    AddActions(ufc, [ "LIST", "8" ], null, WAIT_BASE);
                    AddWhileBlock("IsShowingAAMode", false, null, delegate () { AddAction(ufc, "SEQ"); });
                    AddIfBlock("IsSelectingAAMode", true, null, delegate () { AddAction(ufc, "0"); });
                    AddWhileBlock("IsShowingAGMode", false, null, delegate () { AddAction(ufc, "SEQ"); });
                    AddIfBlock("IsSelectingAGMode", true, null, delegate () { AddAction(ufc, "0"); });
                    AddActions(ufc, [ "RTN", "RTN" ], null, WAIT_BASE);
                });
            }
        }

        // ================================================================================================================

        /// <summary>
        /// generates the command sequence for teardown in the viper. this includes triggering light test feedback
        /// at the end of the sequence.
        /// </summary>
        private sealed class F16CTeardownBuilder(F16CConfiguration cfg, F16CDeviceManager dcsCmds, StringBuilder sb)
                             : CoreTeardownBuilder(dcsCmds, sb), IBuilder
        {
            private readonly F16CConfiguration _cfg = cfg;

            public override void Build(Dictionary<string, object> state = null)
            {
                AddExecFunction("NOP", [ "==== F16CTeardownBuilder:Build()" ]);

                base.Build();

                AirframeDevice ufc = _aircraft.GetDevice("UFC");
                AirframeDevice hotas = _aircraft.GetDevice("HOTAS");

                AddIfBlock("IsInNAVMode", false, null, delegate ()
                {
                    AddAction(hotas, "CENTER");
                    AddActions(ufc, [ "LIST", "8" ], null, WAIT_BASE);
                    AddWhileBlock("IsShowingAAMode", false, null, delegate () { AddAction(ufc, "SEQ"); });
                    AddIfBlock("IsSelectingAAMode", true, null, delegate () { AddAction(ufc, "0"); });
                    AddWhileBlock("IsShowingAGMode", false, null, delegate () { AddAction(ufc, "SEQ"); });
                    AddIfBlock("IsSelectingAGMode", true, null, delegate () { AddAction(ufc, "0"); });
                    AddActions(ufc, [ "RTN", "RTN" ], null, WAIT_BASE);
                });

                if ((Settings.UploadFeedback == SettingsData.UploadFeedbackTypes.AUDIO_LIGHTS) ||
                    (Settings.UploadFeedback == SettingsData.UploadFeedbackTypes.LIGHTS))
                {
                    AirframeDevice intl = _aircraft.GetDevice("INTL");
                    AddDynamicAction(intl, "MAL_IND_LTS_TEST", 0, 1);
                    AddWait(2000);
                    AddDynamicAction(intl, "MAL_IND_LTS_TEST", 1, 0);
                }
            }
        }

        // ================================================================================================================

        /// <summary>
        /// generates the command sequence for opening the dtc in the viper.
        /// </summary>
        private class F16COpenDTCBuilder(F16CDeviceManager dcsCmds, StringBuilder sb)
                      : BuilderBase(dcsCmds, sb), IBuilder
        {
            public override void Build(Dictionary<string, object> state = null)
            {
                AddExecFunction("NOP", ["==== F16COpenDTCBuilder:Build()"]);
                AddLoCommands([ DCSiCommand.CMENU_TOGGLE,
                                DCSiCommand.CMENU_ITEM_11, DCSiCommand.CMENU_ITEM_11, DCSiCommand.CMENU_ITEM_11,
                                DCSiCommand.CMENU_ITEM_08,
                                DCSiCommand.CMENU_ITEM_06,
                                DCSiCommand.CMENU_ITEM_01 ], WAIT_SHORT);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly F16CConfiguration _cfg;
        private readonly F16CDeviceManager _dm;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CUploadAgent(F16CConfiguration cfg) => (_cfg, _dm) = (cfg, new F16CDeviceManager());

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void BuildSystems(StringBuilder sb)
        {
            // perform state queries to capture current avionics state that we are interested in including the mfd
            // format setup and the munitions on jet according to sms pages.
            //
            MFDStateQueryBuilder queryMFD = new(_dm, null);
            Dictionary<string, object> state = queryMFD.QueryCurrentMFDStateForAllModes();

            SMSStateQueryBuilder querySMS = new(_dm, null);
            state = querySMS.QuerySMSMunitionsForMode(MFDSystem.MasterModes.ICP_AG, state);
            // TODO: add this when munitions handle a2a weapons too?
            // state = querySMS.QuerySMSMunitionsForMode(MFDSystem.MasterModes.ICP_AA, state);

            // build the command stream to set up the jet. we will use the query state collected above to drive
            // decisions around how to twiddle the buttons in the jet.
            //
            // NOTE: general expectation by builders is that master mode is NAV and DED page is default on entry.
            // NOTE: builders must preserve master mode, DED state, and MFD state (one exception is MFDBuilder does
            // NOTE: not maintain MFD state to set avionics up as requested).
            //
            new DTEBuilder(_cfg, _dm, sb).Build(state);
            new RadioBuilder(_cfg, _dm, sb).Build(state);
            new MiscBuilder(_cfg, _dm, sb).Build(state);
            new CMDSBuilder(_cfg, _dm, sb).Build(state);
            //
            // NOTE: hts man threat table must be built before the mfd as mfd builds can select threat classes in the
            // NOTE: had format and man is only available if the hts manual table is set up (i.e., non-default).
            //
            new HTSManTableBuilder(_cfg, _dm, sb).Build(state);
            new HARMBuilder(_cfg, _dm, sb).Build(state);
            //
            // NOTE: mfd will invoke the sms builder if there are sms changes and the sms page is added to an mfd in
            // NOTE: the appropriate master mode.
            //
            new MFDBuilder(_cfg, _dm, sb).Build(state);
            new DLNKBuilder(_cfg, _dm, sb).Build(state);
            new STPTBuilder(_cfg, _dm, sb).Build(state);
        }

        public override IBuilder SetupBuilder(StringBuilder sb) => new F16CSetupBuilder(_dm, sb);

        public override IBuilder TeardownBuilder(StringBuilder sb) => new F16CTeardownBuilder(_cfg, _dm, sb);

        public override IBuilder OpenDCSDTCBuilder(StringBuilder sb) => new F16COpenDTCBuilder(_dm, sb);
    }
}