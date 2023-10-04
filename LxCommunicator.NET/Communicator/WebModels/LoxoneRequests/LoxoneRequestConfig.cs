namespace Loxone.Communicator {
	public class LoxoneRequestConfig {
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

		public static LoxoneRequestConfig Auth(int timeout = DefaultTimeout) => new LoxoneRequestConfig {
			Encryption = MessageEncryptionType.None, Timeout = timeout, NeedAuthentication = true,
		};

		public static LoxoneRequestConfig AuthWithEncryptionRequest(int timeout = DefaultTimeout) => new LoxoneRequestConfig {
			Encryption = MessageEncryptionType.Request, Timeout = timeout, NeedAuthentication = true,
		};

		public static LoxoneRequestConfig AuthWithEncryptionRequestAndResponse(int timeout = DefaultTimeout) => new LoxoneRequestConfig {
			Encryption = MessageEncryptionType.RequestAndResponse, Timeout = timeout, NeedAuthentication = true,
		};

		public static LoxoneRequestConfig NoAuth(int timeout = DefaultTimeout) => new LoxoneRequestConfig {
			Encryption = MessageEncryptionType.None, Timeout = timeout, NeedAuthentication = false,
		};

		public static LoxoneRequestConfig NoAuthWithEncryptionRequest(int timeout = DefaultTimeout) => new LoxoneRequestConfig {
			Encryption = MessageEncryptionType.Request, Timeout = timeout, NeedAuthentication = false,
		};

		public static LoxoneRequestConfig NoAuthWithEncryptionRequestAndResponse(int timeout = DefaultTimeout) => new LoxoneRequestConfig {
			Encryption = MessageEncryptionType.RequestAndResponse, Timeout = timeout, NeedAuthentication = false,
		};
	}
}