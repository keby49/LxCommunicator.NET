using Loxone.Communicator.Events;
using System;
using System.Collections.Generic;

namespace Loxone.Communicator {
	/// <summary>
	/// A container for a response received by the miniserver.
	/// </summary>
	public class LoxoneResponseMessageWithEventStates : LoxoneResponseMessage {
		public LoxoneResponseMessageWithEventStates(ReceivedRawLoxoneWebSocketMessage receivedMessage, List<EventState> eventStates)
			: base(receivedMessage, LoxoneResponseMessageCategory.EventStates, LoxoneDataFormat.EventStates) {
			if (eventStates is null) {
				throw new ArgumentNullException(nameof(eventStates));
			}

			EventStates = eventStates;
		}

		public List<EventState> EventStates { get; set; }
	}
}