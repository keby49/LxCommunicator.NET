using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Threading;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Loxone.Communicator {
	/// <summary>
	/// The types of possible encryptions
	/// </summary>
	public enum MessageEncryptionType {
		None,
		Request,
		RequestAndResponse,
	}
}
