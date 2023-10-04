using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Loxone.Communicator {
	/// <summary>
	/// An object containing information about a request to the miniserver, such as command, encryptionType,...
	/// Can be cloned!
	/// </summary>
	public class WebserviceRequest : ICloneable {
		/// <summary>
		/// The matching response to the request
		/// </summary>
		internal LoxoneResponseMessage Response = null;

		internal ManualResetEvent ResponseReceived = new ManualResetEvent(false);

		/// <summary>
		/// Initialises a ned request.
		/// </summary>
		/// <param name="command">The command that should be sent to the miniserver</param>
		/// <param name="encryption">How the command should be encrypted</param>
		/// <param name="needAuthentication">Whether or not the command requires token authentication</param>
		public WebserviceRequest(
			WebserviceRequestConfig config,
			string command,
			string title = null
		) {
			Title = title;
			Config = config ?? throw new ArgumentNullException(nameof(config));
			Command = command;
			Logger = LogManager.GetLogger("WebserviceRequest");
		}

		public string Title { get; private set; }

		/// <summary>
		/// The command that should be sent to the miniserver
		/// </summary>
		public string Command { get; set; }

		public string CommandNotEscaped { get; set; }

		public string CommandNotEncrypted { get; set; }

		public WebserviceRequestConfig Config { get; private set; }

		/// <summary>
		/// A collection of queries that should be appended to the command
		/// </summary>
		public NameValueCollection Queries { get; internal set; } = System.Web.HttpUtility.ParseQueryString("");

		public WebserviceRequestState RequestState { get; set; }

		public LoxoneResponseMessage ResponsedResponse { get; set; }

		protected Logger Logger { get; }

		public static WebserviceRequest Create(
			WebserviceRequestConfig config,
			string title,
			string command,
			Action<WebserviceRequest> advanced = null) {
			if (config is null) {
				throw new ArgumentNullException(nameof(config));
			}

			if (string.IsNullOrEmpty(command)) {
				throw new ArgumentException($"'{nameof(command)}' cannot be null or empty.", nameof(command));
			}

			var result = new WebserviceRequest(config, command, title);

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
		public virtual bool TryValidateResponse(LoxoneResponseMessage response) {
			if (response == null) {
				//this.Logger.Info(string.Format(CultureInfo.InvariantCulture,
				//	"TryValidateResponse NOT_HANDLED REQUEST 1 {4}|{0}: {3}Orginal: {1}{3}Sent: {2}",
				//	this.Config.Encryption, this.CommandNotEncrypted, this.Command, Environment.NewLine, this.GetTitle()
				//));
				RequestState = WebserviceRequestState.Valid;

				Response = response;
				ResponseReceived.Set();
				return true;
			}

			switch (response.LoxoneFormat) {
				case LoxoneDataFormat.ContentWithControl:
					var requestCommandList = new List<string> { CommandNotEscaped, Command, CommandNotEncrypted };
					LoxoneResponseMessageWithContainer withContainer = (LoxoneResponseMessageWithContainer)response;
					var content = withContainer.Container.Response;

					if (content == null) {
					}

					var any = requestCommandList.Any(c => DefaultWebserviceComparer.Comparer.Compare(c, content.Control) == 0);
					if (!any) {
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

					RequestState = WebserviceRequestState.Valid;

					Response = response;
					ResponseReceived.Set();
					return true;
				default:
					// others are handled
					Logger.Info(
						string.Format(
							CultureInfo.InvariantCulture,
							"TryValidateResponse HANDLED REQUEST {4}|{0}: {3}Orginal: {1}{3}Sent: {2}",
							Config.Encryption,
							CommandNotEncrypted,
							Command,
							Environment.NewLine,
							GetTitle()
						));
					RequestState = WebserviceRequestState.Valid;

					Response = response;
					ResponseReceived.Set();
					return true;
			}
		}

		public virtual string GetTitle() {
			return Title ?? "Title[NULL]";
		}

		/// <summary>
		/// Waits until a matching WebserviceResponse is received
		/// </summary>
		/// <returns>The received WebserviceResponse</returns>
		public LoxoneResponseMessage WaitForResponse() {
			if (!ResponseReceived.WaitOne(Config.Timeout)) {
				//throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "WebSocket > Timeout"));
				Response = null;
				RequestState = WebserviceRequestState.Timeouted;
				return Response;
			}

			RequestState = WebserviceRequestState.RespondedInTime;
			ResponsedResponse = Response;
			return Response;
		}

		/// <summary>
		/// Clones the request into a new one
		/// </summary>
		/// <returns>A cloned request</returns>
		public virtual object Clone() {
			//return this.DeepClone();
			return new WebserviceRequest(Config, Command) { Queries = System.Web.HttpUtility.ParseQueryString(Queries.ToString()), };
		}

		/// <summary>
		/// Returns the command
		/// </summary>
		/// <returns>The command</returns>
		public override string ToString() {
			return Command;
		}

		//public static WebserviceRequest<T> Create(
		//	WebserviceRequestConfig config,
		//	string title,
		//	string command,
		//	Action<WebserviceRequest<T>> advanced) {
		//	if (config is null) {
		//		throw new ArgumentNullException(nameof(config));
		//	}

		//	if (string.IsNullOrEmpty(command)) {
		//		throw new ArgumentException($"'{nameof(command)}' cannot be null or empty.", nameof(command));
		//	}

		//	var result = new WebserviceRequest<T>(config, command, title);

		//	if (advanced != null) {
		//		advanced(result);
		//	}

		//	return result;
		//}
	}
}