// ********************************************************************************************************************
//
// WYPTSystem.cs -- m-2000c waypoint system configuration
//
// Copyright(C) 2023-2025 ilominar/raven
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.Models.M2000C.WYPT
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public partial class WYPTSystem : NavpointSystemBase<WaypointInfo>
    {
        public const string SystemTag = "JAFDTC:M2000C:STPT";
        public const string WYPTListTag = $"{SystemTag}:LIST";

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public WYPTSystem() => (Points) = ([ ]);

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
                    Name = (navptInfo.TryGetValue("name", out string name)) ? name : "",
                    Lat = (navptInfo.TryGetValue("lat", out string lat)) ? lat : "",
                    Lon = (navptInfo.TryGetValue("lon", out string lon)) ? lon : "",
                    Alt = (navptInfo.TryGetValue("alt", out string alt)) ? alt : ""
                };
                Add(wypt);
            }
        }

        public override WaypointInfo Add(WaypointInfo wypt = null, int atIndex = -1)
        {
            wypt ??= new();
            atIndex = (atIndex >= Points.Count) ? -1 : atIndex;
            if (Points.Count == 0)
                wypt.Number = 1;
            else if (atIndex == -1)
                wypt.Number = Points[^1].Number + 1;
            else
                wypt.Number = Points[atIndex].Number;
            wypt.Name = (string.IsNullOrEmpty(wypt.Name)) ? $"WP{wypt.Number}" : wypt.Name;
            return base.Add(wypt, atIndex);
        }
    }
}
