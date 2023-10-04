using System.Threading.Tasks;

namespace Loxone.Communicator;

public interface IWebserviceClient {
	Session Session { get; }

	Task<LoxoneResponseMessage> SendWebserviceAndWait(LoxoneRequest request);
	Task<LoxoneMessageLoadContentWitControl<T>> SendWebserviceAndWait<T>(LoxoneRequest<T> request);

	Task SendWebservice(LoxoneRequest request);

	Task<LoxoneResponseMessage> SendApiRequest(LoxoneRequest request);
	Task<LoxoneMessageLoadContentWitControl<T>> SendApiRequest<T>(LoxoneRequest<T> request);
}