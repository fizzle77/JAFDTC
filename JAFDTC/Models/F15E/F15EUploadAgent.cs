// ********************************************************************************************************************
//
// F15EUploadAgent.cs -- f-15e upload agent
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
using JAFDTC.Models.F15E.Upload;
using JAFDTC.Utilities;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F15E
{
    /// <summary>
    /// upload agent responsible for building a stream of commands for use by dcs to set up mudhen avionics according
    /// to a configuration.
    /// </summary>
    public class F15EUploadAgent : UploadAgentBase, IUploadAgent
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // private classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// generates the command sequence for setup in the strike eagle. this includes making sure we're in the
        /// proper seat.
        /// </summary>
        private sealed class F15ESetupBuilder : CoreTeardownBuilder, IBuilder
        {
            private readonly F15EConfiguration _cfg;

            public F15ESetupBuilder(F15EConfiguration cfg, F15EDeviceManager dcsCmds, StringBuilder sb) 
                : base(dcsCmds, sb)
            {
                _cfg = cfg;
            }

            public override void Build()
            {
                base.Build();

                // TODO: not clear what the best way to handle this is? for now, abort on cockpit mismatches
#if ENABLE_FORCE_COCKPIT_CHANGE
                if (_cfg.CrewMember == F15EConfiguration.CrewPositions.PILOT)
                {
                    AddRunFunction("GoToFrontCockpit");
                }
                else if (_cfg.CrewMember == F15EConfiguration.CrewPositions.WSO)
                {
                    AddRunFunction("GoToRearCockpit");
                }
                AddWait(2 * WAIT_LONG);
#else
                if (_cfg.CrewMember == F15EConfiguration.CrewPositions.PILOT)
                {
                    AddIfBlock("IsInRearCockpit", null, delegate ()
                    {
                        AddAbort("ERROR: Configuration is for Pilot Seat");
                    });
                }
                else if (_cfg.CrewMember == F15EConfiguration.CrewPositions.WSO)
                {
                    AddIfBlock("IsInFrontCockpit", null, delegate ()
                    {
                        AddAbort("ERROR: Configuration is for WSO Seat");
                    });
                }
#endif
            }
        }

        // ================================================================================================================

        /// <summary>
        /// generates the command sequence for teardown in the strike eagle. this includes triggering light test
        /// feedback at the end of the sequence.
        /// </summary>
        private sealed class F15ETeardownBuilder : CoreSetupBuilder, IBuilder
        {
            private readonly F15EConfiguration _cfg;

            public F15ETeardownBuilder(F15EConfiguration cfg, F15EDeviceManager dcsCmds, StringBuilder sb)
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
                    if (_cfg.CrewMember == F15EConfiguration.CrewPositions.PILOT)
                    {
                        AirframeDevice intl = _aircraft.GetDevice("INTL_PILOT");
                        AddIfBlock("IsInFrontCockpit", null, delegate ()
                        {
                            AddDynamicAction(intl, "F_INTL_WARN_TEST", 0, 1);
                            AddWait(2000);
                            AddDynamicAction(intl, "F_INTL_WARN_TEST", 1, 0);
                        });
                    }
                    else if (_cfg.CrewMember == F15EConfiguration.CrewPositions.WSO)
                    {
                        AirframeDevice intl = _aircraft.GetDevice("INTL_WSO");
                        AddIfBlock("IsInRearCockpit", null, delegate ()
                        {
                            AddDynamicAction(intl, "R_INTL_WARN_TEST", 0, 1);
                            AddWait(2000);
                            AddDynamicAction(intl, "R_INTL_WARN_TEST", 1, 0);
                        });
                    }
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly F15EConfiguration _cfg;
        private readonly F15EDeviceManager _dcsCmds;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F15EUploadAgent(F15EConfiguration cfg) => (_cfg, _dcsCmds) = (cfg, new F15EDeviceManager());

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void BuildSystems(StringBuilder sb)
        {
            new RadioBuilder(_cfg, _dcsCmds, sb).Build();
            new MPDBuilder(_cfg, _dcsCmds, sb).Build();
            new MiscBuilder(_cfg, _dcsCmds, sb).Build();
            new STPTBuilder(_cfg, _dcsCmds, sb).Build();
            new UFCBuilder(_cfg, _dcsCmds, sb).Build();
        }

        public override IBuilder SetupBuilder(StringBuilder sb) => new F15ESetupBuilder(_cfg, _dcsCmds, sb);

        public override IBuilder TeardownBuilder(StringBuilder sb) => new F15ETeardownBuilder(_cfg, _dcsCmds, sb);
    }
}
