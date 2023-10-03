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
	/// Possible types for a message
	/// </summary>
	public enum MessageType : byte {
		Text = 0,
		Binary = 1,
		EventTableValueStates = 2,
		EventTableTextStates = 3,
		EventTableDaytimerStates = 4,
		OutOfService = 5,
		KeepAlive = 6,
		EventTableWeatherStates = 7
	}
}
