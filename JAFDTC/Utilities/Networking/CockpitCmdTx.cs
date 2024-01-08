﻿// ********************************************************************************************************************
//
// CockpitCmdTx.cs -- cockpit command tcp tx path
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

namespace JAFDTC.Utilities.Networking
{
    /// <summary>
    /// supports sending command sequences to dcs to drive the cockpit controls necessary to set up the avionics on
    /// the jet according to a configuration.
    /// </summary>
    internal sealed class CockpitCmdTx
    {
        /// <summary>
        /// send a command string to dcs through a tcp connection to the lua export functionality. returns true on
        /// success, false on failure.
        /// </summary>
        public static bool Send(string str)
        {
            return TCPTxSocket.SendToPort($"[ {str} ]", Settings.TCPPortCmdTx);
        }
    }
}