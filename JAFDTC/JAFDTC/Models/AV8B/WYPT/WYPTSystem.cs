// ********************************************************************************************************************
//
// WYPTSystem.cs -- a-v8b waypoint system configuration
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
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace JAFDTC.Models.AV8B.WYPT
{
    public class WYPTSystem : NavpointSystemBase<WaypointInfo>
    {
        public const string SystemTag = "JAFDTC:AV8B:STPT";
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
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // TODO: document
        public void CleanUp()
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i].CleanUp();
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // waypoint management
        //
        // ------------------------------------------------------------------------------------------------------------

        // create a new steerpoint or take an existing steerpoint and add it to the end of the steerpoint list. the
        // steerpoint is numbered to follow the last steerpoint in the list. returns the steerpoint added, null on
        // error.
        //
        public override WaypointInfo Add(WaypointInfo wypt = null)
        {
            wypt ??= new();
            wypt.Number = (Points.Count == 0) ? 1 : Points[^1].Number + 1;
            wypt.Name = (string.IsNullOrEmpty(wypt.Name)) ? $"WP{wypt.Number}" : wypt.Name;
            return base.Add(wypt);
        }
    }
}
