// ********************************************************************************************************************
//
// ImportHelperJSON.cs -- helper to import navpoints from a .json file
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
using JAFDTC.Models.DCS;
using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Text.Json;

namespace JAFDTC.Models.Import
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class ImportHelperJSON : ImportHelper
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private AirframeTypes Airframe { get; set; }

        private string Path { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ImportHelperJSON(AirframeTypes airframe, string path)
        {
            Airframe = airframe;
            Path = path;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IImportHelper functions
        //
        // ------------------------------------------------------------------------------------------------------------

        public override bool HasFlights => false;

        public override List<string> Flights() => new();

        public override bool Import(INavpointSystemImport navptSys, string flightName = "", bool isReplace = true,
                                    Dictionary<string, object> options = null)
        {
            string json = FileManager.ReadFile(Path);
            if (json != null)
            {
                if (ImportIsPOIs(json))
                    return navptSys.ImportSerializedPOIs(json, isReplace);
                else
                    return navptSys.ImportSerializedNavpoints(json, isReplace);
            }
            return false;
        }

        /// <summary>
        /// Examine the JSON string and determine if it is actually a point of interest export.
        /// 
        /// Returns true if the JSON looks like POIs, false if not.
        /// </summary>
        protected virtual bool ImportIsPOIs(string json)
        {
            try
            {
                List<PointOfInterest> pois = JsonSerializer.Deserialize<List<PointOfInterest>>(json);

                return pois.Count > 1 &&
                    pois[0].Type != PointOfInterestType.UNKNOWN &&
                    !string.IsNullOrEmpty(pois[0].Theater) &&
                    !string.IsNullOrEmpty(pois[0].Elevation) &&
                    !string.IsNullOrEmpty(pois[0].Latitude) &&
                    !string.IsNullOrEmpty(pois[0].Longitude);
            }
            catch
            {
                return false;
            }
        }
    }
}
