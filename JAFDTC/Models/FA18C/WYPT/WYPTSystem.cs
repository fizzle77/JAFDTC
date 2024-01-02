﻿// ********************************************************************************************************************
//
// WYPTSystem.cs -- fa-18c waypoint system configuration
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

namespace JAFDTC.Models.FA18C.WYPT
{
    public class WYPTSystem : NavpointSystemBase<WaypointInfo>
    {
        public const string SystemTag = "JAFDTC:FA18C:WYPT";
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

        public override WaypointInfo NavpointFromInfo(Dictionary<string, string> navptInfo)
        {
            return new()
            {
                Name = (navptInfo.ContainsKey("name")) ? navptInfo["name"] : "",
                Lat = (navptInfo.ContainsKey("lat")) ? navptInfo["lat"] : "",
                Lon = (navptInfo.ContainsKey("lon")) ? navptInfo["lon"] : "",
                Alt = (navptInfo.ContainsKey("alt")) ? navptInfo["alt"] : ""
            };
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
