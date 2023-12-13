// ********************************************************************************************************************
//
// CMDSProgram.cs -- f-16c cmds program parameters
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
    /// represents settings of a flare/chaff program in the CMDS system. all CMDS fields are strings, with "" meaning
    /// the default value of the field in the avionics.
    /// </summary>
    public sealed class CMDSProgram : BindableObject
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events

        public int Number { get; set; }
        public CMDSProgramCore Chaff { get; set; }
        public CMDSProgramCore Flare { get; set; }

        // ---- following properties are synthesized

        // returns true if the instance indicates a default setup (all fields are ""), false otherwise.
        //
        [JsonIgnore]
        public bool IsDefault => (Chaff.IsDefault && Flare.IsDefault);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public CMDSProgram() => (Number, Chaff, Flare) = (1, new CMDSProgramCore(), new CMDSProgramCore());

        public CMDSProgram(int number) => (Number, Chaff, Flare) = (number, new CMDSProgramCore(), new CMDSProgramCore());

        public CMDSProgram(int number, string chaffBQ, string chaffBI, string chaffSQ, string chaffSI,
                                       string flareBQ, string flareBI, string flareSQ, string flareSI)
        {
            Number = number;
            Chaff = new CMDSProgramCore(chaffBQ, chaffBI, chaffSQ, chaffSI);
            Flare = new CMDSProgramCore(flareBQ, flareBI, flareSQ, flareSI);
        }

        public CMDSProgram(CMDSProgram other)
        {
            Number = other.Number;
            Chaff = new(other.Chaff);
            Flare = new(other.Flare);
        }

        private void SetProgram(string chaffBQ = "", string chaffBI = "", string chaffSQ = "", string chaffSI = "",
                                string flareBQ = "", string flareBI = "", string flareSQ = "", string flareSI = "")
        {
            Chaff.SetProgram(chaffBQ, chaffBI, chaffSQ, chaffSI);
            Flare.SetProgram(flareBQ, flareBI, flareSQ, flareSI);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // reset the instance to defaults (by definition, field value of "" implies default). this does not change
        // the program number.
        //
        public void Reset()
        {
            SetProgram();
        }
    }
}
