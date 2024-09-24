// ********************************************************************************************************************
//
// F16CPilotsDbase.cs -- F-16C pilot database.
//
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

using JAFDTC.Models.F16C.DLNK;
using JAFDTC.Utilities;
using System.Collections.Generic;

namespace JAFDTC.Models.F16C
{
    /// <summary>
    /// model for the viper pilot database stored in the user database area. this database carries entries that are
    /// serialized ViperDriver instances.
    /// </summary>
    public class F16CPilotsDbase
    {
        public readonly static string PilotDbFilename = "jafdtc-pilots-f16c.json";

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// load the viper pilot database from the standard user location and return it.
        /// </summary>
        public static List<ViperDriver> LoadDbase()
        {
            return FileManager.LoadUserDbase<ViperDriver>(PilotDbFilename);
        }

        /// <summary>
        /// update the viper pilot database to match the specified value. returns true on success, false on failure. 
        /// </summary>
        public static bool UpdateDbase(List<ViperDriver> newDb)
        {
            return FileManager.SaveUserDbase<ViperDriver>(F16CPilotsDbase.PilotDbFilename, newDb);
        }
    }
}
