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
	public class EventStatesLoxoneMessage : LoxoneMessage {
		public List<EventState>  EventStates { get; set; }
	}
}