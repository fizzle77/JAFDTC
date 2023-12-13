// ********************************************************************************************************************
//
// A10CBuilderBase.cs -- a-10c abstract base command builder
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
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.A10C.Upload
{
    /// <summary>
    /// abstract base class for the warthog upload functionality. provides functions to support building command
    /// streams.
    /// </summary>
    internal abstract class A10CBuilderBase : BuilderBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        protected readonly A10CConfiguration _cfg;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public A10CBuilderBase(A10CConfiguration cfg, A10CCommands dcsCmds, StringBuilder sb) : base(dcsCmds, sb)
            => (_cfg) = (cfg);
    }
}
