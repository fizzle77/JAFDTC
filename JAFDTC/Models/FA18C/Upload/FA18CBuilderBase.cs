// ********************************************************************************************************************
//
// FA18CBuilderBase.cs -- fa-18c abstract base command builder
//
// Copyright(C) 2023-2025 ilominar/raven
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

namespace JAFDTC.Models.FA18C.Upload
{
    /// <summary>
    /// abstract base class for the hornet upload functionality. provides functions to support building command
    /// streams.
    /// </summary>
    internal abstract class FA18CBuilderBase : BuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        protected readonly FA18CConfiguration _cfg;             // configuration to map

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public FA18CBuilderBase(FA18CConfiguration cfg, FA18CDeviceManager dcsCmds, StringBuilder sb) : base(dcsCmds, sb)
            => (_cfg) = (cfg);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// advance a ddi to the specified page by pressing "MENU" on the indicated ddi until it matches the state
        /// tested for by fnCheckMode then pressing the osb given by pageOSB to advance to the target page.
        /// </summary>
        public void SwitchDDIToPage(AirframeDevice ddi, string fnCheckMode, string pageOSB)
        {
            AddWhileBlock(fnCheckMode, false, null, delegate ()
            {
                AddAction(ddi, "OSB-18", WAIT_BASE);                                        // MENU
            }, 6);
            AddAction(ddi, pageOSB, WAIT_BASE);                                             // page selection osb
        }
    }
}
