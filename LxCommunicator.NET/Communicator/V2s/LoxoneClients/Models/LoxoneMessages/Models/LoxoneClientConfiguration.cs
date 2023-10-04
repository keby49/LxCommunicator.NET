namespace Loxone.Communicator {
	public class LoxoneClientConfiguration {
		public ConnectionConfiguration ConnectionConfiguration { get; set; }

		public LoxoneUser LoxoneUser { get; set; }

		public bool LogMessages { get; set; } = true;
	}
}