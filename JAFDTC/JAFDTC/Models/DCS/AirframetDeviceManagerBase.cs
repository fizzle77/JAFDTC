// ********************************************************************************************************************
//
// AirframeDeviceManagerBase.cs -- abstract base class for an airframe device manager
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

using System.Collections.Generic;
using System.Diagnostics;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// abstract base class for an airframe device manager. instances of this class manage Device objects that are
    /// associated with a paricular airframe.
    /// </summary>
    public abstract class AirframeDeviceManagerBase : IAirframeDeviceManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly Dictionary<string, Device> _devices = new();

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// add the specified device to the device list for the airframe.
        /// </summary>
        protected void AddDevice(Device d)
        {
            _devices.Add(d.Name, d);
        }

        /// <summary>
        /// returns the device associated with the specified device name.
        /// </summary>
        public Device GetDevice(string name)
        {
            return _devices[name];
        }
    }
}
