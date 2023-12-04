// ********************************************************************************************************************
//
// A10CEditWaypointHelper.cs : IEditNavpointPageHelper for the a10c configuration
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

using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.WYPT;
using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using JAFDTC.UI.Base;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// TODO: docuemnt
    /// </summary>
    internal class A10CEditWaypointHelper : IEditNavpointPageHelper
    {
        public string SystemTag => WYPTSystem.SystemTag;

        public string NavptName => "Waypoint";

        public Dictionary<string, string> LatExtProperties
            => new()
            {
                ["MaskPlaceholder"] = "–",
                ["Regex"] = "^[nNsS] [\\d]{2}° [\\d]{2}’ [\\d]{2}’’$",
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
            A10CConfiguration a10cConfig = (A10CConfiguration)config;
            WaypointInfo a10cWypt = (WaypointInfo)edit;
            a10cWypt.Number = a10cConfig.WYPT.Points[index].Number;
            a10cWypt.Name = new(a10cConfig.WYPT.Points[index].Name);
            a10cWypt.LatUI = NavpointInfoBase.ConvertLatDDtoDDM(new(a10cConfig.WYPT.Points[index].Lat));
            a10cWypt.LonUI = NavpointInfoBase.ConvertLonDDtoDDM(new(a10cConfig.WYPT.Points[index].Lon));
            a10cWypt.Alt = new(a10cConfig.WYPT.Points[index].Alt);
        }

        public bool CopyEditToConfig(int index, INavpointInfo edit, IConfiguration config)
        {
            A10CConfiguration a10cConfig = (A10CConfiguration)config;
            WaypointInfo a10cWypt = (WaypointInfo)edit;
            if (!a10cWypt.HasErrors)
            {
                a10cConfig.WYPT.Points[index].Number = a10cWypt.Number;
                a10cConfig.WYPT.Points[index].Name = a10cWypt.Name;
                a10cConfig.WYPT.Points[index].Lat = a10cWypt.Lat;
                a10cConfig.WYPT.Points[index].Lon = a10cWypt.Lon;
                a10cConfig.WYPT.Points[index].Alt = a10cWypt.Alt;
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
            return ((A10CConfiguration)config).WYPT.Points.Count;
        }

        public void ApplyPoI(INavpointInfo edit, PointOfInterest poi)
        {
            if (poi != null)
            {
                ((WaypointInfo)edit).Name = poi.Name;
                ((WaypointInfo)edit).LatUI = NavpointInfoBase.ConvertLatDDtoDDM(poi.Latitude);
                ((WaypointInfo)edit).LonUI = NavpointInfoBase.ConvertLonDDtoDDM(poi.Longitude);
                ((WaypointInfo)edit).Alt = poi.Elevation.ToString();
                ((WaypointInfo)edit).ClearErrors();
            }
        }

        public int AddNavpoint(IConfiguration config)
        {
            WaypointInfo wypt = ((A10CConfiguration)config).WYPT.Add();
            return ((A10CConfiguration)config).WYPT.Points.IndexOf(wypt);
        }
    }
}
