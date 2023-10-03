namespace Loxone.Communicator {

	public enum WebserviceRequestState {
		None,
		Timeouted,
		RespondedInTime,
		Valid,
		NotValidNullContent,
		NotValidWrongHttpStatusCode
	}
}
