// ********************************************************************************************************************
//
// FA18CEditWaypointListHelper.cs : IEditNavpointListPageHelper for the fa-18c configuration
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
using JAFDTC.Models.FA18C;
using JAFDTC.Models.FA18C.WYPT;
using JAFDTC.Models;
using JAFDTC.UI.A10C;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;
using JAFDTC.Models.A10C;

namespace JAFDTC.UI.FA18C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    internal class FA18CEditWaypointListHelper : IEditNavpointListPageHelper
    {
        public static ConfigEditorPageInfo PageInfo
            => new(WYPTSystem.SystemTag, "Waypoints", "WYPT", Glyphs.WYPT,
                   typeof(EditNavpointListPage), typeof(FA18CEditWaypointListHelper));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public string SystemTag => WYPTSystem.SystemTag;

        public string NavptListTag => WYPTSystem.WYPTListTag;

        public AirframeTypes AirframeType => AirframeTypes.FA18C;

        public string NavptName => "Waypoint";

        public Type NavptEditorType => typeof(EditNavpointPage);

        // TODO: validate maximum navpoint count
        public int NavptMaxCount => int.MaxValue;

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // TODO: document
        public void SetupUserInterface(IConfiguration config, ListView listView)
        {
        }

        public void CopyConfigToEdit(IConfiguration config, ObservableCollection<INavpointInfo> edit)
        {
            FA18CConfiguration a10cConfig = (FA18CConfiguration)config;
            edit.Clear();
            foreach (WaypointInfo wypt in a10cConfig.WYPT.Points)
            {
                edit.Add(new WaypointInfo(wypt));
            }
        }

        public bool CopyEditToConfig(ObservableCollection<INavpointInfo> edit, IConfiguration config)
        {
            FA18CConfiguration a10cConfig = (FA18CConfiguration)config;
            a10cConfig.WYPT.Points.Clear();
            foreach (WaypointInfo wypt in edit.Cast<WaypointInfo>())
            {
                a10cConfig.WYPT.Points.Add(new WaypointInfo(wypt));
            }
            return true;
        }

        public void ResetSystem(IConfiguration config)
        {
            ((FA18CConfiguration)config).WYPT.Reset();
        }

        public void AddNavpoint(IConfiguration config)
        {
            ((FA18CConfiguration)config).WYPT.Add();
        }

        public bool PasteNavpoints(IConfiguration config, string cbData, bool isReplace = false)
        {
            return ((FA18CConfiguration)config).WYPT.DeserializeNavpoints(cbData, isReplace);
        }

        public void ImportNavpoints(IConfiguration config, List<Dictionary<string, string>> importNavpts, bool isReplace)
        {
            if (isReplace)
            {
                ((FA18CConfiguration)config).WYPT.Points.Clear();
            }
            foreach (Dictionary<string, string> importStpt in importNavpts)
            {
                WaypointInfo wypt = new()
                {
                    Name = (importStpt.ContainsKey("name")) ? importStpt["name"] : "",
                    Lat = (importStpt.ContainsKey("lat")) ? importStpt["lat"] : "",
                    Lon = (importStpt.ContainsKey("lon")) ? importStpt["lon"] : "",
                    Alt = (importStpt.ContainsKey("alt")) ? importStpt["alt"] : ""
                };
                ((FA18CConfiguration)config).WYPT.Add();
            }
        }

        public string ExportNavpoints(IConfiguration config)
        {
            return ((FA18CConfiguration)config).WYPT.SerializeNavpoints();
        }

        public void CaptureNavpoints(IConfiguration config, WyptCaptureData[] wypts, int startIndex)
        {
            // TODO: implement target points
            WYPTSystem wyptSys = ((FA18CConfiguration)config).WYPT;
            for (int i = 0; i < wypts.Length; i++)
            {
                if (!wypts[i].IsTarget && (startIndex < wyptSys.Count))
                {
                    wyptSys.Points[startIndex].Name = $"WP{i + 1} DCS Capture";
                    wyptSys.Points[startIndex].Lat = wypts[i].Latitude;
                    wyptSys.Points[startIndex].Lon = wypts[i].Longitude;
                    wyptSys.Points[startIndex].Alt = wypts[i].Elevation;
                    startIndex++;
                }
                else if (!wypts[i].IsTarget)
                {
                    WaypointInfo wypt = new()
                    {
                        Name = $"WP{i + 1} DCS Capture",
                        Lat = wypts[i].Latitude,
                        Lon = wypts[i].Longitude,
                        Alt = wypts[i].Elevation
                    };
                    wyptSys.Add(wypt);
                    startIndex++;
                }
            }
        }

        public object NavptEditorArg(Page parentEditor, IConfiguration config, int indexNavpt)
        {
            bool isUnlinked = string.IsNullOrEmpty(config.SystemLinkedTo(SystemTag));
            return new EditNavptPageNavArgs(parentEditor, config, indexNavpt, isUnlinked, typeof(FA18CEditWaypointHelper));
        }
    }
}
