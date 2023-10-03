using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Loxone.Communicator {

	/// <summary>
	/// A container for a response received by the miniserver.
	/// </summary>
	public class LoxoneResponseMessage {
		public LoxoneResponseMessage(ReceivedRawLoxoneWebSocketMessage receivedMessage, LoxoneResponseMessageCategory loxoneCategory, LoxoneDataFormat loxoneFormat) {
			this.ReceivedMessage = receivedMessage ?? throw new ArgumentNullException(nameof(receivedMessage));
			this.Header = this.ReceivedMessage.Header;
			this.LoxoneCategory = loxoneCategory;
			this.LoxoneFormat = loxoneFormat;
		}

		public ReceivedRawLoxoneWebSocketMessage ReceivedMessage { get; }

		public LoxoneMessageHeader Header { get; }

		//[JsonProperty(Order =0)]
		public LoxoneResponseMessageCategory LoxoneCategory { get; }

		public LoxoneDataFormat LoxoneFormat { get; }

		public MessageType? LoxoneMessageType => this.Header?.Type;

		//[JsonProperty(Order = 1)]
		public bool Handled { get; set; }

		public void SetContent(byte[] bytes) {
			this.ReceivedMessage?.SetContent(bytes);
			this.contentAsString = null;
		}

		private string contentAsString = null;

		public string TryGetContentAsString() {
			if (this.contentAsString == null) {
				this.contentAsString = LoxoneContentHelper.GetStringFromBytes(this.ReceivedMessage.Content);
			}

			return this.contentAsString;
		}

	}
}