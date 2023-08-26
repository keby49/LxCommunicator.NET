using System.Threading.Tasks;

namespace Loxone.Communicator;
public interface IWebserviceClient {

	Session Session { get; }

	Task<WebserviceResponse> SendWebserviceAndWait(WebserviceRequest request);
	Task<WebserviceContent<T>> SendWebserviceAndWait<T>(WebserviceRequest<T> request);

	Task SendWebservice(WebserviceRequest request);

	Task<WebserviceResponse> SendApiRequest(WebserviceRequest request);
	Task<WebserviceContent<T>> SendApiRequest<T>(WebserviceRequest<T> request);
}