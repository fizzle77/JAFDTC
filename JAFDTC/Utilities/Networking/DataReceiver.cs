using JAFDTC.Utilities;
using System;
using System.Diagnostics;
using System.Text.Json;

namespace JAFDTC.Utilities.Networking
{
	/// <summary>
	/// TODO: document
	/// </summary>
	public class DataReceiver
	{
		public class Data
		{
			public string Model { get; set; }

			public string Marker { get; set; }
			
			public string Latitude { get; set; }
			
			public string Longitude { get; set; }
			
			public string Elevation { get; set; }
			
			public string Clock { get; set; }
            
			public string Upload { get; set; }
            
			public string Increment { get; set; }
            
			public string Decrement { get; set; }
            
			public string ShowJAFDTC { get; set; }
			
			public string HideJAFDTC { get; set; }
			
			public string ToggleJAFDTC { get; set; }
        }

		public static long NumPackets { get; set; } = 0;

		public static event Action<Data> DataReceived;

		public static void Start()
		{
			UDPSocket.StartReceiving("127.0.0.1", Settings.UDPPortRx, (string s) =>
			{
				Data data = JsonSerializer.Deserialize<Data>(s);
				if (data != null)
				{
					NumPackets += 1;
                    DataReceived?.Invoke(data);
                }
            });
		}

		public static void Stop()
		{
			UDPSocket.Stop();
		}
	}
}