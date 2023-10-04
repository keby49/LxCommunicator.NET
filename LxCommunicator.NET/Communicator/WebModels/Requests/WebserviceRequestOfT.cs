using System;

namespace Loxone.Communicator {
	/// <summary>
	/// A generic request to the miniserver. Inherits from non-generic webserviceRequest
	/// </summary>
	/// <typeparam name="T">The type of the requested response</typeparam>
	public class WebserviceRequest<T> : WebserviceRequest {
		public WebserviceRequest(WebserviceRequestConfig config, string command, string title = null)
			: base(config, command, title) {
		}

		public static WebserviceRequest<T> Create(
			WebserviceRequestConfig config,
			string title,
			string command,
			Action<WebserviceRequest<T>> advanced = null) {
			if (config is null) {
				throw new ArgumentNullException(nameof(config));
			}

			if (string.IsNullOrEmpty(command)) {
				throw new ArgumentException($"'{nameof(command)}' cannot be null or empty.", nameof(command));
			}

			var result = new WebserviceRequest<T>(config, command, title);

			if (advanced != null) {
				advanced(result);
			}

			return result;
		}

		/// <summary>
		/// Validates the given WebserviceResponse
		/// </summary>
		/// <param name="response">The WebserviceResponse to validate</param>
		/// <returns>Whether the validation succeeded or not</returns>
		public override bool TryValidateResponse(LoxoneResponseMessage response) {
			//			this.Logger.Info(string.Format(CultureInfo.InvariantCulture,
			//	"TryValidateResponse HANDLED REQUEST TYPED {4}|{0}: {3}Orginal: {1}{3}Sent: {2}",
			//	this.Config.Encryption, this.CommandNotEncrypted, this.Command, Environment.NewLine, this.GetTitle()
			//));

			if (response == null) {
				RequestState = WebserviceRequestState.Valid;
				Response = response;
				ResponseReceived.Set();
				return true;
			}

			switch (response.LoxoneFormat) {
				case LoxoneDataFormat.ContentWithControl:
					var requestCommand = CommandNotEscaped ?? Command;
					LoxoneResponseMessageWithContainer withContainer = (LoxoneResponseMessageWithContainer)response;
					var content = withContainer.Container.Response;

					if (!(DefaultWebserviceComparer.Comparer.Compare(requestCommand, content.Control) == 0)) {
						// different request
						return false;
					}

					Response = response;
					if (content.Code != System.Net.HttpStatusCode.OK) {
						RequestState = WebserviceRequestState.NotValidWrongHttpStatusCode;

						Response = response;
						ResponseReceived.Set();
						return true;
					}

					var contentAsString = LoxoneContentHelper.GetStringFromBytes(response.ReceivedMessage.Content);
					var typedContent = LoxoneContentHelper.ParseWebserviceContainer<T>(contentAsString);

					if (typedContent != null) {
						RequestState = WebserviceRequestState.Valid;

						Response = response;
						ResponseReceived.Set();
						return true;
					}

					return false;
				default:
					//this.RequestState = WebserviceRequestState.Valid;

					//Response = response;
					//ResponseReceived.Set();
					//return true;

					return false;

				//// cannot handle messages withou content
				//if (response.ReceivedMessage.ClientCode != System.Net.HttpStatusCode.OK) {
				//	this.RequestState = WebserviceRequestState.NotValidWrongHttpStatusCode;
				//	ResponseReceived.Set();
				//	return true;
				//}
			}
		}

		/// <summary>
		/// Clones the request
		/// </summary>
		/// <returns>the cloned request</returns>
		public override object Clone() {
			return new WebserviceRequest<T>(Config, Command) { Queries = System.Web.HttpUtility.ParseQueryString(Queries.ToString()), };
		}
	}
}