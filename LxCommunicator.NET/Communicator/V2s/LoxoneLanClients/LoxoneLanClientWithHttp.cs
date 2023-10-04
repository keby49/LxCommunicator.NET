namespace Loxone.Communicator {
	public abstract class LoxoneLanClientWithHttp : LoxoneLanClient {
		protected LoxoneLanClientWithHttp(ConnectionConfiguration connectionConfiguration)
			: base(connectionConfiguration) {
			Session = new Session(null, ConnectionConfiguration.SessionConfiguration);
			HttpWebserviceClient = new LoxoneRestServiceClient(connectionConfiguration, Session);
			Session.Client = HttpWebserviceClient;
		}

		/// <summary>
		/// The httpClient used for sending the messages
		/// </summary>
		public LoxoneRestServiceClient HttpWebserviceClient { get; set; }

		public override void Dispose() {
			base.Dispose();
			HttpWebserviceClient?.Dispose();
		}
	}
}