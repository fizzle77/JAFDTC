// ********************************************************************************************************************
//
// StatusMessageTx.cs -- status message tcp tx path
//
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
    /// supports sending a string to dcs to be displayed on screen via the lua hook tcp status message server.
    /// </summary>
    internal class StatusMessageTx
    {
        /// <summary>
        /// send a string to dcs through a tcp connection to the tcp status message server the lua hook functionality
        /// sets up. the connection is torn down after the sequence is sent. the returns true on success, false on
        /// failure.
        /// </summary>
        public static bool Send(string str)
        {
            return TCPTxSocket.SendToPort(str, Settings.TCPPortCfgNameTx);
        }
    }
}
