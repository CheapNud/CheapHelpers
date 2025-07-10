using CheapHelpers.Models.Contracts;

namespace CheapHelpers.Services.TemplateData
{
    // Example template data class
    public class WelcomeEmailTemplateData : BaseEmailTemplateData
    {
        public override string Subject => $"Welcome to {BrandName}, {UserName}!";

        public string UserName { get; set; }
        public string BrandName { get; set; }
        public string ActivationLink { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}