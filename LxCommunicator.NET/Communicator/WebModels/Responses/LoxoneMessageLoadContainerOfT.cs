using Newtonsoft.Json;

namespace Loxone.Communicator {
	/// <summary>
	/// A generic container for a received Webservice
	/// </summary>
	/// <typeparam name="T">The type of the value contained in the response</typeparam>
	public class LoxoneMessageLoadContainer<T> {
		/// <summary>
		/// The content of the received Webservice
		/// </summary>
		[JsonProperty("LL")]
		public LoxoneMessageLoadContentWitControl<T> Response { get; set; }
	}
}