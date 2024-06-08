// ********************************************************************************************************************
//
// INavpointSystemImport.cs -- interface for a navpoint system object that can perform imports
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

using System.Collections.Generic;

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// interface for an object that supports updates of navpoints in a navpoint system from an import.
    /// </summary>
    public interface INavpointSystemImport
    {
        /// <summary>
        /// deserialize an array of navpoints from .json and incorporate them into the navpoint list. the deserialized
        /// navpoints can either replace the existing navpoints or be appended to the end of the navpoint list. returns
        /// true on success, false on error (previous navpoints preserved on errors).
        /// 
        /// imports from serialized navpoints support all navpoint properties.
        /// </summary>
        public bool ImportSerializedNavpoints(string json, bool isReplace = true);

        /// <summary>
        /// deserialize an array of POIs from .json and incorporate them into the navpoint list. the deserialized
        /// navpoints can either replace the existing navpoints or be appended to the end of the navpoint list. returns
        /// true on success, false on error (previous navpoints preserved on errors).
        /// 
        /// imports from serialized POIs support all navpoint properties.
        /// </summary>
        public bool ImportSerializedPOIs(string json, bool isReplace = true);

        /// <summary>
        /// incorporate a list of navpoints specified by navpoint info dictionaries (see navptInfoList) into the
        /// navpoint list. the new navpoints can either replace the existing navpoints or be appended to the end of
        /// the navpoing list. returns true on success, false on error (previous navpoints preserved on errors).
        /// 
        /// imports from navpoint info dictionaries may only support basic navpoint properties (like coordinates).
        /// </summary>
        public bool ImportNavpointInfoList(List<Dictionary<string, string>> navptInfoList, bool isReplace = true);
    }
}
