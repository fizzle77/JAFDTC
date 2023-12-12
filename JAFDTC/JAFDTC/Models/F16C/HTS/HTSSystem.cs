// ********************************************************************************************************************
//
// HTSSystem.cs -- f-16c hts system configuration
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
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F16C.HTS
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class HTSSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:F16C:HTS";

        private const int NUM_ENABLED_THREATS = 12;         // [0] = MAN table; [i] Table #, for i > 0
        private const int NUM_MANTABLE_ENTRIES = 8;         // MAN table has 8 entries

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events.

        public ObservableCollection<TableCode> MANTable { get; set; }

        public bool[] EnabledThreats { get; set; }

        // ---- following properties are synthesized.

        // returns true if the instance indicates a default setup (all fields are ""), false otherwise.
        //
        [JsonIgnore]
        public bool IsDefault
        {
            get
            {
                if (IsMANTablePopulated || EnabledThreats[0])
                {
                    return false;
                }
                for (int i = 1; i < EnabledThreats.Length; i++)
                {
                    if (!EnabledThreats[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        // returns true if the MAN table is populated with any entries, false otherwise.
        //
        [JsonIgnore]
        public bool IsMANTablePopulated
        {
            get
            {
                for (int i = 0; i < MANTable.Count; i++)
                {
                    if (!MANTable[i].IsDefault)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public HTSSystem()
        {
            MANTable = new ObservableCollection<TableCode>();
            for (int i = 0; i < NUM_MANTABLE_ENTRIES; i++)
            {
                MANTable.Add(new TableCode());
            }
            EnabledThreats = new bool[NUM_ENABLED_THREATS];
            EnabledThreats[0] = false;
            for (int i = 1; i < EnabledThreats.Length; i++)
            {
                EnabledThreats[i] = true;
            }
        }

        public HTSSystem(HTSSystem other)
        {
            MANTable = new ObservableCollection<TableCode>();
            for (int i = 0; i < other.MANTable.Count; i++)
            {
                MANTable.Add(new(other.MANTable[i]));
            }
            EnabledThreats = new bool[NUM_ENABLED_THREATS];
            for (int i = 0; i < EnabledThreats.Length; i++)
            {
                EnabledThreats[i] = other.EnabledThreats[i];
            }
        }

        public virtual object Clone()
        {
            return new HTSSystem(this);
        }

        public void Reset()
        {
            for (int i = 0; i < MANTable.Count; i++)
            {
                MANTable[i].Reset();
            }
            for (int i = 1; i < EnabledThreats.Length; i++)
            {
                EnabledThreats[i] = true;
            }
            EnabledThreats[0] = false;
        }
    }
}
