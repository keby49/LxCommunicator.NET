using FluentAssertions;
using Loxone.Communicator;
using Loxone.Communicator.Events;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace LxCommunicator.NET.Tests;

public class LoxoneWebsocketClientTests : WebsocketClientTestsV3Base {

	public LoxoneWebsocketClientTests(ITestOutputHelper output)
		: base(output) {
	}

	private LoxoneWebsocketClient GetWebsockerClient() {
		return new LoxoneWebsocketClient(this.GetConfig());
	}

	static ManualResetEvent receivedUpdates = new ManualResetEvent(false);

	static ManualResetEvent receivedMessage = new ManualResetEvent(false);
	static int tableEvents = 0;
	[Fact]
	public async Task Test1() {
		var wsClient = GetWebsockerClient();
		var apiClient = GetHttpClient();
		var user = GetUser();

		receivedUpdates = new ManualResetEvent(false);

		TokenHandlerV3 handler = null;

		tableEvents = 0;
		using (wsClient) {
			handler = new TokenHandlerV3(apiClient, wsClient, user.UserName);

			handler.SetPassword(user.UserPassword);
			wsClient.OnReceiveEventTable += Client_OnReceiveEventTable;
			wsClient.OnAuthenticated += Client_OnAuthenticated;
			
			await wsClient.Authenticate(handler);
			wsClient.SendWebserviceAndWait(new WebserviceRequest<string>("jdev/sps/enablebinstatusupdate", EncryptionType.None));

			//receivedUpdates.WaitOne(TimeSpan.FromSeconds(30));
			Thread.Sleep(3000);

			for (int i = 0; i < 100; i++) {
				var r = await wsClient.SendWebserviceAndWait(new WebserviceRequest("keepalive", EncryptionType.None));
				Thread.Sleep(1000);
			}
			
			//receivedMessage.WaitOne(TimeSpan.FromSeconds(30));

			Thread.Sleep(3000);
			await handler.KillToken();
		}


	}

	private void Client_OnAuthenticated(object sender, ConnectionAuthenticatedEventArgs e) {
		this.OutputHelper.WriteLine("Successfully authenticated!");

	}

	private void Client_OnReceiveEventTable(object sender, EventStatesParsedEventArgs e) {
		foreach (EventState state in e.States) {
			this.OutputHelper.WriteLine(state.ToString());
		}

		tableEvents++;

		if (tableEvents == 4) {
			receivedUpdates.Set();
		}
	}
}