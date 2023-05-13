using Loxone.Communicator;
using Loxone.Communicator.Events;
using System;
using System.Threading.Tasks;

namespace LxCommunicator.NET.Example.Websocket {
    internal class Program {
        private static WebsocketWebserviceClient client;

        private static async Task Main(string[] args) {
            using (client = new WebsocketWebserviceClient(
				"192-168-50-50.504f94a181b0.dyndns.loxonecloud.com", 
				80, 
				2, 
				"098802e1-02b4-603c-ffffeee000d80cfd", 
				"LxCommunicator.NET.Websocket")) {
                using (TokenHandler handler = new TokenHandler(client, "lan")) {
                    handler.SetPassword("JQ9Hsa9tP5xtnW");
                    client.OnReceiveEventTable += Client_OnReceiveEventTable;
                    client.OnAuthenticated += Client_OnAuthenticated;
                    await client.Authenticate(handler);
					var response = await client.SendWebservice(new WebserviceRequest<string>("jdev/sps/enablebinstatusupdate", EncryptionType.None));

					string result = response.Value;
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