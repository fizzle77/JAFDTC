// ********************************************************************************************************************
//
// CMDSProgramCore.cs -- f-16c cmds core program parameters
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

using JAFDTC.Utilities;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F16C.CMDS
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed class CMDSProgramCore : BindableObject
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties post change and validation events.

        private string _bq;                         // integer, on [0, 99]
        public string BQ
        {
            get => _bq;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 0, 99))
                {
                    value = FixupIntegerField(value, "D");
                    error = null;
                }
                SetProperty(ref _bq, value, error);
            }
        }

        private string _bi;                         // decimal 0.000, on [0.020, 10.000]
        public string BI
        {
            get => _bi;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsDecimalFieldValid(value, 0.020, 10.000))
                {
                    value = FixupDecimalField(value, "F3");
                    error = null;
                }
                SetProperty(ref _bi, value, error);
            }
        }

        private string _sq;                         // integer, on [0, 99]
        public string SQ
        {
            get => _sq;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 0, 99))
                {
                    value = FixupIntegerField(value, "D");
                    error = null;
                }
                SetProperty(ref _sq, value, error);
            }
        }

        private string _si;                         // decimal 0.00, on [0.50, 150.00]
        public string SI
        {
            get => _si;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsDecimalFieldValid(value, 0.50, 150.00))
                {
                    value = FixupDecimalField(value, "F2");
                    error = null;
                }
                SetProperty(ref _si, value, error);
            }
        }

        // ---- following properties are synthesized.

        // returns true if the instance indicates a default setup (all fields are ""), false otherwise.
        //
        [JsonIgnore]
        public bool IsDefault => ((BQ.Length == 0) && (BI.Length == 0) && (SQ.Length == 0) && (SI.Length == 0));

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public CMDSProgramCore()
        {
            SetProgram();
        }

        public CMDSProgramCore(string bq, string bi, string sq, string si)
        {
            SetProgram(bq, bi, sq, si);
        }

        public CMDSProgramCore(CMDSProgramCore other)
        {
            BQ = new(other.BQ);
            BI = new(other.BI);
            SQ = new(other.SQ);
            SI = new(other.SI);
        }

        public void SetProgram(string bq = "", string bi = "", string sq = "", string si = "")
        {
            BQ = bq;
            BI = bi;
            SQ = sq;
            SI = si;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // reset the instance to defaults (by definition, field value of "" implies default).
        //
        public void Reset()
        {
            SetProgram();
        }

        // TODO: document
        public void CleanUp()
        {
        }
    }
}
