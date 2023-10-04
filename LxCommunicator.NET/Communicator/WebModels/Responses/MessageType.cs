namespace Loxone.Communicator {
	/// <summary>
	/// Possible types for a message
	/// </summary>
	public enum MessageType : byte {
		Text = 0,
		Binary = 1,
		EventTableValueStates = 2,
		EventTableTextStates = 3,
		EventTableDaytimerStates = 4,
		OutOfService = 5,
		KeepAlive = 6,
		EventTableWeatherStates = 7
	}
}