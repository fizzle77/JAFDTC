// ********************************************************************************************************************
//
// UDPSocket.cs : udp socket
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

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace JAFDTC.Utilities.Networking
{
    /// <summary>
    /// TODO: document
    /// </summary>
    internal sealed class UDPSocket
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties & events
        //
        // ------------------------------------------------------------------------------------------------------------

        public bool IsRunning { get; private set; }

        public delegate void ReceiveCallback(string s);

        private class State
        {
            public byte[] buffer;
        }

        private bool _stop;

        private Socket _socket;
        private AsyncCallback _recvCallback;

        private EndPoint _epFrom;

        private readonly int _bufSize;
        private readonly State _state;

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties & events
        //
        // ------------------------------------------------------------------------------------------------------------

        public UDPSocket(int bufSize = 8 * 1024)
        {
            IsRunning = false;

            _bufSize = bufSize;
            _state = new()
            {
                buffer = new byte[_bufSize]
            };
            _epFrom = new IPEndPoint(IPAddress.Any, 0);
            _recvCallback = null;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        public void StartReceiving(string address, int port, ReceiveCallback callback)
        {
            if (!IsRunning)
            {
                IsRunning = true;
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
                _socket.Bind(new IPEndPoint(IPAddress.Parse(address), port));
                Receive(callback);
            }
        }

        public void Stop()
        {
            _stop = true;
        }

        private void Receive(ReceiveCallback callback)
        {
            _socket.BeginReceiveFrom(_state.buffer, 0, _bufSize, SocketFlags.None, ref _epFrom, _recvCallback = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndReceiveFrom(ar, ref _epFrom);
                string str = Encoding.ASCII.GetString(so.buffer, 0, bytes);
                callback(str);

                if (!_stop)
                {
                    _socket.BeginReceiveFrom(so.buffer, 0, _bufSize, SocketFlags.None, ref _epFrom, _recvCallback, so);
                }
                else
                {
                    _socket.Close();
                    _socket.Dispose();
                    _stop = false;
                    IsRunning = false;
                }
            }, _state);
        }
    }
}
