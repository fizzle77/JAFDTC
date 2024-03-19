// ********************************************************************************************************************
//
// AirframeDevice.cs -- airframe device
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

// define to enable the debug command format. this format includes additional information helpful for debug, but
// CANNOT be processed by the lua export on dcs.
//
#define noDEBUG_CMD_FORMAT

using System;
using System.Collections.Generic;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// models an airframe device that has a number of actions that can be invoked through the clickable cockpit
    /// in dcs. AirframeDeviceManagerBase manages a collection of these objects.
    /// </summary>
    public class AirframeDevice
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public readonly string Name;

        // ---- private properties

        private readonly int _id;
        private readonly Dictionary<string, Action> _actions = new();

        // ------------------------------------------------------------------------------------------------------------
        //
        // operators
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        public string this[string name] => (_actions.ContainsKey(name)) ? DCSActionCommand(_actions[name]) : "";

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public AirframeDevice(int id, string name) => (_id, Name) = (id, name);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the dcs "action" command for an action targeted at this device.
        /// </summary>
        private string DCSActionCommand(Action action)
        {
            string cmd = $"{{\"f\":\"Actn\",\"a\":{{" +
#if DEBUG_CMD_FORMAT
                         $"\"key\":\"{Name}.{action.Name}\", " +
#endif
                         $"\"dev\":{_id},\"code\":{action.ID},\"dn\":{action.ValueDn}";
            if (action.ValueUp != 0)
            {
                cmd += $",\"up\":{action.ValueUp}";
            }
            if (action.Delay != 0)
            {
                cmd += $",\"dt\":{action.Delay}";
            }
            return cmd + $"}}}},";
        }

        /// <summary>
        /// add an action with the specified parameters to the airframe device.
        /// </summary>
        public void AddAction(int id, string name, int delay, double valueDn, double valueUp = 0)
        {
            _actions.Add(name, new Action(id, name, delay, valueDn, valueUp));
        }

        /// <summary>
        /// return the dcs "action" command for an action customized with different ValueDn/ValueUp values.
        /// </summary>
        public string CustomizedDCSActionCommand(string name, double valueDn, double valueUp)
        {
            if (_actions.ContainsKey(name))
            {
                Action baseAction = _actions[name];
                return DCSActionCommand(new(baseAction.ID, baseAction.Name, baseAction.Delay, valueDn, valueUp));
            }
            return "";
        }
    }
}
