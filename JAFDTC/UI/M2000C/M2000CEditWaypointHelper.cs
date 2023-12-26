// ********************************************************************************************************************
//
// M2000CEditWaypointHelper.cs : IEditNavpointPageHelper for the m-2000c configuration
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
using JAFDTC.Models.DCS;
using JAFDTC.Models.M2000C;
using JAFDTC.Models.M2000C.WYPT;
using JAFDTC.Models;
using JAFDTC.UI.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.M2000C
{
    /// <summary>
    /// helper class for EditNavpointPage that implements IEditNavpointPageHelper. this handles the specialization
    /// of the general navpoint page for the m-2000c airframe.
    /// </summary>
    internal class M2000CEditWaypointHelper : IEditNavpointPageHelper
    {
        public string SystemTag => WYPTSystem.SystemTag;

        public string NavptName => "Waypoint";

        public Dictionary<string, string> LatExtProperties
            => new()
            {
                ["MaskPlaceholder"] = "–",
                ["Regex"] = "^[nNsS] [\\d]{2}° [\\d]{2}\\.[\\d]{3}’$",
                ["CustomMask"] = "N:[nNsS]",
                ["Mask"] = "N 99° 99.999’",
            };

        public Dictionary<string, string> LonExtProperties
            => new()
            {
                ["MaskPlaceholder"] = "–",
                ["Regex"] = "^[eEwW] [\\d]{3}° [\\d]{2}\\.[\\d]{3}’$",
                ["CustomMask"] = "E:[eEwW]",
                ["Mask"] = "E 999° 99.999’",
            };

        public NavpointInfoBase CreateEditNavpt(PropertyChangedEventHandler propChanged,
                                                EventHandler<DataErrorsChangedEventArgs> dataErr)
        {
            WaypointInfo wypt = new();
            wypt.PropertyChanged += propChanged;
            wypt.ErrorsChanged += dataErr;
            return wypt;
        }

        public void CopyConfigToEdit(int index, IConfiguration config, INavpointInfo edit)
        {
            WaypointInfo wyptSrc = ((M2000CConfiguration)config).WYPT.Points[index];
            WaypointInfo wyptDst = (WaypointInfo)edit;
            wyptDst.Number = wyptSrc.Number;
            wyptDst.Name = new(wyptSrc.Name);
            wyptDst.LatUI = NavpointInfoBase.ConvertFromLatDD(wyptSrc.Lat, NavpointInfoBase.LLFormat.DDM_P3ZF);
            wyptDst.LonUI = NavpointInfoBase.ConvertFromLonDD(wyptSrc.Lon, NavpointInfoBase.LLFormat.DDM_P3ZF);
            wyptDst.Alt = new(wyptSrc.Alt);
        }

        public bool CopyEditToConfig(int index, INavpointInfo edit, IConfiguration config)
        {
            WaypointInfo wyptDst = ((M2000CConfiguration)config).WYPT.Points[index];
            WaypointInfo wyptSrc = (WaypointInfo)edit;
            if (!wyptSrc.HasErrors)
            {
                wyptDst.Number = wyptSrc.Number;
                wyptDst.Name = wyptSrc.Name;
                wyptDst.Lat = wyptSrc.Lat;
                wyptDst.Lon = wyptSrc.Lon;
                wyptDst.Alt = wyptSrc.Alt;
                return true;
            }
            return false;
        }

        public bool HasErrors(INavpointInfo edit)
        {
            return ((WaypointInfo)edit).HasErrors;
        }

        public List<string> GetErrors(INavpointInfo edit, string propertyName)
        {
            return ((WaypointInfo)edit).GetErrors(propertyName).Cast<string>().ToList();
        }

        public int NavpointCount(IConfiguration config)
        {
            return ((M2000CConfiguration)config).WYPT.Points.Count;
        }

        public void ApplyPoI(INavpointInfo edit, PointOfInterest poi)
        {
            if (poi != null)
            {
                WaypointInfo wyptDst = (WaypointInfo)edit;
                wyptDst.Name = poi.Name;
                wyptDst.LatUI = NavpointInfoBase.ConvertFromLatDD(poi.Latitude, NavpointInfoBase.LLFormat.DDM_P3ZF);
                wyptDst.LonUI = NavpointInfoBase.ConvertFromLonDD(poi.Longitude, NavpointInfoBase.LLFormat.DDM_P3ZF);
                wyptDst.Alt = poi.Elevation;
                wyptDst.ClearErrors();
            }
        }

        public void ApplyCapture(INavpointInfo edit, WyptCaptureData wypt)
        {
            if (wypt != null)
            {
                WaypointInfo wyptDst = (WaypointInfo)edit;
                wyptDst.Name = "DCS Capture";
                wyptDst.LatUI = NavpointInfoBase.ConvertFromLatDD(wypt.Latitude, NavpointInfoBase.LLFormat.DDM_P3ZF);
                wyptDst.LonUI = NavpointInfoBase.ConvertFromLonDD(wypt.Longitude, NavpointInfoBase.LLFormat.DDM_P3ZF);
                wyptDst.Alt = wypt.Elevation.ToString();
                wyptDst.ClearErrors();
            }
        }

        public int AddNavpoint(IConfiguration config)
        {
            WaypointInfo wypt = ((M2000CConfiguration)config).WYPT.Add();
            return ((M2000CConfiguration)config).WYPT.Points.IndexOf(wypt);
        }
    }
}
