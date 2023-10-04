using Newtonsoft.Json;

namespace Loxone.Communicator {
	/// <summary>
	/// A container for a response received by the miniserver.
	/// </summary>
	public class LoxoneMessage {
		public LoxoneMessage(LoxoneMessageKind messageType) {
			MessageType = messageType;
		}

		[JsonProperty(Order = 0)]
		public LoxoneMessageKind MessageType { get; set; }

		[JsonProperty(Order = 1)]
		public bool Handled { get; set; }
	}
}