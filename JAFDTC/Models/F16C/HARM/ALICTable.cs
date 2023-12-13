// ********************************************************************************************************************
//
// ALICTable.cs -- f-16c alic table
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

namespace JAFDTC.Models.F16C.HARM
{
    public class ALICTable : BindableObject
    {
        private const int NUM_ALICTABLE_ENTRIES = 5;        // alic tables have 5 entries

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events.

        public int Number { get; set; }
        public ObservableCollection<TableCode> Table { get; set; }

        // ---- following properties are synthesized.

        // returns true if the instance indicates a default setup (all fields are ""), false otherwise.
        //
        [JsonIgnore]
        public bool IsDefault
        {
            get
            {
                for (int i = 0; i < Table.Count; i++)
                {
                    if (!Table[i].IsDefault)
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

        public ALICTable()
        {
            Number = 1;
            Table = new ObservableCollection<TableCode>();
            for (int i = 0; i < NUM_ALICTABLE_ENTRIES; i++)
            {
                Table.Add(new TableCode());
            }
        }

        public ALICTable(int number, string t1 = "", string t2 = "", string t3 = "", string t4 = "", string t5 = "")
        {
            Number = number;
            Table = new ObservableCollection<TableCode>
            {
                new(t1),
                new(t2),
                new(t3),
                new(t4),
                new(t5)
            };
        }

        public ALICTable(ALICTable other)
        {
            Number = other.Number;
            Table = new ObservableCollection<TableCode>();
            for (int i = 0; i < other.Table.Count; i++)
            {
                Table.Add(new(other.Table[i]));
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // reset the instance to defaults (by definition, field value of "" implies default).
        //
        public void Reset()
        {
            for (int i = 0; i < NUM_ALICTABLE_ENTRIES; i++)
            {
                Table.Add(new TableCode());
            }
        }
    }
}
