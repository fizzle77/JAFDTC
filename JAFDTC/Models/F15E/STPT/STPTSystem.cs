// ********************************************************************************************************************
//
// STPTSystem.cs -- f-15e steerpoint system configuration
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.F15E.STPT
{
    /// <summary>
    /// mudhen steerpoint system configuration using a custom mudhen steerpoint (SteerpointInfo) along with the base
    /// navigationpoint system (NavpointSystemBase). the modeled configuration includes both steerpoints along with
    /// reference points asociated with a steerpoint.
    /// 
    /// the f-15e avionics support three routes: a, b, and c. for compatibilty with other abstractions, we keep
    /// navpoints for all three routes in Points, maintaing them in route than number order. this means that some
    /// operations, such as Add or Renumber, will operate on the "active route" rather than all steerpoints in Points.
    /// </summary>
    public partial class STPTSystem : NavpointSystemBase<SteerpointInfo>
    {
        public const string SystemTag = "JAFDTC:F15E:STPT";
        public const string STPTListTag = $"{SystemTag}:LIST";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        [JsonIgnore]
        public string ActiveRoute { get; set; }

        // ---- private properties, static

        private static readonly Regex _hashRefPtRegex = new(@"#\.([1-7])");

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public STPTSystem() => (Points, ActiveRoute) = ([ ], "A");

        public STPTSystem(STPTSystem other)
            => (Points, ActiveRoute) = (new ObservableCollection<SteerpointInfo>(other.Points), other.ActiveRoute);

        public virtual object Clone() => new STPTSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // NavpointSystemBase overrides
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO
        /// </summary>
        public override void AddNavpointsFromInfoList(List<Dictionary<string, string>> navptInfoList)
        {
           // TODO: handle nav table ref points per razbam
            SteerpointInfo stptCur = null;
            foreach (Dictionary<string, string> navptInfo in navptInfoList)
            {
                SteerpointInfo stpt = new()
                {
                    Name = (navptInfo.TryGetValue("name", out string valName)) ? valName : "",
                    Route = (navptInfo.TryGetValue("route", out string route)) ? route : "A",
                    Lat = (navptInfo.TryGetValue("lat", out string lat)) ? lat : "",
                    Lon = (navptInfo.TryGetValue("lon", out string lon)) ? lon : "",
                    Alt = (navptInfo.TryGetValue("alt", out string alt)) ? alt : "",
                    TOT = (navptInfo.TryGetValue("ton", out string ton)) ? ton : "",
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
                    RefPointInfo rfpt = new(int.Parse(match.Groups[1].Value))
                    {
                        Lat = stpt.Lat,
                        Lon = stpt.Lon,
                        Alt = stpt.Alt
                    };
                    stptCur.RefPoints.Add(rfpt);
                }
                else if (!name.Contains('#'))
                {
                    Add(stpt);
                    stptCur = stpt;
                }
            }
        }

        /// <summary>
        /// RenumberFrom picks up the additional semantics in the f-15e of only renumbering those steerpoints on
        /// the active route.
        /// </summary>
        public override void RenumberFrom(int startNumber)
        {
            for (int i = 0; i < Count; i++)
                if (Points[i].Route == ActiveRoute)
                    Points[i].Number = startNumber++;
        }

        /// <summary>
        /// Add picks up the additional semantics in the f-15e adding new (ie, stpt == null) steerpoints to the
        /// active route; otherwise the route in the steerpoint is honored. when adding the steerpoint, it is
        /// inserted into Points to maintain by route, by number ordering.
        /// </summary>
        public override SteerpointInfo Add(SteerpointInfo stpt = null, int atIndex = -1)
        {
            string route = (stpt != null) ? stpt.Route : ActiveRoute;

            int number = 0;
            int iInsert = (Count > 0) ? -1 : 0;
            int numRoute = 0;
            for (int i = 0; i < Count; i++)
                if (Points[i].Route == route)
                {
                    if (Points[i].Number > number)
                        number = Points[i].Number;
                    iInsert = i + 1;
                    numRoute += 1;
                }
            if ((iInsert == -1) && (route == "A"))
                iInsert = 0;
            else if ((iInsert == -1) && (route == "B"))
                for (iInsert = 0; (iInsert < Count) && (Points[iInsert].Route != "B"); iInsert++)
                    ;
            else if ((iInsert == -1) && (route == "C"))
                iInsert = Count;

            if ((atIndex != -1) && (numRoute > atIndex))
            {
                iInsert = (iInsert - numRoute) + atIndex;
                number = Points[iInsert].Number - 1;
            }

            stpt ??= new();
            stpt.Route = route;
            stpt.Number = number + 1;
            stpt.Name = (string.IsNullOrEmpty(stpt.Name)) ? $"SP{stpt.Number}{stpt.Route}" : stpt.Name;
            return base.Add(stpt, iInsert);
        }
    }
}
