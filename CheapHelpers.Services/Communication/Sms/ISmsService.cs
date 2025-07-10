namespace CheapHelpers.Services.Communication.Sms
{
    public interface ISmsService
    {
        Task Send(string number, string body);
    }
}