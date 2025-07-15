using CheapHelpers.Blazor.Helpers;
using CheapHelpers.Blazor.Shared;
using MudBlazor;

namespace CheapHelpers.Blazor.Services
{

    public class SmsDialogService(IDialogService dialogService)
    {
        public async Task<bool> SendSimpleSmsAsync(IEnumerable<string> phoneNumbers, string? defaultMessage = null)
        {
            var recipients = phoneNumbers.Select(phone => new SimplePhoneRecipient { PhoneNumber = phone }).ToList();

            var parameters = new DialogParameters
            {
                ["Recipients"] = recipients,
                ["Message"] = defaultMessage ?? string.Empty
            };

            var dialog = await dialogService.ShowAsync<SmsDialog<SimplePhoneRecipient>>("Send SMS", parameters);
            var result = await dialog.Result;

            return !result.Canceled;
        }
    }
}