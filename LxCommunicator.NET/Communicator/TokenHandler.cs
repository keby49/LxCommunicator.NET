using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Loxone.Communicator {
	/// <summary>
	/// A handler used for managing the Token. Can create, renew and kill tokens.
	/// When enabled, the <see cref="TokenHandler"/> also updates the token automatically
	/// </summary>
	public class TokenHandler : IDisposable, ITokenHandler {
		/// <summary>
		/// The cancellationSource for cancelling autoRenew
		/// </summary>
		private readonly CancellationTokenSource CancellationSource = new CancellationTokenSource();

		/// <summary>
		/// Whether the can tokenHandler renew the token automatically
		/// </summary>
		private bool NeedRenewToken = true;

		/// <summary>
		/// Initialises a new tokenHandler object.
		/// </summary>
		/// <param name="client">The webserviceClient that should be used for communication</param>
		/// <param name="user">The username of the user</param>
		/// <param name="token">The token object that should be used (optional)</param>
		/// <param name="canRenewToken">Whether or not the tokenHandler should be allowed to renew the token automatically (true if not set!)</param>
		public TokenHandler(WebserviceClient client, string user, Token token = null, bool canRenewToken = true) {
			WsClient = client;
			ApiClient = client;
			Username = user;
			Token = token;
			NeedRenewToken = canRenewToken;
			RenewTokenOrScheduleIfNeeded().Wait();
		}

		/// <summary>
		/// Event, fired when the token updates.
		/// Contains the tokenHandler with the updated token in the eventArgs
		/// </summary>
		public event EventHandler<ConnectionAuthenticatedEventArgs> OnUpdateToken;

		/// <summary>
		/// The webserviceClient used for communication with the miniserver
		/// </summary>
		public IWebserviceClient WsClient { get; private set; }

		public IWebserviceClient ApiClient { get; private set; }

		/// <summary>
		/// The token used for authentication
		/// </summary>
		public Token Token { get; private set; }

		/// <summary>
		/// The username of the current user
		/// </summary>
		public string Username { get; private set; }

		/// <summary>
		/// Whether the tokenHandler is allowed to renew the token automatically
		/// </summary>
		public bool CanRenewToken {
			get => NeedRenewToken;
			set {
				NeedRenewToken = value;
				RenewTokenOrScheduleIfNeeded().Wait();
			}
		}

		/// <summary>
		/// The password f the current user
		/// </summary>
		private string Password { get; set; }

		/// <summary>
		/// Disposes the current TokenHandler
		/// </summary>
		public void Dispose() {
			CancellationSource.Cancel();
		}

		/// <summary>
		/// Sets the password required for authentication
		/// </summary>
		/// <param name="password">The password</param>
		public void SetPassword(string password) {
			Password = password;
		}

		/// <summary>
		/// Request a new Token from the Miniserver
		/// </summary>
		/// <returns>Wheter acquiring the new Token succeeded or not</returns>
		public async Task<bool> RequestNewToken() {
			if (Password == null) {
				throw new WebserviceException("Password is not set!");
			}

			UserKey userKey = await WsClient.Session.GetUserKey(Username);
			HashAlgorithm sha = userKey.GetHashAlgorithm();
			HMAC hmacSha = userKey.GetHMAC();
			string pwHash = Cryptography.GetHexFromByteArray(sha.ComputeHash(Encoding.UTF8.GetBytes($"{Password}:{userKey.Salt}"))).ToUpper();
			hmacSha.Key = Cryptography.GetByteArrayFromHex(userKey.Key);
			string hash = Cryptography.GetHexFromByteArray(hmacSha.ComputeHash(Encoding.UTF8.GetBytes($"{Username}:{pwHash}")));

			var request = WebserviceRequest<Token>.Create(
				WebserviceRequestConfig.NoAuthWithEncryptionRequestAndResponse(),
				nameof(RequestNewToken),
				$"jdev/sys/getjwt/{hash}/{Username}/{WsClient.Session.TokenPermission}/{WsClient.Session.DeviceUuid}/{WsClient.Session.DeviceInfo}"
			);

			LoxoneMessageLoadContentWitControl<Token> response = await WsClient.SendWebserviceAndWait(request);

			Token = response.Value;

			await RenewTokenOrScheduleIfNeeded();
			if (Token != null && Token.JsonWebToken != default && Token.Key != default && Token.ValidUntil != default) {
				return true;
			}
			else {
				return false;
			}
		}

		/// <summary>
		/// Renew the current Token
		/// </summary>
		public async Task RenewToken() {
			if (WsClient is LoxoneRestServiceClient) {
				throw new WebserviceException("Renewing Tokens is not supported with HTTP!");
			}

			string hash = await GetTokenHash();
			var request = WebserviceRequest<Token>.Create(
				WebserviceRequestConfig.AuthWithEncryptionRequestAndResponse(),
				nameof(RenewToken),
				$"jdev/sys/refreshjwt/{hash}/{Username}"
			);

			var response = await WsClient.SendWebserviceAndWait(request);
			Token = response.Value;
			OnUpdateToken?.Invoke(this, new ConnectionAuthenticatedEventArgs(this));
			await RenewTokenOrScheduleIfNeeded();
		}

		/// <summary>
		/// Kill the current Token
		/// </summary>
		public async Task KillToken() {
			string hash = await GetTokenHash();
			try {
				var request = WebserviceRequest<object>.Create(
					WebserviceRequestConfig.AuthWithEncryptionRequestAndResponse(),
					nameof(KillToken),
					$"jdev/sys/killtoken/{hash}/{Username}",
					r => r.Config.Timeout = 0
				);

				var response = await WsClient.SendWebserviceAndWait(request);
			}
			catch {
			}

			Token = null;
			CancellationSource.Cancel();
		}

		/// <summary>
		/// Gets the tokenHash required for authentication
		/// </summary>
		/// <returns></returns>
		public async Task<string> GetTokenHash() {
			UserKey userKey = await WsClient.Session.GetUserKey(Username);
			HMAC hmac = userKey.GetHMAC();
			hmac.Key = Cryptography.GetByteArrayFromHex(userKey.Key);
			return Cryptography.GetHexFromByteArray(hmac.ComputeHash(Encoding.UTF8.GetBytes(Token.JsonWebToken))).ToLower();
		}

		public async Task CleanToken() {
			Token = null;
		}

		/// <summary>
		/// Checks if the token needs to be renewed and renews it if needed.
		/// </summary>
		private async Task RenewTokenOrScheduleIfNeeded() {
			CancellationSource.Cancel();
			if (Token != null && NeedRenewToken) {
				double seconds = (Token.ValidUntil - DateTime.Now).TotalSeconds * 0.9;
				if (seconds < 0) {
					await RenewToken();
				}
				else {
					var delayMiliseconds = Convert.ToInt32(Math.Min(seconds * 1000, int.MaxValue));
					Task task = Task
						.Delay(delayMiliseconds, CancellationSource.Token)
						.ContinueWith(
							async (t) => {
								await RenewTokenOrScheduleIfNeeded();
							},
							CancellationSource.Token
						);
				}
			}
		}
	}
}