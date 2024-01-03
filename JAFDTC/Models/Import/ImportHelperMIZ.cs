// ********************************************************************************************************************
//
// ImportHelperMIZ.cs -- helper to import navpoints from a .miz file
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
using JAFDTC.Models.DCS;
using JAFDTC.Utilities;
using JAFDTC.Utilities.LsonLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace JAFDTC.Models.Import
{
    /// <summary>
    /// import helper class to extract navpoints from a flight in a dcs .miz file. flights from the .miz are only
    /// considered if the airframe matches an airframe type provided at consturction.
    /// </summary>
    public class ImportHelperMIZ : ImportHelper
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private AirframeTypes Airframe { get; set; }

        private string Path { get; set; }

        private string Theater { get; set; }

        private Dictionary<string, LsonValue> Parsed { get; set; }

        private Dictionary<string, LsonDict> MizRouteNodes { get; set; }

        private const double M_TO_FT = 3.2808399;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ImportHelperMIZ(AirframeTypes airframe, string path)
        {
            Airframe = airframe;
            Path = path;
            Theater = FileManager.ReadFileFromZip(Path, "theatre");
            MizRouteNodes = new Dictionary<string, LsonDict>();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        private bool IsMatchingAirframe(string airframe)
        {
            return Airframe switch
            {
                // TODO: should .None allow any airframe type to match?
                AirframeTypes.None => false,
                AirframeTypes.A10C => (airframe == "A-10C_2"),
                AirframeTypes.AH64D => (airframe == "AH-64D_BLK_II"),
                AirframeTypes.AV8B => (airframe == "AV8BNA"),
                AirframeTypes.F14AB => (airframe == "F-14A-135-GR") || (airframe == "F-14B"),
                AirframeTypes.F15E => (airframe == "F-15ESE"),
                AirframeTypes.F16C => (airframe == "F-16C_50"),
                AirframeTypes.FA18C => (airframe == "FA-18C_hornet"),
                AirframeTypes.M2000C => (airframe == "M-2000C"),
                _ => false,
            };
        }

        /// <summary>
        /// return a list of navpoints from the import data source for the flight with the given name. for sources
        /// where HasFlights is false, the flight name is ignored. for sources where HasFlights is true, the flight
        /// name must match one of the flights from Flights(). navpoints are represented by a string/string
        /// dictionary with the following key/value pairs:
        /// 
        ///   ["name"]      (string) name of navpoint
        ///   ["lat"]       (string) latitude of navpoint, decimal degrees with no units
        ///   ["lon"]       (string) longitude of navpoint, decimal degrees with no units
        ///   ["alt"]       (string) elevation of navpoint, feet
        /// </summary>
        private List<Dictionary<string, string>> Navpoints(string flightName)
        {
            List<Dictionary<string, string>> waypoints = null;
            if (MizRouteNodes.ContainsKey(flightName))
            {
                waypoints = new List<Dictionary<string, string>>();

                // walk the points in the route dictionary, skipping the first point as it is the initial point of the
                // unit on the ramp.
                //
                for (int i = 2; i <= MizRouteNodes[flightName].Count; i++)
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

                        // TODO: consider pulling "TOS" node from waypoint...
                        // TODO: put "ERROR" in dictionary if there were parse or conversion errors?
                    };
                    waypoints.Add(waypoint);
                }
            }
            return waypoints;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IImportHelper functions
        //
        // ------------------------------------------------------------------------------------------------------------

        public override bool HasFlights => true;

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
                    FileManager.Log("----");
                    FileManager.Log($"  coalition = {coalitionKey}");

                    LsonDict countryDict = coalitionDict[coalitionKey].GetDict()["country"].GetDict();
                    foreach (LsonNumber countryKey in countryDict.Keys.Cast<LsonNumber>())
                    {
                        LsonDict countryInfoDict = countryDict[countryKey].GetDict();

                        FileManager.Log($"    country = {countryKey}");

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
                            foreach (LsonNumber planeGroupKey in planeGroupArray.Keys.Cast<LsonNumber>())
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

                                        FileManager.Log($"      plane/group = {planeGroupKey}, {routeInfo.Keys.Count} --> {groupName}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"ImportHelperMIZ:Flights exception {ex}");
            }

            return flights;
        }

        public override bool Import(INavpointSystemImport navptSys, string flightName = "", bool isReplace = true)
        {
            List<Dictionary<string, string>> navptInfoList = Navpoints(flightName);
            if (navptInfoList != null)
            {
                return navptSys.ImportNavpointInfoList(navptInfoList, isReplace);
            }
            return false;
        }
    }
}
