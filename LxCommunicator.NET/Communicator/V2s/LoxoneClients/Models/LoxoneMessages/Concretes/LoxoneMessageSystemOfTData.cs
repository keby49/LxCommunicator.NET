using Newtonsoft.Json;

namespace Loxone.Communicator {
	public class LoxoneMessageSystem<TData> : LoxoneMessageSystem {
		public LoxoneMessageSystem(LoxoneMessageSystemType loxoneMessageSystemType, TData data)
			: base(loxoneMessageSystemType) {
			Data = data;
		}

		[JsonProperty(Order = 40)]
		public TData Data { get; set; }
	}
}