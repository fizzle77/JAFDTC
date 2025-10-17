// ********************************************************************************************************************
//
// AV8BEditWaypointHelper.cs : IEditNavpointPageHelper for the av8b configuration
//
// Copyright(C) 2023- ilominar/raven
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
using JAFDTC.Models.DCS;
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.AV8B
{
    /// <summary>
    /// TODO: docuemnt
    /// </summary>
    internal class AV8BEditWaypointHelper : IEditNavpointPageHelper
    {
        public string SystemTag => WYPTSystem.SystemTag;

        public string NavptName => "Waypoint";

        public LLFormat NavptCoordFmt => LLFormat.DMS;

        public int MaxNameLength => 0;

        public Dictionary<string, string> LatExtProperties => null;

        public Dictionary<string, string> LonExtProperties => null;

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
            WaypointInfo wyptSrc = ((AV8BConfiguration)config).WYPT.Points[index];
            WaypointInfo wyptDst = (WaypointInfo)edit;
            wyptDst.Number = wyptSrc.Number;
            wyptDst.Name = new(wyptSrc.Name);
            wyptDst.LatUI = Coord.ConvertFromLatDD(wyptSrc.Lat, LLFormat.DMS);
            wyptDst.LonUI = Coord.ConvertFromLonDD(wyptSrc.Lon, LLFormat.DMS);
            wyptDst.Alt = new(wyptSrc.Alt);
        }

        public bool CopyEditToConfig(int index,INavpointInfo edit, IConfiguration config)
        {
            WaypointInfo wyptSrc = (WaypointInfo)edit;
            WaypointInfo wyptDst = ((AV8BConfiguration)config).WYPT.Points[index];
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
            return [.. ((WaypointInfo)edit).GetErrors(propertyName).Cast<string>() ];
        }

        public int NavpointCount(IConfiguration config)
        {
            return ((AV8BConfiguration)config).WYPT.Points.Count;
        }

        public void ApplyPoI(INavpointInfo edit, PointOfInterest poi)
        {
            if (poi != null)
            {
                WaypointInfo wyptDst = (WaypointInfo)edit;
                wyptDst.Name = poi.Name;
                wyptDst.LatUI = Coord.ConvertFromLatDD(poi.Latitude, LLFormat.DMS);
                wyptDst.LonUI = Coord.ConvertFromLonDD(poi.Longitude, LLFormat.DMS);
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
                wyptDst.LatUI = Coord.ConvertFromLatDD(wypt.Latitude, LLFormat.DMS);
                wyptDst.LonUI = Coord.ConvertFromLonDD(wypt.Longitude, LLFormat.DMS);
                wyptDst.Alt = wypt.Elevation.ToString();
                wyptDst.ClearErrors();
            }
        }

        public int AddNavpoint(IConfiguration config, int atIndex = -1)
        {
            WaypointInfo wypt = ((AV8BConfiguration)config).WYPT.Add(null, atIndex);
            return ((AV8BConfiguration)config).WYPT.Points.IndexOf(wypt);
        }
    }
}
