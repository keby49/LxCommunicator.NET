using Loxone.Communicator.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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

	public class LoxoneClient : IDisposable, ILoxoneOperations {

		private static readonly object LockObject = new object();

		public LoxoneWebsocketClient client;
		private IDisposable loxoneMessageReceivedSubscription;
		private TokenHandlerV3 handler;


		public LoxoneClient(LoxoneClientConfiguration loxoneClientConfiguration) {
			this.LoxoneClientConfiguration = loxoneClientConfiguration ?? throw new ArgumentNullException(nameof(loxoneClientConfiguration));
			this.MessageLogger = LogManager.GetLogger("LoxoneMessages");

			JsonConvert.DefaultSettings = (() => {
				var settings = new JsonSerializerSettings();
				settings.Converters.Add(new StringEnumConverter { });
				return settings;
			});
		}

		//private readonly Subject<ResponseMessage> _messageReceivedSubject = new Subject<ResponseMessage>();

		//public IObservable<ResponseMessage> MessageReceived => _messageReceivedSubject.AsObservable();



		private readonly Subject<LoxoneMessage> messageReceivedAll = new Subject<LoxoneMessage>();

		public IObservable<LoxoneMessage> MessageReceivedAll => messageReceivedAll.AsObservable();

		public LoxoneClientConfiguration LoxoneClientConfiguration { get; }
		public Logger MessageLogger { get; }

		public async Task StartAndAuthenticate() {
			if (this.handler?.Token != null) {
				await this.StopAndKillToken();
			}

			this.client = new LoxoneWebsocketClient(this.LoxoneClientConfiguration.ConnectionConfiguration);

			this.loxoneMessageReceivedSubscription = this.client.LoxoneMessageReceived.Subscribe(async msg => {
				if (this.LoxoneClientConfiguration.LogMessages) {
					this.MessageLogger.Info(string.Format(CultureInfo.InvariantCulture, "Loxone message: {0}", JsonConvert.SerializeObject(msg, Formatting.None)));
				}

				this.messageReceivedAll.OnNext(msg);

				//if(msg is LoxoneMessageWithResponse withResponse) {
				//	if(withResponse.Header.Type)
				//}

				if (msg.MessageType == LoxoneMessageType.Systems) {
					var systemMsg = (LoxoneMessageSystem)msg;
					if (systemMsg != null) {
						switch (systemMsg.LoxoneMessageSystemType) {
							case LoxoneMessageSystemType.Keepalive:
								await this.SendKeepalive();
								break;
							case LoxoneMessageSystemType.Reconnection:
								await this.ReconnectAndAuthenticate();
								break;
						}
					}
				}
			});

			this.handler = new TokenHandlerV3(this.client, this.LoxoneClientConfiguration.LoxoneUser.UserName);
			handler.SetPassword(this.LoxoneClientConfiguration.LoxoneUser.UserPassword);
			await this.client.StartAndConnection(handler);
		}

		public async Task ReconnectAndAuthenticate() {
			//if (this.handler?.Token != null) {
			//	await this.StopAndKillToken();
			//}

			//this.client = new LoxoneWebsocketClient(this.LoxoneClientConfiguration.ConnectionConfiguration);

			//this.loxoneMessageReceivedSubscription = this.client.LoxoneMessageReceived.Subscribe(async msg => {
			//	if (this.LoxoneClientConfiguration.LogMessages) {
			//		this.MessageLogger.Info(string.Format(CultureInfo.InvariantCulture, "Loxone message: {0}", JsonConvert.SerializeObject(msg, Formatting.None)));
			//	}

			//	this.messageReceivedAll.OnNext(msg);

			//	if (msg.MessageType == LoxoneMessageType.Systems) {
			//		var systemMsg = (LoxoneMessageSystem)msg;
			//		if (systemMsg != null) {
			//			switch (systemMsg.LoxoneMessageSystemType) {
			//				case LoxoneMessageSystemType.Keepalive:
			//					await this.SendKeepalive();
			//					break;
			//			}
			//		}
			//	}
			//});


			//this.handler = new TokenHandlerV3(this.client, this.LoxoneClientConfiguration.LoxoneUser.UserName);
			await handler.CleanToken();
			handler.SetPassword(this.LoxoneClientConfiguration.LoxoneUser.UserPassword);
			//await this.client.StartAndConnection(handler);

			if (await this.client.MiniserverReachable()) {
				//this.TokenHandler = handler;
				//await this.client.CreateClientAndStartToListen();
				await this.client.HandleAuthenticate();
			}
			else {
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Loxone not reacheble."));
			}

			await this.EnablebInStatusUpdate();
		}

		public async Task SendMessage(string messageTitle, MessageEncryptionType encryptionType, string messageContent) {
			var config = new WebserviceRequestConfig() {
				NeedAuthentication = true,
				Encryption = encryptionType,
			};

			WebserviceRequest request = WebserviceRequest.Create(config, messageTitle, messageContent);
			await this.client.SendWebservice(request);
		}

		

		public string Decrypt(string contentToDecrypt) {
			return this.client.Decrypt(contentToDecrypt);
		}

		public bool IsConnected() {
			return this.client.WebSocket.IsRunning;
		}

		public async Task StopAndKillToken() {
			if (
				this.handler == null
				&&
				this.client == null
				) {
				return;
			}

			if (this.handler != null) {
				await handler.KillToken();
				handler.Dispose();
				handler = null;
			}

			if (this.client != null) {
				this.client.Dispose();
				this.client = null;
			}

			if (this.loxoneMessageReceivedSubscription != null) {
				this.loxoneMessageReceivedSubscription.Dispose();
				this.loxoneMessageReceivedSubscription = null;
			}


		}
		
		private async Task EnsureConnected() {
			await Task.CompletedTask;
			//TODO >> Ensure connected before message
			//if(this.client.Session)
		}

		/// <summary>
		/// Disposes the WebserviceClient
		/// </summary>
		public async virtual void Dispose() {
			this.messageReceivedAll.OnCompleted();
			await this.StopAndKillToken();
		}
		        

        public async Task<string> GetTextFile(string fileName)
        {
			await this.EnsureConnected();
			return await this.client.GetTextFile(fileName);
        }

        public async Task<string> GetLoxoneStructureAsJson()
        {
			await this.EnsureConnected();
			return await this.client.GetLoxoneStructureAsJson();
		}

		public async Task<bool> EnablebInStatusUpdate() {

			await this.EnsureConnected();
			return await this.client.EnablebInStatusUpdate();

		}

		public async Task SendKeepalive() {

			await this.EnsureConnected();
			await this.client.SendKeepalive();
		}
	}
}