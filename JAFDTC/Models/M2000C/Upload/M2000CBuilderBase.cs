// ********************************************************************************************************************
//
// M2000CBuilderBase.cs -- m-2000c abstract base command builder
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

using JAFDTC.Models.DCS;
using JAFDTC.Models.F14AB;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.M2000C.Upload
{
    /// <summary>
    /// abstract base class for the mirage upload functionality. provides functions to support building command
    /// streams.
    /// </summary>
    internal abstract class M2000CBuilderBase : BuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        protected readonly M2000CConfiguration _cfg;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public M2000CBuilderBase(M2000CConfiguration cfg, M2000CCommands dcsCmds, StringBuilder sb)
            : base(dcsCmds, sb) => (_cfg) = (cfg);
    }
}
