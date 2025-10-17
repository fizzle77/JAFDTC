// ********************************************************************************************************************
//
// FA18CEditWaypointListHelper.cs : IEditNavpointListPageHelper for the fa-18c configuration
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
using JAFDTC.Models.DCS;
using JAFDTC.Models.FA18C;
using JAFDTC.Models.FA18C.WYPT;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.FA18C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    internal class FA18CEditWaypointListHelper : EditWaypointListHelperBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(WYPTSystem.SystemTag, "Waypoints", "WYPT", Glyphs.WYPT,
                   typeof(EditNavpointListPage), typeof(FA18CEditWaypointListHelper));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public override string SystemTag => WYPTSystem.SystemTag;

        public override string NavptListTag => WYPTSystem.WYPTListTag;

        public override AirframeTypes AirframeType => AirframeTypes.FA18C;

        public override LLFormat NavptCoordFmt => LLFormat.DDM_P2ZF;

        // TODO: validate maximum navpoint count
        public override int NavptMaxCount => int.MaxValue;

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override int NavptCurrentCount(IConfiguration config) => ((FA18CConfiguration)config).WYPT.Count;

        public override void CopyConfigToEdit(IConfiguration config, ObservableCollection<INavpointInfo> edit)
        {
            FA18CConfiguration hornetConfig = (FA18CConfiguration)config;
            edit.Clear();
            foreach (WaypointInfo wypt in hornetConfig.WYPT.Points)
            {
                edit.Add(new WaypointInfo(wypt));
            }
        }

        public override bool CopyEditToConfig(ObservableCollection<INavpointInfo> edit, IConfiguration config)
        {
            FA18CConfiguration hornetConfig = (FA18CConfiguration)config;
            hornetConfig.WYPT.Points.Clear();
            foreach (WaypointInfo wypt in edit.Cast<WaypointInfo>())
            {
                hornetConfig.WYPT.Points.Add(new WaypointInfo(wypt));
            }
            return true;
        }

        public override void AppendFromPOIsToConfig(IEnumerable<PointOfInterest> pois, IConfiguration config)
        {
            FA18CConfiguration hornetConfig = (FA18CConfiguration)config;
            ObservableCollection<WaypointInfo> points = hornetConfig.WYPT.Points;
            int startNumber = (points.Count == 0) ? 1 : points[^1].Number + 1;
            foreach (Models.DCS.PointOfInterest poi in pois)
            {
                WaypointInfo wypt = new()
                {
                    Number = startNumber++,
                    Name = poi.Name,
                    Lat = poi.Latitude,
                    Lon = poi.Longitude,
                    Alt = poi.Elevation
                };
                hornetConfig.WYPT.Points.Add(new WaypointInfo(wypt));
            }
        }

        public override INavpointSystemImport NavptSystem(IConfiguration config)
        {
            return ((FA18CConfiguration)config).WYPT;
        }

        public override void ResetSystem(IConfiguration config)
        {
            ((FA18CConfiguration)config).WYPT.Reset();
        }

        public override int AddNavpoint(IConfiguration config, int atIndex = -1)
        {
            WaypointInfo wypt = ((FA18CConfiguration)config).WYPT.Add(null, atIndex);
            return ((FA18CConfiguration)config).WYPT.Points.IndexOf(wypt);
        }

        public override bool PasteNavpoints(IConfiguration config, string cbData, bool isReplace = false)
        {
            return ((FA18CConfiguration)config).WYPT.ImportSerializedNavpoints(cbData, isReplace);
        }

        public override string ExportNavpoints(IConfiguration config)
        {
            return ((FA18CConfiguration)config).WYPT.SerializeNavpoints();
        }

        public override void CaptureNavpoints(IConfiguration config, WyptCaptureData[] wypts, int startIndex)
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

        public override object NavptEditorArg(Page parentEditor, IMapControlVerbMirror verbMirror,
                                              IConfiguration config, int indexNavpt)
        {
            bool isUnlinked = string.IsNullOrEmpty(config.SystemLinkedTo(SystemTag));
            return new EditNavptPageNavArgs(parentEditor, verbMirror, config, indexNavpt, isUnlinked,
                                            typeof(FA18CEditWaypointHelper));
        }
    }
}
