using System;

namespace Loxone.Communicator {
	public class ConnectionConfiguration {
		/// <summary>
		/// Initialises a new instance of the ConnectionConfiguration.
		/// </summary>
		/// <param name="ip">The ip adress of the miniserver</param>
		/// <param name="port">The port of the miniserver</param>
		/// <param name="permissions">The permissions the user should have on the server</param>
		/// <param name="deviceUuid">The uuid of the current device</param>
		/// <param name="deviceInfo">A short info of the current device</param>
		public ConnectionConfiguration(string ip, int port, int permissions, string deviceUuid, string deviceInfo) {
			IP = ip;
			Port = port;

			var session = new ConnectionSessionConfiguration { TokenPermission = permissions, DeviceUuid = deviceUuid, DeviceInfo = deviceInfo, };

			SessionConfiguration = session;
		}

		public ConnectionConfiguration(string ip, int port, ConnectionSessionConfiguration sessionConfiguration) {
			if (sessionConfiguration is null) {
				throw new ArgumentNullException(nameof(sessionConfiguration));
			}

			IP = ip;
			Port = port;
			SessionConfiguration = sessionConfiguration;
		}

		/// <summary>
		/// The ip of the miniserver
		/// </summary>
		public string IP { get; set; }

		/// <summary>
		/// The port of the miniserver
		/// </summary>
		public int Port { get; set; }

		public ConnectionSessionConfiguration SessionConfiguration { get; set; }

		public bool IsReconnectionEnabled { get; set; } = true;

		public TimeSpan ReconnectTimeout { get; set; } = TimeSpan.FromSeconds(60);

		public TimeSpan? KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(1);
	}
}