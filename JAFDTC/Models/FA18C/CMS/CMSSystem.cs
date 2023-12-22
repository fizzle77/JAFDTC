// ********************************************************************************************************************
//
// CMSSystem.cs -- fa-18c cmds system configuration
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
    enum ProgramNumbers
    {
        PROG1 = 0,
        PROG2 = 1,
        PROG3 = 2,
        PROG4 = 3,
        PROG5 = 4
    }

    /// <summary>
    /// TODO: document
    /// </summary>
    public class CMSSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:FA18C:CMD";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change or validation events.

        private CMProgram[] _programs;
        public CMProgram[] Programs
        {
            get => _programs;
            set => _programs = value;
        }
        // ---- following properties are synthesized.

        /// <summary>
        /// returns a CMSSystem with the fields populated with the actual default values (note that usually the field
        /// value "" implies the field's default).
        /// 
        /// defaults are as of DCS v2.9.0.47168.
        /// </summary>
        [JsonIgnore]
        public readonly static CMSSystem ExplicitDefaults = new()
        {
            Programs = new[]
            {
                new CMProgram((int) ProgramNumbers.PROG1, "1", "1", "10", "1.0"),
                new CMProgram((int) ProgramNumbers.PROG2, "1", "1", "10", "0.5"),
                new CMProgram((int) ProgramNumbers.PROG3, "2", "2",  "5", "1.0"),
                new CMProgram((int) ProgramNumbers.PROG4, "2", "2", "10", "0.5"),
                new CMProgram((int) ProgramNumbers.PROG5, "1", "1",  "2", "1.0"),
            }
        };

        /// <summary>
        /// returns true if the instance indicates a default setup (all fields are ""), false otherwise.
        /// </summary>
        [JsonIgnore]
        public bool IsDefault
        {
            get
            {
                for (int i = 0; i < Programs.Length; i++)
                {
                    if (!Programs[i].IsDefault)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public CMSSystem()
        {
            Programs = new CMProgram[5]
            {
                new((int) ProgramNumbers.PROG1),
                new((int) ProgramNumbers.PROG2),
                new((int) ProgramNumbers.PROG3),
                new((int) ProgramNumbers.PROG4),
                new((int) ProgramNumbers.PROG5)
            };
        }

        public CMSSystem(CMSSystem other)
        {
            Programs = new CMProgram[5]
            {
                new(other.Programs[(int)ProgramNumbers.PROG1]),
                new(other.Programs[(int)ProgramNumbers.PROG2]),
                new(other.Programs[(int)ProgramNumbers.PROG3]),
                new(other.Programs[(int)ProgramNumbers.PROG4]),
                new(other.Programs[(int)ProgramNumbers.PROG5])
            };
        }

        public virtual object Clone() => new CMSSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the instance to defaults (by definition, field value of "" implies default). table numbers are not
        /// changed by this operation.
        /// </summary>
        public void Reset()
        {
            foreach (CMProgram program in Programs)
            {
                program.Reset();
            }
        }
    }
}
