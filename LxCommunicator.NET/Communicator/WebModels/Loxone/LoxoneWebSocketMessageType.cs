using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;
using Websocket.Client;

namespace Loxone.Communicator {
	public enum LoxoneWebSocketMessageType {
		Unknow,
		HeaderMessage,
		TextMessage,
		BinarryMessage,
		CloseMessage,
	}
}
