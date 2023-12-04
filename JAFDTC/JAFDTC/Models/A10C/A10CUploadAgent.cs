// ********************************************************************************************************************
//
// A10CUploadAgent.cs -- a-10c upload agent
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

using JAFDTC.Models.A10C.Upload;
using JAFDTC.Models.DCS;
using JAFDTC.Utilities.Networking;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.A10C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    internal class A10CUploadAgent : IUploadAgent
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
            public AV8BSetup(A10CCommands a10c, StringBuilder sb) : base(a10c, sb) { }

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
            public AV8BTeardown(A10CCommands a10c, StringBuilder sb) : base(a10c, sb) { }

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

        private readonly A10CConfiguration _cfg;
        private readonly A10CCommands _a10c;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public A10CUploadAgent(A10CConfiguration cfg) => (_cfg, _a10c) = (cfg, new A10CCommands());

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

            new AV8BSetup(_a10c, sb).Build();

            new WYPTBuilder(_cfg, _a10c, sb).Build();

            new AV8BTeardown(_a10c, sb).Build();

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
