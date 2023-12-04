// ********************************************************************************************************************
//
// ViperDriver.cs -- f-16c datalink system pilot information
//
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
using System;

namespace JAFDTC.Models.F16C.DLNK
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class ViperDriver : BindableObject
    {
        public string UID { get; set; }                         // unique id

        public string Name { get; set; }                        // name, unique (case-insensitive)

        public string TNDL { get; set; }                        // datalink tndl value

        public ViperDriver() => (UID, Name, TNDL) = (Guid.NewGuid().ToString(), "", "");
    }
}
