using CheapHelpers.Models.Contracts;

namespace CheapHelpers.Services.Email.Helpers;

public class PasswordResetTemplateData : BaseEmailTemplateData
{
    public override string Subject => "Forgotten password";

    public string ResetLink { get; set; } = "";
}
