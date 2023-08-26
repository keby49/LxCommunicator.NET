using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Threading;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Loxone.Communicator {
	/// <summary>
	/// The types of possible encryptions
	/// </summary>
	public enum EncryptionType {
		None,
		Request,
		RequestAndResponse,
	}
	/// <summary>
	/// An object containing information about a request to the miniserver, such as command, encryptionType,...
	/// Can be cloned!
	/// </summary>
	public class WebserviceRequest : ICloneable {
		/// <summary>
		/// The command that should be sent to the miniserver
		/// </summary>
		public string Command { get; set; }

		public string CommandNotEscaped { get; set; }

		public string CommandNotEncrypted { get; set; }
		/// <summary>
		/// Whether the command requires token authentication or not
		/// </summary>
		public bool NeedAuthentication { get; set; } = true;
        protected Logger Logger { get; }

        /// <summary>
        /// A collection of queries that should be appended to the command
        /// </summary>
        public NameValueCollection Queries { get; internal set; } = System.Web.HttpUtility.ParseQueryString("");
		/// <summary>
		/// How the command should be encrypted
		/// </summary>
		public EncryptionType Encryption { get; set; }
		/// <summary>
		/// The timeout how long the miniserver may take to respond
		/// </summary>
		public int Timeout { get; set; } = 5000;

		internal ManualResetEvent ResponseReceived = new ManualResetEvent(false);
		/// <summary>
		/// The matching response to the request
		/// </summary>
		internal WebserviceResponse Response = null;
		/// <summary>
		/// Initialises a ned request.
		/// </summary>
		/// <param name="command">The command that should be sent to the miniserver</param>
		/// <param name="encryption">How the command should be encrypted</param>
		/// <param name="needAuthentication">Whether or not the command requires token authentication</param>
		public WebserviceRequest(string command, EncryptionType encryption, bool needAuthentication = true) {
			Command = command;
			Encryption = encryption;
			NeedAuthentication = needAuthentication;
            this.Logger = LogManager.GetLogger("WebserviceRequest");
		}

		/// <summary>
		/// Validates the given WebserviceResponse
		/// </summary>
		/// <param name="response">The WebserviceResponse to validate</param>
		/// <returns>Whether the validation succeeded or not</returns>
		public virtual bool TryValidateResponse(WebserviceResponse response) {
			if (response != null) {
                Response = response;
				this.Logger.Info(string.Format(CultureInfo.InvariantCulture, "TryValidateResponse HANDLED REQUEST {0}: {3}Orginal: {1}{3}Sent: {2}", this.Encryption, this.CommandNotEncrypted, this.Command, Environment.NewLine));
				ResponseReceived.Set();
                return true;
            }
            else {
				this.Logger.Info(string.Format(CultureInfo.InvariantCulture, "TryValidateResponse NOT_HANDLED REQUEST 1 {0}: {3}Orginal: {1}{3}Sent: {2}", this.Encryption, this.CommandNotEncrypted, this.Command, Environment.NewLine));
				return false;
            }
        }

        /// <summary>
        /// Waits until a matching WebserviceResponse is received
        /// </summary>
        /// <returns>The received WebserviceResponse</returns>
        public WebserviceResponse WaitForResponse() {
            if (!ResponseReceived.WaitOne(Timeout)) {

				//throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "WebSocket > Timeout"));
				Response = null;
			}
			return Response;
		}
		/// <summary>
		/// Clones the request into a new one
		/// </summary>
		/// <returns>A cloned request</returns>
		public virtual object Clone() {
			return new WebserviceRequest(Command, Encryption) {
				NeedAuthentication = NeedAuthentication,
				Queries = System.Web.HttpUtility.ParseQueryString(Queries.ToString()),
				Timeout = Timeout,

			};
		}
		/// <summary>
		/// Returns the command
		/// </summary>
		/// <returns>The command</returns>
		public override string ToString() {
			return Command;
		}
	}

	/// <summary>
	/// A generic request to the miniserver. Inherits from non-generic webserviceRequest
	/// </summary>
	/// <typeparam name="T">The type of the requested response</typeparam>
	public class WebserviceRequest<T> : WebserviceRequest {
		/// <summary>
		/// Initialises a ned request.
		/// </summary>
		/// <param name="command">The command that should be sent to the miniserver</param>
		/// <param name="encryption">How the command should be encrypted</param>
		/// <param name="needAuthentication">Whether or not the command requires token authentication</param>
		public WebserviceRequest(string command, EncryptionType encryption, bool needAuthentication = true) : base(command, encryption, needAuthentication) {
		}

		/// <summary>
		/// Validates the given WebserviceResponse
		/// </summary>
		/// <param name="response">The WebserviceResponse to validate</param>
		/// <returns>Whether the validation succeeded or not</returns>
		public override bool TryValidateResponse(WebserviceResponse response) {
			WebserviceContent content = response.GetAsWebserviceContent();
			if (content == null) {
				this.Logger.Info(string.Format(CultureInfo.InvariantCulture, "TryValidateResponse NOT_HANDLED REQUEST TYPED 1 {0}: {3}Orginal: {1}{3}Sent: {2}", this.Encryption, this.CommandNotEncrypted, this.Command, Environment.NewLine));
				return false;
				//throw new WebserviceException($"Validation of response failed. Webservice content was deserialized. {response.Header}", response);
			}
			if (content != null && content.Code != System.Net.HttpStatusCode.OK) {
				throw new WebserviceException($"Sending the Webservice failed! ({content.Code})", response);
				return false;
			}
			content = response.GetAsWebserviceContent<T>();
			if (DefaultWebserviceComparer.Comparer.Compare(this.CommandNotEscaped ?? this.Command, content.Control) == 0) {
				Response = response;
				this.Logger.Info(string.Format(CultureInfo.InvariantCulture, "TryValidateResponse HANDLED REQUEST TYPED {0}: {3}Orginal: {1}{3}Sent: {2}", this.Encryption, this.CommandNotEncrypted, this.Command, Environment.NewLine));
				ResponseReceived.Set();
				return true;
			}
			else {
				this.Logger.Info(string.Format(CultureInfo.InvariantCulture, "TryValidateResponse NOT_HANDLED REQUEST TYPED 2 {0}: {3}Orginal: {1}{3}Sent: {2}{3}Received:{4}{3}NotEscaped={5}", this.Encryption, this.CommandNotEncrypted, this.Command, (Environment.NewLine + "\t\t\t"), content.Control, this.CommandNotEscaped));
				return false;
			}
		}
		/// <summary>
		/// Clones the request
		/// </summary>
		/// <returns>the cloned request</returns>
		public override object Clone() {
			return new WebserviceRequest<T>(Command, Encryption) {
				NeedAuthentication = NeedAuthentication,
				Queries = System.Web.HttpUtility.ParseQueryString(Queries.ToString()),
				Timeout = Timeout
			};
		}
	}
	/// <summary>
	/// Used for determining whether or not a respond belongs to a request
	/// </summary>
	public class DefaultWebserviceComparer : IComparer<string> {
		/// <summary>
		/// The used comparer
		/// </summary>
		public static IComparer<string> Comparer = new DefaultWebserviceComparer();
		/// <summary>
		/// Initialises a new comparer
		/// </summary>
		private DefaultWebserviceComparer() { }
		/// <summary>
		/// Normalises a string
		/// </summary>
		/// <param name="value">the text that should be normalised</param>
		/// <returns>the normalised text</returns>
		private string Normalize(string value) {
			value = value.Trim().TrimStart('/');
			if (value.StartsWith("jdev", StringComparison.OrdinalIgnoreCase)) {
				value = value.Substring(1);
			}

			//value = value.Replace("+", "%2B");
			//value = Uri.EscapeDataString(value);

			return value;
		}
		/// <summary>
		/// Compares 2 texts
		/// </summary>
		/// <param name="x">Text 1</param>
		/// <param name="y">Text 2</param>
		/// <returns>Whether the texts match or not</returns>
		public int Compare(string x, string y) {
			x = Normalize(x);
			y = Normalize(y);
			return StringComparer.OrdinalIgnoreCase.Compare(x, y);
		}
	}
}
