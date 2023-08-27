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


		public async Task StartAndAuthenticate(LoxoneUser loxoneUser) {
			if (this.handler != null) {
				this.StopAndKillToken();
			}

			this.client = new LoxoneWebsocketClient(this.LoxoneClientConfiguration.ConnectionConfiguration);
			this.handler = new TokenHandlerV3(this.client, loxoneUser.UserName);
			handler.SetPassword(loxoneUser.UserPassword);
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

		/// <summary>
		/// Disposes the WebserviceClient
		/// </summary>
		public async virtual void Dispose() {
			_messageReceivedSubject.OnCompleted();
			await this.StopAndKillToken();
		}
	}
}