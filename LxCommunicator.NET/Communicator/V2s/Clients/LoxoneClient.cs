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
using Websocket.Client;

namespace Loxone.Communicator {

	public class LoxoneClient : IDisposable {

		private static readonly object LockObject = new object();

		private LoxoneWebsocketClient client;
		private TokenHandlerV3 handler;



		public LoxoneClient(LoxoneClientConfiguration loxoneClientConfiguration) {
			this.LoxoneClientConfiguration = loxoneClientConfiguration ?? throw new ArgumentNullException(nameof(loxoneClientConfiguration));
		}

		private readonly Subject<ResponseMessage> _messageReceivedSubject = new Subject<ResponseMessage>();

		public IObservable<ResponseMessage> MessageReceived => _messageReceivedSubject.AsObservable();

		public LoxoneClientConfiguration LoxoneClientConfiguration { get; }


		// _messageReceivedSubject.OnNext(value);


		public async Task StartAndAuthenticate() {
			if (this.handler != null) {
				await this.StopAndKillToken();
			}

			this.client = new LoxoneWebsocketClient(this.LoxoneClientConfiguration.ConnectionConfiguration);
			this.handler = new TokenHandlerV3(this.client,this.LoxoneClientConfiguration.LoxoneUser.UserName);
			handler.SetPassword(this.LoxoneClientConfiguration.LoxoneUser.UserPassword);
			await this.client.Authenticate(handler);
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
			_messageReceivedSubject.OnCompleted();
			await this.StopAndKillToken();
		}
	}
}