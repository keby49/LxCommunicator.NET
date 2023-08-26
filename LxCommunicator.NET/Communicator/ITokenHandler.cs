using System;
using System.Threading.Tasks;

namespace Loxone.Communicator;

public interface ITokenHandler
{
	/// <summary>
	/// The webserviceClient used for communication with the miniserver
	/// </summary>
	IWebserviceClient WsClient { get; }

	IWebserviceClient ApiClient { get; }

	/// <summary>
	/// The token used for authentication
	/// </summary>
	Token Token { get; }

	/// <summary>
	/// The username of the current user
	/// </summary>
	string Username { get; }

	/// <summary>
	/// Whether the tokenHandler is allowed to renew the token automatically
	/// </summary>
	bool CanRenewToken { get; set; }

	/// <summary>
	/// Event, fired when the token updates.
	/// Contains the tokenHandler with the updated token in the eventArgs
	/// </summary>
	event EventHandler<ConnectionAuthenticatedEventArgs> OnUpdateToken;

	/// <summary>
	/// Disposes the current TokenHandler
	/// </summary>
	void Dispose();

	/// <summary>
	/// Sets the password required for authentication
	/// </summary>
	/// <param name="password">The password</param>
	void SetPassword(string password);

	/// <summary>
	/// Request a new Token from the Miniserver
	/// </summary>
	/// <returns>Wheter acquiring the new Token succeeded or not</returns>
	Task<bool> RequestNewToken();

	/// <summary>
	/// Renew the current Token
	/// </summary>
	Task RenewToken();

	/// <summary>
	/// Kill the current Token
	/// </summary>
	Task KillToken();

	/// <summary>
	/// Gets the tokenHash required for authentication
	/// </summary>
	/// <returns></returns>
	Task<string> GetTokenHash();
}