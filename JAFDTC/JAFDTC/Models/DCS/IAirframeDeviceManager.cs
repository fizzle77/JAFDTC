// ********************************************************************************************************************
//
// IAircraftDeviceManager.cs -- aircraft device manager interfaace
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

namespace JAFDTC.Models.DCS
{
	/// <summary>
	/// interface for an airframe device manager.
	/// </summary>
	public interface IAirframeDeviceManager
	{
        /// <summary>
        /// returns the device associated with the specified device name.
        /// </summary>
		Device GetDevice(string name);
	}
}
