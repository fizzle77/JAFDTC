// ********************************************************************************************************************
//
// AV8BUploadAgent.cs -- av-8b upload agent
//
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

using JAFDTC.Models.AV8B.Upload;
using JAFDTC.Models.DCS;
using JAFDTC.Utilities.Networking;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.AV8B
{
    /// <summary>
    /// TODO: document
    /// </summary>
    internal class AV8BUploadAgent : IUploadAgent
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // private classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        private class AV8BSetup : BuilderBase
        {
            public AV8BSetup(AV8BCommands av8b, StringBuilder sb) : base(av8b, sb) { }

            public override void Build()
            {
                AppendCommand(Marker("upload"));
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private class AV8BTeardown : BuilderBase
        {
            public AV8BTeardown(AV8BCommands av8b, StringBuilder sb) : base(av8b, sb) { }

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

        private readonly AV8BConfiguration _cfg;
        private readonly AV8BCommands _av8b;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public AV8BUploadAgent(AV8BConfiguration cfg) => (_cfg, _av8b) = (cfg, new AV8BCommands());

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        public bool Load()
        {
            StringBuilder sb = new();

            new AV8BSetup(_av8b, sb).Build();

            new WYPTBuilder(_cfg, _av8b, sb).Build();

            new AV8BTeardown(_av8b, sb).Build();

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
