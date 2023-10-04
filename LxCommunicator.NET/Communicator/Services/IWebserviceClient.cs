using System.Threading.Tasks;

namespace Loxone.Communicator;

public interface IWebserviceClient {
	Session Session { get; }

	Task<LoxoneResponseMessage> SendWebserviceAndWait(WebserviceRequest request);
	Task<LoxoneMessageLoadContentWitControl<T>> SendWebserviceAndWait<T>(WebserviceRequest<T> request);

	Task SendWebservice(WebserviceRequest request);

	Task<LoxoneResponseMessage> SendApiRequest(WebserviceRequest request);
	Task<LoxoneMessageLoadContentWitControl<T>> SendApiRequest<T>(WebserviceRequest<T> request);
}