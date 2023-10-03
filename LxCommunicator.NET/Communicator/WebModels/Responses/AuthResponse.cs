using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Loxone.Communicator {
	/// <summary>
	/// A container for the reponse received when authenticating to the miniserver with a token via websocket
	/// </summary>
	public class AuthResponse {
		/// <summary>
		/// Determines how long the used token is valid.
		/// </summary>
		[JsonProperty("validUntil")]
		public int ValidUntil { get; set; }
		/// <summary>
		/// The rights the current token provides to the user
		/// </summary>
		[JsonProperty("tokenRights")]
		public int TokenRights { get; set; }
		/// <summary>
		/// Determines whether the password of the current user if secured or not.
		/// </summary>
		[JsonProperty("unsecurePass")]
		public bool UnsecurePass { get; set; }
	}
}
