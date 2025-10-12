// ********************************************************************************************************************
//
// AV8BEditWaypointListHelper.cs : IEditNavpointListPageHelper for the av8b configuration
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
using JAFDTC.Models.AV8B;
using JAFDTC.Models.AV8B.WYPT;
using JAFDTC.Models.Base;
using JAFDTC.Models.F14AB;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.AV8B
{
    /// <summary>
    /// TODO: docuemnt
    /// </summary>
    internal class AV8BEditWaypointListHelper : EditWaypointListHelperBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(WYPTSystem.SystemTag, "Waypoints", "WYPT", Glyphs.WYPT,
                   typeof(AV8BEditWaypointListPage), typeof(AV8BEditWaypointListHelper));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public override string SystemTag => WYPTSystem.SystemTag;

        public override string NavptListTag => WYPTSystem.WYPTListTag;

        public override AirframeTypes AirframeType => AirframeTypes.AV8B;

        // TODO: validate maximum navpoint count
        public override int NavptMaxCount => int.MaxValue;

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override int NavptCurrentCount(IConfiguration config) => ((AV8BConfiguration)config).WYPT.Count;


        public override void CopyConfigToEdit(IConfiguration config, ObservableCollection<INavpointInfo> edit)
        {
            AV8BConfiguration av8bConfig = (AV8BConfiguration)config;
            edit.Clear();
            foreach (WaypointInfo wypt in av8bConfig.WYPT.Points)
            {
                edit.Add(new WaypointInfo(wypt));
            }
        }

        public override bool CopyEditToConfig(ObservableCollection<INavpointInfo> edit, IConfiguration config)
        {
            AV8BConfiguration av8bConfig = (AV8BConfiguration)config;
            av8bConfig.WYPT.Points.Clear();
            foreach (WaypointInfo wypt in edit.Cast<WaypointInfo>())
            {
                av8bConfig.WYPT.Points.Add(new WaypointInfo(wypt));
            }
            return true;
        }

        public override void AppendFromPOIsToConfig(IEnumerable<Models.DCS.PointOfInterest> pois, IConfiguration config)
        {
            AV8BConfiguration av8bConfig = (AV8BConfiguration)config;
            ObservableCollection<WaypointInfo> points = av8bConfig.WYPT.Points;
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
                av8bConfig.WYPT.Points.Add(new WaypointInfo(wypt));
            }
        }

        public override INavpointSystemImport NavptSystem(IConfiguration config)
        {
            return ((AV8BConfiguration)config).WYPT;
        }

        public override void ResetSystem(IConfiguration config)
        {
            ((AV8BConfiguration)config).WYPT.Reset();
        }

        public override void AddNavpoint(IConfiguration config)
        {
            ((AV8BConfiguration)config).WYPT.Add();
        }

        public override bool PasteNavpoints(IConfiguration config, string cbData, bool isReplace = false)
        {
            return ((AV8BConfiguration)config).WYPT.ImportSerializedNavpoints(cbData, isReplace);
        }

        public override string ExportNavpoints(IConfiguration config)
        {
            return ((AV8BConfiguration)config).WYPT.SerializeNavpoints();
        }

        public override void CaptureNavpoints(IConfiguration config, WyptCaptureData[] wypts, int startIndex)
        {
            WYPTSystem wyptSys = ((AV8BConfiguration)config).WYPT;
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
            return new EditNavptPageNavArgs(parentEditor, config, indexNavpt, isUnlinked, typeof(AV8BEditWaypointHelper));
        }
    }
}
