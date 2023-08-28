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
	public class LoxoneMessageWithResponse : LoxoneMessage {
		public LoxoneMessageWithResponse(MessageHeader header, WebserviceResponse rawResponse, LoxoneMessageType messageType) : base(messageType) {
			Header = header;
			RawResponse = rawResponse;
		}

		[JsonProperty(Order = 30)]
		public MessageHeader Header { get; set; }

		[JsonProperty(Order = 50)]
		public WebserviceResponse RawResponse { get; set; }
	}
}