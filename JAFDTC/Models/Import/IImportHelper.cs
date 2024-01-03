// ********************************************************************************************************************
//
// IImportHelper.cs -- interface for helper to import navpoints
//
// Copyright(C) 2023-2024 ilominar/raven
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

using JAFDTC.Models.Base;
using System.Collections.Generic;

namespace JAFDTC.Models.Import
{
    /// <summary>
    /// interface for classes that help interface between jafdtc navpoint system configruations an an import data
    /// source to allow import of navpoints.
    /// </summary>
    public interface IImportHelper
    {
        /// <summary>
        /// return true if the import data source has multiple flights, false otherwise
        /// </summary>
        public bool HasFlights { get; }

        /// <summary>
        /// return a list of flights with navpoints that are present in the import data source, null on error. data
        /// sources that do not support multiple flights will return an empty list.
        /// </summary>
        public List<string> Flights();

        /// <summary>
        /// TODO: document
        /// </summary>
        public bool Import(INavpointSystemImport navptSys, string flightName = "", bool isReplace = true);
    }
}
