// ********************************************************************************************************************
//
// PPStation.cs -- fa-18c pre-planned system station
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2024 ilominar/raven
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.FA18C.PP
{
    /// <summary>
    /// pre-planned programs are associated with a station on the hornet. each station can have up to 6 programs
    /// that define target and steerpoint coordinates for the weapon on the station.
    /// </summary>
    public class PPStation : BindableObject
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events

        public int Number { get; set; }

        public Weapons Weapon { get; set; }

        public int BoxedPP { get; set; }

        private PPCoordinateInfo[] _pp;
        public PPCoordinateInfo[] PP
        {
            get => _pp;
            set => _pp = value;
        }

        public List<PPCoordinateInfo> STP { get; set; }

        // ---- following properties are synthesized.

        /// <summary>
        /// returns true if the instance indicates a default setup (all fields are ""), false otherwise.
        /// </summary>
        [JsonIgnore]
        public bool IsDefault
        {
            get
            {
                for (int i = 0; i < _pp.Length; i++)
                    if (PP[i].IsValid)
                        return false;
                return (Weapon == Weapons.NONE);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public PPStation()
        {
            Number = 0;
            BoxedPP = 0;
            Weapon = Weapons.NONE;
            PP = new PPCoordinateInfo[6] { new(), new(), new(), new(), new(), new() };
            STP = new List<PPCoordinateInfo>();
        }

        public PPStation(int number = 0)
        {
            Number = number;
            BoxedPP = 0;
            Weapon = Weapons.NONE;
            PP = new PPCoordinateInfo[6] { new(), new(), new(), new(), new(), new() };
            STP = new List<PPCoordinateInfo>();
        }

        public PPStation(PPStation other)
        {
            Number = other.Number;
            BoxedPP = other.BoxedPP;
            Weapon = other.Weapon;
            PP = new PPCoordinateInfo[6];
            for (int i = 0; i < PP.Length; i++)
                PP[i] = new(other.PP[i]);
            STP = new List<PPCoordinateInfo>();
            for (int i = 0; i < other.STP.Count; i++)
                STP.Add(new(other.STP[i]));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns true if the program in the station is default, false otherwise.
        /// </summary>
        public bool IsPPDefault(int pp)
        {
            return !PP[pp].IsValid;
        }

        /// <summary>
        /// reset the instance to defaults.
        /// </summary>
        public void Reset()
        {
            BoxedPP = 0;
            Weapon = Weapons.NONE;
            foreach (PPCoordinateInfo pp in PP)
                pp.Reset();
            STP.Clear();
        }
    }
}
