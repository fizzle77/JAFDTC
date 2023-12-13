// ********************************************************************************************************************
//
// Command.cs -- aircraft command
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
	public class Command
	{
		public readonly int ID;
		public readonly string Name;
		public readonly int Delay;
		public readonly double Activate;

		public Command(int id, string name, int delay, double activate)
			=> (ID, Name, Delay, Activate) = (id, name, delay, activate);
	}
}
