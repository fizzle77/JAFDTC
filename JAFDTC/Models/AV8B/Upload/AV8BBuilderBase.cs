// ********************************************************************************************************************
//
// AV8BBuilderBase.cs -- av-8b abstract base command builder
//
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
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.AV8B.Upload
{
    /// <summary>
    /// abstract base class for the harrier command builder functionality. all system-specific harrier command builders
    /// derive from this class.
    /// </summary>
    internal abstract class AV8BBuilderBase : BuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        protected readonly AV8BConfiguration _cfg;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public AV8BBuilderBase(AV8BConfiguration cfg, AV8BDeviceManager _dcsCmds, StringBuilder sb) : base(_dcsCmds, sb)
            => (_cfg) = (cfg);
    }
}
