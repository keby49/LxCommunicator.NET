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
	public class LoxoneResponseMessageWithEventStates : LoxoneResponseMessage{
		public LoxoneResponseMessageWithEventStates(ReceivedRawLoxoneWebSocketMessage receivedMessage, List<EventState> eventStates) 
			: base(receivedMessage, LoxoneResponseMessageCategory.EventStates, LoxoneDataFormat.EventStates) {
			if (eventStates is null) {
				throw new ArgumentNullException(nameof(eventStates));
			}
			this.EventStates = eventStates;
		}

		public List<EventState> EventStates { get; set; }
	}
}