// ********************************************************************************************************************
//
// ImportHelperMIZ.cs -- helper to import steerpoints from a .miz file
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

using JAFDTC.Models.DCS;
using JAFDTC.Utilities;
using JAFDTC.Utilities.LsonLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace JAFDTC.Models.Import
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class ImportHelperMIZ : ImportHelper
    {
        private const double M_TO_FT = 3.2808399;

        private AirframeTypes Airframe { get; set; }

        private string Path { get; set; }

        private string Theater { get; set; }

        private Dictionary<string, LsonValue> Parsed { get; set; }

        private Dictionary<string, LsonDict> MizRouteNodes { get; set; }

        public ImportHelperMIZ(AirframeTypes airframe, string path)
        {
            Airframe = airframe;
            Path = path;
            Theater = FileManager.ReadFileFromZip(Path, "theatre");
            MizRouteNodes = new Dictionary<string, LsonDict>();
        }

        private bool IsMatchingAirframe(string airframe)
        {
            return Airframe switch
            {
                AirframeTypes.None => false,
                AirframeTypes.A10C => false,
                AirframeTypes.AH64D => false,
                AirframeTypes.AV8B => false,
                AirframeTypes.F15E => false,
                AirframeTypes.F16C => (airframe == "F-16C_50"),
                AirframeTypes.FA18C => false,
                AirframeTypes.M2000C => false,
                _ => false,
            };
        }

        public override List<string> Flights()
        {
            MizRouteNodes.Clear();

            List<string> flights = new();
            try
            {
                string lua = FileManager.ReadFileFromZip(Path, "mission");
                Parsed = LsonVars.Parse(lua);
                LsonDict coalitionDict = Parsed["mission"].GetDict()["coalition"].GetDict();

                foreach (string coalitionKey in coalitionDict.Keys.Select(v => (string)v))
                {
                    Debug.WriteLine("----");
                    Debug.WriteLine("  coalition = " + coalitionKey);

                    LsonDict countryDict = coalitionDict[coalitionKey].GetDict()["country"].GetDict();
                    foreach (LsonNumber countryKey in countryDict.Keys.Cast<LsonNumber>())
                    {
                        LsonDict countryInfoDict = countryDict[countryKey].GetDict();

                        Debug.WriteLine("    country = " + countryKey.ToString());

#if NOT_YET_SUPPORTED
                        if (countryInfoDict.ContainsKey("helicopter") &&
                            countryInfoDict["helicopter"].GetDict().ContainsKey("group"))
                        {
                            LsonDict heloGroupArray = countryInfoDict["helicopter"].GetDict()["group"].GetDict();
                            foreach (LsonNumber heloGroupKey in heloGroupArray.Keys)
                            {
                                LsonDict heloGroupInfo = heloGroupArray[heloGroupKey].GetDict();
                                string groupName = heloGroupInfo["name"].GetString();
                                LsonDict routeInfo = heloGroupInfo["route"].GetDict()["points"].GetDict();
                                if (routeInfo.Keys.Count > 1)
                                {
                                    LsonDict unitInfo = heloGroupInfo["units"].GetDict()[new LsonNumber(1)].GetDict();
                                    if (unitInfo["callsign"].IsContainer)
                                    {
                                        string callsignName = unitInfo["callsign"].GetDict()["name"].GetString();

                                        Debug.WriteLine("      helo/group = " + heloGroupKey.ToString() + ", " + routeInfo.Keys.Count.ToString() + " --> " + groupName + " / " + callsignName);
                                    }
                                }
                            }
                        }
#endif

                        if (countryInfoDict.ContainsKey("plane") &&
                            countryInfoDict["plane"].GetDict().ContainsKey("group"))
                        {
                            LsonDict planeGroupArray = countryInfoDict["plane"].GetDict()["group"].GetDict();
                            foreach (LsonNumber planeGroupKey in planeGroupArray.Keys)
                            {
                                LsonDict planeGroupInfo = planeGroupArray[planeGroupKey].GetDict();
                                string groupName = planeGroupInfo["name"].GetString();
                                LsonDict routeInfo = planeGroupInfo["route"].GetDict()["points"].GetDict();
                                if (routeInfo.Keys.Count > 1)
                                {
                                    LsonDict unitInfo = planeGroupInfo["units"].GetDict()[new LsonNumber(1)].GetDict();
                                    string airframeType = unitInfo["type"].GetString();
                                    if (IsMatchingAirframe(airframeType))
                                    {
                                        flights.Add(groupName);
                                        MizRouteNodes[groupName] = routeInfo;

                                        Debug.WriteLine("      plane/group = " + planeGroupKey.ToString() + ", " + routeInfo.Keys.Count.ToString() + " --> " + groupName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return flights;
        }

        public override List<Dictionary<string, string>> Waypoints(string flightName)
        {
            List<Dictionary<string, string>> waypoints = null;
            if (MizRouteNodes.ContainsKey(flightName))
            {
                waypoints = new List<Dictionary<string, string>>();

                // walk the points in the route dictionary, skipping the first point as it is the initial point of the
                // unit on the ramp.
                //
                for (int i = 2; i < MizRouteNodes[flightName].Count; i++)
                {
                    LsonDict waypointInfo = MizRouteNodes[flightName][i].GetDict();

                    double x = (double)waypointInfo["x"].GetDecimal();
                    double z = (double)waypointInfo["y"].GetDecimal();
                    CoordLL ll = CoordInterpolator.Instance.XZtoLL(Theater, x, z);

                    double alt = (double)waypointInfo["alt"].GetDecimal() * M_TO_FT;

                    Dictionary<string, string> waypoint = new()
                    {
                        ["name"] = (waypointInfo.ContainsKey("name")) ? waypointInfo["name"].GetString() : $"SP{i - 1}",
                        ["lat"] = ll.Lat.ToString(),
                        ["lon"] = ll.Lon.ToString(),
                        ["alt"] = alt.ToString("0"),

                        // TODO: consider pulling "TOT" node from waypoint...
                        // TODO: put "ERROR" in dictionary if there were parse or conversion errors?
                    };
                    waypoints.Add(waypoint);
                }
            }
            return waypoints;
        }
    }
}
