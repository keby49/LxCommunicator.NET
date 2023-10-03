using Loxone.Communicator.Events;
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
	public class LoxoneMessageEventStates : LoxoneMessageWithResponse {
		public LoxoneMessageEventStates(LoxoneMessageHeader header, WebserviceResponse rawResponse) : base(header, rawResponse, LoxoneMessageType.EventStates) {
		}

		public LoxoneMessageEventStates(LoxoneMessageWithResponse loxoneMessage) : base(loxoneMessage.Header, loxoneMessage.RawResponse, LoxoneMessageType.EventStates) {
		}

		public List<EventState> EventStates { get; set; }
	}
}