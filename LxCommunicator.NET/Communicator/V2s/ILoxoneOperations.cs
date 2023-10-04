using System.Threading.Tasks;

namespace Loxone.Communicator;

public interface ILoxoneOperations {
	Task<string> GetTextFile(string fileName);

	Task<string> GetLoxoneStructureAsJson();

	Task<bool> EnablebInStatusUpdate();

	Task SendKeepalive();
}