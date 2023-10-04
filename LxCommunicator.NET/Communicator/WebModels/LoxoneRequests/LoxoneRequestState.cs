namespace Loxone.Communicator {
	public enum LoxoneRequestState {
		None,
		Timeouted,
		RespondedInTime,
		Valid,
		NotValidNullContent,
		NotValidWrongHttpStatusCode
	}
}