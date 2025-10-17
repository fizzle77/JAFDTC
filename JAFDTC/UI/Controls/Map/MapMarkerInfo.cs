// ********************************************************************************************************************
//
// MapMarkerInfo.cs : map control marker information
//
// Copyright(C) 2025 ilominar/raven
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

using JAFDTC.Models.DCS;
using System;

namespace JAFDTC.UI.Controls.Map
{
    /// <summary>
    /// information for a map marker (route point, poi, etc.) edited by the WorldMapControl. this includes the
    /// type, string tag, and integer tag that uniquely identify the marker along with the current lat/lon position
    /// of the marker on the map.
    /// </summary>
    public sealed partial class MapMarkerInfo
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// types for map markers. includes all PointOfInterestType types so a PointOfInterestType can be directly
        /// cast to this type.
        /// </summary>
        public enum MarkerType
        {
            UNKNOWN = PointOfInterestType.UNKNOWN,
            DCS_CORE = PointOfInterestType.DCS_CORE,
            USER = PointOfInterestType.USER,
            CAMPAIGN = PointOfInterestType.CAMPAIGN,
            NAVPT = 16,
            NAVPT_HANDLE = 17,
            IMPORT_GEN = 18,
            IMPORT_S2A = 19,
            BULLSEYE = 20,
            ANY = 31
        }

        /// <summary>
        /// type mask for MarkerType enum. includes all PointOfInterestTypeMask values so a PointOfInterestTypeMask
        /// can be directly cast to this type.
        /// </summary>
        [Flags]
        public enum MarkerTypeMask
        {
            NONE = 0,
            ANY = -1,
            DCS_CORE = 1 << MarkerType.DCS_CORE,
            USER = 1 << MarkerType.USER,
            CAMPAIGN = 1 << MarkerType.CAMPAIGN,
            NAVPT = 1 << MarkerType.NAVPT,
            NAVPT_HANDLE = 1 << MarkerType.NAVPT_HANDLE,
            IMPORT_GEN = 1 << MarkerType.IMPORT_GEN,
            IMPORT_S2A = 1 << MarkerType.IMPORT_S2A,
            BULLSEYE = 1 << MarkerType.BULLSEYE
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public readonly MapMarkerInfo.MarkerType Type;
        public readonly string TagStr;
        public readonly int TagInt;
        public readonly string Lat;
        public readonly string Lon;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapMarkerInfo()
            => (Type, TagStr, TagInt, Lat, Lon) = (MapMarkerInfo.MarkerType.UNKNOWN, null, -1, null, null);

        public MapMarkerInfo(MapMarkerInfo.MarkerType type, string tagStr = null, int tagInt = -1, string lat = null,
                             string lon = null)
            => (Type, TagStr, TagInt, Lat, Lon) = (type, tagStr, tagInt, lat, lon);

        internal MapMarkerInfo(MapMarkerControl marker)
        {
            Tuple<MapMarkerInfo.MarkerType, string, int> tuple = marker.Tag as Tuple<MapMarkerInfo.MarkerType, string, int>;
            Type = tuple.Item1;
            TagStr = (Type != MapMarkerInfo.MarkerType.UNKNOWN) ? tuple.Item2 : null;
            TagInt = (Type != MapMarkerInfo.MarkerType.UNKNOWN) ? tuple.Item3 : -1;
            Lat = (Type != MapMarkerInfo.MarkerType.UNKNOWN) ? $"{marker.Location.Latitude:F8}" : null;
            Lon = (Type != MapMarkerInfo.MarkerType.UNKNOWN) ? $"{marker.Location.Longitude:F8}" : null;
        }
    }
}
