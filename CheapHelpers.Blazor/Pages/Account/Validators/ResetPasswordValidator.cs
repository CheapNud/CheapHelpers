using CheapHelpers.Blazor.Helpers;
using FluentValidation;

namespace CheapHelpers.Blazor.Pages.Account.Validators
{
    public class ResetPasswordValidator : BaseValidator<ResetPassword.ResetPasswordViewModel>
    {
        public ResetPasswordValidator()
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("Password is empty!")
                .MinimumLength(8)
                .WithMessage("minimum 8 characters");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithMessage("Password is empty!")
                .Must((a, b) => CheckEqual(a, b))
                .WithMessage("The password and confirmation password do not match");
        }
        public bool CheckEqual(ResetPassword.ResetPasswordViewModel a, string pw)
        {
            if (a.NewPassword == pw)
            {
                return true;
            }
            return false;
        }


    }
}
