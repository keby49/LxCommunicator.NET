using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Loxone.Communicator {
	/// <summary>
	/// Client to handle httpWebservices to loxone miniserver. Use <see cref="HttpWebserviceClient"/> for communicating via http or derive from it to create your own httpClient.
	/// </summary>
	public class HttpWebserviceClient : WebserviceClient {
		private CancellationTokenSource CancellationTokenSource;

		/// <summary>
		/// Creates a new instance of the httpWebserviceClient.
		/// </summary>
		/// <param name="ip">IP adress of the miniserver</param>
		/// <param name="port">Port of the miniserver</param>
		/// <param name="permissions">Permissions of the connecting user</param>
		/// <param name="deviceUuid">Uuid of the connecting device</param>
		/// <param name="deviceInfo">Info of the connecting device</param>
		public HttpWebserviceClient(ConnectionConfiguration connectionConfiguration)
			: base(connectionConfiguration) {
			if (connectionConfiguration is null) {
				throw new ArgumentNullException(nameof(connectionConfiguration));
			}

			HttpClient = new HttpClient();
			Session = new Session(this, ConnectionConfiguration.SessionConfiguration);
		}

		/// <summary>
		/// Creates a new instance of the httpWebserviceClient.
		/// </summary>
		/// <param name="ip">IP adress of the miniserver</param>
		/// <param name="port">Port of the miniserver</param>
		/// <param name="session">Session object containing info used for connection</param>
		public HttpWebserviceClient(ConnectionConfiguration connectionConfiguration, Session session)
			: base(connectionConfiguration) {
			HttpClient = new HttpClient();
			Session = session;
		}

		/// <summary>
		/// The httpClient used for sending the messages
		/// </summary>
		private HttpClient HttpClient { get; set; }

		private static LoxoneResponseMessage CreateLoxoneMessage(ReceivedRawLoxoneWebSocketMessage responseRaw) {
			//TODO >> MTV >> CreateLoxoneMessage >> Not implemented
			LoxoneResponseMessage messageToHandle = LoxoneResponseParser.ParseResponseWithHeader(
				false,
				responseRaw,
				c => c,
				null);
			return messageToHandle;
		}

		/// <summary>
		/// Provides info required for the authentication on the miniserver
		/// </summary>
		/// <param name="handler">The tokenHandler that should be used</param>
		public override Task Authenticate(ITokenHandler handler) {
			TokenHandler = handler;
			return Task.CompletedTask;
		}

		/// <summary>
		/// Sends a webservice to the miniserver
		/// </summary>
		/// <param name="request">The Request that should be sent</param>
		/// <returns>The Response the miniserver returns</returns>
		public override async Task<LoxoneResponseMessage> SendWebserviceAndWait(WebserviceRequest request) {
			WebserviceRequest encRequest = await GetEncryptedRequest(request);

			Uri url = GetLoxoneCommandUri(encRequest);

			CancellationTokenSource?.Dispose();
			CancellationTokenSource = new CancellationTokenSource(request.Config.Timeout);

			HttpResponseMessage httpResponse = await HttpClient?.GetAsync(url.OriginalString, CancellationTokenSource.Token);
			byte[] responseContent = await httpResponse?.Content.ReadAsByteArrayAsync();

			CancellationTokenSource?.Dispose();

			if (
				httpResponse.IsSuccessStatusCode
				&&
				request.Config.Encryption == MessageEncryptionType.RequestAndResponse
			) {
				//decypt response if needed
				responseContent = Encoding.UTF8.GetBytes(Cryptography.AesDecrypt(Encoding.UTF8.GetString(responseContent), Session));
			}

			//WebserviceResponse response = new WebserviceResponse(null, responseContent, (int)httpResponse.StatusCode);
			ReceivedRawLoxoneWebSocketMessage responseRaw = new ReceivedRawLoxoneWebSocketMessage(responseContent, LoxoneWebSocketMessageType.BinarryMessage, (int)httpResponse.StatusCode);
			LoxoneResponseMessage response = CreateLoxoneMessage(responseRaw);

			var responseIsValid = encRequest.TryValidateResponse(response);
			if (!responseIsValid) {
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Websocket > Invalid response."));
			}

			return response;
		}

		public override async Task<LoxoneResponseMessage> SendApiRequest(WebserviceRequest request) {
			return await SendWebserviceAndWait(request);
		}

		/// <summary>
		/// Disposes the WebserviceClient
		/// </summary>
		public override void Dispose() {
			base.Dispose();
			HttpClient?.Dispose();
			CancellationTokenSource?.Dispose();
		}

		private Uri GetLoxoneCommandUri(WebserviceRequest encRequest) {
			return new UriBuilder() {
				Scheme = "http",
				Host = ConnectionConfiguration.IP,
				Port = ConnectionConfiguration.Port,
				Path = encRequest.Command,
				Query = encRequest.Queries.ToString()
			}.Uri;
		}

		/// <summary>
		/// Creates a clone of a request and encrypts it
		/// </summary>
		/// <param name="request">The request that should be encrypted</param>
		/// <returns>the encrypted clone of the given request</returns>
		private async Task<WebserviceRequest> GetEncryptedRequest(WebserviceRequest request) {
			if (request == null) {
				return null;
			}

			WebserviceRequest encRequest = (WebserviceRequest)request.Clone();
			if (request.Config.NeedAuthentication && TokenHandler != null) {
				//add authentication if needed
				if (TokenHandler.Token == null) {
					await TokenHandler.RequestNewToken();
				}

				encRequest.Queries.Add("autht", await TokenHandler.GetTokenHash());
				encRequest.Queries.Add("user", TokenHandler.Username);
				encRequest.Config.NeedAuthentication = false;
				if (encRequest.Config.Encryption == MessageEncryptionType.None) {
					encRequest.Config.Encryption = MessageEncryptionType.Request;
				}
			}

			switch (encRequest.Config.Encryption) {
				case MessageEncryptionType.Request:
					encRequest.Command = "jdev/sys/enc/";
					break;
				case MessageEncryptionType.RequestAndResponse:
					encRequest.Command = "jdev/sys/fenc/";
					break;
				case MessageEncryptionType.None:
				default:
					return encRequest;
			}

			string query = encRequest.Queries.HasKeys() ? $"?{encRequest.Queries.ToString()}" : "";
			encRequest.Command += Uri.EscapeDataString(Cryptography.AesEncrypt($"salt/{Session.Salt}/{request.Command}{query}", Session));
			encRequest.Queries.Clear();
			encRequest.Queries.Add("sk", await Session.GetSessionKey());
			encRequest.Config.Encryption = MessageEncryptionType.None;
			return encRequest;
		}
	}
}