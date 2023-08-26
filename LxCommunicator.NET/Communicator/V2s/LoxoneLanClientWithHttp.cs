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
using System.Threading;
using System.Threading.Tasks;

namespace Loxone.Communicator {

	public abstract class LoxoneLanClientWithHttp : LoxoneLanClient {
		protected LoxoneLanClientWithHttp(ConnectionConfiguration connectionConfiguration)
			: base(connectionConfiguration) {
			Session = new Session(null, this.ConnectionConfiguration.SessionConfiguration);
			HttpWebserviceClient = new HttpWebserviceClient(connectionConfiguration, Session);
			Session.Client = HttpWebserviceClient;
		}

		/// <summary>
		/// The httpClient used for sending the messages
		/// </summary>
		public HttpWebserviceClient HttpWebserviceClient { get; set; }

		public override void Dispose() {
			base.Dispose();
			HttpWebserviceClient?.Dispose();
		}
	}
}
