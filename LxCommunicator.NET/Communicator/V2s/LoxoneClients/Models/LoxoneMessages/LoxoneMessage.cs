using Newtonsoft.Json;

namespace Loxone.Communicator {
	/// <summary>
	/// A container for a response received by the miniserver.
	/// </summary>
	public class LoxoneMessage {
		public LoxoneMessage(LoxoneMessageType messageType) {
			MessageType = messageType;
		}

		[JsonProperty(Order = 0)]
		public LoxoneMessageType MessageType { get; set; }

		[JsonProperty(Order = 1)]
		public bool Handled { get; set; }
	}
}