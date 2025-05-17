using System.Threading.Tasks;

namespace CheapHelpers.WebServices
{
    public interface IWebServiceBase
    {
        Task StartAsync();
        Task StopAsync();
        Task DisposeAsync();
    }
}
