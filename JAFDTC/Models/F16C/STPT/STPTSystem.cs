// ********************************************************************************************************************
//
// STPTSystem.cs -- f-16c steerpoint system configuration
//
// Copyright(C) 2021-2023 the-paid-actor & others
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
using System.Text.RegularExpressions;

namespace JAFDTC.Models.F16C.STPT
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public partial class STPTSystem : NavpointSystemBase<SteerpointInfo>
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        public const string SystemTag = "JAFDTC:F16C:STPT";
        public const string STPTListTag = $"{SystemTag}:LIST";

        private const double DEG_TO_RAD = Math.PI / 180.0;
        private const double RAD_TO_DEG = 1.0 / DEG_TO_RAD;
        private const double M_TO_FT = 3.2808399;
        private const double FT_TO_NM = 0.00016458;

        private const double R_EARTH = 6375585.50700497;                // nominal radius in m, from DCS larger sm axis

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- private properties, static

        private static readonly Regex _hashMagRegex = new(@"#OAP\.[12],MAG=([0-9]+[\.]{0,1}[0-9]*)");

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public STPTSystem() => (Points) = ([]);

        public STPTSystem(STPTSystem other) => (Points) = (new ObservableCollection<SteerpointInfo>(other.Points));

        public virtual object Clone() => new STPTSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // utility
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// use the haversine formula to figure out great circle distance between two steerpoints.  as we're doing
        /// this assuming a perfect sphere. returns the distance as a string in the given format (either feet or
        /// nautical miles).
        /// 
        /// NOTE: this computation is not exact as it assumes a perfect sphere while dcs doesn't.
        /// </summary>
        private static string StptRange(SteerpointInfo stptA, SteerpointInfo stptB, bool isFeet)
        {
            double latA = double.Parse(stptA.Lat) * DEG_TO_RAD;
            double lonA = double.Parse(stptA.Lon) * DEG_TO_RAD;
            double latB = double.Parse(stptB.Lat) * DEG_TO_RAD;
            double lonB = double.Parse(stptB.Lon) * DEG_TO_RAD;

            double dLat = latB - latA;
            double dLon = lonB - lonA;

            double sin2Lat = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0);
            double sin2Lon = Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

            double a = sin2Lat + (Math.Cos(latA) * Math.Cos(latB) * sin2Lon);
            double d = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a)) * R_EARTH * M_TO_FT;

            return (isFeet) ? $"{d:F0}" : $"{d * FT_TO_NM:F1}";
        }

        /// <summary>
        /// return the initial bearing for a great circle route between two steerpoints. returns the bearing as a
        /// string.
        /// 
        /// NOTE: this computation is not exact as it assumes a perfect sphere while dcs doesn't.
        /// </summary>
        private static string StptBearing(SteerpointInfo stptA, SteerpointInfo stptB, double magVar)
        {
            double latA = double.Parse(stptA.Lat) * DEG_TO_RAD;
            double lonA = double.Parse(stptA.Lon) * DEG_TO_RAD;
            double latB = double.Parse(stptB.Lat) * DEG_TO_RAD;
            double lonB = double.Parse(stptB.Lon) * DEG_TO_RAD;

            double dLon = lonB - lonA;

            double theta = Math.Atan2(Math.Sin(dLon) * Math.Cos(latB),
                                      Math.Cos(latA) * Math.Sin(latB) - Math.Sin(latA) * Math.Cos(latB) * Math.Cos(dLon));
            double deg = ((theta * RAD_TO_DEG) - magVar + 360.0) % 360.0;

            return $"{deg:F1}";
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private static string StptDeltaElev(SteerpointInfo stptA, SteerpointInfo stptB)
        {
            int elevA = int.Parse(stptA.Alt);
            int elevB = int.Parse(stptB.Alt);

            return $"{elevB - elevA:D}";
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // NavpointSystemBase overrides
        //
        // ------------------------------------------------------------------------------------------------------------
        public override void AddNavpointsFromInfoList(List<Dictionary<string, string>> navptInfoList)
        {
            SteerpointInfo stptCur = null;
            SteerpointInfo stptVIP = null;
            SteerpointInfo stptVRP = null;
            foreach (Dictionary<string, string> navptInfo in navptInfoList)
            {
                SteerpointInfo stpt = new()
                {
                    Name = (navptInfo.TryGetValue("name", out string valName)) ? valName : "",
                    Lat = (navptInfo.TryGetValue("lat", out string lat)) ? lat : "",
                    Lon = (navptInfo.TryGetValue("lon", out string lon)) ? lon : "",
                    Alt = (navptInfo.TryGetValue("alt", out string alt)) ? alt : "",
                    TOS = (navptInfo.TryGetValue("ton", out string ton)) ? ton : ""
                };
                string name = stpt.Name.ToUpper();

                if ((stptCur != null) && (name.Contains("#OAP.1") || name.Contains("#OAP.2")))
                {
                    int index = (name.Contains("#OAP.1")) ? 0 : 1;
                    Match match = _hashMagRegex.Match(name);
                    double magVar = (match.Groups.Count >= 2) ? double.Parse(match.Groups[1].Value) : 0.0;

                    stptCur.OAP[index].Type = RefPointTypes.OAP;
                    stptCur.OAP[index].Range = StptRange(stptCur, stpt, true);
                    stptCur.OAP[index].Brng = StptBearing(stptCur, stpt, 0.0);
                    stptCur.OAP[index].Elev = stpt.Alt;
                }
                else if ((stptCur != null) && (name.Contains("#VIP.V2T") || name.Contains("#VIP.V2P")))
                {
                    if ((stptVIP == null) || (stptVIP == stptCur))
                    {
                        int index = (name.Contains("#VIP.V2T")) ? 0 : 1;
                        stptCur.VxP[index].Type = RefPointTypes.VIP;
                        stptCur.VxP[index].Range = StptRange(stptCur, stpt, true);
                        stptCur.VxP[index].Brng = StptBearing(stptCur, stpt, 0.0);
                        stptCur.VxP[index].Elev = StptDeltaElev(stptCur, stpt);
                        stptVIP = stptCur;
                    }
                }
                else if ((stptCur != null) && (name.Contains("#VRP.T2V") || name.Contains("#VRP.T2P")))
                {
                    if ((stptVRP == null) || (stptVRP == stptCur))
                    {
                        int index = (name.Contains("#VRP.T2V")) ? 0 : 1;
                        stptCur.VxP[index].Type = RefPointTypes.VRP;
                        stptCur.VxP[index].Range = StptRange(stptCur, stpt, false);
                        stptCur.VxP[index].Brng = StptBearing(stptCur, stpt, 0.0);
                        stptCur.VxP[index].Elev = StptDeltaElev(stptCur, stpt);
                        stptVRP = stptCur;
                    }
                }
                else if (!name.Contains("#"))
                {
                    Add(stpt);
                    stptCur = stpt;
                }
            }
        }

        public override SteerpointInfo Add(SteerpointInfo stpt = null, int atIndex = -1)
        {
            stpt ??= new();
            atIndex = (atIndex >= Points.Count) ? -1 : atIndex;
            if (Points.Count == 0)
                stpt.Number = 1;
            else if (atIndex == -1)
                stpt.Number = Points[^1].Number + 1;
            else
                stpt.Number = Points[atIndex].Number;
            stpt.Name = (string.IsNullOrEmpty(stpt.Name)) ? $"SP{stpt.Number}" : stpt.Name;
            return base.Add(stpt, atIndex);
        }
    }
}
