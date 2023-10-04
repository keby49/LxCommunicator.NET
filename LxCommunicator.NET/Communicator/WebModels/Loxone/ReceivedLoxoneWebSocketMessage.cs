using System;
using System.Net.WebSockets;
using Websocket.Client;

namespace Loxone.Communicator {
	/// <summary>
	/// A container for the messageHeader of each webservice
	/// </summary>
	public class ReceivedRawLoxoneWebSocketMessage {
		public ReceivedRawLoxoneWebSocketMessage(ResponseMessage responseMessage, int? clientCode) {
			if (responseMessage is null) {
				throw new ArgumentNullException(nameof(responseMessage));
			}

			Time = DateTime.UtcNow;
			ClientCode = clientCode;

			ProcessResponse(responseMessage);
		}

		public ReceivedRawLoxoneWebSocketMessage(byte[] content, LoxoneWebSocketMessageType webSocketMessageType, int? clientCode) {
			Time = DateTime.UtcNow;
			ClientCode = clientCode;
			Content = content;
			WebSocketMessageType = webSocketMessageType;
		}

		public DateTime Time { get; }

		public LoxoneWebSocketMessageType? WebSocketMessageType { get; private set; }

		public LoxoneMessageHeader Header { get; set; }

		/// <summary>
		/// The actual message received by the miniserver.
		/// </summary>
		public byte[] Content { get; private set; }

		public bool HasHeader => Header != null;

		public bool HasContent => Content != null;

		/// <summary>
		/// The error / success code the webserviceClient returned
		/// </summary>
		public int? ClientCode { get; private set; }

		//public ResponseMessage ResponseMessage { get; private set; }

		public WebSocketMessageType ResponseMessageType { get; private set; }

		[Obsolete]
		public static byte[] EncodeString(string content) {
			if (content is null) {
				throw new ArgumentNullException(nameof(content));
			}

			return LoxoneContentHelper.GetBytesAsString(content);
		}

		public void SetContent(byte[] bytes) {
			Content = bytes;
		}

		private void ProcessResponse(ResponseMessage responseMessage) {
			switch (responseMessage.MessageType) {
				case System.Net.WebSockets.WebSocketMessageType.Text:
					Content = EncodeString(responseMessage.Text);
					WebSocketMessageType = LoxoneWebSocketMessageType.TextMessage;
					break;
				case System.Net.WebSockets.WebSocketMessageType.Binary:
					if (LoxoneMessageHeader.TryParse(responseMessage.Binary, out LoxoneMessageHeader header)) {
						WebSocketMessageType = LoxoneWebSocketMessageType.HeaderMessage;
						Header = header;
						break;
					}

					Content = responseMessage.Binary;
					break;
				case System.Net.WebSockets.WebSocketMessageType.Close:
					WebSocketMessageType = LoxoneWebSocketMessageType.CloseMessage;
					break;
				default:
					WebSocketMessageType = LoxoneWebSocketMessageType.Unknow;
					break;
			}
		}
	}
}