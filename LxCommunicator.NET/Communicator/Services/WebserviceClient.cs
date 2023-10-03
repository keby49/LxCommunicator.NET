using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Loxone.Communicator {
	/// <summary>
	/// Client to handle Webservices to Loxone Minsierver. Derive from <see cref="WebserviceClient"/> to implement your own Client
	/// </summary>
	public abstract class WebserviceClient : IDisposable, IWebserviceClient {
		protected WebserviceClient(ConnectionConfiguration connectionConfiguration) {
			this.ConnectionConfiguration = connectionConfiguration;
		}

		/// <summary>
		/// The tokenhandler that should be used for managing the token
		/// </summary>
		internal ITokenHandler TokenHandler { get; set; }
		/// <summary>
		/// The session object used for storing information about the connection
		/// </summary>
		public Session Session { get; internal set; }
		public ConnectionConfiguration ConnectionConfiguration { get; }

		/// <summary>
		/// Establish an authenticated connection to the miniserver
		/// </summary>
		/// <param name="handler">The tokenhandler that should be used</param>
		public abstract Task Authenticate(ITokenHandler handler);

		/// <summary>
		/// Sends a webservice to the miniserver
		/// </summary>
		/// <typeparam name="T">The object that should be returned in Value</typeparam>
		/// <param name="request">The Request that should be sent</param>
		/// <returns>The Response the miniserver returns</returns>
		public async Task<LoxoneMessageLoadContentWitControl<T>> SendWebserviceAndWait<T>(WebserviceRequest<T> request) {
			var response = (LoxoneResponseMessageWithContainer)await SendWebserviceAndWait((WebserviceRequest)request);
			return response?.TryGetAsWebserviceContent<T>();
		}

		/// <summary>
		/// Sends a webservice to the miniserver
		/// </summary>
		/// <param name="request">The Request that should be sent</param>
		/// <returns>The Response the miniserver returns</returns>
		public virtual async Task<LoxoneResponseMessage> SendWebserviceAndWait(WebserviceRequest request) {
			return await Task.FromResult<LoxoneResponseMessage>(null);
		}

		/// <summary>
		/// Disposes the WebserviceClient
		/// </summary>
		public virtual void Dispose() {
			TokenHandler?.Dispose();
		}

		public virtual async Task<LoxoneResponseMessage> SendApiRequest(WebserviceRequest request) {
			return await Task.FromResult<LoxoneResponseMessage>(null);
		}

		public async Task<LoxoneMessageLoadContentWitControl<T>> SendApiRequest<T>(WebserviceRequest<T> request) {
			var response = (LoxoneResponseMessageWithContainer)await SendApiRequest((WebserviceRequest)request);
			return response?.TryGetAsWebserviceContent<T>();
		}

		public Task SendWebservice(WebserviceRequest request) {
			throw new NotImplementedException();
		}
	}
}
