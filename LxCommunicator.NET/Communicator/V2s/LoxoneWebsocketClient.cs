using Loxone.Communicator.Events;
using Newtonsoft.Json;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Websocket.Client;
using Websocket.Client.Models;

namespace Loxone.Communicator {

	public class LoxoneWebsocketClient : LoxoneLanClientWithHttp, IWebserviceClient {
		///// <summary>
		///// The httpCLient used for checking if the miniserver is available and getting the public key.
		///// </summary>
		//public HttpWebserviceClient HttpClient { get; private set; }

		/// <summary>
		/// The websocket the webservices will be sent with.
		/// </summary>
		public WebsocketClient WebSocket;

		/// <summary>
		/// A Listener to catch every incoming message from the miniserver
		/// </summary>
		private Task Listener;

		/// <summary>
		/// The cancellationTokenSource used for cancelling the listener and receiving messages
		/// </summary>
		private readonly CancellationTokenSource TokenSource = new CancellationTokenSource();

		/// <summary>
		/// List of all sent requests that wait for a response
		/// </summary>
		private readonly List<WebserviceRequest> Requests = new List<WebserviceRequest>();

		private System.Timers.Timer keepAliveTimer;

		/// <summary>
		/// Initialises a new instance of the websocketWebserviceClient.
		/// </summary>
		/// <param name="ip">The ip adress of the miniserver</param>
		/// <param name="port">The port of the miniserver</param>
		/// <param name="permissions">The permissions the user should have on the server</param>
		/// <param name="deviceUuid">The uuid of the current device</param>
		/// <param name="deviceInfo">A short info of the current device</param>
		public LoxoneWebsocketClient(ConnectionConfiguration connectionConfiguration)
			: base(connectionConfiguration) {
			if (connectionConfiguration is null) {
				throw new ArgumentNullException(nameof(connectionConfiguration));
			}
			this.Logger = LogManager.GetLogger(nameof(LoxoneWebsocketClient));
			//Session = new Session(null, this.ConnectionConfiguration.SessionConfiguration);
			//HttpClient = new HttpWebserviceClient(connectionConfiguration, Session);
			//Session.Client = HttpClient;
		}

		/// <summary>
		/// Establish an authenticated connection to the miniserver. Fires the OnAuthenticated event when successfull.
		/// After the event fired, the connection to the miniserver can be used.
		/// </summary>
		/// <param name="handler">The tokenhandler that should be used</param>
		public async Task HandleAuthenticate() {

			string key = await Session.GetSessionKey();
			var requestKey = new WebserviceRequest<string>($"jdev/sys/keyexchange/{key}", EncryptionType.None);
			var responseKey = await SendWebserviceAndWait(requestKey);
			string keyExchangeResponse = responseKey.Value;

			if (TokenHandler?.Token != null) {
				string hash = await TokenHandler?.GetTokenHash();
				string response = (await SendWebserviceAndWait(new WebserviceRequest<string>($"authwithtoken/{hash}/{TokenHandler.Username}", EncryptionType.RequestAndResponse))).Value;
				AuthResponse authResponse = JsonConvert.DeserializeObject<AuthResponse>(response);
				if (authResponse.ValidUntil != default && authResponse.TokenRights != default) {
					this.Authentificated = true;
					LoxoneMessageSystem message = new LoxoneMessageSystem(LoxoneMessageSystemType.Authenticated);
					this.loxoneMessageReceived.OnNext(message);
					return;
				}
			}
			if (await TokenHandler.RequestNewToken()) {
				this.Authentificated = true;
				LoxoneMessageSystem message = new LoxoneMessageSystem(LoxoneMessageSystemType.Authenticated);
				this.loxoneMessageReceived.OnNext(message);
				return;
			}
		}

		public async Task StartAndConnection(ITokenHandler handler) {
			if (await MiniserverReachable()) {
				this.TokenHandler = handler;
				await CreateClientAndStartToListen();
				await HandleAuthenticate();
			}
			else {
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Loxone not reacheble."));
			}
		}

		public async Task CreateClientAndStartToListen() {
			var factory = new Func<ClientWebSocket>(() => new ClientWebSocket {
				Options = {
					KeepAliveInterval = this.ConnectionConfiguration.KeepAliveInterval.HasValue ? this.ConnectionConfiguration.KeepAliveInterval.Value : default(TimeSpan),
				},
			});

			Uri url = this.GetLoxoneWebSocketUri();
			WebSocket = new WebsocketClient(url, factory);
			this.WebSocket.IsReconnectionEnabled = this.ConnectionConfiguration.IsReconnectionEnabled;
			this.WebSocket.ReconnectTimeout = this.ConnectionConfiguration.ReconnectTimeout;

			this.WebSocket.ReconnectionHappened.Subscribe(async info => {
				LoxoneMessageSystem message = new LoxoneMessageSystem<ReconnectionInfo>(LoxoneMessageSystemType.Reconnection, info);
				this.loxoneMessageReceived.OnNext(message);

				//await HandleAuthenticate();
			});

			this.WebSocket.DisconnectionHappened.Subscribe(async info => {
				LoxoneMessageSystem message = new LoxoneMessageSystem<DisconnectionInfo>(LoxoneMessageSystemType.Disconnection, info);
				this.loxoneMessageReceived.OnNext(message);
			});


			this.SetKeepAliveTimer();

			await this.WebSocket.Start();
			this.BeginListening();
		}

		private void SetKeepAliveTimer() {
			if (this.ConnectionConfiguration.KeepAliveInterval != null) {
				// Create a timer with a two second interval.
				keepAliveTimer = new System.Timers.Timer(this.ConnectionConfiguration.KeepAliveInterval.Value.TotalMilliseconds);
				// Hook up the Elapsed event for the timer. 
				keepAliveTimer.Elapsed += OnTimedEvent;
				keepAliveTimer.AutoReset = true;
				keepAliveTimer.Enabled = true;
			}
		}

		private void OnTimedEvent(Object source, ElapsedEventArgs e) {
			if (this.Authentificated) {
				LoxoneMessageSystem message = new LoxoneMessageSystem(LoxoneMessageSystemType.Keepalive);
				this.loxoneMessageReceived.OnNext(message);
			}
		}
		private Uri GetLoxoneWebSocketUri() {
			return new Uri($"ws://{this.ConnectionConfiguration.IP}:{this.ConnectionConfiguration.Port}/ws/rfc6455");
		}

		/// <summary>
		/// Checks if the miniserver is reachable
		/// </summary>
		/// <returns>Wheter the miniserver is reachable or not</returns>
		public async Task<bool> MiniserverReachable() {
			try {
				string response = (await HttpWebserviceClient?.SendWebserviceAndWait(new WebserviceRequest<string>($"jdev/cfg/api", EncryptionType.None) { NeedAuthentication = false })).Value;

				if (response != null && response != "") {
					return true;
				}
				else {
					return false;
				}
			}
			catch (Exception) {
				return false;
			}
		}



		//public Task<WebserviceContent<T>> SendWebservice<T>(WebserviceRequest<T> request) {
		//	throw new NotImplementedException();
		//}

		public async Task<WebserviceContent<T>> SendWebserviceAndWait<T>(WebserviceRequest<T> request) {
			return (await SendWebserviceAndWait((WebserviceRequest)request))?.GetAsWebserviceContent<T>();
		}

		public virtual async Task<WebserviceResponse> SendApiRequest(WebserviceRequest request) {
			return await this.HttpWebserviceClient.SendWebserviceAndWait(request);
		}

		public string Decrypt(string contentToDecrypt) {
			try {
				return Cryptography.AesDecrypt(contentToDecrypt, this.Session);
			}
			catch (Exception ex) {

				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "{0}. ContentToDecrtyp={1}", ex.Message, contentToDecrypt), ex);
			}
		}

		public async Task SendWebservice(WebserviceRequest request) {
			await this.SendWebserviceInternal(false, request);
		}

		public async Task<WebserviceResponse> SendWebserviceAndWait(WebserviceRequest request) {
			return await this.SendWebserviceInternal(true, request);
		}

		private async Task<WebserviceResponse> SendWebserviceInternal(bool wait, WebserviceRequest request) {

			switch (request?.Encryption) {
				case EncryptionType.Request:
					request.CommandNotEncrypted = request.Command;
					this.Logger.Info(string.Format(CultureInfo.InvariantCulture, "SENDing {0}: {3}Orginal: {1}{3}ToSend: {2}", request.Encryption, request.CommandNotEncrypted, request.Command, (Environment.NewLine + "\t\t\t")));
					request.CommandNotEscaped = Cryptography.AesEncrypt($"salt/{Session.Salt}/{request.Command}", Session);
					request.Command = Uri.EscapeDataString(request.CommandNotEscaped);
					request.Command = $"jdev/sys/enc/{request.Command}";
					request.CommandNotEscaped = $"jdev/sys/enc/{request.CommandNotEscaped}";
					request.Encryption = EncryptionType.None;
					return await this.SendMessage(wait, request);

				case EncryptionType.RequestAndResponse:
					request.CommandNotEncrypted = request.Command;
					request.CommandNotEscaped = request.CommandNotEscaped;
					this.Logger.Info(string.Format(CultureInfo.InvariantCulture, "SENDing {0}: {3}Orginal: {1}{3}ToSend: {2}", request.Encryption, request.CommandNotEncrypted, request.Command, (Environment.NewLine + "\t\t\t")));
					string commandNotEscaped = Cryptography.AesEncrypt($"salt/{Session.Salt}/{request.Command}", Session);
					string command = Uri.EscapeDataString(commandNotEscaped);
					command = $"jdev/sys/fenc/{command}";

					commandNotEscaped = $"jdev/sys/fenc/{commandNotEscaped}";
					WebserviceRequest encryptedRequest = new WebserviceRequest(command, EncryptionType.None, request.NeedAuthentication);
					encryptedRequest.Timeout = request.Timeout;
					//encryptedRequest.CommandNotEscaped = commandNotEscaped;
					encryptedRequest.CommandNotEncrypted = request.CommandNotEncrypted;
					encryptedRequest.CommandNotEscaped = commandNotEscaped;

					WebserviceResponse encrypedResponse = await this.SendMessage(wait, encryptedRequest);

					if (encrypedResponse == null) {
						request.TryValidateResponse(new WebserviceResponse(null, null, (int?)WebSocket?.NativeClient.CloseStatus));
					}
					else {

						var decrypted = this.Decrypt(encrypedResponse.GetAsStringContent());
						request.TryValidateResponse(new WebserviceResponse(encrypedResponse.Header, Encoding.UTF8.GetBytes(decrypted), (int?)WebSocket?.NativeClient.CloseStatus));
					}
					return request.WaitForResponse();

				default:
				case EncryptionType.None:
					return await this.SendMessage(wait, request);
			}
		}
		private async Task<WebserviceResponse> SendMessage(bool wait, WebserviceRequest request) {
			this.Logger.Info(string.Format(CultureInfo.InvariantCulture, "SENDing {0}: {3}Orginal: {1}{3}ToSend: {2}", request.Encryption, request.CommandNotEncrypted, request.Command, (Environment.NewLine + "\t\t\t")));
			if (WebSocket == null || WebSocket.NativeClient.State != WebSocketState.Open) {
				if (WebSocket == null) {
					throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "WebSocket is null."));
				}
				if (WebSocket.NativeClient.State != WebSocketState.Open) {
					throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "WebSocket.State is not open. State={0}", WebSocket.NativeClient.State));
				}
				//return null;
			}

			if (wait) {
				lock (Requests) {
					Requests.Add(request);
				}
			}

			//byte[] input = Encoding.UTF8.GetBytes(request.Command);
			//await WebSocket.Send(new ArraySegment<byte>(input, 0, input.Length), WebSocketMessageType.Text, true, CancellationToken.None);
			WebSocket.Send(request.Command);

			if (!wait) {
				return null;
			}
			var responseWait = request.WaitForResponse();
			return responseWait;
		}

		/// <summary>
		/// Handles the assignment of the responses to the right requests.
		/// </summary>
		/// <param name="response">The response that should be handled</param>
		/// <returns>Whether or not the reponse could be assigned to a request</returns>
		private bool HandleWebserviceResponse(LoxoneMessageWithResponse loxoneMessage) {
			if (Requests.Count == 0) {
				this.Logger.Info(string.Format(CultureInfo.InvariantCulture, "NO REQUESTs TO HANDLE."));
			}
			foreach (WebserviceRequest request in Enumerable.Reverse(Requests)) {
				if (request.TryValidateResponse(loxoneMessage.RawResponse)) {
					lock (Requests) {
						Requests.Remove(request);
						this.Logger.Info(string.Format(CultureInfo.InvariantCulture, "REQUESTs (after validate) COUNT= {0}", this.Requests.Count));
						loxoneMessage.Handled = true;
						this.loxoneMessageReceived.OnNext(loxoneMessage);
					}
					return true;
				}
			}
			return false;
		}

		MessageHeader lastHeader;


		public Logger Logger { get; }

		/// <summary>
		/// The listener starts to wait for messsages from the miniserver
		/// </summary>
		private void BeginListening() {
			this.WebSocket.MessageReceived.Subscribe(msg => {
				WebserviceResponse responseToHandle = null;
				switch (msg.MessageType) {
					case WebSocketMessageType.Text:
						if (this.lastHeader == null) {
							throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Message header not found."));
						}

						responseToHandle = new WebserviceResponse(lastHeader, Encoding.UTF8.GetBytes(msg.Text), (int?)WebSocket?.NativeClient.CloseStatus);
						this.lastHeader = null;

						break;
					case WebSocketMessageType.Binary:
						if (this.lastHeader != null) {
							MessageHeader headerSecond;
							if (MessageHeader.TryParse(msg.Binary, out headerSecond)) {
								// cannot have two headers
								break;
							}
						}

						if (this.lastHeader != null) {
							responseToHandle = new WebserviceResponse(lastHeader, msg.Binary, (int?)WebSocket?.NativeClient.CloseStatus);
							this.lastHeader = null;
						}
						else {
							MessageHeader header;
							if (!MessageHeader.TryParse(msg.Binary, out header)) {
								throw new WebserviceException("Received incomplete Data: \n" + Encoding.UTF8.GetString(msg.Binary));
							}
							else {
								lastHeader = header;
							}
						}
						break;
					case WebSocketMessageType.Close:
						break;

				}

				if (responseToHandle != null) {
					this.HandleResponseWithHeader(responseToHandle);
				}
			});
		}

		private readonly Subject<LoxoneMessage> loxoneMessageReceived = new Subject<LoxoneMessage>();

		public IObservable<LoxoneMessage> LoxoneMessageReceived => loxoneMessageReceived.AsObservable();

		public bool Authentificated { get; private set; }

		private void HandleResponseWithHeader(WebserviceResponse responseToHandle) {
			var message = new LoxoneMessageWithResponse(responseToHandle.Header, responseToHandle, LoxoneMessageType.Uknown) {
			};

			if (this.HandleWebserviceResponse(message)) {
				return;
			}

			if (this.ParseEventTable(message)) {
				return;
			}

			this.loxoneMessageReceived.OnNext(message);
		}


		///// <summary>
		///// Receives a webservice from the Miniservers
		///// </summary>
		///// <param name="bufferSize">The size of the buffer that should be used</param>
		///// <param name="token">the cancellationToken to cancel receiving messages</param>
		///// <returns>The received webserviceResponse</returns>
		//private async Task<WebserviceResponse> ReceiveWebsocketMessage(uint bufferSize, CancellationToken token) {
		//	byte[] data;
		//	MessageHeader header;
		//	do {
		//		data = await InternalReceiveWebsocketMessage(bufferSize, token);
		//		if (!MessageHeader.TryParse(data, out header)) {
		//			throw new WebserviceException("Received incomplete Data: \n" + Encoding.UTF8.GetString(data));
		//		}
		//	} while (header == null || header.Estimated);
		//	data = await InternalReceiveWebsocketMessage(header.Length, TokenSource.Token);
		//	return new WebserviceResponse(header, data, (int?)WebSocket?.NativeClient.CloseStatus);
		//}

		///// <summary>
		///// Internally receives messages from the websocket
		///// </summary>
		///// <param name="bufferSize">The bufferSize that should be used</param>
		///// <param name="token">The cancellationToken for cancelling receiving the response</param>
		///// <returns></returns>
		//private async Task<byte[]> InternalReceiveWebsocketMessage(uint bufferSize, CancellationToken token) {
		//	WebSocketReceiveResult result;
		//	byte[] buffer = new byte[bufferSize <= 0 ? 1024 : bufferSize];
		//	int offset = 0;
		//	using (MemoryStream stream = new MemoryStream()) {
		//		do {
		//			result = await WebSocket?.ReceiveAsync(new ArraySegment<byte>(buffer, offset, Math.Max(0, buffer.Length - offset)), CancellationToken.None);
		//			offset += result.Count;
		//			if (offset >= buffer.Length) {
		//				await stream.WriteAsync(buffer, 0, buffer.Length, token);
		//				offset = 0;
		//			}
		//		} while (result != null && !result.EndOfMessage);

		//		if (result.CloseStatus != null) {

		//		}

		//		await stream.WriteAsync(buffer, 0, offset, token);
		//		return stream.ToArray();
		//	}
		//}



		/// <summary>
		/// Parses a received message into an eventTable.
		/// Fires an onReceiveEventTable event if successful
		/// </summary>
		/// <param name="content">The message that should be parsed</param>
		/// <param name="type">The expected type of the eventTable</param>
		/// <returns>Whether or not parsing the eventTable was successful</returns>
		private bool ParseEventTable(LoxoneMessageWithResponse loxoneMessage) {
			byte[] content = loxoneMessage.RawResponse.Content;
			MessageType type = loxoneMessage.Header.Type;

			List<EventState> eventStates = new List<EventState>();
			using (BinaryReader reader = new BinaryReader(new MemoryStream(content))) {
				try {
					do {
						EventState state = null;
						switch (type) {
							case MessageType.EventTableValueStates:
								state = ValueState.Parse(reader);
								break;

							case MessageType.EventTableTextStates:
								state = TextState.Parse(reader);
								break;

							case MessageType.EventTableDaytimerStates:
								state = DaytimerState.Parse(reader);
								break;

							case MessageType.EventTableWeatherStates:
								state = WeatherState.Parse(reader);
								break;

							default:
								return false;
						}
						eventStates.Add(state);
					} while (reader.BaseStream.Length - reader.BaseStream.Position > 0);
				}
				catch {
					return false;
				}
			}

			LoxoneMessageEventStates message = new LoxoneMessageEventStates(loxoneMessage) {
				Handled = true,
				EventStates = eventStates,
			};

			this.loxoneMessageReceived.OnNext(message);
			return true;
		}


		/// <summary>
		/// Disposes the WebserviceClient
		/// </summary>
		public override void Dispose() {
			base.Dispose();
			TokenSource?.Cancel();
			TokenSource?.Dispose();
			WebSocket?.Dispose();
			Requests.Clear();
			loxoneMessageReceived.OnCompleted();

			if (keepAliveTimer != null) {
				this.keepAliveTimer.Stop();
				this.keepAliveTimer.Dispose();
				this.keepAliveTimer = null;
			}
		}

		public async Task<WebserviceContent<T>> SendApiRequest<T>(WebserviceRequest<T> request) {
			return (await SendApiRequest((WebserviceRequest)request))?.GetAsWebserviceContent<T>();

		}

	}
}