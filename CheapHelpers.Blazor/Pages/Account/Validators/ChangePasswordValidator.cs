using CheapHelpers.Blazor.Helpers;
using CheapHelpers.EF;
using CheapHelpers.Models.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CheapHelpers.Blazor.Pages.Account
{
    public class ChangePasswordValidator : BaseValidator<ChangePassword.ChangePasswordViewModel>
    {
        public ChangePasswordValidator(IDbContextFactory<CheapContext<CheapUser>> f)
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("Password is empty!")
                .MinimumLength(8)
                .WithMessage("minimum 8 characters");

            RuleFor(x => x.OldPassword)
                .NotEmpty()
                .WithMessage("Password is empty!");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithMessage("Password is empty!")
                .Must((a, b) => CheckEqual(a, b, f))
                .WithMessage("The password and confirmation password do not match");
        }

        public bool CheckEqual(ChangePassword.ChangePasswordViewModel a, string pw, IDbContextFactory<CheapContext<CheapUser>> f)
        {
            if (a.NewPassword == pw)
            {
                return true;
            }
            return false;
        }
    }
}
