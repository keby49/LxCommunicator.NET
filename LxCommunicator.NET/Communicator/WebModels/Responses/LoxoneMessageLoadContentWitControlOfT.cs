using Newtonsoft.Json;

namespace Loxone.Communicator {
	/// <summary>
	/// A generic container for the content received in webservices
	/// </summary>
	/// <typeparam name="T">The type of the requested value</typeparam>
	public class LoxoneMessageLoadContentWitControl<T> : LoxoneMessageLoadContentWitControl {
		/// <summary>
		/// The value (answer) received from the miniserver
		/// </summary>
		[JsonProperty("value")]
		public T Value { get; set; }
	}
}