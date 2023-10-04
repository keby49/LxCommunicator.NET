namespace Loxone.Communicator {
	/// <summary>
	/// A container for a response received by the miniserver.
	/// </summary>
	public class WebserviceResponse {
		/// <summary>
		/// Initialises a new webserviceResponse
		/// </summary>
		/// <param name="header">The header of the message</param>
		/// <param name="content">The content of the message</param>
		/// <param name="clientCode">The error / success code the webserviceClient returned</param>
		public WebserviceResponse(LoxoneMessageHeader header, byte[] content, int? clientCode) {
			Header = header;
			Content = content;
			ClientCode = clientCode;
		}

		/// <summary>
		/// The header of the message. Contains information about the type and the length of the message.
		/// </summary>
		public LoxoneMessageHeader Header { get; }

		/// <summary>
		/// The actual message received by the miniserver.
		/// </summary>
		public byte[] Content { get; }

		/// <summary>
		/// The error / success code the webserviceClient returned
		/// </summary>
		public int? ClientCode { get; }

		/// <summary>
		/// Get the webserviceResponse as webserviceContent
		/// </summary>
		/// <returns>The webserviceContent (without value!)</returns>
		public LoxoneMessageLoadContentWitControl GetAsWebserviceContent() {
			try {
				string content = GetAsStringContent();
				var container = LoxoneContentHelper.ParseWebserviceContainer(content);
				return container?.Response;
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Get the webserviceResponse as webserviceContent
		/// </summary>
		/// <typeparam name="T">The type of the value</typeparam>
		/// <returns>The webserviceContent (with value)</returns>
		public LoxoneMessageLoadContentWitControl<T> GetAsWebserviceContent<T>() {
			try {
				string content = GetAsStringContent();
				var container = LoxoneContentHelper.ParseWebserviceContainer<T>(content);
				return container?.Response;
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Get the webserviceResponse as text
		/// </summary>
		/// <returns>the text containing the response</returns>
		public string GetAsStringContent() {
			return LoxoneContentHelper.GetStringFromBytes(Content);
		}

		/// <summary>
		/// Get the webserviceResponse as text
		/// </summary>
		/// <returns>The text containing the response</returns>
		public override string ToString() {
			return GetAsStringContent();
		}
	}
}