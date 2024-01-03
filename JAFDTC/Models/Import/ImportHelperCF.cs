// ********************************************************************************************************************
//
// ImportHelperCF.cs -- helper to import navpoints from a .cf file
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
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace JAFDTC.Models.Import
{
    /// <summary>
    /// import helper class to extract navpoints from a flight in a combatflite .cf file. flights from the .cf are only
    /// considered if the airframe matches an airframe type provided at consturction.
    /// </summary>
    public class ImportHelperCF : ImportHelper
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private AirframeTypes Airframe {  get; set; }

        private string Path { get; set; }

        private XmlDocument XmlDoc { get; set; }
        
        private Dictionary<string, XmlNode> XmlWaypointNodes { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ImportHelperCF(AirframeTypes airframe, string path)
        {
            Airframe = airframe;
            Path = path;
            XmlDoc = new XmlDocument();
            XmlWaypointNodes = new Dictionary<string, XmlNode>();
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
                AirframeTypes.F15E => (airframe == "F-15ESE"),
                AirframeTypes.F16C => (airframe == "F-16C_50"),
                AirframeTypes.FA18C => (airframe == "FA-18C_hornet"),
                AirframeTypes.M2000C => (airframe == "M-2000C"),
                AirframeTypes.F14AB => (airframe == "F-14A-135-GR") || (airframe == "F-14B"),
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
            if (XmlWaypointNodes.ContainsKey(flightName))
            {
                waypoints = new List<Dictionary<string, string>>();

                bool isSteerpoint = false;
                foreach (XmlNode node in XmlWaypointNodes[flightName])
                {
                    // TODO: first steerpoint is usually take-off, too lazy right now to check Type node.
                    if (isSteerpoint)
                    {
                        double alt = double.Parse(node.SelectSingleNode("Altitude").InnerText);

                        Dictionary<string, string> steerpoint = new()
                        {
                            ["name"] = node.SelectSingleNode("Name").InnerText,
                            ["lat"] = node.SelectSingleNode("Lat").InnerText,
                            ["lon"] = node.SelectSingleNode("Lon").InnerText,
                            ["alt"] = alt.ToString("0")

                            // TODO: consider pulling "TOT" node from waypoint...
                            // TODO: put "ERROR" in dictionary if there were errors in the stpt?
                        };
                        waypoints.Add(steerpoint);
                    }
                    isSteerpoint = true;
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
            XmlWaypointNodes.Clear();

            List<string> flights = new();
            try
            {
                string xml = FileManager.ReadFileFromZip(Path, "mission.xml");
                XmlDoc.LoadXml(xml);

                XmlNode routes = XmlDoc.DocumentElement.SelectSingleNode("/Mission/Routes");
                foreach (XmlNode route in routes.ChildNodes)
                {
                    XmlNode aircraft = route.SelectSingleNode("FlightMembers/FlightMember/Aircraft");
                    if (IsMatchingAirframe(aircraft.SelectSingleNode("Type").InnerText))
                    {
                        string callsignName = route.SelectSingleNode("CallsignName").InnerText;
                        string callsignNumber = route.SelectSingleNode("CallsignNumber").InnerText;
                        string flightName = callsignName + " " + callsignNumber;

                        if (XmlWaypointNodes.ContainsKey(flightName))
                        {
                            throw new InvalidOperationException("Duplicate flight name in file.");
                        }
                        flights.Add(flightName);
                        XmlWaypointNodes[flightName] = route.SelectSingleNode("Waypoints");
                    }
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"ImportHelperCF:Flights exception {ex}");
                return null;
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
