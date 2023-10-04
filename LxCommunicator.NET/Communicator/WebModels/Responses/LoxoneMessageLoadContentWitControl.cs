using Newtonsoft.Json;
using System.Net;

namespace Loxone.Communicator {
	/// <summary>
	/// A container for the content received in webservices
	/// </summary>
	public class LoxoneMessageLoadContentWitControl {
		/// <summary>
		/// The control (command) which was sent to the miniserver
		/// </summary>
		[JsonProperty("control")]
		public string Control { get; set; }

		/// <summary>
		/// Http sttus code if sending and receiving the webservice succeeded
		/// </summary>
		[JsonProperty("Code")]
		public HttpStatusCode Code { get; set; }
	}
}