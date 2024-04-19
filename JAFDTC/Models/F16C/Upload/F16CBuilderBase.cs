﻿// ********************************************************************************************************************
//
// F16CBuilderBase.cs -- f-16c abstract base command builder
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// abstract base class for the viper command builder functionality. all system-specific viper command builders
    /// derive from this class.
    /// </summary>
    internal abstract class F16CBuilderBase : BuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        protected readonly F16CConfiguration _cfg;              // configuration to map

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CBuilderBase(F16CConfiguration cfg, F16CDeviceManager dcsCmds, StringBuilder sb) : base(dcsCmds, sb)
            => (_cfg) = (cfg);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns a list of actions to enter the numeric value followed by the ENTR key. the list is predicated on
        /// the value being non-null/non-empty (the list is only non-empty if the predicate is true). if negative
        /// values are allowed, prepends "00" to the digits to enter the "-" sign. returns the list of actions (empty
        /// if the value is null/empty).
        /// </summary>
        public List<string> PredActionsForNumAndEnter(string value, bool isNegValOK = false, bool isNoSep = false)
        {
            value = (isNoSep) ? AdjustNoSeparators(value) : value;

            List<string> keys = new();
            if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int intValue))
            {
                if (intValue < 0)
                {
                    value = value[1..];
                    if (isNegValOK)
                    {
                        keys.Add("0");
                        keys.Add("0");
                    }
                }
                keys = keys.Concat(ActionsForString(value)).ToList();
                keys.Add("ENTR");
            }
            return keys;
        }

        /// <summary>
        /// returns a list of actions to enter the numeric value (with leading zeros and separators removed) followed
        /// by the ENTR key. the list is predicated on the value being non-null/non-empty (the list is only non-empty
        /// if the predicate is true). if negative values are allowed, prepends "00" to the digits to enter the "-"
        /// sign. returns the list of actions (empty if the value is null/empty).
        /// </summary>
        public List<string> PredActionsForCleanNumAndEnter(string value, bool isNegValOK = false)
        {
            value = (!string.IsNullOrEmpty(value)) ? AdjustNoLeadZeros(value) : value;
            return PredActionsForNumAndEnter(value, isNegValOK, true);
        }
    }
}
