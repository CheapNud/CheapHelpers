using CheapHelpers.Blazor.Helpers;
using CheapHelpers.Blazor.Pages.Account;
using CheapHelpers.Blazor.Shared;
using FluentValidation;
using static CheapHelpers.Blazor.Pages.Account.ResetPassword;
using static CheapHelpers.Blazor.Pages.Account.SetPassword;

namespace CheapHelpers.Blazor.Pages.Account
{
    public class SetPasswordValidator : BaseValidator<SetPasswordViewModel>
    {
        public SetPasswordValidator()
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("Password is required!")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long")
                .Matches(@"[A-Z]")
                .WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]")
                .WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]")
                .WithMessage("Password must contain at least one number")
                .Matches(@"[\W]")
                .WithMessage("Password must contain at least one special character");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithMessage("Password confirmation is required!")
                .Equal(x => x.NewPassword)
                .WithMessage("The password and confirmation password do not match");
        }
    }
}