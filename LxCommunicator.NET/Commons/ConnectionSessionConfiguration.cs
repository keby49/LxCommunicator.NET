namespace Loxone.Communicator {
	public class ConnectionSessionConfiguration {
		/// <summary>
		/// The permission the current user has / should have
		/// </summary>
		public int TokenPermission { get; set; }

		/// <summary>
		/// The uuid of the current device
		/// </summary>
		public string DeviceUuid { get; set; }

		/// <summary>
		/// The info of the current device
		/// </summary>
		public string DeviceInfo { get; set; }
	}
}