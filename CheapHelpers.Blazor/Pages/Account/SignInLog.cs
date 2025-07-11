
using Microsoft.AspNetCore.Identity;

namespace CheapHelpers.Blazor.Pages.Account
{
    internal class SignInLog
    {
        public DateTime LogTime { get; set; }
        public string IpAddress { get; set; }
        public IdentityUser IdentityUser { get; set; }
        public bool Success { get; set; }
        public string LogDescription { get; set; }
    }
}