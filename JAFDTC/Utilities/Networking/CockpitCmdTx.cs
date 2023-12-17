// ********************************************************************************************************************
//
// CockpitCmdTx.cs -- cockpit command tcp tx path
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

namespace JAFDTC.Utilities.Networking
{
    /// <summary>
    /// supports sending command sequences to dcs to drive the cockpit controls necessary to set up the avionics on
    /// the jet according to a configuration.
    /// </summary>
    internal sealed class CockpitCmdTx
    {
        /// <summary>
        /// send a command string to dcs through the tcp connection with the lua export functionality. returns
        /// true on success, false on failure.
        /// </summary>
        public static bool Send(string str)
        {
            try
            {
                using TcpClient tcpClient = new("127.0.0.1", Settings.TCPPortTx);
                using NetworkStream ns = tcpClient.GetStream();
                using StreamWriter sw = new(ns);
                string data = "[" + str + "]";
                // Debug.WriteLine(data);

                sw.WriteLine(data);
                sw.Flush();
            }
            catch (SocketException)
            {
                return false;
            }
            return true;
        }
    }
}