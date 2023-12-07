// ********************************************************************************************************************
//
// HARMSystem.cs -- f-16c harm (alic) system configuration
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

namespace JAFDTC.Models.F16C.HARM
{
    enum TableNumbers
    {
        TABLE1 = 0,
        TABLE2 = 1,
        TABLE3 = 2,
    }

    /// <summary>
    /// TODO: document
    /// </summary>
	public class HARMSystem : BindableObject, ISystem
	{
        public const string SystemTag = "JAFDTC:F16C:HARM";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change or validation events.

        public ALICTable[] Tables { get; set; }

        // ---- the following properties are synthesized.

        [JsonIgnore]
        public readonly static HARMSystem ExplicitDefaults = new()
        {
            Tables = new ALICTable[3]
            {
                new((int)TableNumbers.TABLE1, "110", "104", "103", "115", "107"),
                new((int)TableNumbers.TABLE2, "120", "119", "117", "121", "109"),
                new((int)TableNumbers.TABLE3, "123", "122", "108", "126", "118")
            }
        };

        // returns true if the instance indicates a default setup (all fields are ""), false otherwise.
        //
        [JsonIgnore]
        public bool IsDefault
        {
            get
            {
				foreach (ALICTable table in Tables)
				{
                    if (!table.IsDefault)
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

        public HARMSystem()
		{
            Tables = new ALICTable[3]
            {
                new((int) TableNumbers.TABLE1),
                new((int) TableNumbers.TABLE2),
                new((int) TableNumbers.TABLE3),
            };
        }

        public HARMSystem(HARMSystem other)
        {
            Tables = new ALICTable[3]
            {
                new(other.Tables[(int) TableNumbers.TABLE1]),
                new(other.Tables[(int) TableNumbers.TABLE2]),
                new(other.Tables[(int) TableNumbers.TABLE3])
            };
        }

        public virtual object Clone()
        {
            return new HARMSystem(this);
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
            foreach (ALICTable table in Tables)
            {
                table.Reset();
            }
        }

        // TODO: document
        public void CleanUp()
        {
            foreach (ALICTable table in Tables)
            {
                table.CleanUp();
            }
        }
    }
}
