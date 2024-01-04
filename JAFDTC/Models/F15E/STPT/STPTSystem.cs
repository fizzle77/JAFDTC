// ********************************************************************************************************************
//
// STPTSystem.cs -- f-15e steerpoint system configuration
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
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.Models.F15E.STPT
{
    /// <summary>
    /// mudhen steerpoint system configuration using a custom mudhen steerpoint (SteerpointInfo) along with the base
    /// navigationpoint system (NavpointSystemBase). the modeled configuration includes both steerpoints along with
    /// reference points asociated with a steerpoint.
    /// </summary>
    public class STPTSystem : NavpointSystemBase<SteerpointInfo>
    {
        public const string SystemTag = "JAFDTC:F15E:STPT";
        public const string STPTListTag = $"{SystemTag}:LIST";

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public STPTSystem() => (Points) = (new ObservableCollection<SteerpointInfo>());

        public STPTSystem(STPTSystem other) => (Points) = (new ObservableCollection<SteerpointInfo>(other.Points));

        public virtual object Clone() => new STPTSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // NavpointSystemBase overrides
        //
        // ------------------------------------------------------------------------------------------------------------

        public override SteerpointInfo NavpointFromInfo(Dictionary<string, string> navptInfo)
        {
            string name = (navptInfo.ContainsKey("name")) ? navptInfo["name"] : "";
            return new()
            {
                // TODO: could include TOS too, if available in navptInfo
                Name = name,
                Lat = (navptInfo.ContainsKey("lat")) ? navptInfo["lat"] : "",
                Lon = (navptInfo.ContainsKey("lon")) ? navptInfo["lon"] : "",
                Alt = (navptInfo.ContainsKey("alt")) ? navptInfo["alt"] : "",
                IsTarget = name.ToLower().Contains("#t")
            };
        }

        public override SteerpointInfo Add(SteerpointInfo stpt = null)
        {
            stpt ??= new();
            stpt.Number = (Points.Count == 0) ? 1 : Points[^1].Number + 1;
            stpt.Name = (string.IsNullOrEmpty(stpt.Name)) ? $"SP{stpt.Number}" : stpt.Name;
            return base.Add(stpt);
        }
    }
}
