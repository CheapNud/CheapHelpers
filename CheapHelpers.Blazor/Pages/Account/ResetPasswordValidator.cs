using Microsoft.EntityFrameworkCore;

namespace CheapHelpers.Blazor.Pages.Account
{
    //public class ResetPasswordValidator : BaseValidator<ResetPassword.ResetPasswordViewModel>
    //{
    //    public ResetPasswordValidator(IDbContextFactory<CheapContext> f)
    //    {
    //        RuleFor(x => x.NewPassword)
    //            .NotEmpty()
    //            .WithMessage("Password is empty!")
    //            .MinimumLength(8)
    //            .WithMessage("minimum 8 characters");

    //        RuleFor(x => x.ConfirmPassword)
    //            .NotEmpty()
    //            .WithMessage("Password is empty!")
    //            .Must((a, b) => CheckEqual(a, b, f))
    //            .WithMessage("The password and confirmation password do not match");
    //    }
    //    public bool CheckEqual(ResetPassword.ResetPasswordViewModel a, string pw, IDbContextFactory<CheapContext> f)
    //    {
    //        if (a.NewPassword == pw)
    //        {
    //            return true;
    //        }
    //        return false;
    //    }


    //}
}
