using System;
using System.Globalization;

namespace Loxone.Communicator {
	/// <summary>
	/// A container for the messageHeader of each webservice
	/// </summary>
	public class LoxoneMessageHeader {
		/// <summary>
		/// The messageType (e.g. textMessage, valueStateEvent, ...) of the received message
		/// </summary>
		public MessageType Type { get; private set; }

		/// <summary>
		/// Whether or not another header is sent by the miniserver before the actual message.
		/// </summary>
		public bool Estimated { get; private set; }

		/// <summary>
		/// The lenght of the received message
		/// </summary>
		public uint Length { get; private set; }

		public DateTime Time { get; private set; }

		/// <summary>
		/// Tries to parse received bytes into a message header.
		/// </summary>
		/// <param name="bytes">The bytes that should be parsed</param>
		/// <param name="header">The header that was parsed, if successful</param>
		/// <returns>Whether or not parsing the header succeeded</returns>
		public static bool TryParse(byte[] bytes, out LoxoneMessageHeader header) {
			header = null;
			if (bytes == null || bytes.Length != 8 || bytes[0] != 3) {
				return false;
			}

			try {
				header = new LoxoneMessageHeader() {
					Type = (MessageType)bytes[1], Length = BitConverter.ToUInt32(bytes, 4), Estimated = (bytes[2] & (byte)128) == 128, Time = DateTime.UtcNow,
				};
				return true;
			}
			catch (Exception ex) {
				throw new WebserviceException("Parse MessageHeader failed: " + ex.Message, ex);
			}
		}

		public override string ToString() {
			return string.Format(CultureInfo.InvariantCulture, "Header: Type={0}, Estimated={1}, Length={2}", Type, Estimated, Length);
		}
	}
}