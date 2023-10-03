using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Loxone.Communicator.Events;
using Newtonsoft.Json;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;
using Websocket.Client;

namespace Loxone.Communicator {

	public static class LoxoneResponseParser {


		private static readonly object MessageHeadersLock = new object();


		private static LoxoneMessageHeader TryGetMessageHeader(ReceivedRawLoxoneWebSocketMessage message, List<LoxoneMessageHeader> messageHeaderList) {

			if (message.WebSocketMessageType != LoxoneWebSocketMessageType.TextMessage && message.WebSocketMessageType != LoxoneWebSocketMessageType.BinarryMessage) {
				return null;
			}

			if (messageHeaderList == null) {
				return null;
			}

			lock (messageHeaderList) {
				if (messageHeaderList.Count == 0) {
					return null;
				}
				else {
					var xxx = new List<LoxoneMessageHeader>(messageHeaderList);
					xxx.Reverse();

					var result = xxx.FirstOrDefault(h => h.Time < message.Time && h.Length == message.Content.Length);
					if (result == null) {
						return null;
					}

					messageHeaderList.Remove(result);
					return result;
				}
			}
		}

		/// <summary>
		/// Parses a received message into an eventTable.
		/// Fires an onReceiveEventTable event if successful
		/// </summary>
		/// <param name="content">The message that should be parsed</param>
		/// <param name="type">The expected type of the eventTable</param>
		/// <returns>Whether or not parsing the eventTable was successful</returns>
		private static bool ParseEventTable(ReceivedRawLoxoneWebSocketMessage loxoneMessage, out LoxoneResponseMessageWithEventStates resultMessage) {
			resultMessage = null;
			byte[] content = loxoneMessage.Content;
			MessageType? type = loxoneMessage.Header?.Type;
			if (type == null) {
				return false;
			}

			List<EventState> eventStates = new List<EventState>();
			using (BinaryReader reader = new BinaryReader(new MemoryStream(content))) {
				try {
					do {
						EventState state = null;
						switch (type) {
							case MessageType.EventTableValueStates:
								state = ValueState.Parse(reader);
								break;

							case MessageType.EventTableTextStates:
								state = TextState.Parse(reader);
								break;

							case MessageType.EventTableDaytimerStates:
								state = DaytimerState.Parse(reader);
								break;

							case MessageType.EventTableWeatherStates:
								state = WeatherState.Parse(reader);
								break;

							default:
								return false;
						}
						eventStates.Add(state);
					} while (reader.BaseStream.Length - reader.BaseStream.Position > 0);
				}
				catch {
					return false;
				}
			}

			resultMessage = new LoxoneResponseMessageWithEventStates(loxoneMessage, eventStates) {
				Handled = true,
			};

			return true;
		}

		public static LoxoneResponseMessage ParseResponseWithHeader(
			bool headerRequired,
			ReceivedRawLoxoneWebSocketMessage message,
			Func<string, string> decrypt,
			List<LoxoneMessageHeader> messageHeaderList
			) {
			if (decrypt is null) {
				throw new ArgumentNullException(nameof(decrypt));
			}

			LoxoneResponseMessage messageToHandle = null;
			switch (message.WebSocketMessageType) {
				case LoxoneWebSocketMessageType.HeaderMessage:
					if (message.Header.Estimated) {
						return null;
					}

					switch (message.Header.Type) {
						case MessageType.OutOfService:
							//TODO >> MTV >> Message >> OutOfService
							messageToHandle = new LoxoneResponseMessage(message, LoxoneResponseMessageCategory.Systems, LoxoneDataFormat.None);
							break;
						case MessageType.KeepAlive:
							messageToHandle = new LoxoneResponseMessage(message, LoxoneResponseMessageCategory.Systems, LoxoneDataFormat.None);
							//TODO >> MTV >> Message >> KeepAlive
							break;
					}
					if (messageHeaderList != null) {
						messageHeaderList.Add(message.Header);
					}

					return null;
				case LoxoneWebSocketMessageType.TextMessage:
				case LoxoneWebSocketMessageType.BinarryMessage:
					var header = TryGetMessageHeader(message, messageHeaderList);

					if (header == null && headerRequired) {
						throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Message without header. {0}", JsonConvert.SerializeObject(message)));
					}
					message.Header = header;

					// Handle events
					if (ParseEventTable(message, out LoxoneResponseMessageWithEventStates messagesWithStates)) {
						messageToHandle = messagesWithStates;
					}
					else {
						// Try handle content message
						string rawContent = LoxoneContentHelper.GetStringFromBytes(message.Content);
						LoxoneMessageLoadContainer container = LoxoneContentHelper.ParseWebserviceContainer(rawContent);
						if (container == null) {
							// may be encrypted responses
							try {
								rawContent = decrypt(rawContent);
								container = LoxoneContentHelper.ParseWebserviceContainer(rawContent);
							}
							catch { }
						}

						if (container?.Response != null) {
							messageToHandle = new LoxoneResponseMessageWithContainer(message, container);
						}
					}

					break;
			}

			if (messageToHandle == null) {
				messageToHandle = new LoxoneResponseMessage(message, LoxoneResponseMessageCategory.Uknown, LoxoneDataFormat.None);
			}

			return messageToHandle;
		}
	}
}
