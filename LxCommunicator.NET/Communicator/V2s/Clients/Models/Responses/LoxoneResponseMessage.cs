using System;

namespace Loxone.Communicator {
	/// <summary>
	/// A container for a response received by the miniserver.
	/// </summary>
	public class LoxoneResponseMessage {
		private string contentAsString = null;

		public LoxoneResponseMessage(ReceivedRawLoxoneWebSocketMessage receivedMessage, LoxoneResponseMessageCategory loxoneCategory, LoxoneDataFormat loxoneFormat) {
			ReceivedMessage = receivedMessage ?? throw new ArgumentNullException(nameof(receivedMessage));
			Header = ReceivedMessage.Header;
			LoxoneCategory = loxoneCategory;
			LoxoneFormat = loxoneFormat;
		}

		public ReceivedRawLoxoneWebSocketMessage ReceivedMessage { get; }

		public LoxoneMessageHeader Header { get; }

		//[JsonProperty(Order =0)]
		public LoxoneResponseMessageCategory LoxoneCategory { get; }

		public LoxoneDataFormat LoxoneFormat { get; }

		public MessageType? LoxoneMessageType => Header?.Type;

		//[JsonProperty(Order = 1)]
		public bool Handled { get; set; }

		public void SetContent(byte[] bytes) {
			ReceivedMessage?.SetContent(bytes);
			contentAsString = null;
		}

		public string TryGetContentAsString() {
			if (contentAsString == null) {
				contentAsString = LoxoneContentHelper.GetStringFromBytes(ReceivedMessage.Content);
			}

			return contentAsString;
		}
	}
}