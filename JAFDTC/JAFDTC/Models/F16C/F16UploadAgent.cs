// ********************************************************************************************************************
//
// F16CUploadAgent.cs -- f-16c upload agent
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023 ilominar/raven
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
using JAFDTC.Utilities.Networking;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class F16CUploadAgent : IUploadAgent
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // private classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// generates the command sequence for setup in the viper.
        /// </summary>
        private class F16CSetup : BuilderBase
        {
            public F16CSetup(F16CCommands f16c, StringBuilder sb) : base(f16c, sb) { }

            public override void Build()
            {
                Device sms = _aircraft.GetDevice("SMS");

                AppendCommand(Marker("upload"));

                AppendCommand(StartCondition("LeftHdptNotOn"));
                AppendCommand(sms.GetCommand("LEFT_HDPT"));
                AppendCommand(EndCondition("LeftHdptNotOn"));

                AppendCommand(StartCondition("RightHdptNotOn"));
                AppendCommand(sms.GetCommand("RIGHT_HDPT"));
                AppendCommand(EndCondition("RightHdptNotOn"));
            }
        }

        /// <summary>
        /// generates the command sequence for teardown in the viper.
        /// </summary>
        private class F16CTeardown : BuilderBase
        {
            public F16CTeardown(F16CCommands f16c, StringBuilder sb) : base(f16c, sb) { }

            public override void Build()
            {
                AppendCommand(Marker(""));
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly F16CConfiguration _cfg;
        private readonly F16CCommands _f16c;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CUploadAgent(F16CConfiguration cfg) => (_cfg, _f16c) = (cfg, new F16CCommands());

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public bool Load()
        {
            StringBuilder sb = new();

            new F16CSetup(_f16c, sb).Build();

            new MiscBuilder(_cfg, _f16c, sb).Build();
            new CMDSBuilder(_cfg, _f16c, sb).Build();
            new STPTBuilder(_cfg, _f16c, sb).Build();
            //
            // NOTE: hts must be done before mfd as mfd might set the man threat class which is only
            // NOTE: available if the hts manual table has been set up.
            //
            new HTSBuilder(_cfg, _f16c, sb).Build();
            new MFDBuilder(_cfg, _f16c, sb).Build();
            new HARMBuilder(_cfg, _f16c, sb).Build();
            new RadioBuilder(_cfg, _f16c, sb).Build();
            new DLNKBuilder(_cfg, _f16c, sb).Build();

            new F16CTeardown(_f16c, sb).Build();

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }
            string str = sb.ToString();
            if (str != "")
            {
                return DataSender.Send(str);
            }
            return true;
        }
    }
}