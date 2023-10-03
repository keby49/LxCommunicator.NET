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
			var versionRequest = WebserviceRequest<string>.Create(
				WebserviceRequestConfig.AuthWithEncryptionRequest(),
				"GetVersion",
				"jdev/cfg/version"
				);
			string version = (await loxoneWebsocketClient.SendWebserviceAndWait(versionRequest)).Value;

			Console.WriteLine($"Version: {version}");
			Thread.Sleep(100);

			versionRequest = WebserviceRequest<string>.Create(
				WebserviceRequestConfig.AuthWithEncryptionRequest(),
				"GetVersion",
				"jdev/cfg/version"
				);
			version = (await loxoneWebsocketClient.SendWebserviceAndWait(versionRequest)).Value;
			Console.WriteLine($"Version: {version}");

			Thread.Sleep(1000);
			
			var keepaliveRequest = WebserviceRequest<string>.Create(
				WebserviceRequestConfig.AuthWithEncryptionRequestAndResponse(),
				"keepalive",
				"keepalive"
				);

			await loxoneWebsocketClient.SendWebservice(keepaliveRequest);
			Console.WriteLine($"keepalive: ok");

			Thread.Sleep(1000);
			keepaliveRequest = WebserviceRequest<string>.Create(
				WebserviceRequestConfig.AuthWithEncryptionRequestAndResponse(),
				"keepalive",
				"keepalive"
				);

			loxoneWebsocketClient.SendWebservice(keepaliveRequest);
			Console.WriteLine($"keepalive: ok");

			Thread.Sleep(1000);
			
			keepaliveRequest = WebserviceRequest<string>.Create(
				WebserviceRequestConfig.AuthWithEncryptionRequestAndResponse(),
				"keepalive",
				"keepalive"
				);

			loxoneWebsocketClient.SendWebservice(keepaliveRequest);
			Console.WriteLine($"keepalive: ok");


			Thread.Sleep(100);

			await handler.KillToken();
		}
	}

}