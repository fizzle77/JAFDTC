// ********************************************************************************************************************
//
// CMProgram.cs -- fa-18c cms core program parameters
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

namespace JAFDTC.Models.FA18C.CMS
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class CMProgram : BindableObject
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events

        public int Number { get; set; }

        // ---- following properties post change and validation events.

        private string _chaffQ;                     // integer, on [0, 100]
        public string ChaffQ
        {
            get => _chaffQ;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid chaff quantity";
                if (IsIntegerFieldValid(value, 0, 100))
                {
                    value = FixupIntegerField(value, "D");
                    error = null;
                }
                SetProperty(ref _chaffQ, value, error);
            }
        }

        private string _flareQ;                     // integer, on [0, 100]
        public string FlareQ
        {
            get => _flareQ;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid flare quantity";
                if (IsIntegerFieldValid(value, 0, 100))
                {
                    value = FixupIntegerField(value, "D");
                    error = null;
                }
                SetProperty(ref _flareQ, value, error);
            }
        }

        private string _sq;                         // integer, on [1, 24]
        public string SQ
        {
            get => _sq;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid repeat count";
                if (IsIntegerFieldValid(value, 1, 24))
                {
                    value = FixupIntegerField(value, "D");
                    error = null;
                }
                SetProperty(ref _sq, value, error);
            }
        }

        // TODO: check valid range
        private string _si;                         // decimal 0.00, on [0.25, 5.00] by 0.25
        public string SI
        {
            get => _si;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid repeat interval";
                if (IsDecimalFieldValid(value, 0.25, 5.00))
                {
                    if (!double.TryParse(value, out double dblVal) || (dblVal == ((double)((int)(dblVal / 0.25)) * 0.25)))
                    {
                        value = FixupDecimalField(value, "F2");
                        error = null;
                    }
                }
                SetProperty(ref _si, value, error);
            }
        }

        // ---- following properties are synthesized.

        // returns true if the instance indicates a default setup (all fields are ""), false otherwise.
        //
        [JsonIgnore]
        public bool IsDefault => (string.IsNullOrEmpty(ChaffQ) && string.IsNullOrEmpty(FlareQ) &&
                                  string.IsNullOrEmpty(SQ) && string.IsNullOrEmpty(SI));

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public CMProgram() => (Number, ChaffQ, FlareQ, SQ, SI) = (1, "", "", "", "");

        public CMProgram(int number) => (Number, ChaffQ, FlareQ, SQ, SI) = (number, "", "", "", "");

        public CMProgram(int number, string chaffQ, string flareQ, string sq, string si)
             => (Number, ChaffQ, FlareQ, SQ, SI) = (number, chaffQ, flareQ, sq, si);

        public CMProgram(CMProgram other)
             => (Number, ChaffQ, FlareQ, SQ, SI) = (other.Number, other.ChaffQ, other.FlareQ, other.SQ, other.SI);

        private void SetProgram(string chaffQ = "", string flareQ = "", string sq = "", string si = "")
        {
            ChaffQ = chaffQ;
            FlareQ = flareQ;
            SQ = sq;
            SI = si;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the instance to defaults (by definition, field value of "" implies default). this does not change
        /// the program number.
        /// </summary>
        public void Reset()
        {
            SetProgram();
        }
    }
}
