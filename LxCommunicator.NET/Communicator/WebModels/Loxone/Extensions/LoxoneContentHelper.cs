using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;
using Websocket.Client;

namespace Loxone.Communicator {

	public static class LoxoneContentHelper {
		public static byte[] GetBytesAsString(string content) {
			if (content is null) {
				throw new ArgumentNullException(nameof(content));
			}

			return Encoding.UTF8.GetBytes(content);
		}

		public static string GetStringFromBytes(byte[] bytes) {
			if (bytes is null) {
				throw new ArgumentNullException(nameof(bytes));
			}

			return Encoding.UTF8.GetString(bytes);
		}

		public static LoxoneMessageLoadContainer ParseWebserviceContainer(string content) {
			try {
				return JsonConvert.DeserializeObject<LoxoneMessageLoadContainer>(content);
			}
			catch {
				return null;
			}
		}

		public static LoxoneMessageLoadContainer<T> ParseWebserviceContainer<T>(string content) {
			try {
				return JsonConvert.DeserializeObject<LoxoneMessageLoadContainer<T>>(content);
			}
			catch {
				return null;
			}
		}
	}
}
