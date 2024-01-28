// ********************************************************************************************************************
//
// IBuilder.cs -- command builder manager interfaace
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
    /// interface for a command builder object that generates a stream of clickable-cockpit commands to change
    /// avionics state in dcs for a system.
    /// </summary>
    public interface IBuilder
    {
        /// <summary>
        /// build the command stream appropriate for the object and add it to internal object state.
        /// </summary>
        public void Build();
    }
}
