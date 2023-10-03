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
	/// A container for a received Webservice
	/// </summary>
	public class LoxoneMessageLoadContainer {
		/// <summary>
		/// The content of the received Webservice
		/// </summary>
		[JsonProperty("LL")]
		public virtual LoxoneMessageLoadContentWitControl Response { get; set; }
	}
}
