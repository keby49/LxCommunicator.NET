using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Loxone.Communicator {
	/// <summary>
	/// A container for the content received in webservices
	/// </summary>
	public class LoxoneMessageLoadContentWitControl {
		/// <summary>
		/// The control (command) which was sent to the miniserver
		/// </summary>
		[JsonProperty("control")]
		public string Control { get; set; }
		/// <summary>
		/// Http sttus code if sending and receiving the webservice succeeded
		/// </summary>
		[JsonProperty("Code")]
		public HttpStatusCode Code { get; set; }
	}
}
