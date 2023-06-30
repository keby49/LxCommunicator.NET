using FluentAssertions;
using Loxone.Communicator;
using Loxone.Communicator.Events;

namespace LxCommunicator.NET.Tests;

public class WebsocketWebserviceClientTests {
	[Fact]
	public async Task Test1() {
		var client = new WebsocketWebserviceClient(
			"192-168-50-50.504f94a181b0.dyndns.loxonecloud.com",
			80,
			2,
			"098802e1-02b4-603c-ffffeee000d80cfd",
			"LxCommunicator.NET.Websocket");

		using (client) {
			using (TokenHandler handler = new TokenHandler(client, "lan")) {
				handler.SetPassword("JQ9Hsa9tP5xtnW");
				client.OnReceiveEventTable += Client_OnReceiveEventTable;
				client.OnAuthenticated += Client_OnAuthenticated;
				await client.Authenticate(handler);
				var response = await client.SendWebservice(new WebserviceRequest<string>("jdev/sps/enablebinstatusupdate", EncryptionType.None));

				string result = response?.Value;

				result.Should().NotBeNull();

				await handler.KillToken();
			}
		}


	}

	private static void Client_OnAuthenticated(object sender, ConnectionAuthenticatedEventArgs e) {
		Console.WriteLine("Successfully authenticated!");

	}

	private static void Client_OnReceiveEventTable(object sender, EventStatesParsedEventArgs e) {
		foreach (EventState state in e.States) {
			Console.WriteLine(state.ToString());
		}
	}
}