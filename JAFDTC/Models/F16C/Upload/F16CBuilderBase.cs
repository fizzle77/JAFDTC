// ********************************************************************************************************************
//
// F16CBuilderBase.cs -- f-16c abstract base command builder
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

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// abstract base class for the viper upload functionality. provides functions to support building command
    /// streams.
    /// </summary>
    internal abstract class F16CBuilderBase : BuilderBase
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

        public F16CBuilderBase(F16CConfiguration cfg, F16CCommands f16c, StringBuilder sb) : base(f16c, sb)
            => (_cfg) = (cfg);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// appends a sequence of ufc commands to enter the value followed by the ENTR key. if negative values are
        /// allowed, prepends "00" to the digits to enter the "-" sign.
        /// </summary>
        private void AppendDigitsWithEnter(Device ufc, string value, bool isNegValOK = false)
        {
            if (isNegValOK && int.TryParse(value, out int intValue) && (intValue < 0))
            {
                AppendCommand(ufc.GetCommand("0"));
                AppendCommand(ufc.GetCommand("0"));
                value = value[1..];
            }
            AppendCommand(BuildDigits(ufc, value));
            AppendCommand(ufc.GetCommand("ENTR"));
        }

        /// <summary>
        /// appends a predicated sequence of ufc commands to enter the value followed by the ENTR key. the sequence
        /// is predicated on the value being non-null/empty. if negative values are allowed, prepends "00" to the
        /// digits to enter the "-" sign. returns the value of the predicate.
        /// </summary>
        protected bool PredAppendDigitsWithEnter(Device ufc, string value, bool isNegValOK = false)
        {
            if (!string.IsNullOrEmpty(value))
            {
                AppendDigitsWithEnter(ufc, value, isNegValOK);
                return true;
            }
            return false;
        }

        /// <summary>
        /// appends a predicated sequence of ufc commands to enter the value (with separators removed via
        /// RemoveSeparators method) followed by the ENTR key. the sequence is predicated on the value being
        /// non-null/empty. if negative values are allowed, prepend "00" to the digits to enter the "-" sign.
        /// returns the value of the predicate.
        /// </summary>
        protected bool PredAppendDigitsNoSepWithEnter(Device ufc, string value, bool isNegValOK = false)
        {
            if (!string.IsNullOrEmpty(value))
            {
                AppendDigitsWithEnter(ufc, RemoveSeparators(value), isNegValOK);
                return true;
            }
            return false;
        }

        /// <summary>
        /// appends a predicated sequence of ufc commands to enter the value (with separators removed via
        /// RemoveSeparators method and leading zeros removed via DeleteLeadingZeros method) followed by the ENTR key.
        /// the sequence is predicated on the value being non-null/empty. if negative values are allowed, prepend "00"
        /// to the digits to enter the "-" sign. returns the value of the predicate.
        /// </summary>
        protected bool PredAppendDigitsDLZRSWithEnter(Device ufc, string value, bool isNegValOK = false)
        {
            if (!string.IsNullOrEmpty(value))
            {
                AppendDigitsWithEnter(ufc, DeleteLeadingZeros(RemoveSeparators(value)), isNegValOK);
                return true;
            }
            return false;
        }
    }
}
