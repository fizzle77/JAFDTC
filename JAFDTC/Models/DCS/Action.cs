// ********************************************************************************************************************
//
// Command.cs -- airframe device action
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

using System.Diagnostics;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// models an action that an airframe device can take in response to a physical interaction such as turning a
    /// knob, pushing a button, flipping a switch. an interaction involves setting dcs state to a "down" value,
    /// waiting for a delay, then setting dcs state to an "up" value. actions are always associated with an
    /// AirframeDevice.
    /// </summary>
    public class Action
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public readonly string Name;                            // unique action name

        public readonly int ID;                                 // dcs clickable cockpit id for action
        
        public readonly int Delay;                              // delay (ms) between "down" state and "up" state
        
        public readonly double ValueDn;                         // value on "down"
        
        public readonly double ValueUp;                         // value on "up"

        // ------------------------------------------------------------------------------------------------------------
        //
        // Construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public Action(int id, string name, int delay, double valueDn, double valueUp = 0)
            => (ID, Name, Delay, ValueDn, ValueUp) = (id, name, delay, valueDn, valueUp);
    }
}
