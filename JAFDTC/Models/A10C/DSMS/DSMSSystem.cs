// ********************************************************************************************************************
//
// MiscSystem.cs -- a-10c dsmss system
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023 ilominar/raven
// Copyright(C) 2024 fizzle
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
using System.Text.Json.Serialization;

namespace JAFDTC.Models.A10C.DSMS
{
    public class DSMSSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:A10C:DSMS";

        public bool IsDefault => throw new System.NotImplementedException();

        public void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}
