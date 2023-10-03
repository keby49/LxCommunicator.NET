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
	public async Task keepalive() {
		var wsClient = GetWebsockerClient();
		var apiClient = GetHttpClient();
		var user = GetUser();

		receivedUpdates = new ManualResetEvent(false);

		TokenHandlerV3 handler = null;

		tableEvents = 0;
		using (wsClient) {
			handler = new TokenHandlerV3(wsClient, user.UserName);

			handler.SetPassword(user.UserPassword);

			await wsClient.StartAndConnection(handler);

			await wsClient.EnablebInStatusUpdate();

			//receivedUpdates.WaitOne(TimeSpan.FromSeconds(30));
			Thread.Sleep(3000);

			for (int i = 0; i < 100; i++) {
				var kr = WebserviceRequest<string>.Create(
				WebserviceRequestConfig.Auth(),
				"keepalive",
				"keepalive"
				);
				var r = await wsClient.SendWebserviceAndWait(kr);
				Thread.Sleep(1000);
			}

			//receivedMessage.WaitOne(TimeSpan.FromSeconds(30));

			Thread.Sleep(3000);
			await handler.KillToken();
		}


	}

	[Fact]
	public async Task GetLoxoneStructureAsJson() {
		var wsClient = GetWebsockerClient();
		var apiClient = GetHttpClient();
		var user = GetUser();

		receivedUpdates = new ManualResetEvent(false);

		TokenHandlerV3 handler = null;

		tableEvents = 0;
		using (wsClient) {
			handler = new TokenHandlerV3(wsClient, user.UserName);

			handler.SetPassword(user.UserPassword);

			await wsClient.StartAndConnection(handler);

			await wsClient.EnablebInStatusUpdate();
			Thread.Sleep(1000);

			var fileCotent = await wsClient.GetLoxoneStructureAsJson();

			fileCotent.Should().NotBeNull();

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