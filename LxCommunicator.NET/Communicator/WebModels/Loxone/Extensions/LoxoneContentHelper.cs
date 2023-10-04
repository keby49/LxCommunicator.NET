using Newtonsoft.Json;
using System;
using System.Text;

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