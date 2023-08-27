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
	public class LoxoneMessage {
		/// <summary>
		/// The header of the message. Contains information about the type and the length of the message.
		/// </summary>
		public MessageHeader Header { get; set; }

		/// <summary>
		/// The error / success code the webserviceClient returned
		/// </summary>
		public int? ClientCode { get; set; }
	}
}