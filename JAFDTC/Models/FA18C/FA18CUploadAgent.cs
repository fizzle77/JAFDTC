// ********************************************************************************************************************
//
// FA18CUploadAgent.cs -- fa-18c upload agent
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
using JAFDTC.Models.FA18C.Upload;
using JAFDTC.Utilities;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.FA18C
{
    /// <summary>
    /// upload agent responsible for building a stream of commands for use by dcs to set up hornet avionics according
    /// to a configuration.
    /// </summary>
    public class FA18CUploadAgent : UploadAgentBase, IUploadAgent
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // private classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// generates the command sequence for teardown in the strike eagle. this includes triggering light test
        /// feedback at the end of the sequence.
        /// </summary>
        private sealed class FA18CTeardownBuilder : CoreSetupBuilder, IBuilder
        {
            private readonly FA18CConfiguration _cfg;

            public FA18CTeardownBuilder(FA18CConfiguration cfg, FA18CDeviceManager dcsCmds, StringBuilder sb)
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
                    AirframeDevice intl = _aircraft.GetDevice("INTL");
                    AddDynamicAction(intl, "LIGHTS_TEST_SW", 0, 1);
                    AddWait(2500);
                    AddDynamicAction(intl, "LIGHTS_TEST_SW", 1, 0);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly FA18CConfiguration _cfg;
        private readonly FA18CDeviceManager _dcsCmds;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public FA18CUploadAgent(FA18CConfiguration cfg) => (_cfg, _dcsCmds) = (cfg, new FA18CDeviceManager());

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void BuildSystems(StringBuilder sb)
        {
            new RadioBuilder(_cfg, _dcsCmds, sb).Build();
            new CMSBuilder(_cfg, _dcsCmds, sb).Build();
            new WYPTBuilder(_cfg, _dcsCmds, sb).Build();
        }

        public override IBuilder TeardownBuilder(StringBuilder sb) => new FA18CTeardownBuilder(_cfg, _dcsCmds, sb);
    }
}
