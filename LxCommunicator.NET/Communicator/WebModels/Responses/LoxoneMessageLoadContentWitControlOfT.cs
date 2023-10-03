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
	/// A generic container for the content received in webservices
	/// </summary>
	/// <typeparam name="T">The type of the requested value</typeparam>
	public class LoxoneMessageLoadContentWitControl<T> : LoxoneMessageLoadContentWitControl {
		/// <summary>
		/// The value (answer) received from the miniserver
		/// </summary>
		[JsonProperty("value")]
		public T Value { get; set; }
	}
}
