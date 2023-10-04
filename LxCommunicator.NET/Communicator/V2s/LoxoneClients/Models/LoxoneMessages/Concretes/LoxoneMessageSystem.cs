using Newtonsoft.Json;

namespace Loxone.Communicator {
	public class LoxoneMessageSystem : LoxoneMessage {
		public LoxoneMessageSystem(LoxoneMessageSystemType loxoneMessageSystemType)
			: base(LoxoneMessageKind.Systems) {
			LoxoneMessageSystemType = loxoneMessageSystemType;
		}

		[JsonProperty(Order = 30)]
		public LoxoneMessageSystemType LoxoneMessageSystemType { get; set; }
	}
}