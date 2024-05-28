// ********************************************************************************************************************
//
// A10CUploadAgent.cs -- a-10c upload agent
//
// Copyright(C) 2023-2024 ilominar/raven, JAFDTC contributors
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

using JAFDTC.Models.A10C.Upload;
using JAFDTC.Models.DCS;
using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.A10C
{
    /// <summary>
    /// upload agent responsible for building a stream of commands for use by dcs to set up warthog avionics according
    /// to a configuration.
    /// </summary>
    internal class A10CUploadAgent : UploadAgentBase, IUploadAgent
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // private classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// generates the command sequence for teardown in the warthog. this includes triggering light test feedback at
        /// the end of the sequence.
        /// </summary>
        private sealed class A10CTeardownBuilder : CoreTeardownBuilder, IBuilder
        {
            private readonly A10CConfiguration _cfg;

            public A10CTeardownBuilder(A10CConfiguration cfg, A10CDeviceManager dcsCmds, StringBuilder sb)
                : base(dcsCmds, sb)
            {
                _cfg = cfg;
            }

            public override void Build()
            {
                base.Build();

                if ((Settings.UploadFeedback == SettingsData.UploadFeedbackTypes.AUDIO_LIGHTS) ||
                    (Settings.UploadFeedback == SettingsData.UploadFeedbackTypes.LIGHTS))
                {
                    AirframeDevice intl = _aircraft.GetDevice("AUX_LTCTL");
                    AddDynamicAction(intl, "LAMP_TEST_BTN", 0, 1);
                    AddWait(2000);
                    AddDynamicAction(intl, "LAMP_TEST_BTN", 1, 0);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly A10CConfiguration _cfg;
        private readonly A10CDeviceManager _dcsCmds;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public A10CUploadAgent(A10CConfiguration cfg) => (_cfg, _dcsCmds) = (cfg, new A10CDeviceManager());

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void BuildSystems(StringBuilder sb)
        {
            new RadioBuilder(_cfg, _dcsCmds, sb).Build();
            new DSMSBuilder(_cfg, _dcsCmds, sb).Build();
            new HMCSBuilder(_cfg, _dcsCmds, sb).Build();
            new TADBuilder(_cfg, _dcsCmds, sb).Build();
            new TGPBuilder(_cfg, _dcsCmds, sb).Build();
            new WYPTBuilder(_cfg, _dcsCmds, sb).Build();
            new MiscBuilder(_cfg, _dcsCmds, sb).Build();
        }

        public override IBuilder TeardownBuilder(StringBuilder sb) => new A10CTeardownBuilder(_cfg, _dcsCmds, sb);
    }
}
