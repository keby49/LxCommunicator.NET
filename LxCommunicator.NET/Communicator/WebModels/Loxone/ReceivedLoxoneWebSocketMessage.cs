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
	/// <summary>
	/// A container for the messageHeader of each webservice
	/// </summary>
	public class ReceivedRawLoxoneWebSocketMessage {
		public ReceivedRawLoxoneWebSocketMessage(ResponseMessage responseMessage, int? clientCode) {
			if (responseMessage is null) {
				throw new ArgumentNullException(nameof(responseMessage));
			}

			this.Time = DateTime.UtcNow;
			this.ClientCode = clientCode;

			this.ProcessResponse(responseMessage);
		}

		public ReceivedRawLoxoneWebSocketMessage(byte[] content, LoxoneWebSocketMessageType webSocketMessageType, int? clientCode) {
			this.Time = DateTime.UtcNow;
			this.ClientCode = clientCode;
			this.Content = content;
			this.WebSocketMessageType = webSocketMessageType;
		}

		private void ProcessResponse(ResponseMessage responseMessage) {
			switch (responseMessage.MessageType) {
				case System.Net.WebSockets.WebSocketMessageType.Text:
					this.Content = EncodeString(responseMessage.Text);
					this.WebSocketMessageType = LoxoneWebSocketMessageType.TextMessage;
					break;
				case System.Net.WebSockets.WebSocketMessageType.Binary:
					if (LoxoneMessageHeader.TryParse(responseMessage.Binary, out LoxoneMessageHeader header)) {
						this.WebSocketMessageType = LoxoneWebSocketMessageType.HeaderMessage;
						this.Header = header;
						break;
					}

					this.Content = responseMessage.Binary;
					break;
				case System.Net.WebSockets.WebSocketMessageType.Close:
					this.WebSocketMessageType = LoxoneWebSocketMessageType.CloseMessage;
					break;
				default:
					this.WebSocketMessageType = LoxoneWebSocketMessageType.Unknow;
					break;
			}
		}
		public DateTime Time { get; }
		public LoxoneWebSocketMessageType? WebSocketMessageType { get; private set; }
		public LoxoneMessageHeader Header { get; set; }

		/// <summary>
		/// The actual message received by the miniserver.
		/// </summary>
		public byte[] Content { get; private set; }

		public void SetContent(byte[] bytes) {
			this.Content = bytes;
		}
		
		public bool HasHeader => this.Header != null;

		public bool HasContent => this.Content != null;

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
	}
}
