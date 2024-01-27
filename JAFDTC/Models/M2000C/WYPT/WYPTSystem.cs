// ********************************************************************************************************************
//
// WYPTSystem.cs -- m-2000c waypoint system configuration
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

using JAFDTC.Models.Base;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.Models.M2000C.WYPT
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class WYPTSystem : NavpointSystemBase<WaypointInfo>
    {
        public const string SystemTag = "JAFDTC:M2000C:STPT";
        public const string WYPTListTag = $"{SystemTag}:LIST";

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public WYPTSystem() => (Points) = (new ObservableCollection<WaypointInfo>());

        public WYPTSystem(WYPTSystem other) => (Points) = (new ObservableCollection<WaypointInfo>(other.Points));

        public virtual object Clone() => new WYPTSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // NavpointSystemBase overrides
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void AddNavpointsFromInfoList(List<Dictionary<string, string>> navptInfoList)
        {
            foreach (Dictionary<string, string> navptInfo in navptInfoList)
            {
                WaypointInfo wypt = new()
                {
                    Name = (navptInfo.ContainsKey("name")) ? navptInfo["name"] : "",
                    Lat = (navptInfo.ContainsKey("lat")) ? navptInfo["lat"] : "",
                    Lon = (navptInfo.ContainsKey("lon")) ? navptInfo["lon"] : "",
                    Alt = (navptInfo.ContainsKey("alt")) ? navptInfo["alt"] : ""
                };
                Add(wypt);
            }
        }

        public override WaypointInfo Add(WaypointInfo wypt = null)
        {
            wypt ??= new();
            wypt.Number = (Points.Count == 0) ? 1 : Points[^1].Number + 1;
            wypt.Name = (string.IsNullOrEmpty(wypt.Name)) ? $"WP{wypt.Number}" : wypt.Name;
            return base.Add(wypt);
        }
    }
}
