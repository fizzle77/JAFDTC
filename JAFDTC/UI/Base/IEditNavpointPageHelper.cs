// ********************************************************************************************************************
//
// IEditNavpointHelper.cs : interface for EditNavPointPage helper classes
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
using JAFDTC.Models.Base;
using JAFDTC.Models.DCS;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// interface for the EditNavpointPage ui page helper class responsible for specializing the EditNavpointPage
    /// base behavior for a specific airframe.
    /// </summary>
    public interface IEditNavpointPageHelper
    {
        /// <summary>
        /// return the system tag for the navpoint system.
        /// </summary>
        public string SystemTag { get; }

        /// <summary>
        /// return the name to refer to a navpoint (waypoint, steerpoint, etc.) by in the user interface. the string
        /// should be singular and capitalized.
        /// </summary>
        public string NavptName { get; }

        /// <summary>
        /// return the coordinate format used by navpoints in the navigation system.
        /// </summary>
        public LLFormat NavptCoordFmt { get; }

        /// <summary>
        /// Returns the maximum number of characters the jet will allow for a navpoint name.
        /// If zero, the UI will not indicate a limit.
        /// </summary>
        public int MaxNameLength { get; }

        /// <summary>
        /// returns a dictionary with the TextBoxExtensions to apply to the latitude field. the keys are property
        /// names for the extensions ("MaskPlaceholder", "Regex", "CustomMask", and "Mask"), values are the strings
        /// to set these to. null or an empty dictionary indicates the DMS coordinate defaults are used.
        /// </summary>
        public Dictionary<string, string> LatExtProperties { get; }

        /// <summary>
        /// returns a dictionary with the TextBoxExtensions to apply to the longitude field. the keys are property
        /// names for the extensions ("MaskPlaceholder", "Regex", "CustomMask", and "Mask"), values are the strings
        /// to set these to. null or an empty dictionary indicates the DMS coordinate defaults are used.
        /// </summary>
        public Dictionary<string, string> LonExtProperties { get; }

        /// <summary>
        /// return a new navpoint instance for use as the data structure the user interface will edit (this object is
        /// marshalled to/from the configuration by CopyConfigToEdit/CopyEditToConfig.
        /// </summary>
        public NavpointInfoBase CreateEditNavpt(PropertyChangedEventHandler propChanged,
                                                EventHandler<DataErrorsChangedEventArgs> dataErr);

        /// <summary>
        /// update the edit navpoint with the indicated index from the configuration. the update will perform a deep
        /// copy of the navpoint from the configuration.
        /// </summary>
        public void CopyConfigToEdit(int indexNavpt, IConfiguration config, INavpointInfo edit);

        /// <summary>
        /// update the configuration navpoint at the indicaited index from the edit navpoint. the update will perform
        /// a deep copy of the navpoint from the configuration.
        /// </summary>
        public bool CopyEditToConfig(int indexNavpt, INavpointInfo edit, IConfiguration config);

        /// <summary>
        // returns true if the edit navpoint has errors; false otherwise.
        /// <summary>
        public bool HasErrors(INavpointInfo edit);

        /// <summary>
        /// returns a list of properties that currently have erorrs in the edit navpoint. the check may be lmited to
        /// a specific property (propertyName != null) or may apply to all properties (propertyName == null). if there
        /// are no errors, the returned list will be empty.
        /// </summary>
        public List<string> GetErrors(INavpointInfo edit, string propertyName);

        /// <summary>
        // returns the count of navpoints in the configuration.
        /// </summary>
        public int NavpointCount(IConfiguration config);

        /// <summary>
        /// apply the poi to the edit navpoint by copying parameters (lat, lon, etc.) from the poi to the edit
        /// navpoint. the edit navpoint is unchanged if the poi is null.
        /// </summary>
        public void ApplyPoI(INavpointInfo edit, PointOfInterest poi);

        /// <summary>
        /// apply the coordinates captured from dcs to the edit navpoint by copying parameters (lat, lon, etc.) from
        /// the capture to the edit navpoint. the edit navpoint is unchanged if the capture is null.
        /// </summary>
        public void ApplyCapture(INavpointInfo edit, WyptCaptureData wypt);

        /// <summary>
        /// add a navigation point to the navpoint list in the configuration at the indicated position (default is
        /// end of list). this updates (but does not save) the configuration. returns index of added navpoint.
        /// </summary>
        public int AddNavpoint(IConfiguration config, int atIndex = -1);
    }
}
