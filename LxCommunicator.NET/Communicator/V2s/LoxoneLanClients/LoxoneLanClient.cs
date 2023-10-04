using System;

namespace Loxone.Communicator {
	public abstract class LoxoneLanClient : IDisposable {
		protected LoxoneLanClient(ConnectionConfiguration connectionConfiguration) {
			ConnectionConfiguration = connectionConfiguration;
		}

		/// <summary>
		/// The tokenhandler that should be used for managing the token
		/// </summary>
		public ITokenHandler TokenHandler { get; set; }

		/// <summary>
		/// The session object used for storing information about the connection
		/// </summary>
		public Session Session { get; internal set; }

		public ConnectionConfiguration ConnectionConfiguration { get; }

		/// <summary>
		/// Disposes the WebserviceClient
		/// </summary>
		public virtual void Dispose() {
			TokenHandler?.Dispose();
		}
	}
}