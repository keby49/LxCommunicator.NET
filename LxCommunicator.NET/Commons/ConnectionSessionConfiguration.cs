using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Loxone.Communicator {
	public class ConnectionSessionConfiguration {

		/// <summary>
		/// The permission the current user has / should have
		/// </summary>
		public int TokenPermission { get;  set; }

		/// <summary>
		/// The uuid of the current device
		/// </summary>
		public string DeviceUuid { get;  set; }

		/// <summary>
		/// The info of the current device
		/// </summary>
		public string DeviceInfo { get;  set; }
	}
}