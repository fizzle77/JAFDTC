// ********************************************************************************************************************
//
// ImportHelperCF.cs -- helper to import steerpoints from a .cf file
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

using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace JAFDTC.Models.Import
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class ImportHelperCF : ImportHelper
    {
        private AirframeTypes Airframe {  get; set; }
        private string Path { get; set; }

        private XmlDocument XmlDoc { get; set; }
        private Dictionary<string, XmlNode> XmlWaypointNodes { get; set; }

        public ImportHelperCF(AirframeTypes airframe, string path)
        {
            Airframe = airframe;
            Path = path;
            XmlDoc = new XmlDocument();
            XmlWaypointNodes = new Dictionary<string, XmlNode>();
        }

        private bool IsMatchingAirframe(string airframe)
        {
            switch (Airframe)
            {
                case AirframeTypes.None: return false;
                case AirframeTypes.A10C: return false;
                case AirframeTypes.AH64D: return false;
                case AirframeTypes.AV8B: return false;
                case AirframeTypes.F15E: return false;
                case AirframeTypes.F16C: return (airframe == "F-16C_50");
                case AirframeTypes.FA18C: return false;
                default: return false;
            }
        }

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
            catch
            {
                return null;
            }
            return flights;
        }

        public override List<Dictionary<string, string>> Waypoints(string flightName)
        {
            List<Dictionary<string, string>> waypoints = null;
            if (XmlWaypointNodes.ContainsKey(flightName))
            {
                bool isSteerpoint = false;
                waypoints = new List<Dictionary<string, string>>();
                foreach (XmlNode node in XmlWaypointNodes[flightName])
                {
                    // TODO: first steerpoint is usually take-off, too lazy right now to check Type node.
                    if (isSteerpoint)
                    {
                        double alt = double.Parse(node.SelectSingleNode("Altitude").InnerText);

                        Dictionary<string, string> steerpoint = new()
                        {
                            ["name"] = node.SelectSingleNode("Name").InnerText,
                            //["lat"] = ConvertDDtoDDM(node.SelectSingleNode("Lat").InnerText, true),
                            //["lon"] = ConvertDDtoDDM(node.SelectSingleNode("Lon").InnerText, false),
                            ["lat"] = node.SelectSingleNode("Lat").InnerText,
                            ["lon"] = node.SelectSingleNode("Lon").InnerText,
                            // ["alt"] = node.SelectSingleNode("Altitude").InnerText
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
    }
}
