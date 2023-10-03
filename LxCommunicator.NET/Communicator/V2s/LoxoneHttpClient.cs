using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Loxone.Communicator;

internal class LoxoneHttpClient {
	private static HttpWebserviceClient client;


	private static ConnectionConfiguration GetConfig() => new ConnectionConfiguration(
	"testminiserver.loxone.com",
	7777,
	2,
		"098802e1-02b4-603c-ffffeee000d80cfd",
		"LxCommunicator.NET.Websocket") { };

	private static LoxoneUser GetUser() => new LoxoneUser { 
		UserName = "lan",
		UserPassword = "JQ9Hsa9tP5xtnW",
	};

	private static HttpWebserviceClient GetClient() {
		return new HttpWebserviceClient(GetConfig());
	}

	//private static async Task Main(string[] args) {
	//	using (client = GetClient()) {
	//		using (TokenHandler handler = new TokenHandler(client, "app")) {
	//			handler.SetPassword("LoxLIVEpasswordTest");
	//			await client.Authenticate(handler);
	//			var request = WebserviceRequest<string>.Create(
	//				WebserviceRequestConfig.AuthWithEncryptionRequest(),
	//				nameof(this.NONE),
	//				"jdev/cfg/version"
	//			);

	//			var response = await client.SendWebserviceAndWait(request);
	//			string version = response.Value;
	//			Console.WriteLine($"Version: {version}");
	//			await handler.KillToken();
	//			Console.ReadLine();
	//		}
	//	}
	//}
}
