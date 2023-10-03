//using Loxone.Communicator.Events;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using System.Net.WebSockets;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Loxone.Communicator {
//	/// <summary>
//	/// Client to handle websocketWebservices to loxone miniserver. Use <see cref="WebsocketWebserviceClient"/> for communicating via websocket or derive from it to create your own websocketClient.
//	/// </summary>
//	public class WebsocketWebserviceClient : WebserviceClient {
//		/// <summary>
//		/// The httpCLient used for checking if the miniserver is available and getting the public key.
//		/// </summary>
//		public HttpWebserviceClient HttpClient { get; private set; }

//		/// <summary>
//		/// The websocket the webservices will be sent with.
//		/// </summary>
//		public ClientWebSocket WebSocket;

//		/// <summary>
//		/// A Listener to catch every incoming message from the miniserver
//		/// </summary>
//		private Task Listener;

//		/// <summary>
//		/// Event, fired when a not expected Message is received.
//		/// Contains the message in the eventArgs
//		/// </summary>
//		private event EventHandler<MessageReceivedEventArgs> OnReceiveMessge;

//		/// <summary>
//		/// Event, fired when an eventTable is received and parsed.
//		/// Contains the eventTable in the eventArgs
//		/// </summary>
//		public event EventHandler<EventStatesParsedEventArgs> OnReceiveEventTable;

//		/// <summary>
//		/// Event, fired when the connection is authenticated or a new token is received.
//		/// Contains the tokenHandler with the used token in the eventArgs.
//		/// </summary>
//		public event EventHandler<ConnectionAuthenticatedEventArgs> OnAuthenticated;

//		/// <summary>
//		/// The cancellationTokenSource used for cancelling the listener and receiving messages
//		/// </summary>
//		private readonly CancellationTokenSource TokenSource = new CancellationTokenSource();

//		/// <summary>
//		/// List of all sent requests that wait for a response
//		/// </summary>
//		private readonly List<WebserviceRequest> Requests = new List<WebserviceRequest>();

//		/// <summary>
//		/// Initialises a new instance of the websocketWebserviceClient.
//		/// </summary>
//		/// <param name="ip">The ip adress of the miniserver</param>
//		/// <param name="port">The port of the miniserver</param>
//		/// <param name="permissions">The permissions the user should have on the server</param>
//		/// <param name="deviceUuid">The uuid of the current device</param>
//		/// <param name="deviceInfo">A short info of the current device</param>
//		public WebsocketWebserviceClient(ConnectionConfiguration connectionConfiguration)
//			: base(connectionConfiguration) {
//			if (connectionConfiguration is null) {
//				throw new ArgumentNullException(nameof(connectionConfiguration));
//			}

//			Session = new Session(null, this.ConnectionConfiguration.SessionConfiguration);
//			HttpClient = new HttpWebserviceClient(connectionConfiguration, Session);
//			Session.Client = HttpClient;
//		}

//		/// <summary>
//		/// Establish an authenticated connection to the miniserver. Fires the OnAuthenticated event when successfull.
//		/// After the event fired, the connection to the miniserver can be used.
//		/// </summary>
//		/// <param name="handler">The tokenhandler that should be used</param>
//		public override async Task Authenticate(ITokenHandler handler) {
//			if (await MiniserverReachable()) {
//				WebSocket = new ClientWebSocket();
//				this.WebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(1);

//				await WebSocket.ConnectAsync(this.GetLoxoneWebSocketUri(), CancellationToken.None);
//				BeginListening();
//				string key = await Session.GetSessionKey();
//				var requestKeyExchange = WebserviceRequest<string>.Create(
//					WebserviceRequestConfig.Auth(),
//					nameof(this.Authenticate) + "_KeyExchange",
//					$"jdev/sys/keyexchange/{key}"
//				);

//				var requestKeyExchangeResponse = await SendWebserviceAndWait(requestKeyExchange);

//				string keyExchangeResponse = requestKeyExchangeResponse.Value;
//				TokenHandler = handler;
//				if (TokenHandler?.Token != null) {
//					string hash = await TokenHandler?.GetTokenHash();
//					var requestAuthWithToken = WebserviceRequest<string>.Create(
//						WebserviceRequestConfig.AuthWithEncryptionRequestAndResponse(),
//						nameof(this.Authenticate) + "_AuthWithToken",
//						$"authwithtoken/{hash}/{TokenHandler.Username}"
//					);

//					var requestAuthWithTokenResponse = await SendWebserviceAndWait(requestAuthWithToken);
//					string response = requestAuthWithTokenResponse.Value;
//					AuthResponse authResponse = JsonConvert.DeserializeObject<AuthResponse>(response);
//					if (authResponse.ValidUntil != default && authResponse.TokenRights != default) {
//						OnAuthenticated.Invoke(this, new ConnectionAuthenticatedEventArgs(TokenHandler));
//						return;
//					}
//				}
//				if (await TokenHandler.RequestNewToken()) {
//					OnAuthenticated.Invoke(this, new ConnectionAuthenticatedEventArgs(TokenHandler));
//					return;
//				}
//				await HttpClient?.Authenticate(new TokenHandler(HttpClient, handler.Username, handler.Token, false));
//			}
//		}

//		private Uri GetLoxoneWebSocketUri() {
//			return new Uri($"ws://{this.ConnectionConfiguration.IP}:{this.ConnectionConfiguration.Port}/ws/rfc6455");
//		}

//		/// <summary>
//		/// Checks if the miniserver is reachable
//		/// </summary>
//		/// <returns>Wheter the miniserver is reachable or not</returns>
//		public async Task<bool> MiniserverReachable() {
//			try {
//				WebserviceRequest<string> request = WebserviceRequest<string>.Create(WebserviceRequestConfig.Auth(), nameof(MiniserverReachable), $"jdev/cfg/api");
//				string result = (await HttpClient?.SendWebserviceAndWait(request)).Value;

//				if (result != null && result != "") {
//					return true;
//				}
//				else {
//					return false;
//				}
//			}
//			catch (Exception) {
//				return false;
//			}
//		}

//		/// <summary>
//		/// The listener starts to wait for messsages from the miniserver
//		/// </summary>
//		private void BeginListening() {
//			if (Listener != null) {
//				TokenSource.Cancel();
//				Listener = null;
//			}
//			Listener = Task.Run(async () => {
//				while (WebSocket.State == WebSocketState.Open) {
//					WebserviceResponse response = await ReceiveWebsocketMessage(1024, TokenSource.Token);
//					if (!HandleWebserviceResponse(response) && !ParseEventTable(response.Header.Type, response.Content)) {
//						OnReceiveMessge?.Invoke(WebSocket, new MessageReceivedEventArgs(response));
//					}
//					await Task.Delay(10);
//				}
//			}, TokenSource.Token);
//		}

//		/// <summary>
//		/// Sends a webservice to the miniserver and waits until its receives an answer
//		/// </summary>
//		/// <param name="request">The Request that should be sent</param>
//		/// <returns>The Response the miniserver returns</returns>
//		public override async Task<WebserviceResponse> SendWebserviceAndWait(WebserviceRequest request) {
//			switch (request?.Config.Encryption) {
//				case MessageEncryptionType.Request:
//					request.CommandNotEncrypted = request.Command;
//					request.Command = Uri.EscapeDataString(Cryptography.AesEncrypt($"salt/{Session.Salt}/{request.Command}", Session));
//					request.Command = $"jdev/sys/enc/{request.Command}";
//					request.Config.Encryption = MessageEncryptionType.None;
//					return await SendWebserviceAndWait(request);

//				case MessageEncryptionType.RequestAndResponse:
//					request.CommandNotEncrypted = request.Command;
//					string command = Uri.EscapeDataString(Cryptography.AesEncrypt($"salt/{Session.Salt}/{request.Command}", Session));
//					command = $"jdev/sys/fenc/{command}";
//					//TODO >> Request >> Set parent
//					var encryptedRequest = WebserviceRequest.Create(
//						WebserviceRequestConfig.Auth(),
//						request.Title ?? "NULL" + "_Encrypted" + "RequestAndResponse",
//						command
//					);

//					WebserviceResponse encrypedResponse = await SendWebserviceAndWait(encryptedRequest);

//					if (encrypedResponse == null) {
//						request.TryValidateResponse(new WebserviceResponse(null, null, (int?)WebSocket?.CloseStatus));
//					}
//					else {
//						request.TryValidateResponse(new WebserviceResponse(encrypedResponse.Header, Encoding.UTF8.GetBytes(Cryptography.AesDecrypt(encrypedResponse.GetAsStringContent(), Session)), (int?)WebSocket?.CloseStatus));
//					}
//					return request.WaitForResponse();

//				default:
//				case MessageEncryptionType.None:
//					if (WebSocket == null || WebSocket.State != WebSocketState.Open) {

//						if (WebSocket == null) {
//							throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "WebSocket is null."));
//						}

//						if (WebSocket.State != WebSocketState.Open) {
//							throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "WebSocket.State is not open. State={0}", WebSocket.State));
//						}
//						//return null;
//					}
//					lock (Requests) {
//						Requests.Add(request);
//					}

//					byte[] input = Encoding.UTF8.GetBytes(request.Command);
//					await WebSocket.SendAsync(new ArraySegment<byte>(input, 0, input.Length), WebSocketMessageType.Text, true, CancellationToken.None);
//					var responseWait = request.WaitForResponse();
//					return responseWait;
//			}
//		}

//		/// <summary>
//		/// Receives a webservice from the Miniservers
//		/// </summary>
//		/// <param name="bufferSize">The size of the buffer that should be used</param>
//		/// <param name="token">the cancellationToken to cancel receiving messages</param>
//		/// <returns>The received webserviceResponse</returns>
//		private async Task<WebserviceResponse> ReceiveWebsocketMessage(uint bufferSize, CancellationToken token) {
//			byte[] data;
//			LoxoneMessageHeader header;
//			do {
//				data = await InternalReceiveWebsocketMessage(bufferSize, token);
//				if (!LoxoneMessageHeader.TryParse(data, out header)) {
//					throw new WebserviceException("Received incomplete Data: \n" + Encoding.UTF8.GetString(data));
//				}
//			} while (header == null || header.Estimated);
//			data = await InternalReceiveWebsocketMessage(header.Length, TokenSource.Token);
//			return new WebserviceResponse(header, data, (int?)WebSocket?.CloseStatus);
//		}

//		/// <summary>
//		/// Internally receives messages from the websocket
//		/// </summary>
//		/// <param name="bufferSize">The bufferSize that should be used</param>
//		/// <param name="token">The cancellationToken for cancelling receiving the response</param>
//		/// <returns></returns>
//		private async Task<byte[]> InternalReceiveWebsocketMessage(uint bufferSize, CancellationToken token) {
//			WebSocketReceiveResult result;
//			byte[] buffer = new byte[bufferSize <= 0 ? 1024 : bufferSize];
//			int offset = 0;
//			using (MemoryStream stream = new MemoryStream()) {
//				do {
//					result = await WebSocket?.ReceiveAsync(new ArraySegment<byte>(buffer, offset, Math.Max(0, buffer.Length - offset)), CancellationToken.None);
//					offset += result.Count;
//					if (offset >= buffer.Length) {
//						await stream.WriteAsync(buffer, 0, buffer.Length, token);
//						offset = 0;
//					}
//				} while (result != null && !result.EndOfMessage);

//				if (result.CloseStatus != null) {

//				}

//				await stream.WriteAsync(buffer, 0, offset, token);
//				return stream.ToArray();
//			}
//		}

//		/// <summary>
//		/// Handles the assignment of the responses to the right requests.
//		/// </summary>
//		/// <param name="response">The response that should be handled</param>
//		/// <returns>Whether or not the reponse could be assigned to a request</returns>
//		private bool HandleWebserviceResponse(WebserviceResponse response) {
//			foreach (WebserviceRequest request in Enumerable.Reverse(Requests)) {
//				if (request.TryValidateResponse(response)) {
//					lock (Requests) {
//						Requests.Remove(request);
//					}
//					return true;
//				}
//			}
//			return false;
//		}

//		/// <summary>
//		/// Parses a received message into an eventTable.
//		/// Fires an onReceiveEventTable event if successful
//		/// </summary>
//		/// <param name="content">The message that should be parsed</param>
//		/// <param name="type">The expected type of the eventTable</param>
//		/// <returns>Whether or not parsing the eventTable was successful</returns>
//		private bool ParseEventTable(MessageType type, byte[] content) {
//			List<EventState> eventStates = new List<EventState>();
//			using (BinaryReader reader = new BinaryReader(new MemoryStream(content))) {
//				try {
//					do {
//						EventState state = null;
//						switch (type) {
//							case MessageType.EventTableValueStates:
//								state = ValueState.Parse(reader);
//								break;

//							case MessageType.EventTableTextStates:
//								state = TextState.Parse(reader);
//								break;

//							case MessageType.EventTableDaytimerStates:
//								state = DaytimerState.Parse(reader);
//								break;

//							case MessageType.EventTableWeatherStates:
//								state = WeatherState.Parse(reader);
//								break;

//							default:
//								return false;
//						}
//						eventStates.Add(state);
//					} while (reader.BaseStream.Length - reader.BaseStream.Position > 0);
//				}
//				catch {
//					return false;
//				}
//			}

//			if (OnReceiveEventTable != null) {
//				OnReceiveEventTable.Invoke(this, new EventStatesParsedEventArgs(type, eventStates));
//				return true;
//			}
//			else {
//				return false;
//			}
//		}

//		/// <summary>
//		/// Disposes the WebserviceClient
//		/// </summary>
//		public override void Dispose() {
//			base.Dispose();
//			TokenSource?.Cancel();
//			TokenSource?.Dispose();
//			HttpClient?.Dispose();
//			WebSocket?.Dispose();
//			Requests.Clear();
//		}
//	}
//}