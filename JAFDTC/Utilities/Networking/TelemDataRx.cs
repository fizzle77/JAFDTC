// ********************************************************************************************************************
//
// TelemDataRx.cs : dcs telemetry data udp receiver
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

using System;
using System.Diagnostics;
using System.Text.Json;

namespace JAFDTC.Utilities.Networking
{
    /// <summary>
    /// singleton object to process telemetry data stream received from dcs via udp. this stream includes game state
    /// such as the active jet along with the state of clickable cockpit controls jafdtc uses to trigger operations
    /// such as upload. received data is processed through an event handler hooked to the TelemDataReceived event.
    /// </summary>
    public sealed class TelemDataRx
	{
        private static readonly Lazy<TelemDataRx> lazy = new(() => new TelemDataRx());

        public static TelemDataRx Instance { get => lazy.Value; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // support classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// telemetry data reported by dcs including the current model, location, relevant control state, etc.
        /// </summary>
        public class TelemData
		{
			public string Model { get; set; }               // active airframe

			public string Marker { get; set; }              // marker from command stream
			
			public string Latitude { get; set; }            // active airframe position, latitude (dd)
			
			public string Longitude { get; set; }           // active airframe position, longitude (dd)

            public string Elevation { get; set; }           // active airframe position, elevation (m)

			public string Upload { get; set; }				// cockpit control: upload configuration
            
			public string Increment { get; set; }           // cockpit control: increment command (WIP, unused)

            public string Decrement { get; set; }           // cockpit control: decrement command (WIP, unused)

            public string ShowJAFDTC { get; set; }          // cockpit control: pin jafdtc window to top layer

            public string HideJAFDTC { get; set; }          // cockpit control: unpin jafdtc window

            public string ToggleJAFDTC { get; set; }        // cockpit control: toggle jafdtc window layer pin
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties & events
        //
        // ------------------------------------------------------------------------------------------------------------

        public long NumPackets { get; set; }

        public event Action<TelemData> TelemDataReceived;

        // ---- private properties

        private readonly UDPRxSocket _socket;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public TelemDataRx() => (NumPackets, _socket) = (0, new UDPRxSocket());

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        public void Start()
		{
			_socket.StartReceiving("127.0.0.1", Settings.UDPPortTelRx, (string s) =>
			{
                TelemData data = JsonSerializer.Deserialize<TelemData>(s);
				if (data != null)
				{
					NumPackets += 1;
                    TelemDataReceived?.Invoke(data);
                }
            });
		}

		public void Stop()
		{
			_socket.Stop();
		}
	}
}