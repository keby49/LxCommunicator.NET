using FluentAssertions;
using System.Net.WebSockets;
using System.Reactive.Linq;
using Websocket.Client;
using Xunit.Abstractions;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.Text;
using Loxone.Communicator;
using System;
using System.Globalization;
using Newtonsoft.Json;

namespace LxCommunicator.NET.Tests;

public class LoxoneClientTests : WebsocketClientTestsV3Base {
	public LoxoneClientTests(ITestOutputHelper output)
		: base(output, true) {
	}

	[Fact]
	public async Task OnStarting_ShouldGetInfoResponse() {
		var user = GetUser();
		using (LoxoneClient loxoneClient = this.GetLoxoneClient(c => {
			c.ConnectionConfiguration.IsReconnectionEnabled = false;
			//c.ConnectionConfiguration.KeepAliveInterval = null;
		})) {
			await loxoneClient.StartAndAuthenticate();

			loxoneClient.MessageReceivedAll.Subscribe<LoxoneMessage>((Action<LoxoneMessage>)(async (message) => {

				if (message.MessageType == Loxone.Communicator.LoxoneMessageType.EventStates) {

				}
			}));

			Thread.Sleep(1000);

			var resu = await loxoneClient.GetLoxoneStructureAsJson();

			this.OutputHelper.WriteLine(resu);

			//await loxoneClient.EnablebInStatusUpdate();

			//await loxoneClient.SendKeepalive();
			//Thread.Sleep(100);
			//await loxoneClient.SendKeepalive();
			//Thread.Sleep(100);
			//await loxoneClient.SendKeepalive();
			//Thread.Sleep(100);

			//Thread.Sleep(5000);


			//handler.SetPassword(user.UserPassword);
			//await loxoneClient.Authenticate(handler);
			//var versionRequest = new WebserviceRequest<string>("jdev/cfg/version", EncryptionType.Request);
			//string version = (await loxoneClient.SendWebserviceAndWait(versionRequest)).Value;
			//Console.WriteLine($"Version: {version}");
			//Thread.Sleep(100);
			//versionRequest = new WebserviceRequest<string>("jdev/cfg/version", EncryptionType.Request);
			//version = (await loxoneClient.SendWebserviceAndWait(versionRequest)).Value;
			//Console.WriteLine($"Version: {version}");

			//Thread.Sleep(1000);
			//var keepaliveRequest = new WebserviceRequest<string>("keepalive", EncryptionType.RequestAndResponse);
			//await loxoneClient.SendWebservice(keepaliveRequest);
			//Console.WriteLine($"keepalive: ok");

			//Thread.Sleep(1000);
			//keepaliveRequest = new WebserviceRequest<string>("keepalive", EncryptionType.RequestAndResponse);
			//loxoneClient.SendWebservice(keepaliveRequest);
			//Console.WriteLine($"keepalive: ok");

			//Thread.Sleep(1000);
			//keepaliveRequest = new WebserviceRequest<string>("keepalive", EncryptionType.RequestAndResponse);
			//loxoneClient.SendWebservice(keepaliveRequest);
			//Console.WriteLine($"keepalive: ok");


			//Thread.Sleep(100);

			//await handler.KillToken();
		}

		Thread.Sleep(100);
	}


	[Fact]
	public async Task ManualReconnect() {
		var user = GetUser();
		using (LoxoneClient loxoneClient = this.GetLoxoneClient(c => {
			c.ConnectionConfiguration.IsReconnectionEnabled = true;
			c.ConnectionConfiguration.ReconnectTimeout = TimeSpan.FromSeconds(1);
			c.ConnectionConfiguration.KeepAliveInterval = null;
		})) {
			await loxoneClient.StartAndAuthenticate();

			//await loxoneClient.EnablebInStatusUpdate();


			var disconnectedEvent1 = new ManualResetEvent(false);

			var reconnectedEvent1 = new ManualResetEvent(false);


			loxoneClient.MessageReceivedAll.Subscribe<LoxoneMessage>((Action<LoxoneMessage>)(async (message) => {

				if (message.MessageType == Loxone.Communicator.LoxoneMessageType.EventStates) {
					this.OutputHelper.WriteLine(string.Format(CultureInfo.InvariantCulture, "XXX: EventStates", JsonConvert.SerializeObject(message, Formatting.Indented)));

				}
			}));


			loxoneClient.client.WebSocket.DisconnectionHappened.Subscribe(di => {
				this.OutputHelper.WriteLine(string.Format(CultureInfo.InvariantCulture, "XXX: DISCONNECTED: \r\n{0}", JsonConvert.SerializeObject(di, Formatting.Indented)));
				disconnectedEvent1.Set();
			});

			loxoneClient.client.WebSocket.ReconnectionHappened.Subscribe(async ri => {
				this.OutputHelper.WriteLine(string.Format(CultureInfo.InvariantCulture, "XXX: RECONNECTED: \r\n{0}", JsonConvert.SerializeObject(ri, Formatting.Indented)));
				//loxoneClient.client.ConnectionConfiguration.IsReconnectionEnabled = false;
				await loxoneClient.ReconnectAndAuthenticate();
				loxoneClient.client.ConnectionConfiguration.KeepAliveInterval = TimeSpan.FromSeconds(1);
				loxoneClient.client.SetKeepAliveTimer();
				reconnectedEvent1.Set();
			});


			Thread.Sleep(100);

			this.OutputHelper.WriteLine("XXX: Reconnect()");
			await loxoneClient.client.WebSocket.Reconnect();

			this.OutputHelper.WriteLine("XXX: STOPPING");
			//await loxoneClient.client.WebSocket.StopOrFail(WebSocketCloseStatus.InternalServerError, "custom");
			//await loxoneClient.client.WebSocket.NativeClient.CloseAsync(WebSocketCloseStatus.InternalServerError, "ss", CancellationToken.None);
			this.OutputHelper.WriteLine("XXX: STOPPING - WAITING");
			disconnectedEvent1.WaitOne(TimeSpan.FromSeconds(5));
			this.OutputHelper.WriteLine("XXX: STOPPED");

			this.OutputHelper.WriteLine("XXX: Wating for reconnection");
			reconnectedEvent1.WaitOne(TimeSpan.FromSeconds(5));
			Thread.Sleep(500);

			this.OutputHelper.WriteLine("XXX: CLEAN TOKEN");
			var h = (TokenHandlerV3)loxoneClient.client.TokenHandler;
			//h.Token.SetTokenToWrong();
			//await h.CleanToken();
			Thread.Sleep(5000);
		}

		Thread.Sleep(100);
	}

}