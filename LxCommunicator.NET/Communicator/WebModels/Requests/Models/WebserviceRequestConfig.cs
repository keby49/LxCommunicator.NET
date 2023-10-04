namespace Loxone.Communicator {
	public class WebserviceRequestConfig {
		private const int DefaultTimeout = 5000;

		/// <summary>
		/// Whether the command requires token authentication or not
		/// </summary>
		public bool NeedAuthentication { get; set; } = true;

		/// <summary>
		/// How the command should be encrypted
		/// </summary>
		public MessageEncryptionType Encryption { get; set; }

		/// <summary>
		/// The timeout how long the miniserver may take to respond
		/// </summary>
		public int Timeout { get; set; } = DefaultTimeout;
		//private const int DefaultTimeout = 5000 * 20;

		public static WebserviceRequestConfig Auth(int timeout = DefaultTimeout) => new WebserviceRequestConfig {
			Encryption = MessageEncryptionType.None, Timeout = timeout, NeedAuthentication = true,
		};

		public static WebserviceRequestConfig AuthWithEncryptionRequest(int timeout = DefaultTimeout) => new WebserviceRequestConfig {
			Encryption = MessageEncryptionType.Request, Timeout = timeout, NeedAuthentication = true,
		};

		public static WebserviceRequestConfig AuthWithEncryptionRequestAndResponse(int timeout = DefaultTimeout) => new WebserviceRequestConfig {
			Encryption = MessageEncryptionType.RequestAndResponse, Timeout = timeout, NeedAuthentication = true,
		};

		public static WebserviceRequestConfig NoAuth(int timeout = DefaultTimeout) => new WebserviceRequestConfig {
			Encryption = MessageEncryptionType.None, Timeout = timeout, NeedAuthentication = false,
		};

		public static WebserviceRequestConfig NoAuthWithEncryptionRequest(int timeout = DefaultTimeout) => new WebserviceRequestConfig {
			Encryption = MessageEncryptionType.Request, Timeout = timeout, NeedAuthentication = false,
		};

		public static WebserviceRequestConfig NoAuthWithEncryptionRequestAndResponse(int timeout = DefaultTimeout) => new WebserviceRequestConfig {
			Encryption = MessageEncryptionType.RequestAndResponse, Timeout = timeout, NeedAuthentication = false,
		};
	}
}