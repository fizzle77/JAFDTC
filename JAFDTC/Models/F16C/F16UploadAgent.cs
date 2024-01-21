// ********************************************************************************************************************
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
using JAFDTC.Models.F16C.Upload;
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
            public F16CSetupBuilder(F16DeviceManager dcsCmds, StringBuilder sb) : base(dcsCmds, sb) { }

            public override void Build()
            {
                base.Build();

                AirframeDevice sms = _aircraft.GetDevice("SMS");

                AddIfBlock("LeftHdptNotOn", null, delegate () { AddAction(sms, "LEFT_HDPT"); });
                AddIfBlock("RightHdptNotOn", null, delegate () { AddAction(sms, "RIGHT_HDPT"); });

                // TODO: ensure cmds is on?
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly F16CConfiguration _cfg;
        private readonly F16DeviceManager _dcsCmds;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CUploadAgent(F16CConfiguration cfg) => (_cfg, _dcsCmds) = (cfg, new F16DeviceManager());

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void BuildSystems(StringBuilder sb)
        {
            new RadioBuilder(_cfg, _dcsCmds, sb).Build();
            new MiscBuilder(_cfg, _dcsCmds, sb).Build();
            new CMDSBuilder(_cfg, _dcsCmds, sb).Build();
            //
            // NOTE: hts must be done before mfd as mfd might set the man threat class which is only available if the
            // NOTE: hts manual table has been set up.
            //
            new HTSBuilder(_cfg, _dcsCmds, sb).Build();
            new MFDBuilder(_cfg, _dcsCmds, sb).Build();
            new HARMBuilder(_cfg, _dcsCmds, sb).Build();
            new DLNKBuilder(_cfg, _dcsCmds, sb).Build();
            new STPTBuilder(_cfg, _dcsCmds, sb).Build();
        }

        public override IBuilder SetupBuilder(StringBuilder sb) => new F16CSetupBuilder(_dcsCmds, sb);
    }
}