using Loxone.Communicator;
using Loxone.Communicator.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client;

namespace Loxone.AdvancedCommunicator {
	/// <summary>
	/// Client to handle websocketWebservices to loxone miniserver. Use <see cref="WebsocketWebserviceClient"/> for communicating via websocket or derive from it to create your own websocketClient.
	/// </summary>
	public static class LoxoneCommunicatorClientAutehentificationExtensons  {
		//public static void AAA(this IWebsocketClient websocketClient) {

		//}

		///// <summary>
		///// Establish an authenticated connection to the miniserver. Fires the OnAuthenticated event when successfull.
		///// After the event fired, the connection to the miniserver can be used.
		///// </summary>
		///// <param name="handler">The tokenhandler that should be used</param>
		//public static async Task Authenticate(this IWebsocketClient websocketClient, TokenHandler handler) {
		//	if (await MiniserverReachable()) {
		//		WebSocket = new ClientWebSocket();
		//		this.WebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(1);

		//		await WebSocket.ConnectAsync(new Uri($"ws://{IP}:{Port}/ws/rfc6455"), CancellationToken.None);
		//		BeginListening();
		//		string key = await Session.GetSessionKey();
		//		string keyExchangeResponse = (await SendWebservice(new WebserviceRequest<string>($"jdev/sys/keyexchange/{key}", EncryptionType.None))).Value;
		//		TokenHandler = handler;
		//		if (TokenHandler?.Token != null) {
		//			string hash = await TokenHandler?.GetTokenHash();
		//			string response = (await SendWebservice(new WebserviceRequest<string>($"authwithtoken/{hash}/{TokenHandler.Username}", EncryptionType.RequestAndResponse))).Value;
		//			AuthResponse authResponse = JsonConvert.DeserializeObject<AuthResponse>(response);
		//			if (authResponse.ValidUntil != default && authResponse.TokenRights != default) {
		//				OnAuthenticated.Invoke(this, new ConnectionAuthenticatedEventArgs(TokenHandler));
		//				return;
		//			}
		//		}
		//		if (await TokenHandler.RequestNewToken()) {
		//			OnAuthenticated.Invoke(this, new ConnectionAuthenticatedEventArgs(TokenHandler));
		//			return;
		//		}
		//		await HttpClient?.Authenticate(new TokenHandler(HttpClient, handler.Username, handler.Token, false));
		//	}
		//}
	}
}