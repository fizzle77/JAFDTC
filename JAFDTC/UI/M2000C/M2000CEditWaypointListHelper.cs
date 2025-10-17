// ********************************************************************************************************************
//
// M2000CEditWaypointListHelper.cs : IEditNavpointListPageHelper for the f-14a/b configuration
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

using JAFDTC.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.F14AB;
using JAFDTC.Models.M2000C;
using JAFDTC.Models.M2000C.WYPT;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.M2000C
{
    /// <summary>
    /// helper class for EditNavpointListPage that implements IEditNavpointListPageHelper. this handles the
    /// specialization of the generate navpoint list page for the m-2000c airframe.
    /// </summary>
    internal class M2000CEditWaypointListHelper : IEditNavpointListPageHelper
    {
        public static ConfigEditorPageInfo PageInfo
            => new(WYPTSystem.SystemTag, "Waypoints", "WYPT", Glyphs.WYPT,
                   typeof(EditNavpointListPage), typeof(M2000CEditWaypointListHelper));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public string SystemTag => WYPTSystem.SystemTag;

        public string NavptListTag => WYPTSystem.WYPTListTag;

        public AirframeTypes AirframeType => AirframeTypes.M2000C;

        public string NavptName => "Waypoint";

        public LLFormat NavptCoordFmt => LLFormat.DDM_P3ZF;

        public Type NavptEditorType => typeof(EditNavpointPage);

        public int NavptMaxCount => 10;

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public void SetupUserInterface(IConfiguration config, ListView listView) { }

        public void CopyConfigToEdit(IConfiguration config, ObservableCollection<INavpointInfo> edit)
        {
            M2000CConfiguration a10cConfig = (M2000CConfiguration)config;
            edit.Clear();
            foreach (WaypointInfo wypt in a10cConfig.WYPT.Points)
            {
                edit.Add(new WaypointInfo(wypt));
            }
        }

        public bool CopyEditToConfig(ObservableCollection<INavpointInfo> edit, IConfiguration config)
        {
            M2000CConfiguration a10cConfig = (M2000CConfiguration)config;
            a10cConfig.WYPT.Points.Clear();
            foreach (WaypointInfo wypt in edit.Cast<WaypointInfo>())
            {
                a10cConfig.WYPT.Points.Add(new WaypointInfo(wypt));
            }
            return true;
        }

        public INavpointSystemImport NavptSystem(IConfiguration config)
        {
            return ((M2000CConfiguration)config).WYPT;
        }

        public void ResetSystem(IConfiguration config)
        {
            ((M2000CConfiguration)config).WYPT.Reset();
        }

        public int AddNavpoint(IConfiguration config, int atIndex = -1)
        {
            WaypointInfo wypt = ((M2000CConfiguration)config).WYPT.Add(null, atIndex);
            return ((M2000CConfiguration)config).WYPT.Points.IndexOf(wypt);
        }

        public bool PasteNavpoints(IConfiguration config, string cbData, bool isReplace = false)
        {
            return ((M2000CConfiguration)config).WYPT.ImportSerializedNavpoints(cbData, isReplace);
        }

        public string ExportNavpoints(IConfiguration config)
        {
            return ((M2000CConfiguration)config).WYPT.SerializeNavpoints();
        }

        public void CaptureNavpoints(IConfiguration config, WyptCaptureData[] wypts, int startIndex)
        {
            WYPTSystem wyptSys = ((M2000CConfiguration)config).WYPT;
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

        public object NavptEditorArg(Page parentEditor, IMapControlVerbMirror verbMirror, IConfiguration config,
                                     int indexNavpt)
        {
            bool isUnlinked = string.IsNullOrEmpty(config.SystemLinkedTo(SystemTag));
            return new EditNavptPageNavArgs(parentEditor, verbMirror, config, indexNavpt, isUnlinked,
                                            typeof(M2000CEditWaypointHelper));
        }
    }
}
