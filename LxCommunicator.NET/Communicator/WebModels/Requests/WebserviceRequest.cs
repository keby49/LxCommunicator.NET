using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Force.DeepCloner;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Loxone.Communicator {
	/// <summary>
	/// An object containing information about a request to the miniserver, such as command, encryptionType,...
	/// Can be cloned!
	/// </summary>
	public class WebserviceRequest : ICloneable {

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
			this.Title = title;
			this.Config = config ?? throw new ArgumentNullException(nameof(config));
			this.Command = command;
			this.Logger = LogManager.GetLogger("WebserviceRequest");
		}

		public string Title { get; private set; }

		protected Logger Logger { get; }

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


		internal ManualResetEvent ResponseReceived = new ManualResetEvent(false);

		public WebserviceRequestState RequestState { get; set; }

		public LoxoneResponseMessage ResponsedResponse { get; set; }

		/// <summary>
		/// The matching response to the request
		/// </summary>
		internal LoxoneResponseMessage Response = null;

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
				this.RequestState = WebserviceRequestState.Valid;

				Response = response;
				ResponseReceived.Set();
				return true;
			}

			switch (response.LoxoneFormat) {
				case LoxoneDataFormat.ContentWithControl:
					var requestCommandList = new List<string> { this.CommandNotEscaped, this.Command, this.CommandNotEncrypted };
					LoxoneResponseMessageWithContainer withContainer = (LoxoneResponseMessageWithContainer)response;
					var content = withContainer.Container.Response;

					if(content == null) {

					}

					var any = requestCommandList.Any(c => DefaultWebserviceComparer.Comparer.Compare(c, content.Control) == 0);
					if (!any) {
						// different request
						return false;
					}

					this.Response = response;
					if (content.Code != System.Net.HttpStatusCode.OK) {
						this.RequestState = WebserviceRequestState.NotValidWrongHttpStatusCode;

						Response = response;
						ResponseReceived.Set();
						return true;
					}
					this.RequestState = WebserviceRequestState.Valid;

					Response = response;
					ResponseReceived.Set();
					return true;
				default:
					// others are handled
					this.Logger.Info(string.Format(CultureInfo.InvariantCulture,
							"TryValidateResponse HANDLED REQUEST {4}|{0}: {3}Orginal: {1}{3}Sent: {2}",
							this.Config.Encryption, this.CommandNotEncrypted, this.Command, Environment.NewLine, this.GetTitle()
						));
					this.RequestState = WebserviceRequestState.Valid;

					Response = response;
					ResponseReceived.Set();
					return true;
			}
		}

		public virtual string GetTitle() {
			return this.Title ?? "Title[NULL]";
		}

		/// <summary>
		/// Waits until a matching WebserviceResponse is received
		/// </summary>
		/// <returns>The received WebserviceResponse</returns>
		public LoxoneResponseMessage WaitForResponse() {
			if (!ResponseReceived.WaitOne(this.Config.Timeout)) {

				//throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "WebSocket > Timeout"));
				Response = null;
				this.RequestState = WebserviceRequestState.Timeouted;
				return Response;
			}

			this.RequestState = WebserviceRequestState.RespondedInTime;
			this.ResponsedResponse = Response;
			return Response;
		}
		/// <summary>
		/// Clones the request into a new one
		/// </summary>
		/// <returns>A cloned request</returns>
		public virtual object Clone() {
			//return this.DeepClone();
			return new WebserviceRequest(this.Config, this.Command) {
				Queries = System.Web.HttpUtility.ParseQueryString(Queries.ToString()),

			};
		}
		/// <summary>
		/// Returns the command
		/// </summary>
		/// <returns>The command</returns>
		public override string ToString() {
			return Command;
		}


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
