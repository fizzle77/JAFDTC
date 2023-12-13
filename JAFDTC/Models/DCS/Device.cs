// ********************************************************************************************************************
//
// Device.cs -- aircraft device
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

using System.Collections.Generic;

namespace JAFDTC.Models.DCS
{
    public class Device
    {
        public readonly int ID;
        public readonly string Name;
        public readonly Dictionary<string, Command> Commands = new();

        public Device(int id, string name) => (ID, Name) = (id, name);

        public void AddCommand(Command cmd)
        {
            Commands.Add(cmd.Name, cmd);
        }

        public string GetCommand(string cmdName, bool isOvrAct = false, double ovrAct = 0.0)
        {
            Command cmd = Commands[cmdName];
            string activate = ("" + ((isOvrAct) ? ovrAct : cmd.Activate) + "").Replace(",", ".");
            string str = "{'device': '" + ID + "', 'code': '" + cmd.ID + "', 'delay': '" + cmd.Delay + "', 'activate': '" + activate + "'},";
            return str.Replace("'", "\"");
        }
    }
}
