using System.Threading.Tasks;

namespace CheapHelpers.Services
{
	public interface ISmsService
	{
		Task Send(string number, string body);
	}
}