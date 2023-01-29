﻿using Loxone.Communicator.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Loxone.Communicator {
	/// <summary>
	/// Client to handle websocketWebservices to loxone miniserver. Use <see cref="WebsocketWebserviceClient"/> for communicating via websocket or derive from it to create your own websocketClient.
	/// </summary>
	public class WebsocketWebserviceClient : WebserviceClient {
		/// <summary>
		/// The httpCLient used for checking if the miniserver is available and getting the public key.
		/// </summary>
		public HttpWebserviceClient HttpClient { get; private set; }

		/// <summary>
		/// The intervall in which the keepalive request should be sent. The miniserver expects to get a request at least every 5 minutes
		/// </summary>
		public TimeSpan KeepaliveIntervall {
			get => keepaliveIntervall;
			set {
				keepaliveIntervall = value;
				HandleKeepalive();
			}
		}

		/// <summary>
		/// Defines if keepalive requests should be sent. See also <seealso cref="KeepaliveIntervall"/>
		/// </summary>
		public bool SendKeepalive {
			get => sendKeepalive;
			set {
				sendKeepalive = value;
				HandleKeepalive();
			}
		}

		/// <summary>
		/// The websocket the webservices will be sent with.
		/// </summary>
		private ClientWebSocket WebSocket;

		/// <summary>
		/// A Listener to catch every incoming message from the miniserver
		/// </summary>
		private Task Listener;
		private bool sendKeepalive = true;
		private TimeSpan keepaliveIntervall = TimeSpan.FromMinutes(2);

		/// <summary>
		/// Event, fired when a not expected Message is received.
		/// Contains the message in the eventArgs
		/// </summary>
		private event EventHandler<MessageReceivedEventArgs> OnReceiveMessge;

		/// <summary>
		/// Event, fired when an eventTable is received and parsed.
		/// Contains the eventTable in the eventArgs
		/// </summary>
		public event EventHandler<EventStatesParsedEventArgs> OnReceiveEventTable;

		/// <summary>
		/// Event, fired when the connection is authenticated or a new token is received.
		/// Contains the tokenHandler with the used token in the eventArgs.
		/// </summary>
		public event EventHandler<ConnectionAuthenticatedEventArgs> OnAuthenticated;

		public event EventHandler<KeepaliveEventArgs> OnKeepalive;

		/// <summary>
		/// The cancellationTokenSource used for cancelling the listener and receiving messages
		/// </summary>
		private readonly CancellationTokenSource TokenSource = new CancellationTokenSource();

		/// <summary>
		/// List of all sent requests that wait for a response
		/// </summary>
		private readonly List<WebserviceRequest> Requests = new List<WebserviceRequest>();

		/// <summary>
		/// Timer for sending keep alive requests
		/// </summary>
		private readonly Timer KeepaliveTimer = new Timer() { AutoReset = false }; // sending keepalive every 2 minutes

		/// <summary>
		/// Initialises a new instance of the websocketWebserviceClient.
		/// </summary>
		/// <param name="ip">The ip adress of the miniserver</param>
		/// <param name="port">The port of the miniserver</param>
		/// <param name="permissions">The permissions the user should have on the server</param>
		/// <param name="deviceUuid">The uuid of the current device</param>
		/// <param name="deviceInfo">A short info of the current device</param>
		public WebsocketWebserviceClient(string ip, int port, int permissions, string deviceUuid, string deviceInfo) {
			KeepaliveTimer.Elapsed += KeepaliveTimer_Elapsed;

			IP = ip;
			Port = port;
			Session = new Session(null, permissions, deviceUuid, deviceInfo);
			HttpClient = new HttpWebserviceClient(IP, Port, Session);
			Session.Client = HttpClient;
		}

		/// <summary>
		/// Gets triggered everytime a Keepalive request should be sent 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void KeepaliveTimer_Elapsed(object sender, ElapsedEventArgs e) {
			if (SendKeepalive) {
				try {
					WebserviceResponse response = await SendWebservice(new WebserviceRequest("keepalive", EncryptionType.None));
					InvokeOnKeepalive(response != null);
				}
				catch {
					InvokeOnKeepalive(false);
				}
			}
			HandleKeepalive();
		}

		/// <summary>
		/// Triggers the invokation of the OnKeepalive event
		/// </summary>
		/// <param name="isRepsonding"></param>
		private void InvokeOnKeepalive(bool isRepsonding) {
			if (SendKeepalive) {
				try {
					OnKeepalive?.Invoke(this, new KeepaliveEventArgs(isRepsonding));
				}
				catch { }
			}
		}

		/// <summary>
		/// Updates the KeepaliveTimer
		/// </summary>
		private void HandleKeepalive() {
			KeepaliveTimer.Interval = KeepaliveIntervall.TotalMilliseconds;
			KeepaliveTimer.Enabled = SendKeepalive;
		}

		/// <summary>
		/// Stops the KeepaliveTimer
		/// </summary>
		private void StopKeepalive() {
			KeepaliveTimer.Enabled = false;
		}

		/// <summary>
		/// Establish an authenticated connection to the miniserver. Fires the OnAuthenticated event when successfull.
		/// After the event fired, the connection to the miniserver can be used.
		/// </summary>
		/// <param name="handler">The tokenhandler that should be used</param>
		public override async Task Authenticate(TokenHandler handler) {
			if (await MiniserverReachable()) {
				WebSocket = new ClientWebSocket();
				await WebSocket.ConnectAsync(new Uri($"ws://{IP}:{Port}/ws/rfc6455"), CancellationToken.None);
				BeginListening();
				string key = await Session.GetSessionKey();
				string keyExchangeResponse = (await SendWebservice(new WebserviceRequest<string>($"jdev/sys/keyexchange/{key}", EncryptionType.None))).Value;
				TokenHandler = handler;
				if (TokenHandler?.Token != null) {
					string hash = await TokenHandler?.GetTokenHash();
					string response = (await SendWebservice(new WebserviceRequest<string>($"authwithtoken/{hash}/{TokenHandler.Username}", EncryptionType.RequestAndResponse))).Value;
					AuthResponse authResponse = JsonConvert.DeserializeObject<AuthResponse>(response);
					if (authResponse.ValidUntil != default && authResponse.TokenRights != default) {
						OnAuthenticated.Invoke(this, new ConnectionAuthenticatedEventArgs(TokenHandler));
						HandleKeepalive();
						return;
					}
				}
				if (await TokenHandler.RequestNewToken()) {
					OnAuthenticated.Invoke(this, new ConnectionAuthenticatedEventArgs(TokenHandler));
					HandleKeepalive();
					return;
				}
				await HttpClient?.Authenticate(new TokenHandler(HttpClient, handler.Username, handler.Token, false));
				HandleKeepalive();
			}
		}

		/// <summary>
		/// Checks if the miniserver is reachable
		/// </summary>
		/// <returns>Wheter the miniserver is reachable or not</returns>
		public async Task<bool> MiniserverReachable() {
			try {
				string response = (await HttpClient?.SendWebservice(new WebserviceRequest<string>($"jdev/cfg/api", EncryptionType.None) { NeedAuthentication = false })).Value;

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

		/// <summary>
		/// The listener starts to wait for messsages from the miniserver
		/// </summary>
		private void BeginListening() {
			if (Listener != null) {
				TokenSource.Cancel();
				Listener = null;
			}
			Listener = Task.Run(async () => {
				while (WebSocket.State == WebSocketState.Open) {
					WebserviceResponse response = await ReceiveWebsocketMessage(1024, TokenSource.Token);
					if (!ParseEventTable(response) && !HandleWebserviceResponse(response)) {
						OnReceiveMessge?.Invoke(WebSocket, new MessageReceivedEventArgs(response));
					}
					await Task.Delay(10);
				}
			}, TokenSource.Token);
		}

		/// <summary>
		/// Sends a webservice to the miniserver and waits until its receives an answer
		/// </summary>
		/// <param name="request">The Request that should be sent</param>
		/// <returns>The Response the miniserver returns</returns>
		public override async Task<WebserviceResponse> SendWebservice(WebserviceRequest request) {
			switch (request?.Encryption) {
				case EncryptionType.Request:
					request.Command = Uri.EscapeDataString(Cryptography.AesEncrypt($"salt/{Session.Salt}/{request.Command}", Session));
					request.Command = $"jdev/sys/enc/{request.Command}";
					request.Encryption = EncryptionType.None;
					return await SendWebservice(request);

				case EncryptionType.RequestAndResponse:
					string command = Uri.EscapeDataString(Cryptography.AesEncrypt($"salt/{Session.Salt}/{request.Command}", Session));
					command = $"jdev/sys/fenc/{command}";
					WebserviceRequest encryptedRequest = new WebserviceRequest(command, EncryptionType.None);
					WebserviceResponse encrypedResponse = await SendWebservice(encryptedRequest);
					if (encrypedResponse == null) {
						request.TryValidateResponse(new WebserviceResponse(null, null, (int?)WebSocket?.CloseStatus));
					}
					else {
						request.TryValidateResponse(new WebserviceResponse(encrypedResponse.Header, Encoding.UTF8.GetBytes(Cryptography.AesDecrypt(encrypedResponse.GetAsStringContent(), Session)), (int?)WebSocket?.CloseStatus));
					}
					return request.WaitForResponse();

				default:
				case EncryptionType.None:
					if (WebSocket == null || WebSocket.State != WebSocketState.Open) {
						return null;
					}
					lock (Requests) {
						Requests.Add(request);
					}
					byte[] input = Encoding.UTF8.GetBytes(request.Command);
					await WebSocket.SendAsync(new ArraySegment<byte>(input, 0, input.Length), WebSocketMessageType.Text, true, CancellationToken.None);
					return request.WaitForResponse();
			}
		}

		/// <summary>
		/// Receives a webservice from the Miniservers
		/// </summary>
		/// <param name="bufferSize">The size of the buffer that should be used</param>
		/// <param name="token">the cancellationToken to cancel receiving messages</param>
		/// <returns>The received webserviceResponse</returns>
		private async Task<WebserviceResponse> ReceiveWebsocketMessage(uint bufferSize, CancellationToken token) {
			byte[] data, content = null;
			MessageHeader header;
			do {
				data = await InternalReceiveWebsocketMessage(bufferSize, token);
				if (!MessageHeader.TryParse(data, out header)) {
					throw new WebserviceException("Received incomplete Data: \n" + Encoding.UTF8.GetString(data));
				}
			} while (header == null || header.Estimated);
			if (header.Length > 0) {
				content = await InternalReceiveWebsocketMessage(header.Length, TokenSource.Token);
			}
			return new WebserviceResponse(header, content, (int?)WebSocket?.CloseStatus);
		}

		/// <summary>
		/// Internally receives messages from the websocket
		/// </summary>
		/// <param name="bufferSize">The bufferSize that should be used</param>
		/// <param name="token">The cancellationToken for cancelling receiving the response</param>
		/// <returns></returns>
		private async Task<byte[]> InternalReceiveWebsocketMessage(uint bufferSize, CancellationToken token) {
			WebSocketReceiveResult result;
			byte[] buffer = new byte[bufferSize <= 0 ? 1024 : bufferSize];
			int offset = 0;
			using (MemoryStream stream = new MemoryStream()) {
				do {
					result = await WebSocket?.ReceiveAsync(new ArraySegment<byte>(buffer, offset, Math.Max(0, buffer.Length - offset)), CancellationToken.None);
					offset += result.Count;
					if (offset >= buffer.Length) {
						await stream.WriteAsync(buffer, 0, buffer.Length, token);
						offset = 0;
					}
				} while (result != null && !result.EndOfMessage);
				await stream.WriteAsync(buffer, 0, offset, token);
				return stream.ToArray();
			}
		}

		/// <summary>
		/// Handles the assignment of the responses to the right requests.
		/// </summary>
		/// <param name="response">The response that should be handled</param>
		/// <returns>Whether or not the reponse could be assigned to a request</returns>
		private bool HandleWebserviceResponse(WebserviceResponse response) {
			foreach (WebserviceRequest request in Enumerable.Reverse(Requests)) {
				if (request.TryValidateResponse(response)) {
					lock (Requests) {
						Requests.Remove(request);
					}
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Parses a received message into an eventTable.
		/// Fires an onReceiveEventTable event if successful
		/// </summary>
		/// <param name="content">The message that should be parsed</param>
		/// <param name="type">The expected type of the eventTable</param>
		/// <returns>Whether or not parsing the eventTable was successful</returns>
		private bool ParseEventTable(WebserviceResponse response) {
			if (response == null || !response.Header.IsEventMessage) {
				return false;
			}
			List<EventState> eventStates = new List<EventState>();
			using (BinaryReader reader = new BinaryReader(new MemoryStream(response.Content))) {
				try {
					do {
						EventState state = null;
						switch (response.Header.Type) {
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
			if (OnReceiveEventTable != null) {
				OnReceiveEventTable.Invoke(this, new EventStatesParsedEventArgs(response.Header.Type, eventStates));
				return true;
			}
			else {
				return false;
			}
		}

		/// <summary>
		/// Disposes the WebserviceClient
		/// </summary>
		public override void Dispose() {
			StopKeepalive();
			base.Dispose();
			TokenSource?.Cancel();
			TokenSource?.Dispose();
			HttpClient?.Dispose();
			WebSocket?.Dispose();
			KeepaliveTimer?.Dispose();
			Requests.Clear();
		}
	}
}