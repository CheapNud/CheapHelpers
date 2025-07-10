using System.Diagnostics;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace CheapHelpers.Services
{
    //TODO: expand
    public class TwilioSmsService : ISmsService
    {
        public async Task Send(string number, string body)
        {
            try
            {
                const string accountSid = "accountsid";
                const string authToken = "authtoken";
                TwilioClient.Init(accountSid, authToken);
                var message = await MessageResource.CreateAsync(
                    body: body,
                    from: new Twilio.Types.PhoneNumber("+12513130246"),
                    to: new Twilio.Types.PhoneNumber(number)
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }
    }
}