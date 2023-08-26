using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Loxone.Communicator {
	
	public abstract class LoxoneLanClient : IDisposable {
		protected LoxoneLanClient(ConnectionConfiguration connectionConfiguration) {
			this.ConnectionConfiguration = connectionConfiguration;
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
