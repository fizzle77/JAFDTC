// ********************************************************************************************************************
//
// F14ABEditWaypointListHelper.cs : IEditNavpointListPageHelper for the f-14a/b configuration
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

using JAFDTC.Models;
using JAFDTC.Models.A10C;
using JAFDTC.Models.Base;
using JAFDTC.Models.F14AB;
using JAFDTC.Models.F14AB.WYPT;
using JAFDTC.Models.FA18C;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.F14AB
{
    /// <summary>
    /// helper class for EditNavpointListPage that implements IEditNavpointListPageHelper. this handles the
    /// specialization of the general navpoint list page for the f-14a/b airframe.
    /// </summary>
    internal class F14ABEditWaypointListHelper : EditWaypointListHelperBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(WYPTSystem.SystemTag, "Waypoints", "WYPT", Glyphs.WYPT,
                   typeof(EditNavpointListPage), typeof(F14ABEditWaypointListHelper));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public override string SystemTag => WYPTSystem.SystemTag;

        public override string NavptListTag => WYPTSystem.WYPTListTag;

        public override AirframeTypes AirframeType => AirframeTypes.F14AB;

        public override int NavptMaxCount => 3;


        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override int NavptCurrentCount(IConfiguration config) => ((F14ABConfiguration)config).WYPT.Count;

        public override void CopyConfigToEdit(IConfiguration config, ObservableCollection<INavpointInfo> edit)
        {
            F14ABConfiguration tomcatConfig = (F14ABConfiguration)config;
            edit.Clear();
            foreach (WaypointInfo wypt in tomcatConfig.WYPT.Points)
            {
                edit.Add(new WaypointInfo(wypt));
            }
        }

        public override bool CopyEditToConfig(ObservableCollection<INavpointInfo> edit, IConfiguration config)
        {
            F14ABConfiguration tomcatConfig = (F14ABConfiguration)config;
            tomcatConfig.WYPT.Points.Clear();
            foreach (WaypointInfo wypt in edit.Cast<WaypointInfo>())
            {
                tomcatConfig.WYPT.Points.Add(new WaypointInfo(wypt));
            }
            return true;
        }

        public override void AppendFromPOIsToConfig(IEnumerable<Models.DCS.PointOfInterest> pois, IConfiguration config)
        {
            F14ABConfiguration tomcatConfig = (F14ABConfiguration)config;
            ObservableCollection<WaypointInfo> points = tomcatConfig.WYPT.Points;
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
                tomcatConfig.WYPT.Points.Add(new WaypointInfo(wypt));
            }
        }

        public override INavpointSystemImport NavptSystem(IConfiguration config)
        {
            return ((F14ABConfiguration)config).WYPT;
        }

        public override void ResetSystem(IConfiguration config)
        {
            ((F14ABConfiguration)config).WYPT.Reset();
        }

        public override void AddNavpoint(IConfiguration config)
        {
            ((F14ABConfiguration)config).WYPT.Add();
        }

        public override bool PasteNavpoints(IConfiguration config, string cbData, bool isReplace = false)
        {
            return ((F14ABConfiguration)config).WYPT.ImportSerializedNavpoints(cbData, isReplace);
        }

        public override string ExportNavpoints(IConfiguration config)
        {
            return ((F14ABConfiguration)config).WYPT.SerializeNavpoints();
        }

        public override void CaptureNavpoints(IConfiguration config, WyptCaptureData[] wypts, int startIndex)
        {
            WYPTSystem wyptSys = ((F14ABConfiguration)config).WYPT;
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

        public override object NavptEditorArg(Page parentEditor, IConfiguration config, int indexNavpt)
        {
            bool isUnlinked = string.IsNullOrEmpty(config.SystemLinkedTo(SystemTag));
            return new EditNavptPageNavArgs(parentEditor, config, indexNavpt, isUnlinked, typeof(F14ABEditWaypointHelper));
        }
    }
}
