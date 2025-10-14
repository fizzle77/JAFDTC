// ********************************************************************************************************************
//
// A10CEditWaypointListHelper.cs : IEditNavpointListPageHelper for the a-10c configuration
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
using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.WYPT;
using JAFDTC.Models.Base;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// TODO: docuemnt
    /// </summary>
    internal class A10CEditWaypointListHelper : IEditNavpointListPageHelper
    {
        public static ConfigEditorPageInfo PageInfo
            => new(WYPTSystem.SystemTag, "Waypoints", "WYPT", Glyphs.WYPT,
                   typeof(EditNavpointListPage), typeof(A10CEditWaypointListHelper));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public string SystemTag => WYPTSystem.SystemTag;

        public string NavptListTag => WYPTSystem.WYPTListTag;

        public AirframeTypes AirframeType => AirframeTypes.A10C;

        public string NavptName => "Waypoint";

        public LLFormat NavptCoordFmt => LLFormat.DDM_P3ZF;

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
            A10CConfiguration a10cConfig = (A10CConfiguration)config;
            edit.Clear();
            foreach (WaypointInfo wypt in a10cConfig.WYPT.Points)
            {
                edit.Add(new WaypointInfo(wypt));
            }
        }

        public bool CopyEditToConfig(ObservableCollection<INavpointInfo> edit, IConfiguration config)
        {
            A10CConfiguration a10cConfig = (A10CConfiguration)config;
            a10cConfig.WYPT.Points.Clear();
            foreach (WaypointInfo wypt in edit.Cast<WaypointInfo>())
            {
                a10cConfig.WYPT.Points.Add(new WaypointInfo(wypt));
            }
            return true;
        }

        public INavpointSystemImport NavptSystem(IConfiguration config)
        {
            return ((A10CConfiguration)config).WYPT;
        }

        public void ResetSystem(IConfiguration config)
        {
            ((A10CConfiguration)config).WYPT.Reset();
        }

        public void AddNavpoint(IConfiguration config)
        {
            ((A10CConfiguration)config).WYPT.Add();
        }

        public bool PasteNavpoints(IConfiguration config, string cbData, bool isReplace = false)
        {
            return ((A10CConfiguration)config).WYPT.ImportSerializedNavpoints(cbData, isReplace);
        }

        public string ExportNavpoints(IConfiguration config)
        {
            return ((A10CConfiguration)config).WYPT.SerializeNavpoints();
        }

        public void CaptureNavpoints(IConfiguration config, WyptCaptureData[] wypts, int startIndex)
        {
            WYPTSystem wyptSys = ((A10CConfiguration)config).WYPT;
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
            return new EditNavptPageNavArgs(parentEditor, config, indexNavpt, isUnlinked, typeof(A10CEditWaypointHelper));
        }
    }
}
