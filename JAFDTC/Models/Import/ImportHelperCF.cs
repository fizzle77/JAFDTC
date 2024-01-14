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
using System.Text.RegularExpressions;
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

        // ---- private properties

        private AirframeTypes Airframe {  get; set; }

        private string Path { get; set; }

        private XmlDocument XmlDoc { get; set; }
        
        private Dictionary<string, XmlNode> XmlNavpointNodes { get; set; }

        private bool IsImportTakeOff { get; set; }

        private bool IsImportTOS { get; set; }

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
            XmlNavpointNodes = new Dictionary<string, XmlNode>();

            IsImportTakeOff = false;
            IsImportTOS = false;
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
        ///   ["ton"]       (string) time on navpoint, hh:mm:ss local
        /// </summary>
        private List<Dictionary<string, string>> Navpoints(string flightName)
        {
            List<Dictionary<string, string>> navpoints = null;
            if (XmlNavpointNodes.ContainsKey(flightName))
            {
                navpoints = new List<Dictionary<string, string>>();
                foreach (XmlNode node in XmlNavpointNodes[flightName])
                {
                    string type = node.SelectSingleNode("Type").InnerText;
                    bool isTakeOffType = ((type != null) && Regex.Match(type.ToLower(), @"^take off").Success);

                    if (IsImportTakeOff || !isTakeOffType)
                    {
                        Dictionary<string, string> navpoint = new()
                        {
                            ["name"] = node.SelectSingleNode("Name").InnerText,
                            ["lat"] = node.SelectSingleNode("Lat").InnerText,
                            ["lon"] = node.SelectSingleNode("Lon").InnerText,
                        };

                        if (!double.TryParse(node.SelectSingleNode("Altitude").InnerText, out double alt))
                        {
                            alt = 0.0;
                        }
                        navpoint["alt"] = $"{(int)alt:D}";

                        string ton = node.SelectSingleNode("TOT").InnerText;
                        if ((ton != null) && IsImportTOS)
                        {
                            string[] parts = ton.Split(' ');
                            if (parts.Length == 3)
                            {
                                string[] hms = parts[1].Split(':');
                                if ((hms.Length == 3) &&
                                    (int.TryParse(hms[0], out int h) && (h >= 1) && (h < 13)) &&
                                    (int.TryParse(hms[1], out int m) && (m >= 0) && (m < 60)) &&
                                    (int.TryParse(hms[2], out int s) && (s >= 0) && (s < 60)))
                                {
                                    if ((parts[2].ToLower() == "pm") && (h < 12))
                                    {
                                        h += 12;
                                    }
                                    navpoint["ton"] = $"{h:D2}:{m:D2}:{s:D2}";
                                }
                            }
                        }

                        // TODO: put "ERROR" in dictionary if there were errors in the stpt?
                        navpoints.Add(navpoint);
                    }
                }
            }
            return navpoints;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IImportHelper functions
        //
        // ------------------------------------------------------------------------------------------------------------

        public override bool HasFlights => true;

        public override List<string> Flights()
        {
            XmlNavpointNodes.Clear();

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

                        if (XmlNavpointNodes.ContainsKey(flightName))
                        {
                            throw new InvalidOperationException("Duplicate flight name in file.");
                        }
                        flights.Add(flightName);
                        XmlNavpointNodes[flightName] = route.SelectSingleNode("Waypoints");
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

        public override Dictionary<string, string> OptionTitles(string what = "Steerpoint")
            => new()
            {
                ["A"] = $"Import Take Off {what}s",
                ["B"] = $"Import Time on {what}",
            };

        public override Dictionary<string, object> OptionDefaults
            => new()
            {
                ["A"] = false,
                ["B"] = false,
            };

        public override bool Import(INavpointSystemImport navptSys, string flightName = "", bool isReplace = true,
                                    Dictionary<string, object> options = null)
        {
            IsImportTakeOff = (bool)options["A"];
            IsImportTOS = (bool)options["B"];

            List<Dictionary<string, string>> navptInfoList = Navpoints(flightName);
            if (navptInfoList != null)
            {
                return navptSys.ImportNavpointInfoList(navptInfoList, isReplace);
            }
            return false;
        }
    }
}
