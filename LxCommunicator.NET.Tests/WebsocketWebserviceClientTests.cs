//using FluentAssertions;
//using Loxone.Communicator;
//using Loxone.Communicator.Events;
//using System.Runtime.CompilerServices;
//using Xunit.Abstractions;

//namespace LxCommunicator.NET.Tests;

//public class WebsocketWebserviceClientTests : WebsocketClientTestsV3Base {

//	//private ConnectionConfiguration GetConfig() => new ConnectionConfiguration(
//	//	"192-168-50-50.504f94a181b0.dyndns.loxonecloud.com",
//	//	80,
//	//	2,
//	//		"098802e1-02b4-603c-ffffeee000d80cfd",
//	//		"LxCommunicator.NET.Websocket") { };

//	private WebsocketWebserviceClient GetWebsockerClient() {
//		return new WebsocketWebserviceClient(this.GetConfig());
//	}

//	static ManualResetEvent receivedUpdates = new ManualResetEvent(false);

//	static ManualResetEvent receivedMessage = new ManualResetEvent(false);
//	static int tableEvents = 0;
//	[Fact]
//	public async Task Test1() {
//		var client = GetWebsockerClient();
//		var user = GetUser();

//		receivedUpdates = new ManualResetEvent(false);

//		TokenHandler handler = null;

//		tableEvents = 0;
//		using (client) {
//			handler = new TokenHandler(client, user.UserName);

//			handler.SetPassword(user.UserPassword);
//			client.OnReceiveEventTable += Client_OnReceiveEventTable;
//			client.OnAuthenticated += Client_OnAuthenticated;
			
//			await client.Authenticate(handler);
//			var request1 = WebserviceRequest<string>.Create(
//			   WebserviceRequestConfig.Auth(),
//			   "enablebinstatusupdate",
//			   "jdev/sps/enablebinstatusupdate"
//			   );
//			var response = await client.SendWebserviceAndWait(request1);

//			string result = response?.Value;
//			result.Should().NotBeNull();

			

//			receivedUpdates.WaitOne(TimeSpan.FromSeconds(30));

//			for (int i = 0; i < 100; i++) {
//				var kr = WebserviceRequest<string>.Create(
//				WebserviceRequestConfig.Auth(),
//				"keepalive",
//				"keepalive"
//				);
//				var r = await client.SendWebserviceAndWait(kr);
//				Thread.Sleep(1000);

//			}
			

//			receivedMessage.WaitOne(TimeSpan.FromSeconds(30));
//			await handler.KillToken();
//		}


//	}

//	private void Client_OnAuthenticated(object sender, ConnectionAuthenticatedEventArgs e) {
//		this.OutputHelper.WriteLine("Successfully authenticated!");

//	}

//	private void Client_OnReceiveEventTable(object sender, EventStatesParsedEventArgs e) {
//		foreach (EventState state in e.States) {
//			this.OutputHelper.WriteLine(state.ToString());
//		}

//		tableEvents++;

//		if (tableEvents == 4) {
//			receivedUpdates.Set();
//		}
//	}

//	public WebsocketWebserviceClientTests(ITestOutputHelper output)
//		: base(output) {
//	}
//}