using Loxone.Communicator;
using System;
using System.Threading.Tasks;

namespace LxCommunicator.NET.Example.Http {
    internal class Program {
        private static HttpWebserviceClient client;


		private static ConnectionConfiguration GetConfig() => new ConnectionConfiguration(
		"testminiserver.loxone.com",
		7777,
		2,
			"098802e1-02b4-603c-ffffeee000d80cfd",
			"LxCommunicator.NET.Websocket") { };

		private static HttpWebserviceClient GetClient() {
			return new HttpWebserviceClient(GetConfig());
		}

		private static async Task Main(string[] args) {
            using (client = GetClient()) {
                using (TokenHandler handler = new TokenHandler(client, "app")) {
                    handler.SetPassword("LoxLIVEpasswordTest");
                    await client.Authenticate(handler);
                    string version = (await client.SendWebserviceAndWait(new WebserviceRequest<string>("jdev/cfg/version", EncryptionType.Request))).Value;
                    Console.WriteLine($"Version: {version}");
                    await handler.KillToken();
                    Console.ReadLine();
                }
            }
        }
    }
}