using CheapHelpers.Models.Contracts;

namespace CheapHelpers.Services.Email.Helpers;

public class EmailConfirmationTemplateData : BaseEmailTemplateData
{
    public override string Subject => "Confirm your email";

    public string ConfirmationLink { get; set; } = "";
}
