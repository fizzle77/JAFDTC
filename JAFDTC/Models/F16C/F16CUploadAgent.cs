﻿// ********************************************************************************************************************
//
// F16CUploadAgent.cs -- f-16c upload agent
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
using JAFDTC.Models.F16C.MFD;
using JAFDTC.Models.F16C.Upload;
using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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
        private class F16CSetupBuilder : CoreSetupBuilder, IBuilder
        {
            public F16CSetupBuilder(F16CDeviceManager dcsCmds, StringBuilder sb) : base(dcsCmds, sb) { }

            public override void Build(Dictionary<string, object> state = null)
            {
                base.Build();

                AirframeDevice sms = _aircraft.GetDevice("SMS");

                AddIfBlock("IsLeftHdptOn", false, null, delegate () { AddAction(sms, "LEFT_HDPT"); });
                AddIfBlock("IsRightHdptOn", false, null, delegate () { AddAction(sms, "RIGHT_HDPT"); });

                // TODO: ensure cmds is on?
            }
        }

        // ================================================================================================================

        /// <summary>
        /// generates the command sequence for teardown in the strike eagle. this includes triggering light test
        /// feedback at the end of the sequence.
        /// </summary>
        private sealed class F16CTeardownBuilder : CoreTeardownBuilder, IBuilder
        {
            private readonly F16CConfiguration _cfg;

            public F16CTeardownBuilder(F16CConfiguration cfg, F16CDeviceManager dcsCmds, StringBuilder sb)
                : base(dcsCmds, sb)
            {
                _cfg = cfg;
            }

            public override void Build(Dictionary<string, object> state = null)
            {
                base.Build();

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

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly F16CConfiguration _cfg;
        private readonly F16CDeviceManager _dcsCmds;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CUploadAgent(F16CConfiguration cfg) => (_cfg, _dcsCmds) = (cfg, new F16CDeviceManager());

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
            MFDStateQueryBuilder queryMFD = new(_dcsCmds, null);
            Dictionary<string, object> state = queryMFD.QueryCurrentMFDStateForAllModes();

            SMSStateQueryBuilder querySMS = new(_dcsCmds, null);
            state = querySMS.QuerySMSMunitionsForMode(MFDSystem.MasterModes.ICP_AG, state);
            // TODO: maybe add this when munitions handle a2a weapons too?
            // state = querySMS.QuerySMSMunitionsForMode(MFDSystem.MasterModes.ICP_AA, state);

            // build the command stream to set up the jet. we will use the query state collected above to drive
            // decisions around how to twiddle the buttons in the jet.
            //
            new RadioBuilder(_cfg, _dcsCmds, sb).Build(state);
            new MiscBuilder(_cfg, _dcsCmds, sb).Build(state);
            new CMDSBuilder(_cfg, _dcsCmds, sb).Build(state);
            //
            // NOTE: hts must be done before mfd as mfd can set the man hts threat class which is only available if
            // NOTE: the hts manual table has been set up.
            //
            // NOTE: mfd will invoke the sms builder if there are sms changes and the sms page is selected
            //
            new HTSBuilder(_cfg, _dcsCmds, sb).Build(state);
            new MFDBuilder(_cfg, _dcsCmds, sb).Build(state);
            new HARMBuilder(_cfg, _dcsCmds, sb).Build(state);
            new DLNKBuilder(_cfg, _dcsCmds, sb).Build(state);
            new STPTBuilder(_cfg, _dcsCmds, sb).Build(state);
        }

        public override IBuilder SetupBuilder(StringBuilder sb) => new F16CSetupBuilder(_dcsCmds, sb);

        public override IBuilder TeardownBuilder(StringBuilder sb) => new F16CTeardownBuilder(_cfg, _dcsCmds, sb);
    }
}