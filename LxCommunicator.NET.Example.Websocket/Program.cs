using Loxone.Communicator;
using Loxone.Communicator.Events;
using System;
using System.Threading.Tasks;

namespace LxCommunicator.NET.Example.Websocket {
    internal class Program {
        private static WebsocketWebserviceClient client;

		static string password = "JQ9Hsa9tP5xtnW";
		static string user = "lan";
		static string ip = "192-168-50-50.504f94a181b0.dyndns.loxonecloud.com";
		static int port = 80;

		private static async Task Main(string[] args) {
            using (client = new WebsocketWebserviceClient(ip, port, 2, "098802e1-02b4-603c-ffffeee000d80cfd", "LxCommunicator.NET.Websocket")) {
                using (TokenHandler handler = new TokenHandler(client, user)) {
                    handler.SetPassword(password);
                    client.OnReceiveEventTable += Client_OnReceiveEventTable;
                    client.OnAuthenticated += Client_OnAuthenticated;
                    await client.Authenticate(handler);
                    string result = (await client.SendWebservice(new WebserviceRequest<string>("jdev/sps/enablebinstatusupdate", EncryptionType.None))).Value;
                    Console.ReadLine();
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
}