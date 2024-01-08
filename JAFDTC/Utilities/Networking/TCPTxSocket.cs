// ********************************************************************************************************************
//
// TCPTxSocket.cs -- tcp tx socket
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

using System.IO;
using System.Diagnostics;
using System.Net.Sockets;

namespace JAFDTC.Utilities.Networking
{
    /// <summary>
    /// supports sending data to dcs via a tcp socket on localhost. the connection is torn down after the data
    /// is sent.
    /// </summary>
    internal class TCPTxSocket
    {
        /// <summary>
        /// send a string to dcs through a tcp connection to localhost on the specified port. returns true on success,
        /// false on failure.
        /// </summary>
        public static bool SendToPort(string str, int port)
        {
            try
            {
                using TcpClient tcpClient = new("127.0.0.1", port);
                using NetworkStream ns = tcpClient.GetStream();
                using StreamWriter sw = new(ns);
                //
                // Debug.WriteLine(str);
                //
                sw.WriteLine(str);
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
