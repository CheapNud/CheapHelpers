using FluentValidation;
using MecamApplication.Blazor.Shared;
using MecamApplication.Context;
using Microsoft.EntityFrameworkCore;

namespace MecamApplication.Blazor.Pages.Account
{
    public class ResetPasswordValidator : BaseValidator<ResetPassword.ResetPasswordViewModel>
    {
        public ResetPasswordValidator(IDbContextFactory<MecamContext> f)
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("Password is empty!")
                .MinimumLength(8)
                .WithMessage("minimum 8 characters");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithMessage("Password is empty!")
                .Must((a, b) => CheckEqual(a, b, f))
                .WithMessage("The password and confirmation password do not match");
        }
        public bool CheckEqual(ResetPassword.ResetPasswordViewModel a, string pw, IDbContextFactory<MecamContext> f)
        {
            if (a.NewPassword == pw)
            {
                return true;
            }
            return false;
        }


    }
}
