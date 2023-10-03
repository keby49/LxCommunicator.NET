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
	/// A container for the messageHeader of each webservice
	/// </summary>
	public class LoxoneWebSocketMessageStructure {
		
		public LoxoneMessageHeader? Header { get; set; }

		//public override string ToString() {
		//	return string.Format(CultureInfo.InvariantCulture, "Header: Type={0}, Estimated={1}, Length={2}", this.Type, this.Estimated, this.Length);
		//}
	}
}
