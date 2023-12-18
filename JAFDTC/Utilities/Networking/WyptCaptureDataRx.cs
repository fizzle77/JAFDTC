// ********************************************************************************************************************
//
// TelemDataRx.cs : dcs telemetry data udp receiver
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
using System.Text.Json;

namespace JAFDTC.Utilities.Networking
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed class WyptCaptureDataRx
    {
        private static readonly Lazy<WyptCaptureDataRx> lazy = new(() => new WyptCaptureDataRx());

        public static WyptCaptureDataRx Instance { get => lazy.Value; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // support classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// waypoint data reported by the coordinate capture dcs hook.
        /// </summary>
        public class WyptCaptureData
        {
            public string Latitude { get; set; }            // latitude of capture (dd)

            public string Longitude { get; set; }           // longitude of capture (dd)

            public string Elevation { get; set; }           // elevation of capture (ft)

            public bool IsTarget { get; set; }              // true => capture is a target point
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties & events
        //
        // ------------------------------------------------------------------------------------------------------------

        public event Action<WyptCaptureData[]> WyptCaptureDataReceived;

        // ---- private properties

        private readonly UDPSocket _socket;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public WyptCaptureDataRx() => (_socket) = (new UDPSocket());

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        public void Start()
        {
            _socket.StartReceiving("127.0.0.1", Settings.UDPPortCapRx, (string s) =>
            {
                WyptCaptureData[] data = JsonSerializer.Deserialize<WyptCaptureData[]>(s);
                WyptCaptureDataReceived?.Invoke(data);
            });
        }

        public void Stop()
        {
            _socket.Stop();
        }
    }
}