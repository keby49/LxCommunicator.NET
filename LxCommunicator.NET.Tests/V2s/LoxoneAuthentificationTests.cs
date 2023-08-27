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

namespace LxCommunicator.NET.Tests;

public class LoxoneAuthentificationTests : WebsocketClientTestsV3Base {
	public LoxoneAuthentificationTests(ITestOutputHelper output)
		: base(output) {
	}

	[Fact]
	public async Task OnStarting_ShouldGetInfoResponse() {
		var user = GetUser();
		using (var loxoneWebsocketClient = this.GetLoxoneWebsocketClient())
		using (TokenHandlerV3 handler = new TokenHandlerV3(loxoneWebsocketClient, user.UserName)) {
			handler.SetPassword(user.UserPassword);
			await loxoneWebsocketClient.StartAndConnection(handler);
			var versionRequest = new WebserviceRequest<string>("jdev/cfg/version", EncryptionType.Request);
			string version = (await loxoneWebsocketClient.SendWebserviceAndWait(versionRequest)).Value;
			Console.WriteLine($"Version: {version}");
			Thread.Sleep(100);
			versionRequest = new WebserviceRequest<string>("jdev/cfg/version", EncryptionType.Request);
			version = (await loxoneWebsocketClient.SendWebserviceAndWait(versionRequest)).Value;
			Console.WriteLine($"Version: {version}");

			Thread.Sleep(1000);
			var keepaliveRequest = new WebserviceRequest<string>("keepalive", EncryptionType.RequestAndResponse);
			await loxoneWebsocketClient.SendWebservice(keepaliveRequest);
			Console.WriteLine($"keepalive: ok");

			Thread.Sleep(1000);
			keepaliveRequest = new WebserviceRequest<string>("keepalive", EncryptionType.RequestAndResponse);
			loxoneWebsocketClient.SendWebservice(keepaliveRequest);
			Console.WriteLine($"keepalive: ok");

			Thread.Sleep(1000);
			keepaliveRequest = new WebserviceRequest<string>("keepalive", EncryptionType.RequestAndResponse);
			loxoneWebsocketClient.SendWebservice(keepaliveRequest);
			Console.WriteLine($"keepalive: ok");


			Thread.Sleep(100);

			await handler.KillToken();
		}
	}

}