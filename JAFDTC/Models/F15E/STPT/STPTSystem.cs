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
using System.Text.RegularExpressions;

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
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- private properties, static

        private static readonly Regex _hashRefPtRegex = new(@"#\.([1-7])");

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

        public override void AddNavpointsFromInfoList(List<Dictionary<string, string>> navptInfoList)
        {
            // TODO: handle route
            // TODO: handle nav table ref points per razbam
            SteerpointInfo stptCur = null;
            foreach (Dictionary<string, string> navptInfo in navptInfoList)
            {
                SteerpointInfo stpt = new()
                {
                    Name = (navptInfo.ContainsKey("name")) ? navptInfo["name"] : "",
                    Lat = (navptInfo.ContainsKey("lat")) ? navptInfo["lat"] : "",
                    Lon = (navptInfo.ContainsKey("lon")) ? navptInfo["lon"] : "",
                    Alt = (navptInfo.ContainsKey("alt")) ? navptInfo["alt"] : "",
                    TOT = (navptInfo.ContainsKey("ton")) ? navptInfo["ton"] : "",
                };
                string name = stpt.Name.ToUpper();

                stpt.IsTarget = (name.Contains("#T"));
                if (stpt.IsTarget)
                {
                    stpt.Name = stpt.Name.Replace("#T", "").Replace("#t", "");
                }
                Match match = _hashRefPtRegex.Match(name);
                if ((stptCur != null) && (match.Groups.Count >= 2))
                {
                    RefPointInfo rfpt = new(int.Parse(match.Groups[1].Value));
                    rfpt.Lat = stpt.Lat;
                    rfpt.Lon = stpt.Lon;
                    rfpt.Alt = stpt.Alt;
                    stptCur.RefPoints.Add(rfpt);
                }
                else if (!name.Contains("#"))
                {
                    Add(stpt);
                    stptCur = stpt;
                }
            }
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
