// ********************************************************************************************************************
//
// CMDSSystem.cs -- f-16c cmds system configuration
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

using System;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F16C.CMDS
{
    enum ProgramNumbers
    {
        MAN1 = 0,
        MAN2 = 1,
        MAN3 = 2,
        MAN4 = 3,
        PANIC = 4,
        BYPASS = 5
    }

    /// <summary>
    /// class to capture the settings of the CMDS system. most CMDS fields are encoded as strings. a field value of
    /// "" implies that the field is set to the default value in the avionics.
    /// </summary>
    public class CMDSSystem : SystemBase
    {
        public const string SystemTag = "JAFDTC:F16C:CMDS";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change or validation events.

        private CMDSProgram[] _programs;
        public CMDSProgram[] Programs
        {
            get => _programs;
            set => _programs = value;
        }

        // ---- following properties post change and validation events.

        private string _bingoChaff;                  // integer, on [0, 99]
        public string BingoChaff
        {
            get => _bingoChaff;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 0, 99))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _bingoChaff, value, error);
            }
        }

        private string _bingoFlare;                 // integer, on [0, 99]
        public string BingoFlare
        {
            get => _bingoFlare;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsIntegerFieldValid(value, 0, 99))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _bingoFlare, value, error);
            }
        }

        // ---- following properties are synthesized.

        // returns a CMDSSystem with the fields populated with the actual default values (note that usually the field
        // value "" implies the field's default).
        //
        // defaults are as of DCS v2.9.0.47168.
        //
        [JsonIgnore]
        public readonly static CMDSSystem ExplicitDefaults = new()
        {
            BingoChaff = "10",
            BingoFlare = "10",
            Programs = new[]
            {
                new CMDSProgram((int) ProgramNumbers.MAN1, "1", "0.020", "10", "1.00", "1", "0.020", "10", "1.00"),
                new CMDSProgram((int) ProgramNumbers.MAN2, "1", "0.020", "10", "0.50", "1", "0.020", "10", "0.50"),
                new CMDSProgram((int) ProgramNumbers.MAN3, "2", "0.100",  "5", "1.00", "2", "0.100",  "5", "1.00"),
                new CMDSProgram((int) ProgramNumbers.MAN4, "2", "0.100",  "5", "0.50", "2", "0.100",  "5", "0.50"),
                new CMDSProgram((int) ProgramNumbers.PANIC, "2", "0.050", "20", "0.75", "2", "0.050", "20", "0.75"),
                new CMDSProgram((int) ProgramNumbers.BYPASS, "1", "0.020",  "1", "0.50", "1", "0.020",  "1", "0.50")
            }
        };

        // returns true if the instance indicates a default setup (all fields are ""), false otherwise.
        //
        [JsonIgnore]
        public override bool IsDefault
        {
            get
            {
                var isDefault = ((BingoChaff.Length == 0) && (BingoFlare.Length == 0));
                for (int i = 0; isDefault && (i < 6); i++)
                    if (!Programs[i].IsDefault)
                        return false;
                return isDefault;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public CMDSSystem()
        {
            BingoChaff = "";
            BingoFlare = "";
            Programs = new CMDSProgram[6]
            {
                new((int)ProgramNumbers.MAN1),
                new((int)ProgramNumbers.MAN2),
                new((int)ProgramNumbers.MAN3),
                new((int)ProgramNumbers.MAN4),
                new((int)ProgramNumbers.PANIC),
                new((int)ProgramNumbers.BYPASS),
            };
        }

        public CMDSSystem(CMDSSystem other)
        {
            BingoChaff = new(other.BingoChaff);
            BingoFlare = new(other.BingoFlare);
            Programs = new CMDSProgram[6]
            {
                new(other.Programs[(int)ProgramNumbers.MAN1]),
                new(other.Programs[(int)ProgramNumbers.MAN2]),
                new(other.Programs[(int)ProgramNumbers.MAN3]),
                new(other.Programs[(int)ProgramNumbers.MAN4]),
                new(other.Programs[(int)ProgramNumbers.PANIC]),
                new(other.Programs[(int)ProgramNumbers.BYPASS]),
            };
        }

        public virtual object Clone() => new CMDSSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// merge core program (bq/bi/sq/si) into dcs dtc dom.
        /// </summary>
        private void MergeProgramIntoSimDTC(JsonNode progRoot, CMDSProgramCore prog, CMDSProgramCore dflt)
        {
            if (int.TryParse((string.IsNullOrEmpty(prog.BQ)) ? dflt.BQ : prog.BQ, out int bq))
                progRoot["BurstQuantity"] = bq;
            if (int.TryParse((string.IsNullOrEmpty(prog.SQ)) ? dflt.BQ : prog.SQ, out int sq))
                progRoot["SalvoQuantity"] = sq;
            if (double.TryParse((string.IsNullOrEmpty(prog.BI)) ? dflt.SI : prog.SI, out double bi))
                progRoot["BurstInterval"] = Math.Truncate(bi * 1000.0) / 1000.0;
            if (double.TryParse((string.IsNullOrEmpty(prog.SI)) ? dflt.SI : prog.SI, out double si))
                progRoot["SalvoInterval"] = Math.Truncate(si * 100.0) / 100.0;
        }

        /// <summary>
        /// merge radio settings into dcs dtc configuration.
        /// </summary>
        public override void MergeIntoSimDTC(JsonNode dataRoot)
        {
            CMDSSystem dflt = ExplicitDefaults;
            JsonNode cmdsRoot = dataRoot["MPD"]["CMDS"];

            JsonNode bingoRoot = cmdsRoot["CMDSBingoSettings"];
            if (int.TryParse((string.IsNullOrEmpty(BingoChaff)) ? dflt.BingoChaff : BingoChaff, out int bingoChaff))
                bingoRoot["ChaffNum"] = bingoChaff;
            if (int.TryParse((string.IsNullOrEmpty(BingoFlare)) ? dflt.BingoFlare : BingoFlare, out int bingoFlare))
                bingoRoot["FlaresNum"] = bingoFlare;

            JsonNode progRoot = cmdsRoot["CMDSProgramSettings"];
            for (int i = (int)ProgramNumbers.MAN1; i < (int)ProgramNumbers.MAN2; i++)
            {
                MergeProgramIntoSimDTC(progRoot[$"MAN{i + 1}"]["Chaff"], Programs[i].Chaff, dflt.Programs[i].Chaff);
                MergeProgramIntoSimDTC(progRoot[$"MAN{i + 1}"]["Flare"], Programs[i].Flare, dflt.Programs[i].Chaff);
            }
        }

        /// <summary>
        /// reset the instance to defaults (by definition, field value of "" implies default). table numbers are not
        /// changed by this operation.
        /// <summary>
        public override void Reset()
        {
            BingoChaff = "";
            BingoFlare = "";
            foreach (CMDSProgram program in Programs)
                program.Reset();
        }
    }
}
