using Newtonsoft.Json;

namespace Loxone.Communicator {
	/// <summary>
	/// A container for a received Webservice
	/// </summary>
	public class LoxoneMessageLoadContainer {
		/// <summary>
		/// The content of the received Webservice
		/// </summary>
		[JsonProperty("LL")]
		public virtual LoxoneMessageLoadContentWitControl Response { get; set; }
	}
}