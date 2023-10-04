﻿using Loxone.Communicator.Events;
using System.Collections.Generic;

namespace Loxone.Communicator {
	/// <summary>
	/// A container for a response received by the miniserver.
	/// </summary>
	public class LoxoneMessageEventStates : LoxoneMessageWithResponse {
		public LoxoneMessageEventStates(LoxoneMessageHeader header, LoxoneResponseMessage rawResponse)
			: base(header, rawResponse, LoxoneMessageKind.EventStates) {
		}

		public LoxoneMessageEventStates(LoxoneMessageWithResponse loxoneMessage)
			: base(loxoneMessage.Header, loxoneMessage.RawResponse, LoxoneMessageKind.EventStates) {
		}

		public List<EventState> EventStates { get; set; }
	}
}