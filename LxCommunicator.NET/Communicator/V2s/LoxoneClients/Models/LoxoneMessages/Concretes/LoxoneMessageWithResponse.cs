﻿using Newtonsoft.Json;

namespace Loxone.Communicator {
	/// <summary>
	/// A container for a response received by the miniserver.
	/// </summary>
	public class LoxoneMessageWithResponse : LoxoneMessage {
		public LoxoneMessageWithResponse(LoxoneMessageHeader header, LoxoneResponseMessage rawResponse, LoxoneMessageKind messageType)
			: base(messageType) {
			Header = header;
			RawResponse = rawResponse;
		}

		[JsonProperty(Order = 30)]
		public LoxoneMessageHeader Header { get; set; }

		[JsonProperty(Order = 50)]
		public LoxoneResponseMessage RawResponse { get; set; }
	}
}