// ********************************************************************************************************************
//
// AV8BEditWaypointHelper.cs : IEditNavpointPageHelper for the av8b configuration
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

using JAFDTC.Models;
using JAFDTC.Models.AV8B;
using JAFDTC.Models.AV8B.WYPT;
using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.UI.Base;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace JAFDTC.UI.AV8B
{
    /// <summary>
    /// TODO: docuemnt
    /// </summary>
    internal class AV8BEditWaypointHelper : IEditNavpointPageHelper
    {
        public string SystemTag => WYPTSystem.SystemTag;

        public string NavptName => "Waypoint";

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
            AV8BConfiguration av8bConfig = (AV8BConfiguration)config;
            WaypointInfo av8bWypt = (WaypointInfo)edit;
            av8bWypt.Number = av8bConfig.WYPT.Points[index].Number;
            av8bWypt.Name = new(av8bConfig.WYPT.Points[index].Name);
            av8bWypt.LatUI = NavpointInfoBase.ConvertLatDDtoDMS(new(av8bConfig.WYPT.Points[index].Lat));
            av8bWypt.LonUI = NavpointInfoBase.ConvertLonDDtoDMS(new(av8bConfig.WYPT.Points[index].Lon));
            av8bWypt.Alt = new(av8bConfig.WYPT.Points[index].Alt);
        }

        public bool CopyEditToConfig(int index,INavpointInfo edit, IConfiguration config)
        {
            AV8BConfiguration av8bConfig = (AV8BConfiguration)config;
            WaypointInfo av8bWypt = (WaypointInfo)edit;
            if (!av8bWypt.HasErrors)
            {
                av8bConfig.WYPT.Points[index].Number = av8bWypt.Number;
                av8bConfig.WYPT.Points[index].Name = av8bWypt.Name;
                av8bConfig.WYPT.Points[index].Lat = av8bWypt.Lat;
                av8bConfig.WYPT.Points[index].Lon = av8bWypt.Lon;
                av8bConfig.WYPT.Points[index].Alt = av8bWypt.Alt;
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
            return ((AV8BConfiguration)config).WYPT.Points.Count;
        }

        public void ApplyPoI(INavpointInfo edit, PointOfInterest poi)
        {
            if (poi != null)
            {
                ((WaypointInfo)edit).Name = poi.Name;
                ((WaypointInfo)edit).LatUI = NavpointInfoBase.ConvertLatDDtoDMS(poi.Latitude);
                ((WaypointInfo)edit).LonUI = NavpointInfoBase.ConvertLonDDtoDMS(poi.Longitude);
                ((WaypointInfo)edit).Alt = poi.Elevation.ToString();
                ((WaypointInfo)edit).ClearErrors();
            }
        }

        public int AddNavpoint(IConfiguration config)
        {
            WaypointInfo wypt = ((AV8BConfiguration)config).WYPT.Add();
            return ((AV8BConfiguration)config).WYPT.Points.IndexOf(wypt);
        }
    }
}
