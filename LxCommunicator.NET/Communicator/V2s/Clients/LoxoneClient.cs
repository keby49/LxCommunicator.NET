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

	public class LoxoneClient : IDisposable {

		private static readonly object LockObject = new object();

		private LoxoneWebsocketClient client;
		private IDisposable loxoneMessageReceivedSubscription;
		private TokenHandlerV3 handler;


		public LoxoneClient(LoxoneClientConfiguration loxoneClientConfiguration) {
			this.LoxoneClientConfiguration = loxoneClientConfiguration ?? throw new ArgumentNullException(nameof(loxoneClientConfiguration));
		}

		//private readonly Subject<ResponseMessage> _messageReceivedSubject = new Subject<ResponseMessage>();

		//public IObservable<ResponseMessage> MessageReceived => _messageReceivedSubject.AsObservable();



		private readonly Subject<LoxoneMessage> messageReceivedAll = new Subject<LoxoneMessage>();

		public IObservable<LoxoneMessage> MessageReceivedAll => messageReceivedAll.AsObservable();

		public LoxoneClientConfiguration LoxoneClientConfiguration { get; }




		public async Task StartAndAuthenticate() {
			if (this.handler != null) {
				await this.StopAndKillToken();
			}

			this.client = new LoxoneWebsocketClient(this.LoxoneClientConfiguration.ConnectionConfiguration);

			this.loxoneMessageReceivedSubscription = this.client.LoxoneMessageReceived.Subscribe(async msg => {
				this.messageReceivedAll.OnNext(msg);

				if (msg.MessageType == LoxoneMessageType.Systems) {
					var systemMsg = (LoxoneMessageSystem)msg;
					if (systemMsg != null) {
						switch (systemMsg.LoxoneMessageSystemType) {
							case LoxoneMessageSystemType.Keepalive:
								await this.SendKeepalive();
								break;
						}
					}
				}
			});


			this.handler = new TokenHandlerV3(this.client, this.LoxoneClientConfiguration.LoxoneUser.UserName);
			handler.SetPassword(this.LoxoneClientConfiguration.LoxoneUser.UserPassword);
			await this.client.StartAndConnection(handler);
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
		public async Task<bool> EnablebInStatusUpdate() {

			await this.EnsureConnected();
			var request = new WebserviceRequest<string>("jdev/sps/enablebinstatusupdate", EncryptionType.None);
			var response = await this.client.SendWebserviceAndWait(request);
			string result = response.Value;
			return result == "1";
		}

		public async Task SendKeepalive() {

			await this.EnsureConnected();
			var keepaliveRequest = new WebserviceRequest<string>("keepalive", EncryptionType.RequestAndResponse);
			await this.client.SendWebservice(keepaliveRequest);
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
	}
}