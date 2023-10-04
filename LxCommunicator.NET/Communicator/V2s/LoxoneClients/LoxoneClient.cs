using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Loxone.Communicator {
	public class LoxoneClient : IDisposable, ILoxoneOperations {
		private static readonly object LockObject = new object();

		private readonly Subject<LoxoneMessage> messageReceivedAll = new Subject<LoxoneMessage>();

		public LoxoneWebsocketClient client;

		private LoxoneTokenHandler handler;

		private IDisposable loxoneMessageReceivedSubscription;

		public LoxoneClient(LoxoneClientConfiguration loxoneClientConfiguration) {
			LoxoneClientConfiguration = loxoneClientConfiguration ?? throw new ArgumentNullException(nameof(loxoneClientConfiguration));
			MessageLogger = LogManager.GetLogger("LoxoneMessages");

			JsonConvert.DefaultSettings = (() => {
				var settings = new JsonSerializerSettings();
				settings.Converters.Add(new StringEnumConverter { });
				return settings;
			});
		}

		public IObservable<LoxoneMessage> MessageReceivedAll => messageReceivedAll.AsObservable();

		public LoxoneClientConfiguration LoxoneClientConfiguration { get; }

		public Logger MessageLogger { get; }

		public async Task StartAndAuthenticate() {
			if (handler?.Token != null) {
				await StopAndKillToken();
			}

			client = new LoxoneWebsocketClient(LoxoneClientConfiguration.ConnectionConfiguration);

			loxoneMessageReceivedSubscription = client.LoxoneMessageReceived.Subscribe(
				async msg => {
					if (LoxoneClientConfiguration.LogMessages) {
						MessageLogger.Info(string.Format(CultureInfo.InvariantCulture, "Loxone message: {0}", JsonConvert.SerializeObject(msg, Formatting.None)));
					}

					messageReceivedAll.OnNext(msg);

					if (msg.MessageType == LoxoneMessageKind.Systems) {
						var systemMsg = (LoxoneMessageSystem)msg;
						if (systemMsg != null) {
							switch (systemMsg.LoxoneMessageSystemType) {
								case LoxoneMessageSystemType.Keepalive:
									await SendKeepalive();
									break;
								case LoxoneMessageSystemType.Reconnection:
									await ReconnectAndAuthenticate();
									break;
							}
						}
					}
				});

			handler = new LoxoneTokenHandler(client, LoxoneClientConfiguration.LoxoneUser.UserName);
			handler.SetPassword(LoxoneClientConfiguration.LoxoneUser.UserPassword);
			await client.StartAndConnection(handler);
		}

		public async Task ReconnectAndAuthenticate() {
			await handler.CleanToken();
			handler.SetPassword(LoxoneClientConfiguration.LoxoneUser.UserPassword);

			if (await client.MiniserverReachable()) {
				await client.HandleAuthenticate();
			}
			else {
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Loxone not reacheble."));
			}

			await EnablebInStatusUpdate();
		}

		public async Task SendMessage(string messageTitle, MessageEncryptionType encryptionType, string messageContent) {
			var config = new LoxoneRequestConfig() { NeedAuthentication = true, Encryption = encryptionType, };

			LoxoneRequest request = LoxoneRequest.Create(config, messageTitle, messageContent);
			await client.SendWebservice(request);
		}

		public string Decrypt(string contentToDecrypt) {
			return client.Decrypt(contentToDecrypt);
		}

		public bool IsConnected() {
			return client.WebSocket.IsRunning;
		}

		public async Task StopAndKillToken() {
			if (
				handler == null
				&&
				client == null
			) {
				return;
			}

			if (handler != null) {
				await handler.KillToken();
				handler.Dispose();
				handler = null;
			}

			if (client != null) {
				client.Dispose();
				client = null;
			}

			if (loxoneMessageReceivedSubscription != null) {
				loxoneMessageReceivedSubscription.Dispose();
				loxoneMessageReceivedSubscription = null;
			}
		}

		/// <summary>
		/// Disposes the WebserviceClient
		/// </summary>
		public virtual async void Dispose() {
			messageReceivedAll.OnCompleted();
			await StopAndKillToken();
		}

		public async Task<string> GetTextFile(string fileName) {
			await EnsureConnected();
			return await client.GetTextFile(fileName);
		}

		public async Task<string> GetLoxoneStructureAsJson() {
			await EnsureConnected();
			return await client.GetLoxoneStructureAsJson();
		}

		public async Task<bool> EnablebInStatusUpdate() {
			await EnsureConnected();
			return await client.EnablebInStatusUpdate();
		}

		public async Task SendKeepalive() {
			await EnsureConnected();
			await client.SendKeepalive();
		}

		private async Task EnsureConnected() {
			await Task.CompletedTask;
			//TODO >> Ensure connected before message
			//if(this.client.Session)
		}
	}
}